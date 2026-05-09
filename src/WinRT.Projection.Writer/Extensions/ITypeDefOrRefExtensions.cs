// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;

namespace WindowsRuntime.ProjectionWriter.Extensions;

/// <summary>
/// Extension methods for <see cref="ITypeDefOrRef"/>.
/// </summary>
internal static class ITypeDefOrRefExtensions
{
    /// <summary>
    /// Returns the namespace and name of <paramref name="type"/> as a tuple, with both fields
    /// guaranteed to be non-<see langword="null"/>: a missing namespace becomes <see cref="string.Empty"/>
    /// and a missing name becomes <see cref="string.Empty"/>.
    /// </summary>
    /// <param name="type">The type reference.</param>
    /// <returns>A tuple of (namespace, name) with both fields non-<see langword="null"/>.</returns>
    public static (string Namespace, string Name) Names(this ITypeDefOrRef type)
    {
        return (type.Namespace?.Value ?? string.Empty, type.Name?.Value ?? string.Empty);
    }
}
