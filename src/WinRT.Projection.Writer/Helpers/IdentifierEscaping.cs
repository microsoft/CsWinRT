// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Helpers for converting raw metadata names into valid C# identifiers.
/// </summary>
internal static class IdentifierEscaping
{
    /// <summary>
    /// Strips a generic-arity backtick suffix from a metadata type name (e.g. <c>"IList`1"</c>
    /// becomes <c>"IList"</c>). Mirrors the C++ tool's <c>write_code</c> behavior for type names.
    /// </summary>
    /// <param name="typeName">The metadata type name to strip.</param>
    /// <returns>The type name without its backtick suffix.</returns>
    public static string StripBackticks(string typeName)
    {
        int idx = typeName.IndexOf('`');
        return idx >= 0 ? typeName.Substring(0, idx) : typeName;
    }

    /// <summary>
    /// Writes <paramref name="identifier"/> to <paramref name="writer"/>, prefixed with <c>@</c>
    /// if it is a reserved C# keyword.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="identifier">The identifier to write.</param>
    public static void WriteEscapedIdentifier(IndentedTextWriter writer, string identifier)
    {
        if (CSharpKeywords.IsKeyword(identifier))
        {
            writer.Write("@");
        }
        writer.Write(identifier);
    }

    /// <summary>Legacy <see cref="TextWriter"/> overload that delegates to <see cref="WriteEscapedIdentifier(IndentedTextWriter, string)"/>.</summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="identifier">The identifier to write.</param>
    public static void WriteEscapedIdentifier(TextWriter writer, string identifier)
        => WriteEscapedIdentifier(writer.Writer, identifier);
}
