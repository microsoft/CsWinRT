// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator.Helpers;

/// <summary>
/// Contains logic to handle special-case types to exclude from the interop API surface.
/// </summary>
internal static class TypeExclusions
{
    /// <summary>
    /// Checks whether a given type should be excluded from the interop API surface.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <returns>Whether <paramref name="type"/> should be excluded from the interop API surface.</returns>
    public static bool IsExcluded(ITypeDescriptor type, InteropReferences interopReferences)
    {
        // If we have a constructed generic type, extract the generic type definition
        // and use that for checking. We don't have exclusion logic for type arguments.
        if (type is GenericInstanceTypeSignature typeSignature)
        {
            return IsExcluded(typeSignature.GenericType, interopReferences);
        }

        // Also handle SZ arrays and check their element type
        if (type is SzArrayTypeSignature arraySignature)
        {
            return IsExcluded(arraySignature.BaseType, interopReferences);
        }

        // Check if the input type matches any of our exclusions
        foreach (TypeReference excludedType in (ReadOnlySpan<TypeReference>)[interopReferences.Task1])
        {
            if (SignatureComparer.IgnoreVersion.Equals(type, excludedType))
            {
                return true;
            }
        }

        return false;
    }
}
