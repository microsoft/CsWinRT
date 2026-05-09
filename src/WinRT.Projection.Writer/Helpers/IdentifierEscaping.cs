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
        if (CSharpKeywords.IsKeyword(identifier))
        {
            writer.Write("@");
        }
        writer.Write(identifier);
    }
}
