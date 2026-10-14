// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.InteropGenerator.Discovery;

/// <inheritdoc cref="TypeSignatureDiscovery{TResult}"/>
internal partial class TypeSignatureDiscovery<TResult>
{
    /// <summary>
    /// Completes cross-module joins after the parallel publishing barrier.
    /// </summary>
    /// <param name="registry">The completed write-only registry.</param>
    /// <returns>The signatures discovered from late joins and their transitive bodies.</returns>
    private IEnumerable<TResult> EnumerateCrossModuleJoins(DispatchContextRegistry registry)
    {
        if (registry.Demands.IsEmpty || registry.Receivers.IsEmpty)
        {
            yield break;
        }

        Dictionary<TypeSignature, HashSet<int>> origins = new(_signatureComparer);
        List<ImportedDemand> previousDemands = [];
        DispatchContextRegistry.DiscoveryPhase[] phases =
        [
            DispatchContextRegistry.DiscoveryPhase.GenericDispatch,
            DispatchContextRegistry.DiscoveryPhase.DefaultInterfaces,
            DispatchContextRegistry.DiscoveryPhase.StaticDispatch
        ];

        foreach (DispatchContextRegistry.DiscoveryPhase phase in phases)
        {
            // Complete join-local instance defaults before advancing to static dispatch
            // Static publications and coverage records must not consume the allowance needed by those default descendants.
            if (phase == DispatchContextRegistry.DiscoveryPhase.StaticDispatch)
            {
                foreach (TResult result in EnumerateDefaultInterfaceMembers())
                {
                    yield return result;
                }
            }

            if (_transitiveTypeLimitReported)
            {
                break;
            }

            _dispatchPhase = phase;

            // Later coverage cannot suppress an earlier body whose descendants still need their existing allowance
            ImportExpandedBodies(registry, phase);

            Dictionary<TypeSignature, int> phaseReceivers = ImportReceivers(registry, origins, phase);

            // Imports skip inline joins, so demands seen before this phase must pair with its receivers explicitly later
            _importedReceiverPhases.Add(phaseReceivers);

            List<ImportedDemand> phaseDemands = ImportDemands(registry, phase);

            // Earlier demands join only this phase's receivers, avoiding retries of pairs already considered
            foreach (ImportedDemand entry in previousDemands)
            {
                _token.ThrowIfCancellationRequested();

                if (_transitiveTypeLimitReported)
                {
                    break;
                }

                ScheduleImportedDemand(entry, origins, phaseReceivers);
            }

            // Newly imported demands can join every receiver admitted so far, including earlier phases
            foreach (ImportedDemand entry in phaseDemands)
            {
                _token.ThrowIfCancellationRequested();

                if (_transitiveTypeLimitReported)
                {
                    break;
                }

                RegisterImportedDemand(entry);
                ScheduleImportedDemand(entry, origins);
            }

            // Finish this phase's transitive descendants before the next phase can reserve any work
            foreach (TResult result in EnumeratePendingMembers())
            {
                yield return result;
            }

            previousDemands.AddRange(phaseDemands);
        }
    }

    /// <summary>
    /// Records a demand discovered by late traversal, which is absent from the completed publication registry.
    /// </summary>
    /// <param name="demand">The newly registered dispatch context.</param>
    private void RecordLateDemand(DispatchContextRegistry.Demand demand)
    {
        // Late demands share the output module's origin; identify them by binding and composed declaring/method arguments
        if (!_lateDemandContexts.TryGetValue(demand.Target.Ordinal, out Dictionary<IList<TypeSignature>, LateDemandObservation>? contexts))
        {
            contexts = new Dictionary<IList<TypeSignature>, LateDemandObservation>(_signatureComparer);

            _lateDemandContexts.Add(demand.Target.Ordinal, contexts);
        }

        IList<TypeSignature> arguments = [demand.DeclarationType, .. demand.Arguments.TypeArguments];

        // The first sighting fixes which imported phases 'RegisterDemand' could already join inline
        if (!contexts.TryGetValue(arguments, out LateDemandObservation? observation))
        {
            observation = new LateDemandObservation(
                demand: demand,
                importedPhaseCount: _importedReceiverPhases.Count);

            contexts.Add(arguments, observation);
            _lateDemandObservations.Add(observation);
        }

        // A shallower registration rejoins current receivers inline; later-phase pairs must use that depth too
        observation.Depth = Math.Min(observation.Depth, demand.Depth);
    }

    /// <summary>
    /// Appends missing late demand/receiver pairs without displacing any previously admitted work.
    /// </summary>
    /// <returns>The signatures discovered from these pairs and their transitive descendants.</returns>
    private IEnumerable<TResult> EnumerateLateDispatchMembers()
    {
        if (_transitiveTypeLimitReported)
        {
            yield break;
        }

        // Scheduling only enqueues bodies; demands recorded by the drains below already see every imported phase
        foreach (LateDemandObservation observation in _lateDemandObservations)
        {
            _token.ThrowIfCancellationRequested();

            if (_transitiveTypeLimitReported)
            {
                break;
            }

            ScheduleLateDemand(observation);
        }

        // New demands and receivers encountered below join inline: every publication phase has already been imported
        foreach (TResult result in EnumeratePendingMembers())
        {
            yield return result;
        }

        // The static/default fixpoint starts only from its deferred queues, so pending bodies must drain first
        foreach (TResult result in EnumerateStaticDispatchMembers())
        {
            yield return result;
        }
    }

