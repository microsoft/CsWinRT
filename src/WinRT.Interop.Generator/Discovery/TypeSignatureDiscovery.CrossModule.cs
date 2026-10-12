// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.InteropGenerator.Discovery;

internal sealed partial class TypeSignatureDiscovery<TResult>
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

        ImportExpandedBodies(registry);

        Dictionary<TypeSignature, HashSet<int>> origins = ImportReceivers(registry);
        List<ImportedDemand> demands = ImportDemands(registry);

        // Only cross-module pairs need the late allowance; local pairs already had their own worklist
        foreach (ImportedDemand entry in demands)
        {
            _token.ThrowIfCancellationRequested();

            if (_transitiveTypeLimitReported)
            {
                break;
            }

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
    private void ImportExpandedBodies(DispatchContextRegistry registry)
    {
        foreach (DispatchContextRegistry.ExpandedBody body in registry.ExpandedBodies)
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
    /// <returns>The modules that already observed each receiver.</returns>
    private Dictionary<TypeSignature, HashSet<int>> ImportReceivers(DispatchContextRegistry registry)
    {
        Dictionary<TypeSignature, HashSet<int>> origins = new(_signatureComparer);

        foreach (DispatchContextRegistry.Receiver receiver in registry.Receivers
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
            RegisterReceiver(receiver.Signature, receiver.Depth);
        }

        return origins;
    }

    /// <summary>
    /// Deduplicates published demands while preserving first-observation order and per-module depths.
    /// </summary>
    /// <param name="registry">The completed registry.</param>
    /// <returns>The distinct demands in deterministic scheduling order.</returns>
    private List<ImportedDemand> ImportDemands(DispatchContextRegistry registry)
    {
        Dictionary<int, Dictionary<IList<TypeSignature>, ImportedDemand>> imported = [];
        List<ImportedDemand> orderedDemands = [];

        foreach (DispatchContextRegistry.Demand demand in registry.Demands
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
    /// Joins an imported demand only with receivers not already available in its originating module.
    /// </summary>
    /// <param name="entry">The demand and its per-module depths.</param>
    /// <param name="origins">The modules that already observed each receiver.</param>
    private void ScheduleImportedDemand(ImportedDemand entry, Dictionary<TypeSignature, HashSet<int>> origins)
    {
        DispatchContextRegistry.Demand demand = entry.Demand with { Depth = entry.Depths.Values.Min() };

        if (!RegisterDemand(demand, expandExistingReceivers: false) ||
            !_receiverContexts.TryGetValue(demand.Target.Receiver, out Dictionary<TypeSignature, int>? receivers))
        {
            return;
        }

        foreach ((TypeSignature receiver, int depth) in receivers)
        {
            int dispatchDepth = int.MaxValue;

            foreach ((int origin, int originDepth) in entry.Depths)
            {
                if (!origins[receiver].Contains(origin))
                {
                    dispatchDepth = Math.Min(dispatchDepth, originDepth);
                }
            }

            if (dispatchDepth != int.MaxValue)
            {
                ScheduleDispatch(demand.Target, demand.DeclarationType, demand.Arguments, receiver, Math.Max(depth, dispatchDepth));
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

        /// <summary>The minimum depth for each origin that observed this binding and method context.</summary>
        public Dictionary<int, int> Depths { get; } = [];
    }
}
