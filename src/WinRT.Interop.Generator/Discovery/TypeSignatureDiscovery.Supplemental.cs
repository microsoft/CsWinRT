// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Models;

namespace WindowsRuntime.InteropGenerator.Discovery;

/// <inheritdoc cref="TypeSignatureDiscovery{TResult}"/>
internal partial class TypeSignatureDiscovery<TResult>
{
    /// <summary>
    /// Records a shorter method path without changing the primary's first-visit state.
    /// </summary>
    /// <param name="method">The resolved method.</param>
    /// <param name="arguments">Its combined declaring and method arguments.</param>
    /// <param name="context">The composed generic context.</param>
    /// <param name="priorDepth">The primary's first-visit depth.</param>
    /// <param name="depth">The shorter observed depth.</param>
    private void RecordShallowerMethod(
        MethodDefinition method,
        IList<TypeSignature> arguments,
        GenericContext context,
        int priorDepth,
        int depth)
    {
        if (depth >= priorDepth)
        {
            return;
        }

        Dictionary<IList<TypeSignature>, ShallowerMethodObservation> contexts = GetMethodContexts(_shallowerMethodContexts, method);

        if (contexts.TryGetValue(arguments, out ShallowerMethodObservation? observation))
        {
            observation.Depth = Math.Min(observation.Depth, depth);
        }
        else
        {
            observation = new ShallowerMethodObservation(
                method: method,
                arguments: arguments,
                context: context,
                priorDepth: priorDepth,
                depth: depth);
            contexts.Add(arguments, observation);
            _shallowerMethods.Add(observation);
        }
    }

    /// <summary>
    /// Retains one minimum-depth observation per composed dispatch context without primary expansion.
    /// </summary>
    /// <param name="operand">The instruction's dispatch descriptor and optional constraint.</param>
    /// <param name="callerContext">The caller's type and method arguments.</param>
    /// <param name="depth">The observed call depth.</param>
    private void RecordDispatchObservation(MethodOperand operand, GenericContext callerContext, int depth)
    {
        if ((!operand.IsVirtual && operand.Constraint is null) ||
            operand.Descriptor is not MethodSpecification { Method: { } declaration, Signature: { } signature } ||
            !declaration.TryResolve(_module.RuntimeContext, out MethodDefinition? method) ||
            !method.IsVirtual || method.IsFinal || !_dispatchIndex.GetTargets(method).Any())
        {
            return;
        }

        if (!TryComposeDispatchContext(declaration, signature, operand.Constraint, callerContext, out DispatchCallContext call))
        {
            return;
        }

        if (call.Complexity > MaxSignatureComplexity)
        {
            // Passive recording must not change primary warnings, so defer the diagnostic too
            if (!_complexDispatchRecorded)
            {
                _complexDispatchRecorded = true;

                _dispatchObservations.Add(new DispatchObservation(
                    operand: operand,
                    context: callerContext,
                    depth: depth));
            }

            return;
        }

        if (!HasConstructedMethodArgument(call.Arguments))
        {
            return;
        }

        Dictionary<IList<TypeSignature>, DispatchObservation> contexts = GetMethodContexts(_dispatchObservationContexts, method);
        IList<TypeSignature> key = call.Constraint is null
            ? [call.DeclaringType, .. call.Arguments.TypeArguments]
            : [call.DeclaringType, .. call.Arguments.TypeArguments, call.Constraint];

        if (contexts.TryGetValue(key, out DispatchObservation? observation))
        {
            observation.Depth = Math.Min(observation.Depth, depth);
        }
        else
        {
            observation = new DispatchObservation(
                operand: operand,
                context: callerContext,
                depth: depth);
            contexts.Add(key, observation);
            _dispatchObservations.Add(observation);
        }
    }

    /// <summary>
    /// Records real member scans for supplemental deduplication and late joins.
    /// </summary>
    /// <param name="method">The method whose signature and body are being scanned.</param>
    /// <param name="context">The declaring-type and method context.</param>
    /// <param name="depth">The original scan depth.</param>
    private void RecordMethodScan(MethodDefinition method, GenericContext context, int depth)
    {
        // Type scans and selected non-generic bodies have no 'MethodSpec' arguments
        // Their coverage is tracked separately; there is no generic-method context to publish.
        if (context.Method is null)
        {
            return;
        }

        IList<TypeSignature> arguments = [.. context.Type?.TypeArguments ?? [], .. context.Method?.TypeArguments ?? []];

        Dictionary<IList<TypeSignature>, int> contexts = GetMethodContexts(_coveredMethods, method);

        if (!contexts.TryGetValue(arguments, out int priorDepth) || depth < priorDepth)
        {
            contexts[arguments] = depth;

            // Only indexed dispatch bodies can be rescheduled by the late join, so publish just those
            if (_dispatchIndex.IsDispatchBody(method))
            {
                _dispatchContexts?.ExpandedBodies.Add(new DispatchContextRegistry.ExpandedBody(
                    Method: method,
                    Arguments: arguments,
                    Depth: depth,
                    Phase: _dispatchPhase));
            }
        }
    }

