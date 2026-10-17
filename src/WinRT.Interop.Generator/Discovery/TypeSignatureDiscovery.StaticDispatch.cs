// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Errors;
using WindowsRuntime.InteropGenerator.Models;
using WindowsRuntime.InteropGenerator.Visitors;

namespace WindowsRuntime.InteropGenerator.Discovery;

/// <inheritdoc cref="TypeSignatureDiscovery{TResult}"/>
internal partial class TypeSignatureDiscovery<TResult>
{
    /// <summary>
    /// Records a constrained static call without changing primary worklist admission.
    /// </summary>
    /// <param name="operand">The constrained invocation or function-pointer operand.</param>
    /// <param name="callerContext">The active type and method arguments.</param>
    /// <param name="depth">The call's transitive depth.</param>
    private void RecordStaticDispatch(MethodOperand operand, GenericContext callerContext, int depth)
    {
        // 'constrained.' supplies the receiver for static interface dispatch; ordinary static calls have no slot to select
        if (!operand.IsStaticDispatch ||
            operand.Constraint is null)
        {
            return;
        }

        // Both call shapes name a declaration, but only a real 'MethodSpec' supplies generic method arguments
        MethodSpecification? specification = operand.Descriptor as MethodSpecification;
        IMethodDefOrRef? reference = specification is null ? operand.Descriptor as IMethodDefOrRef : specification.Method;

        if (reference is null)
        {
            return;
        }

        // Implementation bodies need not be virtual; begin with the static virtual declaration named by the operand
        if (!reference.TryResolve(_module.RuntimeContext, out MethodDefinition? declaration) ||
            !declaration.IsStatic ||
            !declaration.IsVirtual)
        {
            return;
        }

        // Resolving the method loses the reference's constructed interface arguments
        // Retain that signature so different constructions of the same declaration remain distinct slots.
        if (reference.DeclaringType?.TryGetTypeSignature(
            context: _module.RuntimeContext,
            throwOnResolutionFailure: false,
            typeSignature: out TypeSignature? declaringType) is not true)
        {
            return;
        }

        // The constraint can name a caller-owned parameter that becomes a concrete receiver after substitution
        if (!operand.Constraint.TryGetTypeSignature(
            context: _module.RuntimeContext,
            throwOnResolutionFailure: false,
            typeSignature: out TypeSignature? receiverType))
        {
            return;
        }

        // Compose in the caller's parameter space before interpreting the callee's owner or method arguments
        GenericInstanceMethodSignature? arguments = specification?.Signature is { } signature
            ? new GenericInstanceMethodSignature(
                attributes: signature.Attributes,
                typeArguments: signature.TypeArguments.Select(argument => argument.InstantiateGenericTypes(callerContext)))
            : null;

        // Record context only; selection and worklist reservations wait until the appended static phase
        RecordStaticCall(
            call: new StaticCallContext(
                Declaration: declaration,
                DeclaringType: declaringType.InstantiateGenericTypes(callerContext),
                Receiver: receiverType.InstantiateGenericTypes(callerContext),
                Arguments: arguments),
            depth: depth);
    }

    /// <summary>
    /// Retains a minimum-depth static context in observation order, including deferred bound diagnostics.
    /// </summary>
    /// <param name="call">The composed static call context.</param>
    /// <param name="depth">The call's transitive depth.</param>
    private void RecordStaticCall(StaticCallContext call, int depth)
    {
        // An open caller supplies no actual static receiver, even if its declaring signature exceeds a bound
        if (!call.Receiver.AcceptVisitor(IsConstructedGenericTypeVisitor.Instance))
        {
            return;
        }

        StaticCallObservation observation;

        if (call.Complexity > MaxSignatureComplexity)
        {
            // Retain one observation for the deferred warning without hashing an oversized signature tree
            if (_complexStaticCallRecorded)
            {
                return;
            }

            _complexStaticCallRecorded = true;

            observation = new StaticCallObservation(
                call: call,
                depth: depth);

            _staticCallObservations.Add(observation);
        }
        else
        {
            Dictionary<IList<TypeSignature>, StaticCallObservation> contexts = GetMethodContexts(_staticCallContexts, call.Declaration);

            // A declaration can bind differently for each constructed interface, receiver, and method argument list
            IList<TypeSignature> key = [call.DeclaringType, call.Receiver, .. call.Arguments?.TypeArguments ?? []];

            if (contexts.TryGetValue(key, out StaticCallObservation? existing))
            {
                if (existing.Depth <= depth)
                {
                    return;
                }

                // Improve the same sighting in place so its first-observation order does not change
                existing.Depth = depth;

                observation = existing;
            }
            else
            {
                observation = new StaticCallObservation(
                    call: call,
                    depth: depth);

                contexts.Add(key, observation);
                _staticCallObservations.Add(observation);
            }
        }

        // Primary sightings stay passive; supplemental sightings can feed the later static fixpoint
        if (_isSupplemental)
        {
            _pendingStaticCalls.Enqueue(observation);
        }
    }

