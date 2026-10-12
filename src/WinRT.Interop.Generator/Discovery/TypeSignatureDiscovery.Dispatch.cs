// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Models;
using WindowsRuntime.InteropGenerator.Visitors;

namespace WindowsRuntime.InteropGenerator.Discovery;

internal sealed partial class TypeSignatureDiscovery<TResult>
{
    /// <summary>Observed dispatch contexts, deduplicated by slot definition identity.</summary>
    private readonly Dictionary<MethodDefinition, Dictionary<IList<TypeSignature>, int>> _visitedDispatches = new(ReferenceEqualityComparer.Instance);

    /// <summary>Constructed type signatures already considered as sources of receiver contexts.</summary>
    private readonly Dictionary<TypeSignature, int> _receiverCandidates;

    /// <summary>Actually observed receiver contexts, indexed by receiver definition.</summary>
    private readonly Dictionary<TypeDefinition, Dictionary<TypeSignature, int>> _receiverContexts = new(ReferenceEqualityComparer.Instance);

    /// <summary>Dispatch demands waiting for receiver type arguments, indexed by receiver definition.</summary>
    private readonly Dictionary<TypeDefinition, List<DispatchContextRegistry.Demand>> _receiverDemands = new(ReferenceEqualityComparer.Instance);

    /// <summary>Demand contexts grouped by stable binding and origin ordinals.</summary>
    private readonly Dictionary<(int Target, int Origin), Dictionary<IList<TypeSignature>, int>> _demandContexts = [];

    /// <summary>Dispatch-capable body contexts already expanded before the publishing barrier.</summary>
    private readonly Dictionary<MethodDefinition, Dictionary<IList<TypeSignature>, int>> _expandedBodies = new(ReferenceEqualityComparer.Instance);

    /// <summary>The stable ordinal of this enumeration's root module.</summary>
    private readonly int _origin;

    /// <summary>The publication sequence within this enumeration.</summary>
    private int _publicationSequence;

    /// <summary>
    /// Registers virtual calls and method-pointer loads with their composed generic arguments.
    /// </summary>
    /// <param name="operand">The method descriptor, opcode, and optional constrained receiver.</param>
    /// <param name="callerContext">The caller's type and method arguments.</param>
    /// <param name="depth">The transitive dispatch depth, never the root exemption.</param>
    private void EnumerateDispatch(MethodOperand operand, GenericContext callerContext, int depth)
    {
        _token.ThrowIfCancellationRequested();

        if (!_isSupplemental)
        {
            RecordDispatchObservation(operand, callerContext, depth);
            return;
        }

        if (_transitiveTypeLimitReported ||
            (!operand.IsVirtual && operand.Constraint is null) ||
            operand.Descriptor is not MethodSpecification { Method: { } method, Signature: { } signature } ||
            !method.TryResolve(_module.RuntimeContext, out MethodDefinition? definition) ||
            !definition.IsVirtual || definition.IsFinal)
        {
            return;
        }

        TypeSignature declaringType = method.DeclaringType!.ToTypeSignature(_module.RuntimeContext).InstantiateGenericTypes(callerContext);
        GenericInstanceMethodSignature arguments = new(signature.Attributes,
            signature.TypeArguments.Select(argument => argument.InstantiateGenericTypes(callerContext)));
        TypeSignature? constraint = operand.Constraint?.ToTypeSignature(_module.RuntimeContext).InstantiateGenericTypes(callerContext);
        int complexity = declaringType.GetSignatureElementCount(MaxSignatureComplexity + 1) +
            (constraint?.GetSignatureElementCount(MaxSignatureComplexity + 1) ?? 0);

        foreach (TypeSignature argument in arguments.TypeArguments)
        {
            complexity += argument.GetSignatureElementCount(MaxSignatureComplexity + 1);
            if (!IsComplexityWithinLimit(complexity, depth))
            {
                return;
            }
        }

        if (!IsComplexityWithinLimit(complexity, depth) ||
            !arguments.TypeArguments.Any(static argument => argument.AcceptVisitor(IsConstructedGenericTypeVisitor.Instance)))
        {
            return;
        }

        if (constraint is not null)
        {
            if (!constraint.AcceptVisitor(IsConstructedGenericTypeVisitor.Instance))
            {
                return;
            }

            if (!definition.IsStatic && constraint.TryResolve(_module.RuntimeContext, out TypeDefinition? receiver) &&
                !receiver.IsValueType && !receiver.IsSealed)
            {
                constraint = null;
            }
        }

        IList<TypeSignature> key = [declaringType, .. arguments.TypeArguments, .. constraint is null ? [] : new[] { constraint }];
        if (!_visitedDispatches.TryGetValue(definition, out Dictionary<IList<TypeSignature>, int>? contexts))
        {
            contexts = new Dictionary<IList<TypeSignature>, int>(_signatureComparer);
            _visitedDispatches.Add(definition, contexts);
        }

        if (contexts.TryGetValue(key, out int priorDepth) && priorDepth <= depth)
        {
            return;
        }

        contexts[key] = depth;
        foreach (MethodDispatchIndex.Target target in _dispatchIndex.GetTargets(definition))
        {
            _token.ThrowIfCancellationRequested();

            if (_transitiveTypeLimitReported)
            {
                break;
            }

            if (target.Method.DeclaringModule is not ModuleDefinition declaringModule ||
                !_shouldProcessModule(declaringModule) || target.Method.CilMethodBody is null)
            {
                continue;
            }

            if (constraint is not null)
            {
                TypeSignature? receiverType = _dispatchIndex.GetConstrainedReceiver(target, constraint);
                if (receiverType is not null)
                {
                    ScheduleDispatch(target, declaringType, arguments, receiverType, depth);
                }

                continue;
            }

            ScheduleDispatch(target, declaringType, arguments, null, depth);

            if (target.Receiver.GenericParameters.Count > 0)
            {
                _ = RegisterDemand(new DispatchContextRegistry.Demand(target, declaringType, arguments, depth, _origin, _publicationSequence++));
            }
        }
    }

