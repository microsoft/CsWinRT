// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions for <see cref="IHasCustomAttribute"/>.
/// </summary>
internal static class IHasCustomAttributeExtensions
{
    /// <summary>
    /// Tries to get an attribute that matches a particular type from a given metadata member.
    /// </summary>
    /// <param name="member">The metadata member.</param>
    /// <param name="attributeType">The attribute type to look for.</param>
    /// <param name="attribute">The resulting attribute, if found.</param>
    /// <returns>Whether <paramref name="attribute"/> was successfully retrieved.</returns>
    public static bool TryGetCustomAttribute(this IHasCustomAttribute member, TypeReference attributeType, [NotNullWhen(true)] out CustomAttribute? attribute)
    {
        for (int i = 0; i < member.CustomAttributes.Count; i++)
        {
            CustomAttribute currentAttribute = member.CustomAttributes[i];

            // Skip invalid cases and error scenarios (shouldn't happen under normal conditions)
            if (currentAttribute.Type is null)
            {
                continue;
            }

            // Check that the attribute type is a match
            if (SignatureComparer.IgnoreVersion.Equals(currentAttribute.Type, attributeType))
            {
                attribute = currentAttribute;

                return true;
            }
        }

        attribute = null;

        return false;
    }

    /// <summary>
    /// Determines whether a metadata member is assigned an attribute that match a particular type.
    /// </summary>
    /// <param name="member">The metadata member.</param>
    /// <param name="attributeType">The attribute type to look for.</param>
    /// <returns>Whether <paramref name="member"/> has an attribute with the specified type.</returns>
    public static bool HasCustomAttribute(this IHasCustomAttribute member, TypeReference attributeType)
    {
        return TryGetCustomAttribute(member, attributeType, out _);
    }
}