    /// <summary>
    /// Joins a late demand with receiver sightings imported after its first observation.
    /// </summary>
    /// <param name="observation">The demand, minimum depth, and first-observation phase boundary.</param>
    private void ScheduleLateDemand(LateDemandObservation observation)
    {
        DispatchContextRegistry.Demand demand = observation.Demand;

        if (!_receiverContexts.TryGetValue(demand.Target.Receiver, out Dictionary<TypeSignature, int>? receivers))
        {
            return;
        }

        // 'RegisterDemand' already considered earlier receiver phases inline; only later imports can leave missing pairs
        for (int phase = observation.ImportedPhaseCount; phase < _importedReceiverPhases.Count; phase++)
        {
            Dictionary<TypeSignature, int> phaseReceivers = _importedReceiverPhases[phase];

            foreach (TypeSignature receiver in receivers.Keys)
            {
                _token.ThrowIfCancellationRequested();

                if (_transitiveTypeLimitReported)
                {
                    return;
                }

                // Keep this phase's own sighting depth, as 'ScheduleImportedDemand' does
                if (!phaseReceivers.TryGetValue(receiver, out int receiverDepth))
                {
                    continue;
                }

                // No origin filter: late demands carry the output module's ordinal, but no module published or joined them
                ScheduleDispatch(
                    target: demand.Target,
                    declarationType: demand.DeclarationType,
                    arguments: demand.Arguments,
                    receiverType: receiver,
                    depth: Math.Max(receiverDepth, observation.Depth));
            }
        }
    }

    /// <summary>
    /// Imports minimum scan depths so completed local bodies do not consume the late allowance.
    /// </summary>
    /// <param name="registry">The completed registry.</param>
    /// <param name="phase">The publication phase being admitted.</param>
    private void ImportExpandedBodies(DispatchContextRegistry registry, DispatchContextRegistry.DiscoveryPhase phase)
    {
        foreach (DispatchContextRegistry.ExpandedBody body in registry.ExpandedBodies.Where(body => body.Phase == phase))
        {
            _token.ThrowIfCancellationRequested();

            Dictionary<IList<TypeSignature>, int> contexts = GetMethodContexts(_expandedBodies, body.Method);

            if (!contexts.TryGetValue(body.Arguments, out int priorDepth) || body.Depth < priorDepth)
            {
                contexts[body.Arguments] = body.Depth;
            }
        }
    }

    /// <summary>
    /// Imports receiver contexts in publication order and retains their originating modules.
    /// </summary>
    /// <param name="registry">The completed registry.</param>
    /// <param name="origins">The modules that observed each receiver in the phases admitted so far.</param>
    /// <param name="phase">The publication phase being admitted.</param>
    /// <returns>The minimum depths of receiver sightings from this phase only.</returns>
    private Dictionary<TypeSignature, int> ImportReceivers(
        DispatchContextRegistry registry,
        Dictionary<TypeSignature, HashSet<int>> origins,
        DispatchContextRegistry.DiscoveryPhase phase)
    {
        Dictionary<TypeSignature, int> depths = new(_signatureComparer);

        foreach (DispatchContextRegistry.Receiver receiver in registry.Receivers
            .Where(receiver => receiver.Phase == phase)
            .OrderBy(static receiver => receiver.Origin)
            .ThenBy(static receiver => receiver.Sequence))
        {
            _token.ThrowIfCancellationRequested();

            if (!origins.TryGetValue(receiver.Signature, out HashSet<int>? modules))
            {
                modules = [];
                origins.Add(receiver.Signature, modules);
            }

            _ = modules.Add(receiver.Origin);

            if (!depths.TryGetValue(receiver.Signature, out int depth) || receiver.Depth < depth)
            {
                depths[receiver.Signature] = receiver.Depth;
            }

            // Explicit stage joins apply the origin filter; an inline import join could retry a local pair
            RegisterReceiver(receiver.Signature, receiver.Depth, expandExistingDemands: false);
        }

        return depths;
    }

