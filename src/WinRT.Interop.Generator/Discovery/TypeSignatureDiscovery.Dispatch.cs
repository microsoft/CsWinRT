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

/// <inheritdoc cref="TypeSignatureDiscovery{TResult}"/>
internal partial class TypeSignatureDiscovery<TResult>
{
    /// <summary>
    /// Registers virtual calls and method-pointer loads with their composed generic arguments.
    /// </summary>
    /// <param name="operand">The method descriptor, opcode, and optional constrained receiver.</param>
    /// <param name="callerContext">The caller's type and method arguments.</param>
    /// <param name="depth">The transitive dispatch depth, never the root exemption.</param>
    private void RegisterDispatch(MethodOperand operand, GenericContext callerContext, int depth)
    {
        _token.ThrowIfCancellationRequested();

        // Primary traversal only records observations; dispatch expansion belongs to the supplemental allowance
        if (!_isSupplemental)
        {
            RecordDispatchObservation(operand, callerContext, depth);

            return;
        }

        // Generic virtual slots and constrained static dispatch can contribute additional implementation bodies
        if (_transitiveTypeLimitReported ||
            (!operand.IsVirtual && operand.Constraint is null))
        {
            return;
        }

        if (operand.Descriptor is not MethodSpecification { Method: { } method, Signature: { } signature })
        {
            return;
        }

        // Only overridable declarations can contribute additional dispatch implementations
        if (!method.TryResolve(_module.RuntimeContext, out MethodDefinition? definition) ||
            !definition.IsVirtual || definition.IsFinal)
        {
            return;
        }

        if (!TryComposeDispatchContext(method, signature, operand.Constraint, callerContext, out DispatchCallContext call))
        {
            return;
        }

        if (!IsComplexityWithinLimit(call.Complexity, depth))
        {
            return;
        }

        if (!call.Arguments.TypeArguments.Any(static argument => argument.AcceptVisitor(IsConstructedGenericTypeVisitor.Instance)))
        {
            return;
        }

        TypeSignature declaringType = call.DeclaringType;
        GenericInstanceMethodSignature arguments = call.Arguments;
        TypeSignature? constraint = call.Constraint;

        if (constraint is not null)
        {
            if (!constraint.AcceptVisitor(IsConstructedGenericTypeVisitor.Instance))
            {
                return;
            }

            // An unsealed reference constraint still permits derived receivers, so it must not narrow dispatch
            if (!definition.IsStatic && constraint.TryResolve(_module.RuntimeContext, out TypeDefinition? receiver) &&
                !receiver.IsValueType && !receiver.IsSealed)
            {
                constraint = null;
            }
        }

        IList<TypeSignature> key = constraint is null
            ? [declaringType, .. arguments.TypeArguments]
            : [declaringType, .. arguments.TypeArguments, constraint];
        Dictionary<IList<TypeSignature>, int> contexts = GetMethodContexts(_visitedDispatches, definition);

        if (contexts.TryGetValue(key, out int priorDepth) && priorDepth <= depth)
        {
            return;
        }

        contexts[key] = depth;

        // Bind every policy-eligible implementation in the index's stable metadata order
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
                // A constrained call names its receiver, so it never waits for a later receiver join
                TypeSignature? receiverType = _dispatchIndex.GetConstrainedReceiver(target, constraint);

                if (receiverType is not null)
                {
                    ScheduleDispatch(target, declaringType, arguments, receiverType, depth);
                }

                continue;
            }

            ScheduleDispatch(target, declaringType, arguments, null, depth);

