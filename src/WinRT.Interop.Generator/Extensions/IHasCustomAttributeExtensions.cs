// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.Generator;

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions for <see cref="IHasCustomAttribute"/>.
/// </summary>
internal static class IHasCustomAttributeExtensions
{
    /// <summary>
    /// Determines whether a metadata member is assigned an attribute that match a particular type.
    /// </summary>
    /// <param name="member">The metadata member.</param>
    /// <param name="attributeType">The attribute type to look for.</param>
    /// <returns>Whether <paramref name="member"/> has an attribute with the specified type.</returns>
    public static bool HasCustomAttribute(this IHasCustomAttribute member, TypeReference attributeType)
    {
        for (int i = 0; i < member.CustomAttributes.Count; i++)
        {
            ITypeDefOrRef? declaringType = member.CustomAttributes[i].Type;

            // Skip invalid cases and error scenarios (shouldn't happen under normal conditions)
            if (declaringType is null)
            {
                continue;
            }

            // Check that the attribute type is a match
            if (SignatureComparer.IgnoreVersion.Equals(declaringType, attributeType))
            {
                return true;
            }
        }

        return false;
    }
}
