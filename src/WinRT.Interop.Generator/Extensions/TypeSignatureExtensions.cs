// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions for the <see cref="TypeSignature"/> type.
/// </summary>
internal static class TypeSignatureExtensions
{
    extension(TypeSignature signature)
    {
        /// <summary>
        /// Gets whether the current type is a (constructed) generic type.
        /// </summary>
        public bool IsGenericType => signature.ElementType is ElementType.GenericInst;

        /// <summary>
        /// Determines whether the current type is assignable from the provided type.
        /// </summary>
        /// <param name="other">The other type.</param>
        /// <param name="comparer">The comparer to use for comparing type signatures.</param>
        /// <returns>Whether the current type is assignable from <paramref name="other" />.</returns>
        /// <remarks>
        /// Type compatibility is determined according to the rules in ECMA-335 I.8.7.3.
        /// </remarks>
        public bool IsAssignableFrom(TypeSignature other, SignatureComparer comparer)
        {
            return other.IsAssignableTo(signature, comparer);
        }

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
        /// Enumerates all interface types implemented by the specified type, including those implemented by base types.
        /// </summary>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <returns>The sequence of interface types implemented by the input type.</returns>
        /// <remarks>
        /// This method might return the same interface types multiple times, if implemented by multiple types in the hierarchy.
        /// </remarks>
        public IEnumerable<TypeSignature> EnumerateAllInterfaces(InteropReferences interopReferences)
        {
            // Each SZ array also gets a series of interfaces automatically implemented by the runtime.
            // The set is fixed, so we can just hardcode those here to make sure they are also discovered.
            // The normal logic wouldn't work here, because the base type for arrays is the element type.
            if (signature is SzArrayTypeSignature arraySignature)
            {
                yield return interopReferences.IList.ToReferenceTypeSignature();
                yield return interopReferences.ICollection.ToReferenceTypeSignature();
                yield return interopReferences.IEnumerable.ToReferenceTypeSignature();
                yield return interopReferences.IList1.MakeGenericReferenceType(arraySignature.BaseType);
                yield return interopReferences.ICollection1.MakeGenericReferenceType(arraySignature.BaseType);
                yield return interopReferences.IEnumerable1.MakeGenericReferenceType(arraySignature.BaseType);
                yield return interopReferences.IReadOnlyList1.MakeGenericReferenceType(arraySignature.BaseType);

                yield break;
            }

            TypeSignature? currentSignature = signature;

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
                    // signatures for base interfaces here: they will be already instantiated when returned).
                    foreach (TypeSignature baseInterface in interfaceSignature.EnumerateAllInterfaces(interopReferences))
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

        /// <summary>
        /// Enumerates all base types of a given type.
        /// </summary>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <returns>The sequence of base types of the input type.</returns>
        public IEnumerable<TypeSignature> EnumerateBaseTypes(InteropReferences interopReferences)
        {
            // If we see an SZ array, we can directly return 'System.Array' as its only base type.
            // We can't rely on the base type from the signature, as it would be the element type.
            if (signature is SzArrayTypeSignature)
            {
                yield return interopReferences.Array.ToReferenceTypeSignature();

                yield break;
            }

            TypeSignature? currentSignature = signature;

            while (currentSignature is not null)
            {
                // Same validation as above, callers should ensure the type can be resolved
                if (!currentSignature.IsFullyResolvable(out TypeDefinition? currentDefinition))
                {
                    yield break;
                }

                GenericContext context = new(currentSignature as GenericInstanceTypeSignature, null);

                ITypeDefOrRef? baseType = currentDefinition.BaseType;

                // Stop if we have no available base type
                if (baseType is null)
                {
                    yield break;
                }

                // Get the signature for the base type, same as in the method above
                currentSignature = baseType.ToReferenceTypeSignature().InstantiateGenericTypes(context);

                yield return currentSignature;
            }
        }
    }
}