// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using WindowsRuntime.ProjectionWriter.Factories.Callbacks;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Helpers;

/// <summary>
/// Helpers for converting raw metadata names into valid C# identifiers.
/// </summary>
internal static class IdentifierEscaping
{
    /// <summary>
    /// Strips a generic-arity backtick suffix from a metadata type name (e.g. <c>"IList`1"</c>
    /// becomes <c>"IList"</c>).
    /// </summary>
    /// <param name="typeName">The metadata type name to strip.</param>
    /// <returns>The type name without its backtick suffix.</returns>
    public static string StripBackticks(string typeName)
    {
        int idx = typeName.IndexOf('`');
        return idx >= 0 ? typeName[..idx] : typeName;
    }

    /// <summary>
    /// Writes <paramref name="identifier"/> to <paramref name="writer"/>, prefixed with <c>@</c>
    /// if it is a reserved C# keyword.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="identifier">The identifier to write.</param>
    public static void WriteEscapedIdentifier(IndentedTextWriter writer, string identifier)
    {
        writer.WriteIf(CSharpKeywords.IsKeyword(identifier), "@");

        writer.Write(identifier);
    }

    /// <inheritdoc cref="WriteEscapedIdentifier(IndentedTextWriter, string)"/>
    /// <returns>A callback that writes the escaped identifier to the writer it's appended to.</returns>
    public static WriteEscapedIdentifierCallback WriteEscapedIdentifier(string identifier)
    {
        return new(identifier);
    }

    /// <summary>
    /// Returns the camel-case form of <paramref name="name"/>: if the first character is an
    /// upper-case ASCII letter, it is lowered; otherwise <paramref name="name"/> is returned
    /// unchanged. Used to derive C# constructor parameter names from public field names.
    /// </summary>
    /// <param name="name">The name to lower-case the first character of.</param>
    /// <returns>The camel-case form.</returns>
    public static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        char c = name[0];

        if (c is >= 'A' and <= 'Z')
        {
            return char.ToLowerInvariant(c) + name[1..];
        }

        return name;
    }
}
