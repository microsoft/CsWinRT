// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator.Helpers;

/// <summary>
/// A class that provides logic to analyze types implementing Windows Runtime interfaces.
/// </summary>
internal static class WindowsRuntimeTypeAnalyzer
{
    /// <summary>
    /// Tries to retrieve the most derived Windows Runtime interface type implemented by the specified type.
    /// </summary>
    /// <param name="type">The type for which to find the most derived Windows Runtime interface type.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="interfaceType">The resulting Windows Runtime interface, if found.</param>
    /// <returns>Whether <paramref name="interfaceType"/> was successfully retrieved.</returns>
    public static bool TryGetMostDerivedWindowsRuntimeInterfaceType(
        TypeSignature type,
        InteropReferences interopReferences,
        [NotNullWhen(true)] out TypeSignature? interfaceType)
    {
        interfaceType = null;

        // Go through all implemented interfaces for the user-defined type
        foreach (TypeSignature interfaceSignature in type.EnumerateAllInterfaces())
        {
            // If the current interface is not a Windows Runtime type, just skip it.
            // We can only use Windows Runtime interfaces for the runtime class name.
            if (!interfaceSignature.IsWindowsRuntimeType(interopReferences))
            {
                continue;
            }

            // Track the current interface if we haven't seen any other Windows Runtime interfaces
            // before, or if the current interface is more derived than the previous one. That is,
            // if for instance a type implements the 'IDictionary<string, string>' interface, we
            // want to make sure to find that type signature, and not just 'IEnumerable'.
            if (interfaceType is null ||
                interfaceType.IsAssignableFrom(interfaceSignature, SignatureComparer.IgnoreVersion))
            {
                interfaceType = interfaceSignature;
            }
        }

        return interfaceType is not null;
    }

    /// <summary>
    /// Enumerates all covariant interface types that can be derived from a given interface type, if applicable.
    /// It also includes the input interface itself in the returned sequence, as that's also a valid interface.
    /// </summary>
    /// <param name="interfaceType">The input interface type to expand.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <returns>The derived covariant interface types derived from <paramref name="interfaceType"/>, if any.</returns>
    /// <remarks>
    /// The returned sequence can contain duplicate types, callers should take care of deduplicating them if needed.
    /// Callers should also validate what returned generic instantiations are actually valid Windows Runtime types.
    /// </remarks>
    public static IEnumerable<TypeSignature> EnumerateCovarianceExpandedInterfaceTypes(TypeSignature interfaceType, InteropReferences interopReferences)
    {
        // Helper method also taking an 'HashSet<TypeSignature>' to track visited types (see notes below)
        static IEnumerable<TypeSignature> EnumerateCovariantInterfaceTypesCore(
            TypeSignature interfaceType,
            InteropReferences interopReferences,
            HashSet<TypeSignature> visitedTypes)
        {
            // The only Windows Runtime interfaces that support covariance have a single type parameter.
            // In practice, it's just 'IEnumerable<T>', 'IEnumerator<T>', and 'IReadOnlyList<T>'.
            if (interfaceType is not GenericInstanceTypeSignature
                {
                    IsValueType: false,
                    GenericType: ITypeDefOrRef genericInterfaceType,
                    TypeArguments: [TypeSignature { IsValueType: false } elementType]
                })
            {
                yield break;
            }

            // Make sure the interface type itself is one of the valid ones
            if (!SignatureComparer.IgnoreVersion.Equals(genericInterfaceType, interopReferences.IEnumerable1) &&
                !SignatureComparer.IgnoreVersion.Equals(genericInterfaceType, interopReferences.IEnumerator1) &&
                !SignatureComparer.IgnoreVersion.Equals(genericInterfaceType, interopReferences.IReadOnlyList1))
            {
                yield break;
            }

            // Track the current constructed interface as visited, and stop immediately if we have already visited
            // it before. This check is necessary to avoid infinite recursion on types that have circular references.
            // For instance, consider this type declaration:
            //
            // class Node : IEnumerable<Node>;
            //
            // While analyzing it, we'll see 'IEnumerable<Node>'. Which then leads to the following covariant types:
            //   - 'IEnumerable<Node>'
            //   - 'IEnumerable<IEnumerable<object>>'
            //   - 'IEnumerable<IEnumerable>'
            //   - 'IEnumerable<object>'
            //
            // Without this check, the 'IEnumerable<Node>' part would keep expanding forever, as expanding its
            // covariant interfaces would eventually bring us back to the same constructed interface signature
            // (for example, 'IEnumerable<Node>') again. By tracking visited constructed interface types instead,
            // we stop as soon as we encounter the same constructed covariant interface a second time, meaning
            // we'll only yield the set above in this case.
            if (!visitedTypes.Add(interfaceType))
            {
                yield break;
            }

            // First return the current constructed interface too, as it's a valid generic instantiation
            yield return interfaceType;

            // Next, gather all combinations from interfaces implemented by the element type
            foreach (TypeSignature elementInterfaceType in elementType.EnumerateAllInterfaces())
            {
                // Construct the generic interface with the current element type
                yield return genericInterfaceType.MakeGenericReferenceType(elementInterfaceType);

                // Also track any covariant combinations derived from the current element type
                foreach (TypeSignature elementCovariantInterfaceType in EnumerateCovariantInterfaceTypesCore(
                    interfaceType: elementInterfaceType,
                    interopReferences: interopReferences,
                    visitedTypes: visitedTypes))
                {
                    yield return genericInterfaceType.MakeGenericReferenceType(elementCovariantInterfaceType);
                }
            }

            // Then, also gather all base types for the element type
            foreach (TypeSignature baseType in elementType.EnumerateBaseTypes())
            {
                yield return genericInterfaceType.MakeGenericReferenceType(baseType);
            }

            // Lastly, make sure to also always track 'object' as a base type. This would be
            // skipped for element types being interfaces, as they have no base type. However,
            // with respect to variant conversions, 'object' is always a valid covariant type.
            yield return genericInterfaceType.MakeGenericReferenceType(interopReferences.CorLibTypeFactory.Object);

            // We're closing this recursive sub-tree, so we can remove the current interface type
            _ = visitedTypes.Remove(interfaceType);
        }

        return EnumerateCovariantInterfaceTypesCore(
            interfaceType: interfaceType,
            interopReferences: interopReferences,
            visitedTypes: new HashSet<TypeSignature>(SignatureComparer.IgnoreVersion));
    }
}
