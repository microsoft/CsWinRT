// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;

namespace WindowsRuntime.ProjectionWriter.Helpers;

/// <summary>
/// Extension methods on <see cref="ITypeDefOrRef"/> and related metadata types.
/// </summary>
internal static class TypeDefOrRefExtensions
{
    /// <summary>
    /// Returns the type's metadata name with any generic-arity backtick suffix stripped
    /// (e.g. <c>"IList`1"</c> becomes <c>"IList"</c>). When the type has no name, returns
    /// <see cref="string.Empty"/>.
    /// </summary>
    /// <param name="type">The type to derive the stripped name from.</param>
    /// <returns>The type's stripped name.</returns>
    public static string GetStrippedName(this ITypeDefOrRef type)
    {
        return IdentifierEscaping.StripBackticks(type.Name?.Value ?? string.Empty);
    }
}
