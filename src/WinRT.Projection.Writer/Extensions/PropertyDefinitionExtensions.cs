// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;

namespace WindowsRuntime.ProjectionWriter.Extensions;

/// <summary>
/// Extension methods for <see cref="PropertyDefinition"/>.
/// </summary>
internal static class PropertyDefinitionExtensions
{
    /// <summary>
    /// Returns whether <paramref name="property"/> carries the <c>[NoExceptionAttribute]</c>.
    /// </summary>
    /// <param name="property">The property definition to inspect.</param>
    /// <returns><see langword="true"/> if the property is documented to never throw; otherwise <see langword="false"/>.</returns>
    public static bool IsNoExcept(this PropertyDefinition property)
        => property.HasAttribute("Windows.Foundation.Metadata", "NoExceptionAttribute");
}
