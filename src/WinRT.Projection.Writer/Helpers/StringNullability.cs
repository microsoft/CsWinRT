// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text;
using System.Text.RegularExpressions;

namespace WindowsRuntime.ProjectionWriter.Helpers;

/// <summary>
/// Adds string-only nullable annotations to reference projection sources.
/// </summary>
internal static partial class StringNullability
{
    /// <summary>
    /// Annotates string tokens after emission, so type names used to build identifiers are unchanged.
    /// Comments, literals, escaped identifiers, and existing nullable contexts are preserved.
    /// </summary>
    public static string Annotate(string source)
    {
        StringBuilder result = new(source.Length);
        string annotations = "disable";
        int previousEnd = 0;

        foreach (ValueMatch match in Tokens().EnumerateMatches(source))
        {
            ReadOnlySpan<char> token = source.AsSpan(match.Index, match.Length);
            if (token.StartsWith("#nullable", StringComparison.Ordinal))
            {
                string[] parts = token.ToString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && (parts.Length == 2 || parts[2] == "annotations" || parts[2].StartsWith("//", StringComparison.Ordinal)))
                {
                    annotations = parts[1];
                }
            }
            else if (token.SequenceEqual("string") && annotations != "enable")
            {
                _ = result.Append(source.AsSpan(previousEnd, match.Index - previousEnd));
                _ = result.Append("\n#nullable enable annotations\nstring\n#nullable ");
                _ = result.Append(annotations);
                _ = result.Append(" annotations\n");
                previousEnd = match.Index + match.Length;
            }
        }

        _ = result.Append(source.AsSpan(previousEnd));
        return result.ToString();
    }

    // Emitted sources and embedded additions use ordinary/verbatim literals, not interpolated or raw literals.
    [GeneratedRegex("""\#nullable[^\r\n]*|//[^\r\n]*|/\*[\s\S]*?\*/|@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|'(?:\\.|[^'\\])*'|@?[\p{L}_][\p{L}\p{Nd}_]*""")]
    private static partial Regex Tokens();
}
