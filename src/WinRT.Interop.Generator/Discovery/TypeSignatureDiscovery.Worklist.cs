// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Errors;
using WindowsRuntime.InteropGenerator.Visitors;

namespace WindowsRuntime.InteropGenerator.Discovery;

/// <inheritdoc cref="TypeSignatureDiscovery{TResult}"/>
internal partial class TypeSignatureDiscovery<TResult>
{
    /// <summary>
    /// Preserves ordinary generic type discovery's first-visit reservation semantics.
    /// </summary>
    /// <param name="type">The signature whose type contexts should be considered.</param>
    /// <param name="depth">The original traversal depth.</param>
    /// <param name="scanContext">The ordinary member scan exposing the signature, if any.</param>
    private void EnqueuePrimaryGenericTypes(TypeSignature? type, int depth, MemberScanContext? scanContext)
    {
        foreach (GenericInstanceTypeSignature genericType in type?.AcceptVisitor(AllGenericTypesVisitor.Instance) ?? [])
        {
            // Open substitutions cannot supply a concrete context for member expansion
            if (!genericType.AcceptVisitor(IsConstructedGenericTypeVisitor.Instance))
            {
                continue;
            }

            // Keep the first visit's place and save shallower sightings for supplemental replay
            if (_visitedTypes.TryGetValue(genericType, out int priorDepth))
            {
                if (depth < priorDepth && (!_shallowerTypes.TryGetValue(genericType, out int recordedDepth) || depth < recordedDepth))
                {
                    _shallowerTypes[genericType] = depth;
                }

                continue;
            }

            TypeDefinition? resolvedType = null;

            // Only transitive contexts with eligible members are charged (explicit roots keep their one-hop scan)
            if (depth > 0 &&
                (!genericType.TryResolve(_module.RuntimeContext, out resolvedType) ||
                 !GetMethodsToScan(genericType, resolvedType).Any()))
            {
                _visitedTypes.Add(genericType, depth);

                continue;
            }

            // Reusing a closed substitution within its lexical family needs no additional reservation
            if (!CanShareMemberReservation(scanContext, resolvedType, genericType.TypeArguments, isConstructed: true) &&
                !TryReserveTransitiveInstantiation(depth: depth))
            {
                continue;
            }

            // Record admission only after reserving so rejected types remain eligible for a later sighting
            _visitedTypes.Add(genericType, depth);

            // 'Node<T>' -> 'Node<Node<T>>' never repeats an exact signature, so deduplication cannot stop it
            // Keep reporting discovered types beyond the depth limit, but do not expand their members
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
    /// <param name="scanContext">The ordinary member scan exposing the signature, if any.</param>
    private void EnqueueGenericTypes(TypeSignature? type, int depth, MemberScanContext? scanContext = null)
    {
        if (_isSupplemental)
        {
            EnqueueSupplementalGenericTypes(type, depth, scanContext);
        }
        else
        {
            EnqueuePrimaryGenericTypes(type, depth, scanContext);
        }
    }

    /// <summary>
    /// Queues additional type contexts without repeating primary scans at an equal or smaller depth.
    /// </summary>
    /// <param name="type">The signature whose generic contexts should be considered.</param>
    /// <param name="depth">The supplemental traversal depth.</param>
    /// <param name="scanContext">The ordinary member scan exposing the signature, if any.</param>
    private void EnqueueSupplementalGenericTypes(TypeSignature? type, int depth, MemberScanContext? scanContext)
    {
        // Array discovery also needs generic contexts to find arrays exposed only by instantiated members
        foreach (GenericInstanceTypeSignature genericType in type?.AcceptVisitor(AllGenericTypesVisitor.Instance) ?? [])
        {
            bool visited = _visitedTypes.TryGetValue(genericType, out int priorDepth);

            if (!genericType.AcceptVisitor(IsConstructedGenericTypeVisitor.Instance) || (visited && priorDepth <= depth))
            {
                continue;
            }

            // Compare against actual primary scans, not visits, so budget-rejected primary types can still expand
            if (_primary?._coveredTypes.TryGetValue(genericType, out int primaryDepth) is true && primaryDepth <= depth)
            {
                continue;
            }

            TypeDefinition? resolvedType = null;

            // Types with no eligible members need no worklist entry but are still reported by the result visitor
            if (depth > 0 &&
                (!genericType.TryResolve(_module.RuntimeContext, out resolvedType) ||
                 !GetMethodsToScan(genericType, resolvedType).Any()))
            {
                _visitedTypes[genericType] = depth;

                continue;
            }

            // Expanding contexts can stay unique forever, so report the type without expanding beyond the limit
            if (depth > MaxDiscoveryDepth)
            {
                if (!_recursionLimitReported)
                {
                    _recursionLimitReported = true;

                    WellKnownInteropExceptions.GenericTypeDiscoveryRecursionLimitExceededWarning(genericType, _module, MaxDiscoveryDepth, _isSupplemental).LogOrThrow(_treatWarningsAsErrors);
                }

                continue;
            }

            // A shallower requeue reuses this enumeration's earlier reservation.
            // New same-family contexts can instead share the active scan's closed substitution.
            if (!visited &&
                !CanShareMemberReservation(scanContext, resolvedType, genericType.TypeArguments, isConstructed: true) &&
                !TryReserveTransitiveInstantiation(depth: depth))
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
    /// <param name="methodArguments">The substituted method arguments, if this is a 'MethodSpec'.</param>
    /// <param name="genericContext">The composed context to use when scanning the body.</param>
    /// <param name="depth">The expansion depth, or zero for an explicit 'MethodSpec' root.</param>
    /// <param name="scanContext">The ordinary member scan exposing the operand, if any.</param>
    private void EnqueueMethodBody(
        IMethodDefOrRef method,
        TypeSignature? declaringType,
        GenericInstanceMethodSignature? methodArguments,
        GenericContext genericContext,
        int depth,
        MemberScanContext? scanContext = null)
    {
        // Ordinary methods are covered by the type worklist, so only explicit 'MethodSpec' roots retain a one-hop scan
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

        // Skip only wholly unbound argument lists because partially open contexts can still reveal closed types
        if (depth > 0 && arguments.All(static argument => argument is GenericParameterSignature))
        {
            return;
        }

        Dictionary<IList<TypeSignature>, int> contexts = GetMethodContexts(_visitedMethods, definition);
        bool reserved = _isSupplemental
            ? TryReserveSupplementalMethodContext(definition, arguments, contexts, depth, scanContext)
            : TryReservePrimaryMethodContext(definition, arguments, genericContext, contexts, depth, scanContext);

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
    /// <param name="scanContext">The ordinary member scan exposing the operand, if any.</param>
    /// <returns>Whether to enqueue the method body.</returns>
    private bool TryReservePrimaryMethodContext(
        MethodDefinition method,
        IList<TypeSignature> arguments,
        GenericContext context,
        Dictionary<IList<TypeSignature>, int> contexts,
        int depth,
        MemberScanContext? scanContext)
    {
        if (contexts.TryGetValue(arguments, out int priorDepth))
        {
            RecordShallowerMethod(method, arguments, context, priorDepth, depth);

            return false;
        }

        // Reserving before checking limits preserves ordinary discovery's first-visit/FIFO behavior.
        // Defer shorter sightings to supplemental replay rather than changing primary scheduling.
        contexts.Add(arguments, depth);

        return
            (CanShareMemberReservation(scanContext, method.DeclaringType, arguments) || TryReserveTransitiveInstantiation(depth: depth)) &&
            IsMethodWithinDepthLimit(method, depth);
    }

    /// <summary>
    /// Reserves a new supplemental method context only after excluding already covered bodies.
    /// </summary>
    /// <param name="method">The resolved method definition.</param>
    /// <param name="arguments">The combined type and method argument key.</param>
    /// <param name="contexts">The method's visited contexts.</param>
    /// <param name="depth">The supplemental traversal depth.</param>
    /// <param name="scanContext">The ordinary member scan exposing the operand, if any.</param>
    /// <returns>Whether to enqueue the method body.</returns>
    private bool TryReserveSupplementalMethodContext(
        MethodDefinition method,
        IList<TypeSignature> arguments,
        Dictionary<IList<TypeSignature>, int> contexts,
        int depth,
        MemberScanContext? scanContext)
    {
        bool visited = contexts.TryGetValue(arguments, out int priorDepth);

        // Only a shorter path can improve on work already admitted by this traversal
        if (visited && priorDepth <= depth)
        {
            return false;
        }

        // Primary coverage needs no supplemental replay, diagnostics, or reservation
        if (_primary?.IsMethodCovered(method, arguments, depth) is true)
        {
            return false;
        }

        // Check depth before charging, even when a previous or shared reservation is available
        if (!IsMethodWithinDepthLimit(method, depth))
        {
            return false;
        }

        // A shorter replay reuses this traversal's existing reservation
        if (visited)
        {
            return true;
        }

        // An unchanged closed substitution can borrow the active lexical family's reservation
        if (CanShareMemberReservation(scanContext, method.DeclaringType, arguments))
        {
            return true;
        }

        // Only genuinely new expansion spends the remaining supplemental allowance
        return TryReserveTransitiveInstantiation(depth: depth);
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
        // Roots need no reservation, and contexts beyond the depth limit will not be queued
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
        return IsComplexityWithinLimit(GetSignatureComplexity(type), depth: depth);
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

        /// <summary>The substituted method context (type contexts are composed when dequeued).</summary>
        public GenericContext Context { get; } = context;

        /// <summary>The expansion depth, with zero reserved for explicit metadata roots.</summary>
        public int Depth { get; } = depth;

        /// <summary>The method argument key, or <see langword="null"/> for a queued type.</summary>
        public IList<TypeSignature>? Arguments { get; } = arguments;
    }
}