    /// <summary>
    /// Expands additional static calls after earlier work, retaining one shared supplemental allowance.
    /// </summary>
    /// <returns>The signatures discovered by static bodies and their descendants.</returns>
    private IEnumerable<TResult> EnumerateStaticDispatchMembers()
    {
        // Descendants must keep static provenance even when they introduce instance-default receivers
        _dispatchPhase = DispatchContextRegistry.DiscoveryPhase.StaticDispatch;

        // Body traversal can refill either queue, so both participate in the same remaining-budget fixpoint
        while ((_pendingStaticCalls.Count > 0 || _pendingDefaultReceivers.Count > 0) &&
            !_transitiveTypeLimitReported)
        {
            while (_pendingStaticCalls.TryDequeue(out StaticCallObservation? observation))
            {
                _token.ThrowIfCancellationRequested();

                if (_transitiveTypeLimitReported)
                {
                    break;
                }

                // Queued entries share mutable sightings; only a new minimum depth warrants another expansion
                if (observation.ScheduledDepth <= observation.Depth)
                {
                    continue;
                }

                observation.ScheduledDepth = observation.Depth;

                ScheduleStaticCall(observation.Call, depth: Math.Max(1, observation.Depth));
            }

            // Discover descendants before checking the queues again; they can expose further constrained calls
            foreach (TResult result in EnumeratePendingMembers())
            {
                yield return result;
            }

            // Static bodies can expose instance-default receivers, whose descendants retain this phase's provenance
            foreach (TResult result in EnumerateDefaultInterfaceMembers())
            {
                yield return result;
            }
        }
    }

    /// <summary>
    /// Schedules an interface-owned static override using real method arguments, or a type-only body context.
    /// </summary>
    /// <param name="call">The actual constrained call.</param>
    /// <param name="depth">The supplemental expansion depth, never a root exemption.</param>
    private void ScheduleStaticCall(StaticCallContext call, int depth)
    {
        if (!IsComplexityWithinLimit(call.Complexity, depth: depth))
        {
            return;
        }

        if (!call.Receiver.TryResolve(_module.RuntimeContext, out TypeDefinition? receiver))
        {
            return;
        }

        // 'IndexInterfaceImplementations' supplies every eligible generic static body for class and struct receivers
        // That path already selects or retains candidates, so do not requeue it or introduce additional bound diagnostics.
        if (IsHandledByGenericDispatch(call, receiver))
        {
            return;
        }

        // Indexing alone emits no warning; diagnose an incomplete hierarchy only when this receiver is actually called
        MethodDispatchIndex.DefaultHierarchyLimit limit = _dispatchIndex.GetStaticHierarchyLimit(receiver);

        if (limit == MethodDispatchIndex.DefaultHierarchyLimit.Depth &&
            !_recursionLimitReported)
        {
            _recursionLimitReported = true;

            WellKnownInteropExceptions.GenericTypeDiscoveryRecursionLimitExceededWarning(
                type: call.Receiver,
                module: _module,
                limit: MaxDiscoveryDepth,
                supplemental: _isSupplemental)
                .LogOrThrow(_treatWarningsAsErrors);
        }
        else if (limit == MethodDispatchIndex.DefaultHierarchyLimit.Complexity)
        {
            _ = IsComplexityWithinLimit(MaxSignatureComplexity + 1, depth: depth);
        }

        // Incomplete or ambiguous selection cannot safely introduce an additional implementation body
        if (!_dispatchIndex.TryGetStaticTarget(
            declaration: call.Declaration,
            declaringType: call.DeclaringType,
            receiverType: call.Receiver,
            target: out MethodDispatchIndex.Target? target,
            bodyType: out TypeSignature? bodyType))
        {
            return;
        }

        // Class implementations and the declaration's own body already participate in ordinary member traversal
        if (target.Method.DeclaringType?.IsInterface is not true ||
            ReferenceEquals(target.Method, call.Declaration))
        {
            return;
        }

        // Substitution can enlarge the selected owner; bound it together with the method arguments before admission
        if (!IsComplexityWithinLimit(GetContextComplexity(bodyType, call.Arguments?.TypeArguments), depth: depth))
        {
            return;
        }

        // Non-generic slots carry only owner arguments; generic slots reuse their real 'MethodSpec' context
        if (call.Arguments is null)
        {
            EnqueueDefaultBody(target.Method, bodyType, depth: depth);
        }
        else
        {
            EnqueueMethodBody(
                method: target.Method,
                declaringType: bodyType,
                methodArguments: call.Arguments,
                genericContext: new GenericContext(
                    type: bodyType as GenericInstanceTypeSignature,
                    method: call.Arguments),
                depth: depth);
        }
    }