    /// <summary>
    /// Deduplicates published demands while preserving first-observation order and per-module depths.
    /// </summary>
    /// <param name="registry">The completed registry.</param>
    /// <param name="phase">The publication phase being admitted.</param>
    /// <returns>The distinct demands in deterministic scheduling order.</returns>
    private List<ImportedDemand> ImportDemands(DispatchContextRegistry registry, DispatchContextRegistry.DiscoveryPhase phase)
    {
        Dictionary<int, Dictionary<IList<TypeSignature>, ImportedDemand>> imported = [];
        List<ImportedDemand> orderedDemands = [];

        foreach (DispatchContextRegistry.Demand demand in registry.Demands
            .Where(demand => demand.Phase == phase)
            .OrderBy(static demand => demand.Origin)
            .ThenBy(static demand => demand.Sequence)
            .ThenBy(static demand => demand.Target.Ordinal))
        {
            _token.ThrowIfCancellationRequested();

            if (!imported.TryGetValue(demand.Target.Ordinal, out Dictionary<IList<TypeSignature>, ImportedDemand>? contexts))
            {
                contexts = new Dictionary<IList<TypeSignature>, ImportedDemand>(_signatureComparer);
                imported.Add(demand.Target.Ordinal, contexts);
            }

            IList<TypeSignature> arguments = [demand.DeclarationType, .. demand.Arguments.TypeArguments];

            if (!contexts.TryGetValue(arguments, out ImportedDemand? entry))
            {
                entry = new ImportedDemand(demand);
                contexts.Add(arguments, entry);
                orderedDemands.Add(entry);
            }

            if (!entry.Depths.TryGetValue(demand.Origin, out int priorDepth) || demand.Depth < priorDepth)
            {
                entry.Depths[demand.Origin] = demand.Depth;
            }
        }

        return orderedDemands;
    }

    /// <summary>
    /// Activates an imported demand for subsequent receivers without scheduling any pair yet.
    /// </summary>
    /// <param name="entry">The demand and its per-module depths.</param>
    private void RegisterImportedDemand(ImportedDemand entry)
    {
        DispatchContextRegistry.Demand demand = entry.MinimumDepthDemand;

        // Registration defers pair selection so initial demands can later join default-body receiver contexts
        _ = RegisterDemand(demand, expandExistingReceivers: false);
    }

    /// <summary>
    /// Joins an imported demand only with receivers not already available in its originating module.
    /// </summary>
    /// <param name="entry">The demand and its per-module depths.</param>
    /// <param name="origins">The modules that already observed each receiver.</param>
    /// <param name="phaseReceiverDepths">An optional restriction to receiver sightings from the newly admitted phase.</param>
    private void ScheduleImportedDemand(
        ImportedDemand entry,
        Dictionary<TypeSignature, HashSet<int>> origins,
        Dictionary<TypeSignature, int>? phaseReceiverDepths = null)
    {
        DispatchContextRegistry.Demand demand = entry.MinimumDepthDemand;

        if (!_receiverContexts.TryGetValue(demand.Target.Receiver, out Dictionary<TypeSignature, int>? receivers))
        {
            return;
        }

        foreach ((TypeSignature receiver, int depth) in receivers)
        {
            int receiverDepth = depth;

            if (phaseReceiverDepths is not null && !phaseReceiverDepths.TryGetValue(receiver, out receiverDepth))
            {
                continue;
            }

            // Use the shallowest demand from an origin that lacked this receiver; local pairs were already joined
            int dispatchDepth = int.MaxValue;

            foreach ((int origin, int originDepth) in entry.Depths)
            {
                if (!origins.TryGetValue(receiver, out HashSet<int>? receiverOrigins) || !receiverOrigins.Contains(origin))
                {
                    dispatchDepth = Math.Min(dispatchDepth, originDepth);
                }
            }

            if (dispatchDepth != int.MaxValue)
            {
                ScheduleDispatch(demand.Target, demand.DeclarationType, demand.Arguments, receiver, Math.Max(receiverDepth, dispatchDepth));
            }
        }
    }

    /// <summary>
    /// A dispatch context with the minimum observed depth from each publishing module.
    /// </summary>
    /// <param name="demand">The first stable representative used for binding and ordering.</param>
    private sealed class ImportedDemand(DispatchContextRegistry.Demand demand)
    {
        /// <summary>The first stable representative used for binding and ordering.</summary>
        public DispatchContextRegistry.Demand Demand { get; } = demand;

        /// <summary>A copy of the representative with the minimum depth across its originating modules.</summary>
        public DispatchContextRegistry.Demand MinimumDepthDemand => Demand with { Depth = Depths.Values.Min() };

        /// <summary>The minimum depth for each origin that observed this binding and method context.</summary>
        public Dictionary<int, int> Depths { get; } = [];
    }

    /// <summary>
    /// A late-local demand and the receiver phase boundary already available at its first sighting.
    /// </summary>
    /// <param name="demand">The first stable representative used for binding and ordering.</param>
    /// <param name="importedPhaseCount">The number of receiver phases already imported at first sighting.</param>
    private sealed class LateDemandObservation(DispatchContextRegistry.Demand demand, int importedPhaseCount)
    {
        /// <summary>The first stable representative used for binding and ordering.</summary>
        public DispatchContextRegistry.Demand Demand { get; } = demand;

        /// <summary>The receiver phases that were already available for inline joining.</summary>
        public int ImportedPhaseCount { get; } = importedPhaseCount;

        /// <summary>The minimum depth at which this binding and method context was observed.</summary>
        public int Depth { get; set; } = demand.Depth;
    }
}
