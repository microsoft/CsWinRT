// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.Errors;
using WindowsRuntime.InteropGenerator.Visitors;

namespace WindowsRuntime.InteropGenerator.Discovery;

/// <summary>
/// Discovers type signatures from a module's metadata roots and instantiated members.
/// </summary>
/// <typeparam name="TResult">The type signatures selected by the result visitor.</typeparam>
/// <remarks>
/// Each instance belongs to a single enumeration. Generic and SZ array discovery share this traversal,
/// including its generic contexts, member eligibility rules, and expansion limits.
/// </remarks>
internal sealed class TypeSignatureDiscovery<TResult>
    where TResult : TypeSignature
{
    /// <summary>The maximum depth at which discovered members can be queued.</summary>
    private const int MaxDiscoveryDepth = 32;

    /// <summary>The maximum signature element count for a transitive signature or method context.</summary>
    private const int MaxSignatureComplexity = 256;

    /// <summary>The shared budget for transitive type and method instantiations.</summary>
    private const int MaxTransitiveTypes = 1024;

    /// <summary>The root module, also used for resolution and diagnostics.</summary>
    private readonly ModuleDefinition _module;

    /// <summary>The visitor selecting the signatures to report.</summary>
    private readonly ITypeSignatureVisitor<IEnumerable<TResult>> _visitor;

    /// <summary>The resolution-aware comparer supplied for this invocation.</summary>
    private readonly SignatureComparer _signatureComparer;

    /// <summary>Determines whether a referenced module's members can be expanded transitively.</summary>
    private readonly Func<ModuleDefinition, bool> _shouldProcessModule;

    /// <summary>Identifies explicitly excluded modules, whose transitive static initializers are also skipped.</summary>
    private readonly Func<ModuleDefinition, bool> _isMarshallingDisabledModule;

    /// <summary>Whether discovery warnings are promoted to errors.</summary>
    private readonly bool _treatWarningsAsErrors;

    /// <summary>The cancellation token for this enumeration.</summary>
    private readonly CancellationToken _token;

    /// <summary>The signatures already reported to the consumer.</summary>
    private readonly HashSet<TResult> _results;

    /// <summary>Explicit TypeSpec roots, whose members remain eligible for a one-hop scan regardless of module policy.</summary>
    private readonly HashSet<TypeSignature> _typeSpecifications;

    /// <summary>Type contexts already considered for member expansion, distinct from reported results.</summary>
    private readonly HashSet<TypeSignature> _visitedTypes;

    /// <summary>The FIFO worklist of type or method contexts awaiting member discovery.</summary>
    private readonly Queue<PendingMember> _pendingMembers;

    /// <summary>Argument contexts grouped by resolved method definition identity, not by method signature.</summary>
    private readonly Dictionary<MethodDefinition, HashSet<IList<TypeSignature>>> _visitedMethods;

    /// <summary>Whether a recursion warning has been reported for either a type or a method.</summary>
    private bool _recursionLimitReported;

    /// <summary>Whether the signature complexity warning has been reported.</summary>
    private bool _complexityLimitReported;

    /// <summary>Whether the shared transitive instantiation budget warning has been reported.</summary>
    private bool _transitiveTypeLimitReported;

    /// <summary>The number of transitive type and method instantiations reserved so far.</summary>
    private int _transitiveTypeCount;

    /// <summary>
    /// Creates the state for a single enumeration.
    /// </summary>
    /// <param name="module">The root module to analyze.</param>
    /// <param name="visitor">The visitor selecting the signatures to report.</param>
    /// <param name="signatureComparer">The comparer for discovered signatures and argument contexts.</param>
    /// <param name="shouldProcessModule">Determines whether to transitively discover members in a referenced module.</param>
    /// <param name="isMarshallingDisabledModule">Determines whether a module was explicitly excluded from member discovery.</param>
    /// <param name="treatWarningsAsErrors">Whether to promote discovery warnings to errors.</param>
    /// <param name="token">The cancellation token for discovery.</param>
    private TypeSignatureDiscovery(
        ModuleDefinition module,
        ITypeSignatureVisitor<IEnumerable<TResult>> visitor,
        SignatureComparer signatureComparer,
        Func<ModuleDefinition, bool> shouldProcessModule,
        Func<ModuleDefinition, bool> isMarshallingDisabledModule,
        bool treatWarningsAsErrors,
        CancellationToken token)
    {
        _module = module;
        _visitor = visitor;
        _signatureComparer = signatureComparer;
        _shouldProcessModule = shouldProcessModule;
        _isMarshallingDisabledModule = isMarshallingDisabledModule;
        _treatWarningsAsErrors = treatWarningsAsErrors;
        _token = token;
        _results = new(signatureComparer);
        _typeSpecifications = new(signatureComparer);
        _visitedTypes = new(signatureComparer);
        _pendingMembers = new();
        _visitedMethods = [];
    }

    /// <summary>
    /// Enumerates all unique type signatures selected by a visitor.
    /// </summary>
    /// <param name="module">The root module to analyze.</param>
    /// <param name="visitor">The visitor selecting the signatures to report.</param>
    /// <param name="signatureComparer">The comparer for discovered signatures and argument contexts.</param>
    /// <param name="shouldProcessModule">Determines whether to transitively discover members in a referenced module.</param>
    /// <param name="isMarshallingDisabledModule">Determines whether a module was explicitly excluded from member discovery.</param>
    /// <param name="treatWarningsAsErrors">Whether to promote discovery warnings to errors.</param>
    /// <param name="token">The cancellation token for discovery.</param>
    /// <returns>The unique type signatures of interest.</returns>
    public static IEnumerable<TResult> Enumerate(
        ModuleDefinition module,
        ITypeSignatureVisitor<IEnumerable<TResult>> visitor,
        SignatureComparer signatureComparer,
        Func<ModuleDefinition, bool> shouldProcessModule,
        Func<ModuleDefinition, bool> isMarshallingDisabledModule,
        bool treatWarningsAsErrors,
        CancellationToken token)
    {
        // Keep construction inside this iterator: repeated or interleaved enumerations must not share state
        TypeSignatureDiscovery<TResult> discovery = new(
            module, visitor, signatureComparer, shouldProcessModule, isMarshallingDisabledModule, treatWarningsAsErrors, token);

        foreach (TResult result in discovery.EnumerateRoots())
        {
            yield return result;
        }

        // Finish gathering explicit roots before applying their one-hop member eligibility to the worklist
        foreach (TResult result in discovery.EnumeratePendingMembers())
        {
            yield return result;
        }
    }

    /// <summary>
    /// Visits metadata roots in field, method, TypeSpec, and MethodSpec order, queuing their member contexts.
    /// </summary>
    /// <returns>The signatures visible from metadata roots.</returns>
    private IEnumerable<TResult> EnumerateRoots()
    {
        // Field definitions can have type signatures inline, without them appearing in the TypeSpec table
        foreach (FieldDefinition field in _module.EnumerateTableMembers<FieldDefinition>(TableIndex.Field))
        {
            foreach (TResult result in EnumerateTypeSignatures(field.Signature?.FieldType))
            {
                yield return result;
            }
        }

        // Method definitions expose return types, parameters, locals, allocations, field accesses, and type operands.
        // Their generic arguments are not available here, but even partially open signatures can contain closed types.
        foreach (MethodDefinition method in _module.EnumerateTableMembers<MethodDefinition>(TableIndex.Method))
        {
            foreach (TypeSignature visibleType in method.EnumerateAllVisibleTypes(_module.RuntimeContext))
            {
                foreach (TResult result in EnumerateTypeSignatures(visibleType))
                {
                    yield return result;
                }
            }
        }

        // TypeSpecs contain signatures referenced by metadata tokens, including base types and implemented interfaces
        foreach (TypeSpecification specification in _module.EnumerateTableMembers<TypeSpecification>(TableIndex.TypeSpec))
        {
            foreach (TResult result in EnumerateTypeSignatures(specification.Signature))
            {
                yield return result;
            }

            // Keep scanning partially open specifications too, as their members can contain closed types
            if (specification.Signature is TypeSignature signature)
            {
                _ = _typeSpecifications.Add(signature);

                if (_visitedTypes.Add(signature))
                {
                    _pendingMembers.Enqueue(new PendingMember(
                        type: signature,
                        method: null,
                        context: default,
                        depth: 0));
                }
            }
        }

        // MethodSpecs supply arguments for generic calls and delegate targets. For example:
        //
        // static List<T> M<T>() => [];
        // static object N() => M<int>();
        //
        // Substitution reveals 'List<int>' even when it appears nowhere else in the caller's metadata.
        foreach (MethodSpecification specification in _module.EnumerateTableMembers<MethodSpecification>(TableIndex.MethodSpec))
        {
            foreach (TResult result in EnumerateMethodTypes(specification, default, 0))
            {
                yield return result;
            }
        }
    }

    /// <summary>
    /// Expands queued member contexts in discovery order.
    /// </summary>
    /// <returns>The signatures visible from instantiated members.</returns>
    private IEnumerable<TResult> EnumeratePendingMembers()
    {
        // A type context can close an ordinary method that has no MethodSpec of its own:
        //
        // class C<T>
        // {
        //     List<T> M() => [];
        // }
        //
        // A 'C<int>' TypeSpec lets us discover 'List<int>' from 'M()', unlike the open method definition.
        // Closed types discovered through generic factories need the same traversal, including ordinary
        // methods and cache initializers, to expose nested property/indexer descriptors transitively.
        while (_pendingMembers.TryDequeue(out PendingMember current))
        {
            _token.ThrowIfCancellationRequested();

            if (current.Method is MethodDefinition currentMethod)
            {
                // A method item's context was already counted at its own depth; only its callees advance it
                foreach (TResult result in EnumerateMethodBodyTypes(currentMethod, current.Context, current.Depth, current.Depth + 1))
                {
                    yield return result;
                }

                continue;
            }

            TypeSignature typeSignature = current.Type!;

            if (!typeSignature.TryResolve(_module.RuntimeContext, out TypeDefinition? type))
            {
                continue;
            }

            GenericContext genericContext = new(typeSignature as GenericInstanceTypeSignature, null);

            foreach (MethodDefinition method in GetMethodsToScan(typeSignature, type))
            {
                foreach (TResult result in EnumerateMethodBodyTypes(method, genericContext, current.Depth + 1, current.Depth + 1))
                {
                    yield return result;
                }
            }
        }
    }

    /// <summary>
    /// Queues generic member contexts and reports the selected signatures within a signature tree.
    /// </summary>
    /// <param name="type">The signature to visit, if available.</param>
    /// <param name="depth">The expansion depth, or zero for an explicit root.</param>
    /// <returns>The signatures not yet reported by this enumeration.</returns>
    private IEnumerable<TResult> EnumerateTypeSignatures(TypeSignature? type, int depth = 0)
    {
        _token.ThrowIfCancellationRequested();

        // 'Node<Pair<T, T>>' grows exponentially before reaching the depth limit.
        // Bound the work before visiting, hashing, or formatting a newly expanded signature.
        if (!IsSignatureWithinComplexityLimit(type, depth))
        {
            yield break;
        }

        EnqueueGenericTypes(type, depth);

        foreach (TResult result in type?.AcceptVisitor(_visitor) ?? [])
        {
            if (_results.Add(result))
            {
                yield return result;
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
        // Array discovery also needs generic contexts to find arrays exposed only by instantiated members
        foreach (GenericInstanceTypeSignature genericType in type?.AcceptVisitor(AllGenericTypesVisitor.Instance) ?? [])
        {
            if (genericType.AcceptVisitor(IsConstructedGenericTypeVisitor.Instance) &&
                !_visitedTypes.Contains(genericType))
            {
                // Types with no eligible members need no worklist entry but are still reported by the result visitor
                if (depth > 0 &&
                    (!genericType.TryResolve(_module.RuntimeContext, out TypeDefinition? resolvedType) ||
                     !GetMethodsToScan(genericType, resolvedType).Any()))
                {
                    _ = _visitedTypes.Add(genericType);

                    continue;
                }

                // Bound the worklist when branching methods create many distinct types below the depth limit
                if (!TryReserveTransitiveInstantiation(depth))
                {
                    continue;
                }

                _ = _visitedTypes.Add(genericType);

                // 'Node<T>' -> 'Node<Node<T>>' never repeats an exact signature. Keep reporting the
                // discovered type beyond the depth limit, but do not expand its members.
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
    }

    /// <summary>
    /// Composes a referenced method's context, reports its visible signatures, and queues eligible bodies.
    /// </summary>
    /// <param name="descriptor">The referenced method or method specification.</param>
    /// <param name="callerContext">The caller's declaring-type and method arguments.</param>
    /// <param name="depth">The expansion depth, or zero for an explicit MethodSpec root.</param>
    /// <returns>The signatures visible after substituting the callee's context.</returns>
    private IEnumerable<TResult> EnumerateMethodTypes(IMethodDescriptor descriptor, GenericContext callerContext, int depth)
    {
        MethodSpecification? specification = descriptor as MethodSpecification;
        IMethodDefOrRef? method = specification is not null ? specification.Method : descriptor as IMethodDefOrRef;

        if (method is null)
        {
            yield break;
        }

        // The operand's parameters belong to the callee. Substitute the caller into its declaring
        // type and method arguments first, then use the composed context for its signature and body.
        TypeSignature? declaringType = (method.DeclaringType as TypeSpecification)?.Signature?.InstantiateGenericTypes(callerContext);
        GenericInstanceMethodSignature? methodArguments = specification?.Signature is { } signature
            ? new GenericInstanceMethodSignature(signature.Attributes,
                [.. signature.TypeArguments.Select(argument => argument.InstantiateGenericTypes(callerContext))])
            : null;

        // Bound the aggregate context before any argument is visited or hashed, not just each argument separately
        int complexity = declaringType?.GetSignatureElementCount(MaxSignatureComplexity + 1) ?? 0;
        foreach (TypeSignature argument in methodArguments?.TypeArguments ?? [])
        {
            complexity += argument.GetSignatureElementCount(MaxSignatureComplexity + 1);

            if (!IsComplexityWithinLimit(complexity, depth))
            {
                yield break;
            }
        }

        if (!IsComplexityWithinLimit(complexity, depth))
        {
            yield break;
        }

        GenericContext genericContext = new(declaringType as GenericInstanceTypeSignature, methodArguments);

        foreach (TypeSignature argument in methodArguments?.TypeArguments ?? [])
        {
            foreach (TResult result in EnumerateTypeSignatures(argument, depth))
            {
                yield return result;
            }
        }

        foreach (TResult result in EnumerateTypeSignatures(declaringType, depth))
        {
            yield return result;
        }

        // Referenced signatures remain visible even when module policy prevents following the body
        if (method.Signature is MethodSignature methodSignature)
        {
            foreach (TypeSignature visibleType in new[] { methodSignature.ReturnType }.Concat(methodSignature.ParameterTypes))
            {
                foreach (TResult result in EnumerateTypeSignatures(visibleType.InstantiateGenericTypes(genericContext), depth))
                {
                    yield return result;
                }
            }
        }

        // Queue the body after reporting its signatures, so their type contexts keep their place in the FIFO
        EnqueueMethodBody(method, declaringType, methodArguments, genericContext, depth);
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
        // Ordinary methods on constructed declaring types are already covered by the type worklist.
        // Only explicit MethodSpec roots retain the one-hop body scan of skipped modules.
        if (methodArguments is null ||
            !method.TryResolve(_module.RuntimeContext, out MethodDefinition? definition) ||
            definition.CilMethodBody is null ||
            (depth > 0 && (definition.DeclaringModule is not ModuleDefinition declaringModule || !_shouldProcessModule(declaringModule))))
        {
            return;
        }

        IList<TypeSignature> arguments = [
            .. (declaringType as GenericInstanceTypeSignature)?.TypeArguments ?? [],
            .. methodArguments.TypeArguments
        ];

        // Partially open contexts can still reveal closed types; skip only wholly unbound argument lists
        if (depth > 0 && arguments.All(static argument => argument is GenericParameterSignature))
        {
            return;
        }

        if (!_visitedMethods.TryGetValue(definition, out HashSet<IList<TypeSignature>>? contexts))
        {
            contexts = new HashSet<IList<TypeSignature>>(_signatureComparer);
            _visitedMethods.Add(definition, contexts);
        }

        if (!contexts.Add(arguments) || !TryReserveTransitiveInstantiation(depth))
        {
            return;
        }

        if (depth <= MaxDiscoveryDepth)
        {
            _pendingMembers.Enqueue(new PendingMember(
                type: null,
                method: definition,
                context: genericContext,
                depth: depth));
        }
        else if (!_recursionLimitReported)
        {
            _recursionLimitReported = true;

            WellKnownInteropExceptions.GenericMethodDiscoveryRecursionLimitExceededWarning(definition, _module, MaxDiscoveryDepth).LogOrThrow(_treatWarningsAsErrors);
        }
    }

    /// <summary>
    /// Visits a method's visible types and generic operands using an instantiated context.
    /// </summary>
    /// <param name="method">The method whose signature and body should be scanned.</param>
    /// <param name="genericContext">The context to substitute into visible types and referenced members.</param>
    /// <param name="typeDepth">The depth assigned to types found directly in the method.</param>
    /// <param name="methodDepth">The depth assigned to referenced method contexts.</param>
    /// <returns>The signatures visible from the instantiated method.</returns>
    private IEnumerable<TResult> EnumerateMethodBodyTypes(MethodDefinition method, GenericContext genericContext, int typeDepth, int methodDepth)
    {
        foreach (TypeSignature visibleType in method.EnumerateAllVisibleTypes(_module.RuntimeContext))
        {
            foreach (TResult result in EnumerateTypeSignatures(visibleType.InstantiateGenericTypes(genericContext), typeDepth))
            {
                yield return result;
            }
        }

        foreach (IMethodDescriptor descriptor in method.EnumerateMethodOperands())
        {
            // Non-generic calls cannot provide new substitutions. Do not crawl their dependency bodies
            if (descriptor is MethodSpecification || descriptor.DeclaringType is TypeSpecification)
            {
                foreach (TResult result in EnumerateMethodTypes(descriptor, genericContext, methodDepth))
                {
                    yield return result;
                }
            }
        }
    }

    /// <summary>
    /// Selects the same eligible members for both budget accounting and queued type expansion.
    /// </summary>
    /// <param name="typeSignature">The type context being expanded.</param>
    /// <param name="type">The resolved type definition.</param>
    /// <returns>The methods eligible for discovery in this context.</returns>
    private IEnumerable<MethodDefinition> GetMethodsToScan(TypeSignature typeSignature, TypeDefinition type)
    {
        // Explicit type specifications provide the caller's generic context for a one-hop member scan.
        // Further traversal of ordinary methods follows the module's marshalling policy.
        if (_typeSpecifications.Contains(typeSignature) ||
            (type.DeclaringModule is ModuleDefinition declaringModule && _shouldProcessModule(declaringModule)))
        {
            return type.Methods;
        }

        // Mode-based skips still scan static initializers, but explicit exclusions do not
        if (type.DeclaringModule is ModuleDefinition excludedModule && _isMarshallingDisabledModule(excludedModule))
        {
            return [];
        }

        // Static initializers can reveal concrete cached instances and arrays hidden behind
        // fields declared as 'object' or an interface.
        return type.TryGetStaticConstructor(out MethodDefinition? initializer) ? [initializer] : [];
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

                WellKnownInteropExceptions.GenericTypeDiscoveryTransitiveTypeLimitExceededWarning(_module, MaxTransitiveTypes).LogOrThrow(_treatWarningsAsErrors);
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
        return IsComplexityWithinLimit(type?.GetSignatureElementCount(MaxSignatureComplexity + 1) ?? 0, depth);
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

            WellKnownInteropExceptions.GenericTypeDiscoveryComplexityLimitExceededWarning(_module, MaxSignatureComplexity).LogOrThrow(_treatWarningsAsErrors);
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
    private readonly struct PendingMember(
        TypeSignature? type,
        MethodDefinition? method,
        GenericContext context,
        int depth)
    {
        /// <summary>The type context to expand, or <see langword="null"/> for a method body.</summary>
        public TypeSignature? Type { get; } = type;

        /// <summary>The method body to scan, or <see langword="null"/> for a type context.</summary>
        public MethodDefinition? Method { get; } = method;

        /// <summary>The substituted method context; type contexts are composed when dequeued.</summary>
        public GenericContext Context { get; } = context;

        /// <summary>The expansion depth, with zero reserved for explicit metadata roots.</summary>
        public int Depth { get; } = depth;
    }
}
