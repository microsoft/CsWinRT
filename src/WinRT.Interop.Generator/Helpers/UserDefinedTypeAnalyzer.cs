// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator.Helpers;

/// <summary>
/// A class that provides logic to analyze user-defined types.
/// </summary>
internal static class UserDefinedTypeAnalyzer
{
    /// <summary>
    /// Tries to retrieve the most derived Windows Runtime interface type implemented by the specified user-defined type.
    /// </summary>
    /// <param name="userDefinedType">The user-defined type for which to find the most derived Windows Runtime interface type.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="interfaceType">The resulting Windows Runtime interface, if found.</param>
    /// <returns>Whether <paramref name="interfaceType"/> was successfully retrieved.</returns>
    public static bool TryGetMostDerivedWindowsRuntimeInterfaceType(
        TypeSignature userDefinedType,
        InteropReferences interopReferences,
        [NotNullWhen(true)] out TypeSignature? interfaceType)
    {
        interfaceType = null;

        // Go through all implemented interfaces for the user-defined type
        foreach (TypeSignature interfaceSignature in userDefinedType.EnumerateAllInterfaces())
        {
            // If the current interface is not a Windows Runtime type, just skip it.
            // We can only use Windows Runtime interfaces for the runtime class name.
            if (!interfaceSignature.IsWindowsRuntimeType(interopReferences))
            {
                continue;
            }

            // Track the current interface if we haven't seen any other Windows Runtime interfaces
            // before, or if the current interface is more derived than the previous one. That is
            // because we want to try to always find the most derived Windows Runtime interface for
            // a given user-defined type (e.g. 'IDictionary<string, string>', not 'IEnumerable').
            if (interfaceType is null ||
                interfaceType.IsAssignableFrom(interfaceSignature, SignatureComparer.IgnoreVersion))
            {
                interfaceType = interfaceSignature;
            }
        }

        return interfaceType is not null;
    }
}
