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

        ImportExpandedBodies(registry, isDefaultBody: false);

        Dictionary<TypeSignature, HashSet<int>> origins = new(_signatureComparer);

        _ = ImportReceivers(registry, origins, isDefaultBody: false);

        List<ImportedDemand> initialDemands = ImportDemands(registry, isDefaultBody: false);

        // Default-body contexts must remain outside the active maps until the initial joins and their descendants drain
        // Inline joins would otherwise compete with those descendants for the same transitive allowance.
        foreach (ImportedDemand entry in initialDemands)
        {
            _token.ThrowIfCancellationRequested();

            if (_transitiveTypeLimitReported)
            {
                break;
            }

            RegisterImportedDemand(entry);
            ScheduleImportedDemand(entry, origins);
        }

        foreach (TResult result in EnumeratePendingMembers())
        {
            yield return result;
        }

        if (_transitiveTypeLimitReported)
        {
            yield break;
        }

        // Retain default-phase provenance on join-local demand records even though this pass does not republish them
        _isDefaultInterfaceTraversal = true;

        // A completed default-phase body may have unexpanded descendants after a local cutoff
        // Delay its coverage as well as its contexts so it cannot suppress an initial cross-module scan.
        ImportExpandedBodies(registry, isDefaultBody: true);

        Dictionary<TypeSignature, int> defaultBodyReceivers = ImportReceivers(registry, origins, isDefaultBody: true);
        List<ImportedDemand> defaultBodyDemands = ImportDemands(registry, isDefaultBody: true);

        // Existing demands can now join newly imported receivers without retrying their old pairs
        foreach (ImportedDemand entry in initialDemands)
        {
            _token.ThrowIfCancellationRequested();

            if (_transitiveTypeLimitReported)
            {
                break;
            }

            ScheduleImportedDemand(entry, origins, defaultBodyReceivers);
        }

        foreach (ImportedDemand entry in defaultBodyDemands)
        {
            _token.ThrowIfCancellationRequested();

            if (_transitiveTypeLimitReported)
            {
                break;
            }

            RegisterImportedDemand(entry);
            ScheduleImportedDemand(entry, origins);
        }

        foreach (TResult result in EnumeratePendingMembers())
        {
            yield return result;
        }
    }

    /// <summary>
    /// Imports minimum scan depths so completed local bodies do not consume the late allowance.
    /// </summary>
    /// <param name="registry">The completed registry.</param>
    /// <param name="isDefaultBody">The publication phase being admitted.</param>
    private void ImportExpandedBodies(DispatchContextRegistry registry, bool isDefaultBody)
    {
        foreach (DispatchContextRegistry.ExpandedBody body in registry.ExpandedBodies.Where(body => body.IsDefaultBody == isDefaultBody))
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
    /// <param name="isDefaultBody">The publication phase being admitted.</param>
    /// <returns>The minimum depths of receiver sightings from this phase only.</returns>
    private Dictionary<TypeSignature, int> ImportReceivers(
        DispatchContextRegistry registry,
        Dictionary<TypeSignature, HashSet<int>> origins,
        bool isDefaultBody)
    {
        Dictionary<TypeSignature, int> depths = new(_signatureComparer);

        foreach (DispatchContextRegistry.Receiver receiver in registry.Receivers
            .Where(receiver => receiver.IsDefaultBody == isDefaultBody)
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
    /// <param name="isDefaultBody">The publication phase being admitted.</param>
    /// <returns>The distinct demands in deterministic scheduling order.</returns>
    private List<ImportedDemand> ImportDemands(DispatchContextRegistry registry, bool isDefaultBody)
    {
        Dictionary<int, Dictionary<IList<TypeSignature>, ImportedDemand>> imported = [];
        List<ImportedDemand> orderedDemands = [];

        foreach (DispatchContextRegistry.Demand demand in registry.Demands
            .Where(demand => demand.IsDefaultBody == isDefaultBody)
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
}