    /// <summary>
    /// Checks whether the primary actually scanned a method context at an equal or smaller depth.
    /// </summary>
    /// <param name="method">The resolved method body.</param>
    /// <param name="arguments">The combined declaring and method arguments.</param>
    /// <param name="depth">The proposed supplemental depth.</param>
    /// <returns>Whether scanning this body again is unnecessary.</returns>
    private bool IsMethodCovered(MethodDefinition method, IList<TypeSignature> arguments, int depth)
    {
        return _coveredMethods.TryGetValue(method, out Dictionary<IList<TypeSignature>, int>? contexts) &&
            contexts.TryGetValue(arguments, out int coveredDepth) && coveredDepth <= depth;
    }

    /// <summary>
    /// Runs additive work only after the primary traversal is complete and immutable.
    /// </summary>
    /// <returns>Additional signatures not already reported by the primary.</returns>
    private IEnumerable<TResult> EnumerateSupplementalMembers()
    {
        TypeSignatureDiscovery<TResult> primary = _primary!;

        // Seed with the primary results so this phase only yields additions
        _results.UnionWith(primary._results);

        // Preserve first-sighting order without expanding defaults ahead of the existing supplemental FIFO
        foreach (DefaultReceiverObservation observation in primary._defaultReceiverObservations)
        {
            RecordDefaultReceiver(observation.Signature, observation.Depth);
        }

        foreach (StaticCallObservation observation in primary._staticCallObservations)
        {
            RecordStaticCall(observation.Call, depth: observation.Depth);
        }

        // Report the complexity warning that passive primary receiver registration deferred
        if (primary._complexReceiver is { } complexReceiver)
        {
            _ = IsSignatureWithinComplexityLimit(complexReceiver, primary._complexReceiverDepth);
        }

        // Replay passive receivers before demands so local joins see the complete primary context
        foreach (Dictionary<TypeSignature, int> contexts in primary._receiverContexts.Values)
        {
            foreach ((TypeSignature receiver, int depth) in contexts)
            {
                RegisterReceiver(receiver, depth);
            }
        }

        // Only supplemental traversal can expand the recorded virtual targets
        foreach (DispatchObservation observation in primary._dispatchObservations)
        {
            RegisterDispatch(observation.Operand, observation.Context, observation.Depth);
        }

        // Replay shorter paths after a depth-limit hit, when they can recover skipped descendants
        // Keep primary budget-rejected methods out of this replay instead of extending their allowance.
        if (primary._recursionLimitReported)
        {
            foreach ((TypeSignature type, int depth) in primary._shallowerTypes)
            {
                EnqueueGenericTypes(type, depth);
            }

            foreach (ShallowerMethodObservation method in primary._shallowerMethods)
            {
                if (method.PriorDepth > MaxDiscoveryDepth || primary.IsMethodCovered(method.Method, method.Arguments, int.MaxValue))
                {
                    EnqueueMethodBody(
                        method: method.Method,
                        declaringType: method.Context.Type as TypeSignature,
                        methodArguments: method.Context.Method as GenericInstanceMethodSignature,
                        genericContext: method.Context,
                        depth: method.Depth);
                }
            }
        }

        foreach (TResult result in EnumeratePendingMembers())
        {
            yield return result;
        }

        foreach (TResult result in EnumerateDefaultInterfaceMembers())
        {
            yield return result;
        }

        foreach (TResult result in EnumerateStaticDispatchMembers())
        {
            yield return result;
        }
    }

    /// <summary>
    /// A primary dispatch observation with its original caller context and depth.
    /// </summary>
    /// <param name="operand">The instruction's dispatch descriptor and constraint.</param>
    /// <param name="context">The caller's generic arguments.</param>
    /// <param name="depth">The observed transitive depth.</param>
    private sealed class DispatchObservation(MethodOperand operand, GenericContext context, int depth)
    {
        /// <summary>The recorded dispatch operand.</summary>
        public MethodOperand Operand { get; } = operand;

        /// <summary>The first caller context composing to this dispatch key.</summary>
        public GenericContext Context { get; } = context;

        /// <summary>The minimum observed depth.</summary>
        public int Depth { get; set; } = depth;
    }

    /// <summary>
    /// A shorter path that may recover a primary depth-limited method context.
    /// </summary>
    /// <param name="method">The method definition.</param>
    /// <param name="arguments">Its combined argument key.</param>
    /// <param name="context">The composed generic context.</param>
    /// <param name="priorDepth">The primary first-visit depth.</param>
    /// <param name="depth">The shortest observed depth.</param>
    private sealed class ShallowerMethodObservation(
        MethodDefinition method,
        IList<TypeSignature> arguments,
        GenericContext context,
        int priorDepth,
        int depth)
    {
        /// <summary>The body definition.</summary>
        public MethodDefinition Method { get; } = method;

        /// <summary>The combined argument key.</summary>
        public IList<TypeSignature> Arguments { get; } = arguments;

        /// <summary>The composed method context.</summary>
        public GenericContext Context { get; } = context;

        /// <summary>The primary's original visit depth.</summary>
        public int PriorDepth { get; } = priorDepth;

        /// <summary>The minimum sighting depth.</summary>
        public int Depth { get; set; } = depth;
    }
}
