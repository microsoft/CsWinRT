// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.Helpers;
using WindowsRuntime.InteropGenerator.Visitors;

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions for the <see cref="ModuleDefinition"/> type.
/// </summary>
internal static class ModuleDefinitionExtensions
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
        foreach (TypeDefinition type in module.TopLevelTypes)
        {
            if (type.Namespace == ns && type.Name == name)
            {
                return type;
            }
        }

        throw new ArgumentException($"Type with name '{ns}.{name}' not found.");
    }

    /// <summary>
    /// Enumerates all methods (including from nested types) defined in a given module.
    /// </summary>
    /// <param name="module">The input <see cref="ModuleDefinition"/> instance.</param>
    /// <returns>The resulting methods.</returns>
    public static IEnumerable<MethodDefinition> GetAllMethods(this ModuleDefinition module)
    {
        foreach (TypeDefinition type in module.GetAllTypes())
        {
            foreach (MethodDefinition method in type.Methods)
            {
                yield return method;
            }
        }
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

        // Enumerate the type specification table first. This will contain all type signatures for types that
        // are referenced by a metadata token anywhere in the module. This will also include things such as
        // base types (for generic types or not), as well as implemented (generic) interfaces.
        foreach (TypeSpecification type in module.EnumerateTableMembers<TypeSpecification>(TableIndex.TypeSpec))
        {
            foreach (TResult result in EnumerateTypeSignatures(type.Signature, results, visitor))
            {
                yield return result;
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

        // Enumerate the method table, to ensure we can detect signatures for return types and parameter types
        foreach (MethodDefinition method in module.EnumerateTableMembers<MethodDefinition>(TableIndex.Method))
        {
            // Gather return type signatures
            foreach (TResult result in EnumerateTypeSignatures(method.Signature?.ReturnType, results, visitor))
            {
                yield return result;
            }

            // Walk all parameters as well
            foreach (TypeSignature parameter in method.Signature?.ParameterTypes ?? [])
            {
                foreach (TResult result in EnumerateTypeSignatures(parameter, results, visitor))
                {
                    yield return result;
                }
            }

            // Also walk all declared locals, just in case
            foreach (CilLocalVariable local in method.CilMethodBody?.LocalVariables ?? [])
            {
                foreach (TResult result in EnumerateTypeSignatures(local.VariableType, results, visitor))
                {
                    yield return result;
                }
            }
        }

        // Enumerate method specifications as well. These are used to detect generic instantiations of methods being invoked
        // or passed around in some way (eg. as delegates). Crucially, this allows us to catch constructed delegates that
        // don't appear anywhere else, as they're just a result of specific instantiations of a generic method. For instance:
        //
        // public static List<T> M<T>() => [];
        // public static object N() => M<int>();
        //
        // This will correctly detect that constructed 'List<int>' on the constructed return for the 'M<int>()' invocation.
        foreach (MethodSpecification specification in module.EnumerateTableMembers<MethodSpecification>(TableIndex.MethodSpec))
        {
            GenericContext genericContext = new(specification.DeclaringType?.ToTypeSignature() as GenericInstanceTypeSignature, specification.Signature);

            // Instantiate and gather the return type
            foreach (TResult result in EnumerateTypeSignatures(
                specification.Method!.Signature!.ReturnType.InstantiateGenericTypes(genericContext),
                results,
                visitor))
            {
                yield return result;
            }

            // Instantiate and gather all parameter types
            foreach (TypeSignature parameterType in specification.Method!.Signature!.ParameterTypes)
            {
                foreach (TResult result in EnumerateTypeSignatures(
                    parameterType.InstantiateGenericTypes(genericContext),
                    results,
                    visitor))
                {
                    yield return result;
                }
            }

            // Resolve the method definition to be able to process its body
            if (specification.Method!.Resolve() is not MethodDefinition method)
            {
                continue;
            }

            // Process all declared locals as well
            foreach (CilLocalVariable localVariable in method.CilMethodBody?.LocalVariables ?? [])
            {
                foreach (TResult result in EnumerateTypeSignatures(
                    localVariable.VariableType.InstantiateGenericTypes(genericContext),
                    results,
                    visitor))
                {
                    yield return result;
                }
            }

            // Look for all 'newobj' instructions and instantiate the object types
            foreach (ITypeDefOrRef objectType in method.EnumerateNewobjTypes())
            {
                foreach (TResult result in EnumerateTypeSignatures(
                    objectType.ToTypeSignature().InstantiateGenericTypes(genericContext),
                    results,
                    visitor))
                {
                    yield return result;
                }
            }

            // Look for all 'newarr' instructions and instantiate the element types
            foreach (ITypeDefOrRef elementType in method.EnumerateNewarrElementTypes())
            {
                foreach (TResult result in EnumerateTypeSignatures(
                    elementType.MakeSzArrayType().InstantiateGenericTypes(genericContext),
                    results,
                    visitor))
                {
                    yield return result;
                }
            }
        }

        // We also want to process all declared methods, so we can discover objects and array types being
        // instantiated. These might result in new type signastures that we wouldn't otherwise discover.
        foreach (MethodDefinition method in module.GetAllMethods())
        {
            // Ignore all methods that require a generic type parameter, since they would
            // not be instantiated here. We might discover them when 
            if (method.HasGenericParameters || method.DeclaringType!.HasGenericParameters)
            {
                continue;
            }

            // Look for all 'newobj' instructions and gather all object types
            foreach (ITypeDefOrRef objectType in method.EnumerateNewobjTypes())
            {
                foreach (TResult result in EnumerateTypeSignatures(
                    objectType.ToTypeSignature(),
                    results,
                    visitor))
                {
                    yield return result;
                }
            }

            // Look for all 'newarr' instructions and gather all element types
            foreach (ITypeDefOrRef elementType in method.EnumerateNewarrElementTypes())
            {
                foreach (TResult result in EnumerateTypeSignatures(
                    elementType.MakeSzArrayType(),
                    results,
                    visitor))
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