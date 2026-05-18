// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.ProjectionWriter.Models;

/// <summary>
/// Extension methods on <see cref="ParameterInfo"/>.
/// </summary>
internal static class ParameterInfoExtensions
{
    /// <summary>
    /// Returns the parameter's metadata name (or <paramref name="defaultName"/> if the metadata
    /// name is <see langword="null"/>) prefixed with <c>@</c> if it is a reserved C# keyword.
    /// Used to derive a call-argument or formal-parameter identifier from a <see cref="ParameterInfo"/>.
    /// </summary>
    /// <param name="parameter">The parameter to derive the escaped name from.</param>
    /// <param name="defaultName">The fallback name to use when the parameter has no metadata name. Defaults to <c>"param"</c>.</param>
    /// <returns>The escaped parameter name.</returns>
    public static string GetEscapedName(this ParameterInfo parameter, string defaultName = "param")
    {
        return Helpers.IdentifierEscaping.EscapeIdentifier(parameter.Parameter.Name ?? defaultName);
    }
}
