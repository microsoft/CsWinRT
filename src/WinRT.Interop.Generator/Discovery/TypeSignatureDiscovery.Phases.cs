// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Errors;
using WindowsRuntime.InteropGenerator.Models;
using WindowsRuntime.InteropGenerator.Visitors;

namespace WindowsRuntime.InteropGenerator.Discovery;

internal sealed partial class TypeSignatureDiscovery<TResult>
{
    /// <summary>Type contexts whose members were actually scanned, not merely marked visited.</summary>
    private readonly Dictionary<TypeSignature, int> _coveredTypes;

    /// <summary>Method contexts actually scanned at their minimum observed depth.</summary>
    private readonly Dictionary<MethodDefinition, Dictionary<IList<TypeSignature>, int>> _coveredMethods = new(ReferenceEqualityComparer.Instance);

    /// <summary>Dispatch observations recorded without affecting the primary queue or budget.</summary>
    private readonly List<DispatchObservation> _dispatchObservations = [];

    /// <summary>Composed dispatch keys deduplicated before replaying the supplemental phase.</summary>
    private readonly Dictionary<MethodDefinition, Dictionary<IList<TypeSignature>, DispatchObservation>> _dispatchObservationContexts = new(ReferenceEqualityComparer.Instance);

    /// <summary>Whether an oversized dispatch observation has already been retained for diagnostics.</summary>
    private bool _complexDispatchRecorded;

    /// <summary>One oversized receiver retained without hashing so its supplemental diagnostic remains visible.</summary>
    private TypeSignature? _complexReceiver;

    /// <summary>The traversal depth of the retained oversized receiver.</summary>
    private int _complexReceiverDepth;

    /// <summary>Shallower type sightings that may recover a depth-limited primary path.</summary>
    private readonly Dictionary<TypeSignature, int> _shallowerTypes;

    /// <summary>Shallower method sightings, retained in their first observation order.</summary>
    private readonly List<ShallowerMethodObservation> _shallowerMethods = [];

    /// <summary>Minimum method sighting depths, without repeated list entries for hot contexts.</summary>
    private readonly Dictionary<MethodDefinition, Dictionary<IList<TypeSignature>, ShallowerMethodObservation>> _shallowerMethodContexts = new(ReferenceEqualityComparer.Instance);

    /// <summary>
    /// Preserves the parent's ordinary generic type worklist and first-visit reservation semantics.
    /// </summary>
    /// <param name="type">The signature whose type contexts should be considered.</param>
    /// <param name="depth">The original traversal depth.</param>
    private void EnqueuePrimaryGenericTypes(TypeSignature? type, int depth)
    {
        foreach (GenericInstanceTypeSignature genericType in type?.AcceptVisitor(AllGenericTypesVisitor.Instance) ?? [])
        {
            if (!genericType.AcceptVisitor(IsConstructedGenericTypeVisitor.Instance))
            {
                continue;
            }

            if (_visitedTypes.TryGetValue(genericType, out int priorDepth))
            {
                if (depth < priorDepth && (!_shallowerTypes.TryGetValue(genericType, out int recordedDepth) || depth < recordedDepth))
                {
                    _shallowerTypes[genericType] = depth;
                }

                continue;
            }

            if (depth > 0 &&
                (!genericType.TryResolve(_module.RuntimeContext, out TypeDefinition? resolvedType) ||
                 !GetMethodsToScan(genericType, resolvedType).Any()))
            {
                _visitedTypes.Add(genericType, depth);
                continue;
            }

            if (!TryReserveTransitiveInstantiation(depth))
            {
                continue;
            }

            _visitedTypes.Add(genericType, depth);
            if (depth <= MaxDiscoveryDepth)
            {
                _pendingMembers.Enqueue(new PendingMember(genericType, null, default, depth));
            }
            else if (!_recursionLimitReported)
            {
                _recursionLimitReported = true;
                WellKnownInteropExceptions.GenericTypeDiscoveryRecursionLimitExceededWarning(genericType, _module, MaxDiscoveryDepth).LogOrThrow(_treatWarningsAsErrors);
            }
        }
    }

