// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.Generator;
using WindowsRuntime.InteropGenerator.Errors;
using WindowsRuntime.InteropGenerator.Helpers;
using WindowsRuntime.InteropGenerator.Visitors;

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions for the <see cref="ModuleDefinition"/> type.
/// </summary>
internal static partial class ModuleDefinitionExtensions
{
    /// <summary>
    /// Gets the first type with a given namespace and name from the specified type.
    /// </summary>
    /// <param name="module">The input <see cref="ModuleDefinition"/> instance.</param>
    /// <param name="ns">The namespace of the type.</param>
    /// <param name="name">The name of the type to get.</param>
    /// <returns>The resulting type.</returns>
    /// <exception cref="ArgumentException">Thrown if the type couldn't be found.</exception>
    public static TypeDefinition GetType(this ModuleDefinition module, Utf8String ns, Utf8String name)
    {
        return TryGetType(module, ns, name, out TypeDefinition? type)
            ? type
            : throw new ArgumentException($"Type with name '{ns}.{name}' not found.");
    }

    /// <summary>
    /// Tries to get the first type with a given namespace and name from the specified type.
    /// </summary>
    /// <param name="module">The input <see cref="ModuleDefinition"/> instance.</param>
    /// <param name="ns">The namespace of the type.</param>
    /// <param name="name">The name of the type to get.</param>
    /// <param name="type">The resulting type, if found.</param>
    /// <returns>Whether <paramref name="type"/> was found.</returns>
    public static bool TryGetType(this ModuleDefinition module, Utf8String? ns, Utf8String? name, [NotNullWhen(true)] out TypeDefinition? type)
    {
        foreach (TypeDefinition item in module.TopLevelTypes)
        {
            if (item.Namespace == ns && item.Name == name)
            {
                type = item;

                return true;
            }
        }

        type = null;

        return false;
    }

