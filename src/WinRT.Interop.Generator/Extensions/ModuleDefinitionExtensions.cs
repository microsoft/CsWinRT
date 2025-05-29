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
    /// Gets the first type with a given name from the specified module.
    /// </summary>
    /// <param name="name">The target module.</param>
    /// <param name="ns">The namespace of the type to get.</param>
    /// <param name="module">The name of the type to get.</param>
    /// <returns>The resulting type.</returns>
    /// <exception cref="ArgumentException">Thrown if the type couldn't be found.</exception>
    public static TypeDefinition GetType(this ModuleDefinition module, ReadOnlySpan<byte> ns, ReadOnlySpan<byte> name)
    {
        if (ns.IsEmpty)
        {
            foreach (TypeDefinition type in module.TopLevelTypes)
            {
                if (type.Namespace is null && type.Name?.AsSpan().SequenceEqual(name) is true)
                {
                    return type;
                }
            }
        }
        else
        {
            foreach (TypeDefinition type in module.TopLevelTypes)
            {
                if (type.Namespace?.AsSpan().SequenceEqual(ns) is true &&
                    type.Name?.AsSpan().SequenceEqual(name) is true)
                {
                    return type;
                }
            }
        }

        throw new ArgumentException($"Type with name '{new Utf8String(ns) + new Utf8String(name)}' not found.", nameof(name));
    }

    /// <summary>
    /// Enumerates all generic instance type signatures in the module.
    /// </summary>
    /// <param name="module">The input <see cref="ModuleDefinition"/> instance.</param>
    /// <returns>All (unique) generic type signatures in the module.</returns>
    public static IEnumerable<GenericInstanceTypeSignature> EnumerateGenericInstanceTypeSignatures(this ModuleDefinition module)
    {
        HashSet<GenericInstanceTypeSignature> genericInstantiations = new(SignatureComparer.IgnoreVersion);

        // Helper to crawl a signature, if generic
        static IEnumerable<GenericInstanceTypeSignature> EnumerateTypeSignatures(TypeSignature? type, HashSet<GenericInstanceTypeSignature> genericInstantiations)
        {
            foreach (GenericInstanceTypeSignature genericType in (type as GenericInstanceTypeSignature)?.AcceptVisitor(AllGenericTypesVisitor.Instance) ?? [])
            {
                if (genericInstantiations.Add(genericType))
                {
                    yield return genericType;
                }
            }
        }

        // Enumerate the type specification table first. This will contain all type signatures for types that
        // are referenced by a metadata token anywhere in the module. This will also include things such as
        // base types (for generic types or not), as well as implemented (generic) interfaces.
        foreach (TypeSpecification type in module.EnumerateTableMembers<TypeSpecification>(TableIndex.TypeSpec))
        {
            foreach (GenericInstanceTypeSignature genericType in EnumerateTypeSignatures(type.Signature, genericInstantiations))
            {
                yield return genericType;
            }
        }

        // Enumerate the fields table. This is needed because field definitions can have type signatures inline,
        // without them appearing in the type specification table. This ensures that we're not missing those.
        foreach (FieldDefinition field in module.EnumerateTableMembers<FieldDefinition>(TableIndex.Field))
        {
            foreach (GenericInstanceTypeSignature genericType in EnumerateTypeSignatures(field.Signature?.FieldType, genericInstantiations))
            {
                yield return genericType;
            }
        }

        // Enumerate the method table, to ensure we can detect signatures for return types and parameter types
        foreach (MethodDefinition method in module.EnumerateTableMembers<MethodDefinition>(TableIndex.Method))
        {
            // Gather return type signatures
            foreach (GenericInstanceTypeSignature genericType in EnumerateTypeSignatures(method.Signature?.ReturnType, genericInstantiations))
            {
                yield return genericType;
            }

            // Walk all parameters as well
            foreach (TypeSignature parameter in method.Signature?.ParameterTypes ?? [])
            {
                foreach (GenericInstanceTypeSignature genericType in EnumerateTypeSignatures(parameter, genericInstantiations))
                {
                    yield return genericType;
                }
            }

            // Also walk all declared locals, just in case
            foreach (CilLocalVariable local in method.CilMethodBody?.LocalVariables ?? [])
            {
                foreach (GenericInstanceTypeSignature genericType in EnumerateTypeSignatures(local.VariableType, genericInstantiations))
                {
                    yield return genericType;
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
            foreach (GenericInstanceTypeSignature genericType in EnumerateTypeSignatures(specification.Method!.Signature!.ReturnType.InstantiateGenericTypes(genericContext), genericInstantiations))
            {
                yield return genericType;
            }

            // Instantiate and gather all parameter types
            foreach (TypeSignature parameterType in specification.Method!.Signature!.ParameterTypes)
            {
                foreach (GenericInstanceTypeSignature genericType in EnumerateTypeSignatures(parameterType.InstantiateGenericTypes(genericContext), genericInstantiations))
                {
                    yield return genericType;
                }
            }

            // And process locals as well
            foreach (CilLocalVariable localVariable in specification.Method!.Resolve()?.CilMethodBody?.LocalVariables ?? [])
            {
                foreach (GenericInstanceTypeSignature genericType in EnumerateTypeSignatures(localVariable.VariableType.InstantiateGenericTypes(genericContext), genericInstantiations))
                {
                    yield return genericType;
                }
            }
        }
    }
}