            // Generic receivers can still be closed later by a receiver observed here or in another module
            if (target.Receiver.GenericParameters.Count > 0)
            {
                _ = RegisterDemand(new DispatchContextRegistry.Demand(
                    Target: target,
                    DeclarationType: declaringType,
                    Arguments: arguments,
                    Depth: depth,
                    Origin: _moduleOrdinal,
                    Sequence: _publicationSequence++,
                    IsDefaultBody: _isDefaultInterfaceTraversal));
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
    private void ScheduleDispatch(
        MethodDispatchIndex.Target target,
        TypeSignature declarationType,
        GenericInstanceMethodSignature arguments,
        TypeSignature? receiverType,
        int depth)
    {
        _token.ThrowIfCancellationRequested();

        if (_transitiveTypeLimitReported)
        {
            return;
        }

        // Validate templates before binding can compose a larger constructed body context
        if (!IsSignatureWithinComplexityLimit(target.DeclarationType, depth) ||
            !IsSignatureWithinComplexityLimit(target.BodyType, depth))
        {
            return;
        }

        if (!_dispatchIndex.TryBind(target, declarationType, receiverType, out TypeSignature bodyType))
        {
            return;
        }

        if (IsComplexityWithinLimit(GetContextComplexity(bodyType, arguments.TypeArguments), depth))
        {
            IList<TypeSignature> key = [.. (bodyType as GenericInstanceTypeSignature)?.TypeArguments ?? [], .. arguments.TypeArguments];

            // Bodies already expanded by a publishing module at this depth or shallower need no late rescan
            if (_expandedBodies.TryGetValue(target.Method, out Dictionary<IList<TypeSignature>, int>? contexts) &&
                contexts.TryGetValue(key, out int expandedDepth) && expandedDepth <= depth)
            {
                return;
            }

            // Possible implementations never inherit the depth-zero root exemption from module policy
            EnqueueMethodBody(
                method: target.Method,
                declaringType: bodyType,
                methodArguments: arguments,
                genericContext: new GenericContext(
                    type: bodyType as GenericInstanceTypeSignature,
                    method: arguments),
                depth: Math.Max(1, depth));
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
            if (!candidate.AcceptVisitor(IsConstructedGenericTypeVisitor.Instance))
            {
                continue;
            }

            // Revisit only a shallower actual receiver, preserving the first-admission order
            if (_receiverCandidates.TryGetValue(candidate, out int priorDepth) && priorDepth <= depth)
            {
                continue;
            }

            _receiverCandidates[candidate] = depth;

            RecordDefaultReceiver(candidate, depth);

            foreach (TypeSignature receiver in _dispatchIndex.GetReceiverContexts(candidate))
            {
                if (IsReceiverWithinComplexityLimit(receiver, depth))
                {
                    RegisterReceiver(receiver, depth);
                }
            }
        }
    }

    /// <summary>
    /// Checks receiver complexity without allowing passive primary observations to emit warnings.
    /// </summary>
    /// <param name="receiver">The observed receiver signature.</param>
    /// <param name="depth">Its traversal depth.</param>
    /// <returns>Whether the receiver may be registered.</returns>
    private bool IsReceiverWithinComplexityLimit(TypeSignature receiver, int depth)
    {
        if (_isSupplemental)
        {
            return IsSignatureWithinComplexityLimit(receiver, depth);
        }

        if (depth == 0 || GetSignatureComplexity(receiver) <= MaxSignatureComplexity)
        {
            return true;
        }

        if (_complexReceiver is null)
        {
            _complexReceiver = receiver;
            _complexReceiverDepth = depth;
        }

        return false;
    }

    /// <summary>
    /// Joins a new receiver context with the demands already registered for that definition.
    /// </summary>
    /// <param name="signature">The actual receiver context.</param>
    /// <param name="depth">The discovery depth of the receiver context.</param>
    /// <param name="expandExistingDemands">Whether to join existing demands or defer imported pairs to their stage.</param>
    private void RegisterReceiver(
        TypeSignature signature,
        int depth,
        bool expandExistingDemands = true)
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

        // Primary only records receivers; publication and joins wait for supplemental replay
        if (!_isSupplemental)
        {
            return;
        }

        _dispatchContexts?.Receivers.Add(new DispatchContextRegistry.Receiver(
            Signature: signature,
            Depth: depth,
            Origin: _moduleOrdinal,
            Sequence: _publicationSequence++,
            IsDefaultBody: _isDefaultInterfaceTraversal));

        if (expandExistingDemands &&
            _receiverDemands.TryGetValue(definition, out List<DispatchContextRegistry.Demand>? demands))
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
    /// Resolves and composes a dispatch operand without applying phase-specific policy or diagnostics.
    /// </summary>
    /// <param name="method">The declaration operand.</param>
    /// <param name="arguments">Its method arguments before substitution.</param>
    /// <param name="constraint">The optional constrained receiver operand.</param>
    /// <param name="callerContext">The caller's type and method arguments.</param>
    /// <param name="context">The composed context, including the unmodified receiver constraint.</param>
    /// <returns>Whether all declaring and constraint signatures are available.</returns>
    private bool TryComposeDispatchContext(
        IMethodDefOrRef method,
        GenericInstanceMethodSignature arguments,
        ITypeDefOrRef? constraint,
        GenericContext callerContext,
        out DispatchCallContext context)
    {
        context = default;

        if (method.DeclaringType?.TryGetTypeSignature(
            context: _module.RuntimeContext,
            throwOnResolutionFailure: false,
            typeSignature: out TypeSignature? declaringType) is not true)
        {
            return false;
        }

        TypeSignature? constraintType = null;

        if (constraint is not null && !constraint.TryGetTypeSignature(
            context: _module.RuntimeContext,
            throwOnResolutionFailure: false,
            typeSignature: out constraintType))
        {
            return false;
        }

        context = new DispatchCallContext(
            declaringType: declaringType.InstantiateGenericTypes(callerContext),
            arguments: new GenericInstanceMethodSignature(
                attributes: arguments.Attributes,
                typeArguments: arguments.TypeArguments.Select(argument => argument.InstantiateGenericTypes(callerContext))),
            constraint: constraintType?.InstantiateGenericTypes(callerContext));

        return true;
    }

    /// <summary>
    /// A composed dispatch context before phase-specific constraint normalization or filtering.
    /// </summary>
    /// <param name="declaringType">The substituted declaration type.</param>
    /// <param name="arguments">The substituted method arguments.</param>
    /// <param name="constraint">The original substituted constraint, if available.</param>
    private readonly struct DispatchCallContext(
        TypeSignature declaringType,
        GenericInstanceMethodSignature arguments,
        TypeSignature? constraint)
    {
        /// <summary>The substituted declaration type.</summary>
        public TypeSignature DeclaringType { get; } = declaringType;

        /// <summary>The substituted method arguments.</summary>
        public GenericInstanceMethodSignature Arguments { get; } = arguments;

        /// <summary>The original constraint, or <see langword="null"/> for an unconstrained call.</summary>
        public TypeSignature? Constraint { get; } = constraint;

        /// <summary>The aggregate context complexity, before any context is hashed.</summary>
        public int Complexity { get; } = GetContextComplexity(declaringType, arguments.TypeArguments, constraint);
    }
}
