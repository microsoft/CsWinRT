// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;
using Microsoft.CodeAnalysis.CSharp;

namespace WindowsRuntime.Generator.Helpers;

/// <summary>
/// Helpers for C# identifiers, shared by the build tools that emit C# code.
/// </summary>
internal static class CSharpIdentifiers
{
    /// <summary>
    /// Returns whether <paramref name="identifier"/> is a reserved C# keyword.
    /// </summary>
    /// <param name="identifier">The identifier to test.</param>
    /// <returns><see langword="true"/> if <paramref name="identifier"/> is a C# keyword; otherwise <see langword="false"/>.</returns>
    public static bool IsKeyword(string identifier)
    {
        return SyntaxFacts.GetKeywordKind(identifier) is not SyntaxKind.None;
    }

    /// <summary>
    /// Escapes an arbitrary name (e.g. an assembly name, which can contain dots) into a valid C# identifier, by replacing
    /// every character not valid in an identifier with <c>'_'</c>, and adding a leading <c>'_'</c> if the result would
    /// otherwise start with an invalid character or be a keyword.
    /// </summary>
    /// <param name="value">The name to escape.</param>
    /// <returns>The escaped identifier name.</returns>
    /// <remarks>
    /// This has to match what the CsWinRT source generator does when it emits the <c>ABI.&lt;AssemblyName&gt;</c> namespace
    /// holding an assembly's <c>ManagedExports</c> type, as a component's generated activation entry point forwards to
    /// <c>ABI.&lt;AssemblyName&gt;.ManagedExports</c> in <c>WinRT.Component.dll</c> by name.
    /// </remarks>
    public static string EscapeIdentifierName(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "_";
        }

        string escapedValue;

        if (SyntaxFacts.IsValidIdentifier(value))
        {
            escapedValue = value;
        }
        else
        {
            StringBuilder builder = new(value.Length + 1);

            if (!SyntaxFacts.IsIdentifierStartCharacter(value[0]))
            {
                _ = builder.Append('_');
            }

            foreach (char c in value)
            {
                _ = builder.Append(SyntaxFacts.IsIdentifierPartCharacter(c) ? c : '_');
            }

            escapedValue = builder.ToString();
        }

        return IsKeyword(escapedValue) ? $"_{escapedValue}" : escapedValue;
    }
}
