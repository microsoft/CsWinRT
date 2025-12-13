// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions for the <see cref="TypeSignature"/> type.
/// </summary>
internal static class TypeSignatureExtensions
{
    extension(TypeSignature signature)
    {
        /// <summary>
        /// Gets a value indicating whether a given <see cref="TypeSignature"/> instance can be fully resolved to type definitions.
        /// </summary>
        /// <param name="definition">The resulting <see cref="TypeDefinition"/>, if the type can be resolved.</param>
        /// <returns>Whether the type can be fully resolved.</returns>
        public bool IsFullyResolvable([NotNullWhen(true)] out TypeDefinition? definition)
        {
            definition = signature.Resolve();

            // Ensure that we can resolve the type (if we can't, we're likely missing a .dll)
            if (definition is null)
            {
                return false;
            }

            // Recurse on all type arguments as well
            if (signature is GenericInstanceTypeSignature genericInstanceTypeSignature)
            {
                foreach (TypeSignature typeArgument in genericInstanceTypeSignature.TypeArguments)
                {
                    if (!typeArgument.IsFullyResolvable(out _))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Enumerates all interface types implementation by the specified type, including those implemented by base types.
        /// </summary>
        /// <returns>The sequence of interface types implemented by the input type.</returns>
        /// <remarks>
        /// This method might return the same interface types multiple times, if implemented by multiple types in the hierarchy.
        /// </remarks>
        public IEnumerable<TypeSignature> EnumerateAllInterfaces()
        {
            TypeSignature currentSignature = signature;

            while (currentSignature is not null)
            {
                // If we can't resolve the current type signature, we have to stop.
                // Callers should validate the type hierarchy before calling this.
                if (!currentSignature.IsFullyResolvable(out TypeDefinition? currentDefinition))
                {
                    yield break;
                }

                GenericContext context = new(currentSignature as GenericInstanceTypeSignature, null);

                // Go over all interfaces implemented on the current type. We don't need
                // to recurse on them, as classes always declare the full transitive set.
                foreach (InterfaceImplementation interfaceImplementation in currentDefinition.Interfaces)
                {
                    // Ignore this interface if we can't actually retrieve the interface type.
                    // This should never happen for valid .NET assemblies, but just in case.
                    if (interfaceImplementation.Interface?.ToReferenceTypeSignature() is not TypeSignature interfaceSignature)
                    {
                        continue;
                    }

                    // Return either the current non-generic interface, or the constructed generic one.
                    // We don't have to check: if the interface is not generic, this will be a no-op.
                    yield return interfaceSignature.InstantiateGenericTypes(context);

                    // Also recurse on the base interfaces (no need to instantiate the returned interface type
                    // signatures for base interfaces here: they will be already instantiate when returned).
                    foreach (TypeSignature baseInterface in interfaceSignature.EnumerateAllInterfaces())
                    {
                        yield return baseInterface;
                    }
                }

                ITypeDefOrRef? baseType = currentDefinition.BaseType;

                // Stop if we have no available base type or if we reached 'object'
                if (baseType is null or CorLibTypeSignature { ElementType: ElementType.Object })
                {
                    yield break;
                }

                // Get the signature for the base type, adding back any generic context.
                // Note that the base type will always be a reference type, even for
                // struct types (in that case, the base type will be 'System.ValueType').
                currentSignature = baseType.ToReferenceTypeSignature().InstantiateGenericTypes(context);
            }
        }
    }
}