    /// <summary>
    /// Checks whether a <see cref="ModuleDefinition"/> references the Windows Runtime assembly.
    /// </summary>
    /// <param name="module">The input <see cref="ModuleDefinition"/> instance.</param>
    /// <param name="assemblyName">The name of the assembly to check for references to.</param>
    /// <returns>Whether the module references the Windows Runtime assembly.</returns>
    public static bool ReferencesAssembly(this ModuleDefinition module, Utf8String assemblyName)
    {
        // Use a visited set to guard against cycles in the transitive assembly reference graph.
        // Such cycles are possible (eg. mutual references between assemblies) and would otherwise
        // cause this method to recurse infinitely, leading to a stack overflow.
        static bool ReferencesAssemblyCore(ModuleDefinition module, Utf8String assemblyName, HashSet<ModuleDefinition> visitedModules)
        {
            // Skip modules we've already visited, to break reference cycles
            if (!visitedModules.Add(module))
            {
                return false;
            }

            // Check all direct assembly references and check if they match
            foreach (AssemblyReference reference in module.AssemblyReferences)
            {
                if (reference.Name == assemblyName)
                {
                    return true;
                }

                // Try to resolve the current assembly, skip it if it fails
                if (!reference.TryResolve(module.RuntimeContext, out AssemblyDefinition? assembly))
                {
                    continue;
                }

                // Also traverse the entire transitive dependency graph and check those assemblies
                foreach (ModuleDefinition transitiveModule in assembly.Modules ?? [])
                {
                    if (ReferencesAssemblyCore(transitiveModule, assemblyName, visitedModules))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        return ReferencesAssemblyCore(module, assemblyName, new HashSet<ModuleDefinition>(SignatureComparer.IgnoreVersion));
    }

    /// <summary>
    /// Enumerates all (transitive) assembly references for a given <see cref="ModuleDefinition"/>.
    /// </summary>
    /// <param name="module">The input <see cref="ModuleDefinition"/> instance.</param>
    /// <returns>All (transitive) assembly references for <paramref name="module"/>.</returns>
    public static IEnumerable<AssemblyReference> EnumerateAssemblyReferences(this ModuleDefinition module)
    {
        // Use a visited set to guard against cycles in the transitive assembly reference graph,
        // which would otherwise cause infinite recursion and a stack overflow (see 'ReferencesAssembly').
        static IEnumerable<AssemblyReference> EnumerateAssemblyReferencesCore(ModuleDefinition module, HashSet<ModuleDefinition> visitedModules)
        {
            // Skip modules we've already visited, to break reference cycles
            if (!visitedModules.Add(module))
            {
                yield break;
            }

            foreach (AssemblyReference reference in module.AssemblyReferences)
            {
                yield return reference;

                // Try to resolve the current assembly, skip it if it fails
                if (!reference.TryResolve(module.RuntimeContext, out AssemblyDefinition? assembly))
                {
                    continue;
                }

                // Also enumerate all transitive references as well
                foreach (ModuleDefinition transitiveModule in assembly.Modules ?? [])
                {
                    foreach (AssemblyReference transitiveReference in EnumerateAssemblyReferencesCore(transitiveModule, visitedModules))
                    {
                        yield return transitiveReference;
                    }
                }
            }
        }

        return EnumerateAssemblyReferencesCore(module, new HashSet<ModuleDefinition>(SignatureComparer.IgnoreVersion));
    }

    /// <summary>
    /// Enumerates all generic instance type signatures in the module.
    /// </summary>
    /// <param name="module">The input <see cref="ModuleDefinition"/> instance.</param>
    /// <param name="signatureComparer">The comparer for discovered signatures.</param>
    /// <param name="shouldProcessModule">Determines whether to transitively discover members in a referenced module.</param>
    /// <param name="isMarshallingDisabledModule">Determines whether a module was explicitly excluded from member discovery.</param>
    /// <param name="treatWarningsAsErrors">Whether to promote discovery warnings to errors.</param>
    /// <param name="token">The cancellation token for discovery.</param>
    /// <returns>All (unique) generic type signatures in the module.</returns>
    public static IEnumerable<GenericInstanceTypeSignature> EnumerateGenericInstanceTypeSignatures(
        this ModuleDefinition module,
        SignatureComparer signatureComparer,
        Func<ModuleDefinition, bool> shouldProcessModule,
        Func<ModuleDefinition, bool> isMarshallingDisabledModule,
        bool treatWarningsAsErrors,
        CancellationToken token)
    {
        return EnumerateTypeSignatures(
            module: module,
            visitor: AllGenericTypesVisitor.Instance,
            signatureComparer: signatureComparer,
            shouldProcessModule: shouldProcessModule,
            isMarshallingDisabledModule: isMarshallingDisabledModule,
            treatWarningsAsErrors: treatWarningsAsErrors,
            token: token);
    }

    /// <summary>
    /// Enumerates all SZ array type signatures in the module.
    /// </summary>
    /// <param name="module">The input <see cref="ModuleDefinition"/> instance.</param>
    /// <param name="signatureComparer">The comparer for discovered signatures.</param>
    /// <param name="shouldProcessModule">Determines whether to transitively discover members in a referenced module.</param>
    /// <param name="isMarshallingDisabledModule">Determines whether a module was explicitly excluded from member discovery.</param>
    /// <param name="treatWarningsAsErrors">Whether to promote discovery warnings to errors.</param>
    /// <param name="token">The cancellation token for discovery.</param>
    /// <returns>All (unique) generic type signatures in the module.</returns>
    public static IEnumerable<SzArrayTypeSignature> EnumerateSzArrayTypeSignatures(
        this ModuleDefinition module,
        SignatureComparer signatureComparer,
        Func<ModuleDefinition, bool> shouldProcessModule,
        Func<ModuleDefinition, bool> isMarshallingDisabledModule,
        bool treatWarningsAsErrors,
        CancellationToken token)
    {
        return EnumerateTypeSignatures(
            module: module,
            visitor: AllSzArrayTypesVisitor.Instance,
            signatureComparer: signatureComparer,
            shouldProcessModule: shouldProcessModule,
            isMarshallingDisabledModule: isMarshallingDisabledModule,
            treatWarningsAsErrors: treatWarningsAsErrors,
            token: token);
    }

    /// <summary>
    /// Enumerates all target type signatures in the module.
    /// </summary>
    /// <param name="module">The input <see cref="ModuleDefinition"/> instance.</param>
    /// <param name="visitor">The <see cref="ITypeSignatureVisitor{TResult}"/> instance to use to discover type signatures of interest.</param>
    /// <param name="signatureComparer">The comparer for discovered signatures.</param>
    /// <param name="shouldProcessModule">Determines whether to transitively discover members in a referenced module.</param>
    /// <param name="isMarshallingDisabledModule">Determines whether a module was explicitly excluded from member discovery.</param>
    /// <param name="treatWarningsAsErrors">Whether to promote discovery warnings to errors.</param>
    /// <param name="token">The cancellation token for discovery.</param>
    /// <returns>All (unique) type signatures of interest in the module.</returns>
    private static IEnumerable<TResult> EnumerateTypeSignatures<TResult>(
        this ModuleDefinition module,
        ITypeSignatureVisitor<IEnumerable<TResult>> visitor,
        SignatureComparer signatureComparer,
        Func<ModuleDefinition, bool> shouldProcessModule,
        Func<ModuleDefinition, bool> isMarshallingDisabledModule,
        bool treatWarningsAsErrors,
        CancellationToken token)
        where TResult : TypeSignature
    {
        const int MaxDiscoveryDepth = 32;
        const int MaxSignatureComplexity = 256;
        const int MaxTransitiveTypes = 1024;

        HashSet<TResult> results = new(signatureComparer);
        HashSet<TypeSignature> typeSpecifications = new(signatureComparer);
        HashSet<TypeSignature> visitedTypes = new(signatureComparer);
        Queue<(TypeSignature? Type, MethodDefinition? Method, GenericContext Context, int Depth)> pendingMembers = new();
        Dictionary<MethodDefinition, HashSet<IList<TypeSignature>>> visitedMethods = [];
        bool recursionLimitReported = false;
        bool complexityLimitReported = false;
        bool transitiveTypeLimitReported = false;
        int transitiveTypeCount = 0;

        // Share the expansion budget between type members and instantiated method bodies.
        bool TryReserveTransitiveInstantiation(int depth)
        {
            if (depth is 0 or > MaxDiscoveryDepth)
            {
                return true;
            }

            if (transitiveTypeCount == MaxTransitiveTypes)
            {
                if (!transitiveTypeLimitReported)
                {
                    transitiveTypeLimitReported = true;

                    WellKnownInteropExceptions.GenericTypeDiscoveryTransitiveTypeLimitExceededWarning(module, MaxTransitiveTypes).LogOrThrow(treatWarningsAsErrors);
                }

                return false;
            }

            transitiveTypeCount++;

            return true;
        }

        bool IsSignatureWithinComplexityLimit(TypeSignature? type, int depth)
            => IsComplexityWithinLimit(type?.GetSignatureElementCount(MaxSignatureComplexity + 1) ?? 0, depth);

        bool IsComplexityWithinLimit(int complexity, int depth)
        {
            if (depth == 0 || complexity <= MaxSignatureComplexity)
            {
                return true;
            }

            if (!complexityLimitReported)
            {
                complexityLimitReported = true;

                WellKnownInteropExceptions.GenericTypeDiscoveryComplexityLimitExceededWarning(module, MaxSignatureComplexity).LogOrThrow(treatWarningsAsErrors);
            }

            return false;
        }

        // Keep budget accounting aligned with the members actually traversed
        IEnumerable<MethodDefinition> GetMethodsToScan(TypeSignature typeSignature, TypeDefinition type)
        {
            // Explicit type specifications provide the caller's generic context for a one-hop member scan.
            // Further traversal of ordinary methods follows the module's marshalling policy.
            if (typeSpecifications.Contains(typeSignature) ||
                (type.DeclaringModule is ModuleDefinition declaringModule && shouldProcessModule(declaringModule)))
            {
                return type.Methods;
            }

            // Mode-based skips still scan static initializers, but explicit exclusions do not
            if (type.DeclaringModule is ModuleDefinition excludedModule && isMarshallingDisabledModule(excludedModule))
            {
                return [];
            }

            // Static initializers can reveal concrete cached instances and arrays hidden behind
            // fields declared as 'object' or an interface.
            return type.TryGetStaticConstructor(out MethodDefinition? initializer) ? [initializer] : [];
        }

        // Helper to crawl a signature, recursively
        IEnumerable<TResult> EnumerateTypeSignatures(TypeSignature? type, int depth = 0)
        {
            token.ThrowIfCancellationRequested();

            // 'Node<Pair<T, T>>' grows exponentially before reaching the depth limit.
            // Bound the work before visiting, hashing, or formatting a newly expanded signature.
            if (!IsSignatureWithinComplexityLimit(type, depth))
            {
                yield break;
            }

            // Member discovery needs closed generic contexts even when we are only collecting array signatures
            foreach (GenericInstanceTypeSignature genericType in type?.AcceptVisitor(AllGenericTypesVisitor.Instance) ?? [])
            {
                if (genericType.AcceptVisitor(IsConstructedGenericTypeVisitor.Instance) &&
                    !visitedTypes.Contains(genericType))
                {
                    // Types with no eligible members need no worklist entry but are still reported below
                    if (depth > 0 &&
                        (!genericType.TryResolve(module.RuntimeContext, out TypeDefinition? resolvedType) ||
                         !GetMethodsToScan(genericType, resolvedType).Any()))
                    {
                        _ = visitedTypes.Add(genericType);

                        continue;
                    }

                    // Bound the worklist when branching methods create many distinct types below the depth limit
                    if (!TryReserveTransitiveInstantiation(depth))
                    {
                        continue;
                    }

                    _ = visitedTypes.Add(genericType);

                    // An expanding cycle, such as 'Node<T>' -> 'Node<Node<T>>', never repeats an exact signature.
                    // Keep the discovered type, but bound further member traversal to avoid unbounded expansion.
                    if (depth <= MaxDiscoveryDepth)
                    {
                        pendingMembers.Enqueue((genericType, null, default, depth));
                    }
                    else if (!recursionLimitReported)
                    {
                        recursionLimitReported = true;

                        WellKnownInteropExceptions.GenericTypeDiscoveryRecursionLimitExceededWarning(genericType, module, MaxDiscoveryDepth).LogOrThrow(treatWarningsAsErrors);
                    }
                }
            }

            foreach (TResult result in type?.AcceptVisitor(visitor) ?? [])
            {
                if (results.Add(result))
                {
                    yield return result;
                }
            }
        }

        // A method operand's parameters belong to the callee. Substitute the caller into its declaring
        // type and method arguments first, then use that context for its signature and body.
        IEnumerable<TResult> EnumerateMethodTypes(IMethodDescriptor descriptor, GenericContext callerContext, int depth)
        {
            MethodSpecification? specification = descriptor as MethodSpecification;
            IMethodDefOrRef? method = specification is not null ? specification.Method : descriptor as IMethodDefOrRef;

            if (method is null)
            {
                yield break;
            }

            TypeSignature? declaringType = (method.DeclaringType as TypeSpecification)?.Signature?.InstantiateGenericTypes(callerContext);
            GenericInstanceMethodSignature? methodArguments = specification?.Signature is { } signature
                ? new GenericInstanceMethodSignature(signature.Attributes,
                    [.. signature.TypeArguments.Select(argument => argument.InstantiateGenericTypes(callerContext))])
                : null;

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

            // Referenced signatures remain visible even when module policy prevents following the body.
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

            // Ordinary methods on constructed declaring types are already covered by the type worklist.
            // Only explicit MethodSpec roots retain the existing one-hop body scan of skipped modules.
            if (methodArguments is null ||
                !method.TryResolve(module.RuntimeContext, out MethodDefinition? definition) ||
                definition.CilMethodBody is null ||
                (depth > 0 && (definition.DeclaringModule is not ModuleDefinition declaringModule || !shouldProcessModule(declaringModule))))
            {
                yield break;
            }

            IList<TypeSignature> arguments = [
                .. (declaringType as GenericInstanceTypeSignature)?.TypeArguments ?? [],
                .. methodArguments.TypeArguments
            ];

            if (depth > 0 && arguments.All(static argument => argument is GenericParameterSignature))
            {
                yield break;
            }

            if (!visitedMethods.TryGetValue(definition, out HashSet<IList<TypeSignature>>? contexts))
            {
                contexts = new HashSet<IList<TypeSignature>>(signatureComparer);
                visitedMethods.Add(definition, contexts);
            }

            if (!contexts.Add(arguments) || !TryReserveTransitiveInstantiation(depth))
            {
                yield break;
            }

            if (depth <= MaxDiscoveryDepth)
            {
                pendingMembers.Enqueue((null, definition, genericContext, depth));
            }
            else if (!recursionLimitReported)
            {
                recursionLimitReported = true;

                WellKnownInteropExceptions.GenericMethodDiscoveryRecursionLimitExceededWarning(definition, module, MaxDiscoveryDepth).LogOrThrow(treatWarningsAsErrors);
            }
        }

        IEnumerable<TResult> EnumerateMethodBodyTypes(MethodDefinition method, GenericContext genericContext, int typeDepth, int methodDepth)
        {
            foreach (TypeSignature visibleType in method.EnumerateAllVisibleTypes(module.RuntimeContext))
            {
                foreach (TResult result in EnumerateTypeSignatures(visibleType.InstantiateGenericTypes(genericContext), typeDepth))
                {
                    yield return result;
                }
            }

            foreach (IMethodDescriptor descriptor in method.EnumerateMethodOperands())
            {
                // Non-generic calls cannot provide new substitutions. Do not crawl their dependency bodies.
                if (descriptor is MethodSpecification || descriptor.DeclaringType is TypeSpecification)
                {
                    foreach (TResult result in EnumerateMethodTypes(descriptor, genericContext, methodDepth))
                    {
                        yield return result;
                    }
                }
            }
        }

        // Enumerate the fields table. This is needed because field definitions can have type signatures inline,
        // without them appearing in the type specification table. This ensures that we're not missing those.
        foreach (FieldDefinition field in module.EnumerateTableMembers<FieldDefinition>(TableIndex.Field))
        {
            foreach (TResult result in EnumerateTypeSignatures(field.Signature?.FieldType))
            {
                yield return result;
            }
        }

        // Enumerate the method table, to ensure we can detect signatures for return types and parameter types.
        // In each method, we also walk the body to find locals, allocations, and types used in field accesses.
        // Note that methods in this table might require type arguments, which we don't have from here. However,
        // rather than just ignoring them here, we rely on types not fully constructed to be filtered out later.
        // This is still useful even in those cases, as we might see partially constructed signatures where
        // one or more type arguments is statically known, and which might be a type relevant for marshalling.
        foreach (MethodDefinition method in module.EnumerateTableMembers<MethodDefinition>(TableIndex.Method))
        {
            foreach (TypeSignature visibleType in method.EnumerateAllVisibleTypes(module.RuntimeContext))
            {
                foreach (TResult result in EnumerateTypeSignatures(visibleType))
                {
                    yield return result;
                }
            }
        }

        // Enumerate the type specification table. This will contain all type signatures for types that are
        // referenced by a metadata token anywhere in the module. This will also include things such as base
        // types (for generic types or not), as well as implemented (generic) interfaces.
        foreach (TypeSpecification specification in module.EnumerateTableMembers<TypeSpecification>(TableIndex.TypeSpec))
        {
            foreach (TResult result in EnumerateTypeSignatures(specification.Signature))
            {
                yield return result;
            }

            // Keep scanning partially open specifications too, as their members can contain closed types
            if (specification.Signature is TypeSignature signature)
            {
                _ = typeSpecifications.Add(signature);

                if (visitedTypes.Add(signature))
                {
                    pendingMembers.Enqueue((signature, null, default, 0));
                }
            }
        }

        // Enumerate method specifications as well. These are used to detect generic instantiations of methods being invoked
        // or passed around in some way (eg. as delegates). Crucially, this allows us to catch constructed delegates that
        // don't appear anywhere else, as they're just a result of specific instantiations of a generic method. For instance:
        //
        // static List<T> M<T>() => [];
        // static object N() => M<int>();
        //
        // This will correctly detect that constructed 'List<int>' on the constructed return for the 'M<int>()' invocation.
        foreach (MethodSpecification specification in module.EnumerateTableMembers<MethodSpecification>(TableIndex.MethodSpec))
        {
            foreach (TResult result in EnumerateMethodTypes(specification, default, 0))
            {
                yield return result;
            }
        }

        // Also manually enumerate all methods from the types we have a generic context for. The reason for doing this
        // is that it might allow us to see more constructed types than we can from just the methods table, and the
        // method specification table. For instance:
        //
        // class C<T>
        // {
        //     List<T> M() => [];
        // }
        //
        // If we have a 'C<int>' type specification, we can resolve 'C<T>', enumerate its methods, which will give us
        // the definition for 'M()', and then we'll be able to instantiate its return type with the generic context
        // from the type specification, so we'll be able to construct 'List<int>'. If we only saw 'C<T>.M()' from the
        // methods table, we wouldn't have the necessary generic context. And because 'M()' is not itself generic,
        // it also wouldn't appear in the method specification table. So this is the only way to cover these cases.
        //
        // Closed types discovered after substituting a generic factory's arguments need the same traversal, including
        // ordinary methods and cache initializers, to discover their nested property/indexer descriptors transitively.
        while (pendingMembers.TryDequeue(out (TypeSignature? Type, MethodDefinition? Method, GenericContext Context, int Depth) current))
        {
            token.ThrowIfCancellationRequested();

            if (current.Method is MethodDefinition currentMethod)
            {
                foreach (TResult result in EnumerateMethodBodyTypes(currentMethod, current.Context, current.Depth, current.Depth + 1))
                {
                    yield return result;
                }

                continue;
            }

            TypeSignature typeSignature = current.Type!;

            if (!typeSignature.TryResolve(module.RuntimeContext, out TypeDefinition? type))
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
    /// Sorts the <see cref="ModuleDefinition"/> values of a sequence in ascending order, based on their fully qualified names.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{T}"/> whose elements are sorted.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="modules"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// This method is implemented by using deferred execution. The immediate return value is an object that stores all the
    /// information that is required to perform the action. The query represented by this method is not executed until the
    /// object is enumerated by calling its <see cref="IEnumerable{T}.GetEnumerator"/> method.
    /// </remarks>
    public static IEnumerable<ModuleDefinition> OrderByFullyQualifiedName(this IEnumerable<ModuleDefinition> modules)
    {
        return modules.Order(ModuleDefinitionComparer.Instance);
    }
}