    /// <summary>
    /// Schedules a compatible dispatch body through the existing worklist and shared limits.
    /// </summary>
    /// <param name="target">The indexed metadata binding.</param>
    /// <param name="declarationType">The observed slot declaring context.</param>
    /// <param name="arguments">The composed method arguments.</param>
    /// <param name="receiverType">An actually observed receiver context, if available.</param>
    /// <param name="depth">The transitive expansion depth.</param>
    private void ScheduleDispatch(MethodDispatchIndex.Target target, TypeSignature declarationType, GenericInstanceMethodSignature arguments, TypeSignature? receiverType, int depth)
    {
        _token.ThrowIfCancellationRequested();

        if (_transitiveTypeLimitReported ||
            !IsSignatureWithinComplexityLimit(target.DeclarationType, depth) ||
            !IsSignatureWithinComplexityLimit(target.BodyType, depth) ||
            !_dispatchIndex.TryBind(target, declarationType, receiverType, out TypeSignature bodyType))
        {
            return;
        }

        int complexity = bodyType.GetSignatureElementCount(MaxSignatureComplexity + 1);
        foreach (TypeSignature argument in arguments.TypeArguments)
        {
            complexity += argument.GetSignatureElementCount(MaxSignatureComplexity + 1);
        }

        if (IsComplexityWithinLimit(complexity, depth))
        {
            IList<TypeSignature> key = [.. (bodyType as GenericInstanceTypeSignature)?.TypeArguments ?? [], .. arguments.TypeArguments];
            if (_expandedBodies.TryGetValue(target.Method, out Dictionary<IList<TypeSignature>, int>? contexts) &&
                contexts.TryGetValue(key, out int expandedDepth) && expandedDepth <= depth)
            {
                return;
            }

            EnqueueMethodBody(target.Method, bodyType, arguments, new GenericContext(bodyType as GenericInstanceTypeSignature, arguments), Math.Max(1, depth));
        }
    }

