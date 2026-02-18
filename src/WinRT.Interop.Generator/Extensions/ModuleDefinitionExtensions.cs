// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
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
        // Check all direct assembly references and check if they match
        foreach (AssemblyReference reference in module.AssemblyReferences)
        {
            if (reference.Name == assemblyName)
            {
                return true;
            }

            // Also traverse the entire transitive dependency graph and check those assemblies
            foreach (ModuleDefinition transitiveModule in reference.Resolve()?.Modules ?? [])
            {
                if (transitiveModule.ReferencesAssembly(assemblyName))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Enumerates all (transitive) assembly references for a given <see cref="ModuleDefinition"/>.
    /// </summary>
    /// <param name="module">The input <see cref="ModuleDefinition"/> instance.</param>
    /// <returns>All (transitive) assembly references for <paramref name="module"/>.</returns>
    public static IEnumerable<AssemblyReference> EnumerateAssemblyReferences(this ModuleDefinition module)
    {
        foreach (AssemblyReference reference in module.AssemblyReferences)
        {
            yield return reference;

            // Also enumerate all transitive references as well
            foreach (ModuleDefinition transitiveModule in reference.Resolve()?.Modules ?? [])
            {
                foreach (AssemblyReference transitiveReference in transitiveModule.EnumerateAssemblyReferences())
                {
                    yield return transitiveReference;
                }
            }
        }
    }

    /// <summary>
    /// Enumerates all generic instance type signatures in the module.
    /// </summary>
    /// <param name="module">The input <see cref="ModuleDefinition"/> instance.</param>
    /// <returns>All (unique) generic type signatures in the module.</returns>
    public static IEnumerable<GenericInstanceTypeSignature> EnumerateGenericInstanceTypeSignatures(this ModuleDefinition module)
    {
        return EnumerateTypeSignatures(module, AllGenericTypesVisitor.Instance);
    }

    /// <summary>
    /// Enumerates all SZ array type signatures in the module.
    /// </summary>
    /// <param name="module">The input <see cref="ModuleDefinition"/> instance.</param>
    /// <returns>All (unique) generic type signatures in the module.</returns>
    public static IEnumerable<SzArrayTypeSignature> EnumerateSzArrayTypeSignatures(this ModuleDefinition module)
    {
        return EnumerateTypeSignatures(module, AllSzArrayTypesVisitor.Instance);
    }

    /// <summary>
    /// Enumerates all target type signatures in the module.
    /// </summary>
    /// <param name="module">The input <see cref="ModuleDefinition"/> instance.</param>
    /// <param name="visitor">The <see cref="ITypeSignatureVisitor{TResult}"/> instance to use to discover type signatures of interest.</param>
    /// <returns>All (unique) type signatures of interest in the module.</returns>
    public static IEnumerable<TResult> EnumerateTypeSignatures<TResult>(this ModuleDefinition module, ITypeSignatureVisitor<IEnumerable<TResult>> visitor)
        where TResult : TypeSignature
    {
        HashSet<TResult> results = new(SignatureComparer.IgnoreVersion);

        // Helper to crawl a signature, recursively
        static IEnumerable<TResult> EnumerateTypeSignatures(
            TypeSignature? type,
            HashSet<TResult> results,
            ITypeSignatureVisitor<IEnumerable<TResult>> visitor)
        {
            foreach (TResult result in type?.AcceptVisitor(visitor) ?? [])
            {
                if (results.Add(result))
                {
                    yield return result;
                }
            }
        }

        // Enumerate the fields table. This is needed because field definitions can have type signatures inline,
        // without them appearing in the type specification table. This ensures that we're not missing those.
        foreach (FieldDefinition field in module.EnumerateTableMembers<FieldDefinition>(TableIndex.Field))
        {
            foreach (TResult result in EnumerateTypeSignatures(field.Signature?.FieldType, results, visitor))
            {
                yield return result;
            }
        }

        // Enumerate the method table, to ensure we can detect signatures for return types and parameter types.
        // In each method, we also walk the method body to find local variable types, 'newobj' and 'newarr' types.
        // Note that methods in this table might require type arguments, which we don't have from here. However,
        // rather than just ignoring them here, we rely on types not fully constructed to be filtered out later.
        // This is still useful even in those cases, as we might see partially constructed signatures where
        // one or more type arguments is statically known, and which might be a type relevant for marshalling.
        foreach (MethodDefinition method in module.EnumerateTableMembers<MethodDefinition>(TableIndex.Method))
        {
            foreach (TypeSignature visibleType in method.EnumerateAllVisibleTypes())
            {
                foreach (TResult result in EnumerateTypeSignatures(visibleType, results, visitor))
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
            foreach (TResult result in EnumerateTypeSignatures(specification.Signature, results, visitor))
            {
                yield return result;
            }

            // Resolve the type definition to be able to process its methods
            if (specification.Resolve() is not TypeDefinition type)
            {
                continue;
            }

            GenericContext genericContext = new(specification.Signature as GenericInstanceTypeSignature, null);

            // Also manually enumerate all methods from the types we have the specification for. The reason for doing this
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
            foreach (MethodDefinition method in type.Methods)
            {
                foreach (TypeSignature visibleType in method.EnumerateAllVisibleTypes())
                {
                    foreach (TResult result in EnumerateTypeSignatures(visibleType.InstantiateGenericTypes(genericContext), results, visitor))
                    {
                        yield return result;
                    }
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
            GenericContext genericContext = new(specification.DeclaringType?.ToTypeSignature() as GenericInstanceTypeSignature, specification.Signature);

            foreach (TypeSignature visibleType in specification.Method!.EnumerateAllVisibleTypes())
            {
                foreach (TResult result in EnumerateTypeSignatures(visibleType.InstantiateGenericTypes(genericContext), results, visitor))
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