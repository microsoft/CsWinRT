// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions for the <see cref="ModuleDefinition"/> type.
/// </summary>
internal static class ModuleDefinitionExtensions
{
    /// <summary>
    /// Enumerates all generic instance type signatures in the module.
    /// </summary>
    /// <param name="module">The input <see cref="ModuleDefinition"/> instance.</param>
    /// <returns>All (unique) generic type signatures in the module.</returns>
    public static IEnumerable<GenericInstanceTypeSignature> EnumerateGenericInstanceTypeSignatures(this ModuleDefinition module)
    {
        HashSet<GenericInstanceTypeSignature> genericInstantiations = new(SignatureComparer.IgnoreVersion);

        // Enumerate the type specification table first. This will contain all type signatures for types that
        // are referenced by a metadata token anywhere in the module. This will also include things such as
        // base types (for generic types or not), as well as implemented (generic) interfaces.
        foreach (TypeSpecification type in module.EnumerateTableMembers<TypeSpecification>(TableIndex.TypeSpec))
        {
            if (type.Signature is GenericInstanceTypeSignature typeSignature && genericInstantiations.Add(typeSignature))
            {
                yield return typeSignature;
            }
        }

        // Enumerate the fields table. This is needed because field definitions can have type signatures inline,
        // without them appearing in the type specification table. This ensures that we're not missing those.
        foreach (FieldDefinition field in module.EnumerateTableMembers<FieldDefinition>(TableIndex.Field))
        {
            if (field.Signature?.FieldType is GenericInstanceTypeSignature typeSignature && genericInstantiations.Add(typeSignature))
            {
                yield return typeSignature;
            }
        }

        // Enumerate the method table, to ensure we can detect signatures for return types and parameter types
        foreach (MethodDefinition method in module.EnumerateTableMembers<MethodDefinition>(TableIndex.Method))
        {
            // Gather return type signatures
            if (method.Signature?.ReturnType is GenericInstanceTypeSignature returnSignature && genericInstantiations.Add(returnSignature))
            {
                yield return returnSignature;
            }

            // Walk all parameters as well
            foreach (TypeSignature parameter in method.Signature?.ParameterTypes ?? [])
            {
                if (parameter is GenericInstanceTypeSignature parameterSignature && genericInstantiations.Add(parameterSignature))
                {
                    yield return parameterSignature;
                }
            }

            // Also walk all declared locals, just in case
            foreach (CilLocalVariable local in method.CilMethodBody?.LocalVariables ?? [])
            {
                if (local.VariableType is GenericInstanceTypeSignature localSignature && genericInstantiations.Add(localSignature))
                {
                    yield return localSignature;
                }
            }
        }
    }
}