    /// <summary>
    /// Registers actual closed generic receiver contexts regardless of the enumeration's result visitor.
    /// </summary>
    /// <param name="signature">A discovered signature tree.</param>
    /// <param name="depth">Its expansion depth.</param>
    private void RegisterReceiverContexts(TypeSignature? signature, int depth)
    {
        foreach (GenericInstanceTypeSignature candidate in signature?.AcceptVisitor(AllGenericTypesVisitor.Instance) ?? [])
        {
            if (candidate.AcceptVisitor(IsConstructedGenericTypeVisitor.Instance) &&
                (!_receiverCandidates.TryGetValue(candidate, out int priorDepth) || priorDepth > depth))
            {
                _receiverCandidates[candidate] = depth;
                foreach (TypeSignature receiver in _dispatchIndex.GetReceiverContexts(candidate))
                {
                    if ((_isSupplemental && IsSignatureWithinComplexityLimit(receiver, depth)) ||
                        (!_isSupplemental && (depth == 0 || receiver.GetSignatureElementCount(MaxSignatureComplexity + 1) <= MaxSignatureComplexity)))
                    {
                        RegisterReceiver(receiver, depth);
                    }
                    else if (!_isSupplemental && _complexReceiver is null)
                    {
                        _complexReceiver = receiver;
                        _complexReceiverDepth = depth;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Joins a new receiver context with the demands already registered for that definition.
    /// </summary>
    /// <param name="signature">The actual receiver context.</param>
    /// <param name="depth">The discovery depth of the receiver context.</param>
    private void RegisterReceiver(TypeSignature signature, int depth)
    {
        if (!signature.TryResolve(_module.RuntimeContext, out TypeDefinition? definition))
        {
            return;
        }

        if (!_receiverContexts.TryGetValue(definition, out Dictionary<TypeSignature, int>? contexts))
        {
            contexts = new Dictionary<TypeSignature, int>(_signatureComparer);
            _receiverContexts.Add(definition, contexts);
        }

        if (contexts.TryGetValue(signature, out int priorDepth) && priorDepth <= depth)
        {
            return;
        }

        contexts[signature] = depth;
        if (!_isSupplemental)
        {
            return;
        }

        _dispatchContexts?.Receivers.Add(new DispatchContextRegistry.Receiver(signature, depth, _origin, _publicationSequence++));

        if (_receiverDemands.TryGetValue(definition, out List<DispatchContextRegistry.Demand>? demands))
        {
            foreach (DispatchContextRegistry.Demand demand in demands)
            {
                ScheduleDispatch(demand.Target, demand.DeclarationType, demand.Arguments, signature, Math.Max(depth, demand.Depth));
            }
        }
    }

    /// <summary>
    /// Joins a new dispatch demand with the already observed receiver contexts.
    /// </summary>
    /// <param name="demand">The target binding and observed method arguments.</param>
    /// <param name="expandExistingReceivers">Whether to join existing contexts immediately or let the cross-module filter select them.</param>
    /// <returns>Whether the demand introduced a new context or a shallower traversal depth.</returns>
    private bool RegisterDemand(DispatchContextRegistry.Demand demand, bool expandExistingReceivers = true)
    {
        (int, int) key = (demand.Target.Ordinal, demand.Origin);
        if (!_demandContexts.TryGetValue(key, out Dictionary<IList<TypeSignature>, int>? visited))
        {
            visited = new Dictionary<IList<TypeSignature>, int>(_signatureComparer);
            _demandContexts.Add(key, visited);
        }

        IList<TypeSignature> arguments = [demand.DeclarationType, .. demand.Arguments.TypeArguments];
        if (visited.TryGetValue(arguments, out int priorDepth) && priorDepth <= demand.Depth)
        {
            return false;
        }

        visited[arguments] = demand.Depth;
        if (!_receiverDemands.TryGetValue(demand.Target.Receiver, out List<DispatchContextRegistry.Demand>? demands))
        {
            demands = [];
            _receiverDemands.Add(demand.Target.Receiver, demands);
        }

        demands.Add(demand);
        _dispatchContexts?.Demands.Add(demand);

        if (expandExistingReceivers && _receiverContexts.TryGetValue(demand.Target.Receiver, out Dictionary<TypeSignature, int>? contexts))
        {
            foreach ((TypeSignature receiver, int depth) in contexts)
            {
                ScheduleDispatch(demand.Target, demand.DeclarationType, demand.Arguments, receiver, Math.Max(depth, demand.Depth));
            }
        }

        return true;
    }

    /// <summary>
    /// Completes cross-module joins after the parallel publishing barrier.
    /// </summary>
    /// <param name="registry">The completed write-only registry.</param>
    /// <returns>The signatures discovered from late joins and their transitive bodies.</returns>
    private IEnumerable<TResult> EnumerateAdditionalDispatchContexts(DispatchContextRegistry registry)
    {
        if (registry.Demands.IsEmpty || registry.Receivers.IsEmpty)
        {
            yield break;
        }

        foreach (DispatchContextRegistry.ExpandedBody body in registry.ExpandedBodies)
        {
            _token.ThrowIfCancellationRequested();
            if (!_expandedBodies.TryGetValue(body.Method, out Dictionary<IList<TypeSignature>, int>? contexts))
            {
                contexts = new Dictionary<IList<TypeSignature>, int>(_signatureComparer);
                _expandedBodies.Add(body.Method, contexts);
            }

            if (!contexts.TryGetValue(body.Arguments, out int priorDepth) || body.Depth < priorDepth)
            {
                contexts[body.Arguments] = body.Depth;
            }
        }

        Dictionary<TypeSignature, HashSet<int>> origins = new(_signatureComparer);
        foreach (DispatchContextRegistry.Receiver receiver in registry.Receivers
            .OrderBy(static receiver => receiver.Origin).ThenBy(static receiver => receiver.Sequence))
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

        Dictionary<int, Dictionary<IList<TypeSignature>, ImportedDemand>> imported = [];
        List<ImportedDemand> orderedDemands = [];
        foreach (DispatchContextRegistry.Demand demand in registry.Demands
            .OrderBy(static demand => demand.Origin).ThenBy(static demand => demand.Sequence).ThenBy(static demand => demand.Target.Ordinal))
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

        foreach (ImportedDemand entry in orderedDemands)
        {
            _token.ThrowIfCancellationRequested();
            if (_transitiveTypeLimitReported)
            {
                break;
            }

            DispatchContextRegistry.Demand demand = entry.Demand with { Depth = entry.Depths.Values.Min() };
            if (!RegisterDemand(demand, expandExistingReceivers: false))
            {
                continue;
            }

            if (_receiverContexts.TryGetValue(demand.Target.Receiver, out Dictionary<TypeSignature, int>? receivers))
            {
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
        }

        foreach (TResult result in EnumeratePendingMembers())
        {
            yield return result;
        }
    }

    /// <summary>A dispatch context with the minimum observed depth from each publishing module.</summary>
    /// <param name="demand">The first stable representative used for binding and ordering.</param>
    private sealed class ImportedDemand(DispatchContextRegistry.Demand demand)
    {
        /// <summary>The first stable representative used for binding and ordering.</summary>
        public DispatchContextRegistry.Demand Demand { get; } = demand;

        /// <summary>The minimum depth for each origin that observed this binding and method context.</summary>
        public Dictionary<int, int> Depths { get; } = [];
    }
}