    /// <summary>
    /// Checks whether the existing generic dispatch path owns this call context.
    /// </summary>
    /// <param name="call">The actual constrained call.</param>
    /// <param name="receiver">The closed receiver definition.</param>
    /// <returns>Whether additional static replay would repeat already supported work.</returns>
    private bool IsHandledByGenericDispatch(StaticCallContext call, TypeDefinition receiver)
    {
        if (receiver.IsInterface ||
            call.Arguments is null)
        {
            return false;
        }

        if (!HasConstructedMethodArgument(call.Arguments))
        {
            return false;
        }

        bool hasIndexedTargets = _dispatchIndex.GetTargets(call.Declaration).Any();

        return hasIndexedTargets;
    }

    /// <summary>
    /// Filters an existing generic static candidate against a complete constrained-slot selection.
    /// </summary>
    /// <param name="candidate">The existing indexed binding.</param>
    /// <param name="declaringType">The call's declaring interface.</param>
    /// <param name="receiverType">The candidate's projected receiver context.</param>
    /// <param name="selected">The unique actual implementation.</param>
    /// <param name="bodyType">Its exact constructed owner.</param>
    /// <returns>Whether this candidate contributes that implementation context.</returns>
    private bool MatchesStaticTarget(
        MethodDispatchIndex.Target candidate,
        TypeSignature declaringType,
        TypeSignature receiverType,
        MethodDispatchIndex.Target selected,
        TypeSignature bodyType)
    {
        // Matching the body definition alone would conflate implementations on different constructed owners
        return ReferenceEquals(candidate.Method, selected.Method) &&
            _dispatchIndex.TryBind(candidate, declaringType, receiverType, out TypeSignature candidateOwner) &&
            _signatureComparer.Equals(candidateOwner, bodyType);
    }

    /// <summary>
    /// A static slot with its exact substituted receiver and optional real 'MethodSpec' arguments.
    /// </summary>
    /// <param name="Declaration">The interface declaration identity.</param>
    /// <param name="DeclaringType">The constructed declaring interface.</param>
    /// <param name="Receiver">The constrained receiver after caller substitution.</param>
    /// <param name="Arguments">The actual method arguments, or null for a non-generic slot.</param>
    private readonly record struct StaticCallContext(
        MethodDefinition Declaration,
        TypeSignature DeclaringType,
        TypeSignature Receiver,
        GenericInstanceMethodSignature? Arguments)
    {
        /// <summary>The bounded total complexity before hashing or body selection.</summary>
        public int Complexity => GetContextComplexity(DeclaringType, Arguments?.TypeArguments, Receiver);
    }

    /// <summary>
    /// A first-observed static call whose minimum traversal depth may improve.
    /// </summary>
    /// <param name="call">The composed call context.</param>
    /// <param name="depth">The initial call depth.</param>
    private sealed class StaticCallObservation(
        StaticCallContext call,
        int depth)
    {
        /// <summary>The exact call context.</summary>
        public StaticCallContext Call { get; } = call;

        /// <summary>The minimum observed depth.</summary>
        public int Depth { get; set; } = depth;

        /// <summary>The minimum depth already considered for expansion.</summary>
        public int ScheduledDepth { get; set; } = int.MaxValue;
    }
}
