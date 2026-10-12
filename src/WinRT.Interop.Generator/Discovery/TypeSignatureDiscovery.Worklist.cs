// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Errors;
using WindowsRuntime.InteropGenerator.Visitors;

namespace WindowsRuntime.InteropGenerator.Discovery;

internal sealed partial class TypeSignatureDiscovery<TResult>
{
    /// <summary>
    /// Preserves ordinary generic type discovery's first-visit reservation semantics.
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
                _pendingMembers.Enqueue(new PendingMember(
                    type: genericType,
                    method: null,
                    context: default,
                    depth: depth));
            }
            else if (!_recursionLimitReported)
            {
                _recursionLimitReported = true;

                WellKnownInteropExceptions.GenericTypeDiscoveryRecursionLimitExceededWarning(genericType, _module, MaxDiscoveryDepth).LogOrThrow(_treatWarningsAsErrors);
            }
        }
    }

    /// <summary>
    /// Queues closed generic types independently of which signatures the result visitor selects.
    /// </summary>
    /// <param name="type">The signature whose generic contexts should be considered.</param>
    /// <param name="depth">The expansion depth, or zero for an explicit root.</param>
    private void EnqueueGenericTypes(TypeSignature? type, int depth)
    {
        if (_isSupplemental)
        {
            EnqueueSupplementalGenericTypes(type, depth);
        }
        else
        {
            EnqueuePrimaryGenericTypes(type, depth);
        }
    }

    /// <summary>
    /// Queues additional type contexts without repeating primary scans at an equal or smaller depth.
    /// </summary>
    /// <param name="type">The signature whose generic contexts should be considered.</param>
    /// <param name="depth">The supplemental traversal depth.</param>
    private void EnqueueSupplementalGenericTypes(TypeSignature? type, int depth)
    {
        // Array discovery also needs generic contexts to find arrays exposed only by instantiated members
        foreach (GenericInstanceTypeSignature genericType in type?.AcceptVisitor(AllGenericTypesVisitor.Instance) ?? [])
        {
            bool visited = _visitedTypes.TryGetValue(genericType, out int priorDepth);

            if (!genericType.AcceptVisitor(IsConstructedGenericTypeVisitor.Instance) || (visited && priorDepth <= depth))
            {
                continue;
            }

            if (_primary?._coveredTypes.TryGetValue(genericType, out int primaryDepth) is true && primaryDepth <= depth)
            {
                continue;
            }

            // Types with no eligible members need no worklist entry but are still reported by the result visitor
            if (depth > 0 &&
                (!genericType.TryResolve(_module.RuntimeContext, out TypeDefinition? resolvedType) ||
                 !GetMethodsToScan(genericType, resolvedType).Any()))
            {
                _visitedTypes[genericType] = depth;

                continue;
            }

            // Expanding contexts can remain unique forever; report the type without expanding beyond the limit
            if (depth > MaxDiscoveryDepth)
            {
                if (!_recursionLimitReported)
                {
                    _recursionLimitReported = true;

                    WellKnownInteropExceptions.GenericTypeDiscoveryRecursionLimitExceededWarning(genericType, _module, MaxDiscoveryDepth, _isSupplemental).LogOrThrow(_treatWarningsAsErrors);
                }

                continue;
            }

            if (!visited && !TryReserveTransitiveInstantiation(depth))
            {
                continue;
            }

            _visitedTypes[genericType] = depth;

            _pendingMembers.Enqueue(new PendingMember(
                type: genericType,
                method: null,
                context: default,
                depth: depth));
        }
    }

    /// <summary>
    /// Queues an instantiated method body if its context has not already been considered.
    /// </summary>
    /// <param name="method">The method to resolve.</param>
    /// <param name="declaringType">The declaring type with the caller's arguments substituted.</param>
    /// <param name="methodArguments">The substituted method arguments, if this is a MethodSpec.</param>
    /// <param name="genericContext">The composed context to use when scanning the body.</param>
    /// <param name="depth">The expansion depth, or zero for an explicit MethodSpec root.</param>
    private void EnqueueMethodBody(
        IMethodDefOrRef method,
        TypeSignature? declaringType,
        GenericInstanceMethodSignature? methodArguments,
        GenericContext genericContext,
        int depth)
    {
        // Ordinary methods are covered by the type worklist, so only explicit MethodSpec roots retain a one-hop scan
        if (methodArguments is null ||
            !method.TryResolve(_module.RuntimeContext, out MethodDefinition? definition) ||
            definition.CilMethodBody is null ||
            (depth > 0 && (definition.DeclaringModule is not ModuleDefinition declaringModule || !_shouldProcessModule(declaringModule))))
        {
            return;
        }

        IList<TypeSignature> arguments = [
            .. (declaringType as GenericInstanceTypeSignature)?.TypeArguments ?? [],
            .. methodArguments.TypeArguments];

        // Partially open contexts can still reveal closed types; skip only wholly unbound argument lists
        if (depth > 0 && arguments.All(static argument => argument is GenericParameterSignature))
        {
            return;
        }

        Dictionary<IList<TypeSignature>, int> contexts = GetMethodContexts(_visitedMethods, definition);
        bool reserved = _isSupplemental
            ? TryReserveSupplementalMethodContext(definition, arguments, contexts, depth)
            : TryReservePrimaryMethodContext(definition, arguments, genericContext, contexts, depth);

        if (!reserved)
        {
            return;
        }

        contexts[arguments] = depth;

        _pendingMembers.Enqueue(new PendingMember(
            type: null,
            method: definition,
            context: genericContext,
            depth: depth,
            arguments: arguments));
    }

    /// <summary>
    /// Reserves a primary method's first visit before checking whether its body can be expanded.
    /// </summary>
    /// <param name="method">The resolved method definition.</param>
    /// <param name="arguments">The combined type and method argument key.</param>
    /// <param name="context">The context retained for a possible shorter-path replay.</param>
    /// <param name="contexts">The method's visited contexts.</param>
    /// <param name="depth">The primary traversal depth.</param>
    /// <returns>Whether to enqueue the method body.</returns>
    private bool TryReservePrimaryMethodContext(
        MethodDefinition method,
        IList<TypeSignature> arguments,
        GenericContext context,
        Dictionary<IList<TypeSignature>, int> contexts,
        int depth)
    {
        if (contexts.TryGetValue(arguments, out int priorDepth))
        {
            RecordShallowerMethod(method, arguments, context, priorDepth, depth);

            return false;
        }

        // First visits stay reserved even when the budget or depth limit rejects expansion
        contexts.Add(arguments, depth);

        return TryReserveTransitiveInstantiation(depth) && IsMethodWithinDepthLimit(method, depth);
    }

    /// <summary>
    /// Reserves a new supplemental method context only after excluding already covered bodies.
    /// </summary>
    /// <param name="method">The resolved method definition.</param>
    /// <param name="arguments">The combined type and method argument key.</param>
    /// <param name="contexts">The method's visited contexts.</param>
    /// <param name="depth">The supplemental traversal depth.</param>
    /// <returns>Whether to enqueue the method body.</returns>
    private bool TryReserveSupplementalMethodContext(
        MethodDefinition method,
        IList<TypeSignature> arguments,
        Dictionary<IList<TypeSignature>, int> contexts,
        int depth)
    {
        bool visited = contexts.TryGetValue(arguments, out int priorDepth);

        // Exclude covered work before diagnostics or budget charges; a shorter replay reuses its reservation
        return
            (!visited || priorDepth > depth) &&
            _primary?.IsMethodCovered(method, arguments, depth) is not true &&
            IsMethodWithinDepthLimit(method, depth) &&
            (visited || TryReserveTransitiveInstantiation(depth));
    }

    /// <summary>
    /// Applies the shared method depth limit without changing context reservation order.
    /// </summary>
    /// <param name="method">The method being considered.</param>
    /// <param name="depth">Its expansion depth.</param>
    /// <returns>Whether the body is within the depth limit.</returns>
    private bool IsMethodWithinDepthLimit(MethodDefinition method, int depth)
    {
        if (depth > MaxDiscoveryDepth)
        {
            if (!_recursionLimitReported)
            {
                _recursionLimitReported = true;

                WellKnownInteropExceptions.GenericMethodDiscoveryRecursionLimitExceededWarning(method, _module, MaxDiscoveryDepth, _isSupplemental).LogOrThrow(_treatWarningsAsErrors);
            }

            return false;
        }

        return true;
    }

    /// <summary>
    /// Reserves from the shared type and method expansion budget.
    /// </summary>
    /// <param name="depth">The expansion depth, or zero for an explicit root.</param>
    /// <returns>Whether expansion may proceed, or no reservation is needed.</returns>
    private bool TryReserveTransitiveInstantiation(int depth)
    {
        // Roots are unbounded; contexts beyond the depth limit will not be queued at all
        if (depth is 0 or > MaxDiscoveryDepth)
        {
            return true;
        }

        if (_transitiveTypeCount == MaxTransitiveTypes)
        {
            if (!_transitiveTypeLimitReported)
            {
                _transitiveTypeLimitReported = true;

                (_isSupplemental
                    ? WellKnownInteropExceptions.SupplementalGenericDiscoveryLimitExceededWarning(_module, MaxTransitiveTypes)
                    : WellKnownInteropExceptions.GenericTypeDiscoveryTransitiveTypeLimitExceededWarning(_module, MaxTransitiveTypes)).LogOrThrow(_treatWarningsAsErrors);
            }

            return false;
        }

        _transitiveTypeCount++;

        return true;
    }

    /// <summary>
    /// Checks a signature's element count without walking beyond what is needed to detect an overflow.
    /// </summary>
    /// <param name="type">The signature to check, if available.</param>
    /// <param name="depth">The expansion depth, or zero for an explicit root.</param>
    /// <returns>Whether the signature may be visited.</returns>
    private bool IsSignatureWithinComplexityLimit(TypeSignature? type, int depth)
    {
        return IsComplexityWithinLimit(GetSignatureComplexity(type), depth);
    }

    /// <summary>
    /// Counts only enough signature elements to determine whether the limit was exceeded.
    /// </summary>
    /// <param name="type">The signature to inspect, if available.</param>
    /// <returns>The bounded element count.</returns>
    private static int GetSignatureComplexity(TypeSignature? type)
    {
        return type?.GetSignatureElementCount(MaxSignatureComplexity + 1) ?? 0;
    }

    /// <summary>
    /// Measures a complete substituted context without emitting phase-specific diagnostics.
    /// </summary>
    /// <param name="declaringType">The substituted declaring signature.</param>
    /// <param name="arguments">The substituted method arguments, if available.</param>
    /// <param name="constraint">The optional substituted receiver constraint.</param>
    /// <returns>The aggregate bounded element count.</returns>
    private static int GetContextComplexity(TypeSignature? declaringType, IList<TypeSignature>? arguments, TypeSignature? constraint = null)
    {
        int complexity = GetSignatureComplexity(declaringType) + GetSignatureComplexity(constraint);

        foreach (TypeSignature argument in arguments ?? [])
        {
            complexity += GetSignatureComplexity(argument);

            if (complexity > MaxSignatureComplexity)
            {
                break;
            }
        }

        return complexity;
    }

    /// <summary>
    /// Retrieves a method's resolution-aware argument-context map without changing visit state.
    /// </summary>
    /// <typeparam name="TValue">The per-context state retained by the caller.</typeparam>
    /// <param name="methods">The map keyed by resolved method identity.</param>
    /// <param name="method">The resolved method.</param>
    /// <returns>The method's argument-context map.</returns>
    private Dictionary<IList<TypeSignature>, TValue> GetMethodContexts<TValue>(
        Dictionary<MethodDefinition, Dictionary<IList<TypeSignature>, TValue>> methods,
        MethodDefinition method)
    {
        if (!methods.TryGetValue(method, out Dictionary<IList<TypeSignature>, TValue>? contexts))
        {
            contexts = new Dictionary<IList<TypeSignature>, TValue>(_signatureComparer);
            methods.Add(method, contexts);
        }

        return contexts;
    }

    /// <summary>
    /// Checks signature or aggregate context complexity, exempting explicit roots.
    /// </summary>
    /// <param name="complexity">The signature element count accumulated so far.</param>
    /// <param name="depth">The expansion depth, or zero for an explicit root.</param>
    /// <returns>Whether discovery may proceed within this complexity.</returns>
    private bool IsComplexityWithinLimit(int complexity, int depth)
    {
        if (depth == 0 || complexity <= MaxSignatureComplexity)
        {
            return true;
        }

        if (!_complexityLimitReported)
        {
            _complexityLimitReported = true;

            WellKnownInteropExceptions.GenericTypeDiscoveryComplexityLimitExceededWarning(_module, MaxSignatureComplexity, _isSupplemental).LogOrThrow(_treatWarningsAsErrors);
        }

        return false;
    }

    /// <summary>
    /// A type or method context awaiting member discovery.
    /// </summary>
    /// <param name="type">The type context to expand, or <see langword="null"/> for a method body.</param>
    /// <param name="method">The method body to scan, or <see langword="null"/> for a type context.</param>
    /// <param name="context">The substituted method context, or the default value for type expansion.</param>
    /// <param name="depth">The expansion depth, or zero for an explicit metadata root.</param>
    /// <param name="arguments">The method context key used to discard superseded depth entries.</param>
    private readonly struct PendingMember(
        TypeSignature? type,
        MethodDefinition? method,
        GenericContext context,
        int depth,
        IList<TypeSignature>? arguments = null)
    {
        /// <summary>The type context to expand, or <see langword="null"/> for a method body.</summary>
        public TypeSignature? Type { get; } = type;

        /// <summary>The method body to scan, or <see langword="null"/> for a type context.</summary>
        public MethodDefinition? Method { get; } = method;

        /// <summary>The substituted method context; type contexts are composed when dequeued.</summary>
        public GenericContext Context { get; } = context;

        /// <summary>The expansion depth, with zero reserved for explicit metadata roots.</summary>
        public int Depth { get; } = depth;

        /// <summary>The method argument key, or <see langword="null"/> for a queued type.</summary>
        public IList<TypeSignature>? Arguments { get; } = arguments;
    }
}
