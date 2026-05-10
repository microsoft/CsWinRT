// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;

namespace WindowsRuntime.ProjectionWriter.Extensions;

/// <summary>
/// Extension methods for <see cref="IHasCustomAttribute"/>.
/// </summary>
internal static class HasCustomAttributeExtensions
{
    /// <summary>
    /// Returns whether <paramref name="member"/> carries a custom attribute matching the given
    /// <paramref name="ns"/> and <paramref name="name"/>.
    /// </summary>
    /// <param name="member">The metadata member to inspect.</param>
    /// <param name="ns">The namespace of the attribute type.</param>
    /// <param name="name">The unqualified type name of the attribute.</param>
    /// <returns><see langword="true"/> if a matching custom attribute is found; otherwise <see langword="false"/>.</returns>
    public static bool HasAttribute(this IHasCustomAttribute member, string ns, string name)
    {
        foreach (CustomAttribute attr in member.CustomAttributes)
        {
            if (attr.Constructor?.DeclaringType is { } dt &&
                (dt.Namespace?.Value == ns) &&
                (dt.Name?.Value == name))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Returns the matching custom attribute on <paramref name="member"/>, or <see langword="null"/>
    /// if none is found.
    /// </summary>
    /// <param name="member">The metadata member to inspect.</param>
    /// <param name="ns">The namespace of the attribute type.</param>
    /// <param name="name">The unqualified type name of the attribute.</param>
    /// <returns>The matching custom attribute, or <see langword="null"/> if none is found.</returns>
    public static CustomAttribute? GetAttribute(this IHasCustomAttribute member, string ns, string name)
    {
        foreach (CustomAttribute attr in member.CustomAttributes)
        {
            if (attr.Constructor?.DeclaringType is { } dt &&
                (dt.Namespace?.Value == ns) &&
                (dt.Name?.Value == name))
            {
                return attr;
            }
        }
        return null;
    }
}