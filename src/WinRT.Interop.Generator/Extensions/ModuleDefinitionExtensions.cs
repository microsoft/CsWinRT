// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
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
    /// Checks whether a <see cref="ModuleDefinition"/> references the Windows Runtime assembly.
    /// </summary>
    /// <param name="module">The input <see cref="ModuleDefinition"/> instance.</param>
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

            // And process locals as well
            foreach (CilLocalVariable localVariable in specification.Method!.Resolve()?.CilMethodBody?.LocalVariables ?? [])
            {
                foreach (TResult result in EnumerateTypeSignatures(
                    localVariable.VariableType.InstantiateGenericTypes(genericContext),
                    results,
                    visitor))
                {
                    yield return result;
                }
            }
        }
    }
}