    /// <summary>
    /// Records a shorter method path without changing the primary's first-visit state.
    /// </summary>
    /// <param name="method">The resolved method.</param>
    /// <param name="arguments">Its combined declaring and method arguments.</param>
    /// <param name="context">The composed generic context.</param>
    /// <param name="priorDepth">The primary's first-visit depth.</param>
    /// <param name="depth">The shorter observed depth.</param>
    private void RecordShallowerMethod(MethodDefinition method, IList<TypeSignature> arguments, GenericContext context, int priorDepth, int depth)
    {
        if (depth < priorDepth)
        {
            if (!_shallowerMethodContexts.TryGetValue(method, out Dictionary<IList<TypeSignature>, ShallowerMethodObservation>? contexts))
            {
                contexts = new Dictionary<IList<TypeSignature>, ShallowerMethodObservation>(_signatureComparer);
                _shallowerMethodContexts.Add(method, contexts);
            }

            if (contexts.TryGetValue(arguments, out ShallowerMethodObservation? observation))
            {
                observation.Depth = System.Math.Min(observation.Depth, depth);
            }
            else
            {
                observation = new ShallowerMethodObservation(method, arguments, context, priorDepth, depth);
                contexts.Add(arguments, observation);
                _shallowerMethods.Add(observation);
            }
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

        TypeSignature declaringType = declaration.DeclaringType!.ToTypeSignature(_module.RuntimeContext).InstantiateGenericTypes(callerContext);
        TypeSignature? constraint = operand.Constraint?.ToTypeSignature(_module.RuntimeContext).InstantiateGenericTypes(callerContext);
        IList<TypeSignature> methodArguments = [.. signature.TypeArguments.Select(argument => argument.InstantiateGenericTypes(callerContext))];
        int complexity = declaringType.GetSignatureElementCount(MaxSignatureComplexity + 1) +
            (constraint?.GetSignatureElementCount(MaxSignatureComplexity + 1) ?? 0);
        foreach (TypeSignature argument in methodArguments)
        {
            complexity += argument.GetSignatureElementCount(MaxSignatureComplexity + 1);
        }

        if (complexity > MaxSignatureComplexity)
        {
            // Defer the diagnostic too: passive recording must not change primary warnings.
            if (!_complexDispatchRecorded)
            {
                _complexDispatchRecorded = true;
                _dispatchObservations.Add(new DispatchObservation(operand, callerContext, depth));
            }

            return;
        }

        if (!methodArguments.Any(static argument => argument.AcceptVisitor(IsConstructedGenericTypeVisitor.Instance)))
        {
            return;
        }

        if (!_dispatchObservationContexts.TryGetValue(method, out Dictionary<IList<TypeSignature>, DispatchObservation>? contexts))
        {
            contexts = new Dictionary<IList<TypeSignature>, DispatchObservation>(_signatureComparer);
            _dispatchObservationContexts.Add(method, contexts);
        }

        IList<TypeSignature> key = [declaringType, .. methodArguments, .. constraint is null ? [] : new[] { constraint }];
        if (contexts.TryGetValue(key, out DispatchObservation? observation))
        {
            observation.Depth = System.Math.Min(observation.Depth, depth);
        }
        else
        {
            observation = new DispatchObservation(operand, callerContext, depth);
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
        // Type worklist contexts are covered by _coveredTypes; method items always have MethodSpec arguments.
        if (context.Method is null)
        {
            return;
        }

        IList<TypeSignature> arguments = [.. context.Type?.TypeArguments ?? [], .. context.Method?.TypeArguments ?? []];
        if (!_coveredMethods.TryGetValue(method, out Dictionary<IList<TypeSignature>, int>? contexts))
        {
            contexts = new Dictionary<IList<TypeSignature>, int>(_signatureComparer);
            _coveredMethods.Add(method, contexts);
        }

        if (!contexts.TryGetValue(arguments, out int priorDepth) || depth < priorDepth)
        {
            contexts[arguments] = depth;
            if (_dispatchIndex.IsDispatchBody(method))
            {
                _dispatchContexts?.ExpandedBodies.Add(new DispatchContextRegistry.ExpandedBody(method, arguments, depth));
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
        _results.UnionWith(primary._results);

        if (primary._complexReceiver is { } complexReceiver)
        {
            _ = IsSignatureWithinComplexityLimit(complexReceiver, primary._complexReceiverDepth);
        }

        foreach (Dictionary<TypeSignature, int> contexts in primary._receiverContexts.Values)
        {
            foreach ((TypeSignature receiver, int depth) in contexts)
            {
                RegisterReceiver(receiver, depth);
            }
        }

        foreach (DispatchObservation observation in primary._dispatchObservations)
        {
            EnumerateDispatch(observation.Operand, observation.Context, observation.Depth);
        }

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
                    EnqueueMethodBody(method.Method, method.Context.Type as TypeSignature,
                        method.Context.Method as GenericInstanceMethodSignature, method.Context, method.Depth);
                }
            }
        }

        foreach (TResult result in EnumeratePendingMembers())
        {
            yield return result;
        }
    }

    /// <summary>A primary dispatch observation with its original caller context and depth.</summary>
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

    /// <summary>A shorter path that may recover a primary depth-limited method context.</summary>
    /// <param name="method">The method definition.</param>
    /// <param name="arguments">Its combined argument key.</param>
    /// <param name="context">The composed generic context.</param>
    /// <param name="priorDepth">The primary first-visit depth.</param>
    /// <param name="depth">The shortest observed depth.</param>
    private sealed class ShallowerMethodObservation(MethodDefinition method, IList<TypeSignature> arguments, GenericContext context, int priorDepth, int depth)
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
