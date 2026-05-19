// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.ProjectionWriter.Models;

/// <summary>
/// Extension methods on <see cref="ParameterInfo"/>.
/// </summary>
internal static class ParameterInfoExtensions
{
    /// <summary>
    /// Returns the parameter's raw metadata name, falling back to <paramref name="defaultName"/>
    /// when the metadata name is <see langword="null"/>. This is the un-escaped form (no C#
    /// keyword <c>@</c> prefix); use it for derived identifiers such as <c>__{rawName}</c>
    /// fixed-block locals or marshalling-state field names. For the C# keyword-escaped form,
    /// use <see cref="GetEscapedName(ParameterInfo, string)"/> instead.
    /// </summary>
    /// <param name="parameter">The parameter to derive the raw name from.</param>
    /// <param name="defaultName">The fallback name to use when the parameter has no metadata name. Defaults to <c>"param"</c>.</param>
    /// <returns>The raw parameter name.</returns>
    public static string GetRawName(this ParameterInfo parameter, string defaultName = "param")
    {
        return parameter.Parameter.Name ?? defaultName;
    }

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

    /// <summary>
    /// Returns the C#-escaped parameter name, allowing the caller to inject a synthesized
    /// override (e.g. FastAbi-merged Methods classes). When <paramref name="paramNameOverride"/>
    /// is non-<see langword="null"/>, it replaces the metadata name as the source for escaping.
    /// </summary>
    /// <param name="parameter">The parameter to name.</param>
    /// <param name="paramNameOverride">Optional override; takes precedence over the metadata name when non-<see langword="null"/>.</param>
    /// <returns>The escaped parameter name (with <c>@</c> prefix for C# keywords).</returns>
    public static string GetParamName(this ParameterInfo parameter, string? paramNameOverride)
    {
        string name = paramNameOverride ?? parameter.GetRawName();
        return Helpers.IdentifierEscaping.EscapeIdentifier(name);
    }

    /// <summary>
    /// Returns the local-variable name for <paramref name="parameter"/>: the raw metadata
    /// name (no C#-keyword <c>@</c> escape, since helper locals like <c>__event</c> remain
    /// valid identifiers even when the underlying parameter name is a C# keyword).
    /// </summary>
    /// <param name="parameter">The parameter to name.</param>
    /// <param name="paramNameOverride">Optional override; takes precedence over the metadata name when non-<see langword="null"/>.</param>
    /// <returns>The unescaped local-variable name.</returns>
    public static string GetParamLocalName(this ParameterInfo parameter, string? paramNameOverride)
    {
        return paramNameOverride ?? parameter.GetRawName();
    }
}
