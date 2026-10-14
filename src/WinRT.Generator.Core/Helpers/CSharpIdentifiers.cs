// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Frozen;
using System.Globalization;
using System.Text;

namespace WindowsRuntime.Generator.Helpers;

/// <summary>
/// Helpers for C# identifiers, shared by the build tools that emit C# code.
/// </summary>
internal static class CSharpIdentifiers
{
    /// <summary>
    /// The set of reserved C# keywords (matching what <c>SyntaxFacts.GetKeywordKind</c> recognizes in Roslyn, which the
    /// CsWinRT source generator uses, so names escaped by both agree).
    /// </summary>
    private static readonly FrozenSet<string> Keywords =
    [
        "__arglist", "__makeref", "__reftype", "__refvalue",
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue",
        "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally",
        "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long",
        "namespace", "new", "null", "object", "operator", "out", "override", "params", "private", "protected", "public",
        "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch",
        "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void",
        "volatile", "while"
    ];

    /// <summary>
    /// Returns whether <paramref name="identifier"/> is a reserved C# keyword.
    /// </summary>
    /// <param name="identifier">The identifier to test.</param>
    /// <returns><see langword="true"/> if <paramref name="identifier"/> is a C# keyword; otherwise <see langword="false"/>.</returns>
    public static bool IsKeyword(string identifier)
    {
        return Keywords.Contains(identifier);
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

        if (IsValidIdentifier(value))
        {
            escapedValue = value;
        }
        else
        {
            StringBuilder builder = new(value.Length + 1);

            if (!IsIdentifierStartCharacter(value[0]))
            {
                _ = builder.Append('_');
            }

            foreach (char c in value)
            {
                _ = builder.Append(IsIdentifierPartCharacter(c) ? c : '_');
            }

            escapedValue = builder.ToString();
        }

        return IsKeyword(escapedValue) ? $"_{escapedValue}" : escapedValue;

        static bool IsValidIdentifier(string value)
        {
            if (!IsIdentifierStartCharacter(value[0]))
            {
                return false;
            }

            foreach (char c in value)
            {
                if (!IsIdentifierPartCharacter(c))
                {
                    return false;
                }
            }

            return true;
        }

        static bool IsIdentifierStartCharacter(char c) => c == '_' || IsLetterChar(CharUnicodeInfo.GetUnicodeCategory(c));

        static bool IsIdentifierPartCharacter(char c)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);

            return IsLetterChar(category) || category is
                UnicodeCategory.DecimalDigitNumber or
                UnicodeCategory.ConnectorPunctuation or
                UnicodeCategory.NonSpacingMark or
                UnicodeCategory.SpacingCombiningMark or
                UnicodeCategory.Format;
        }

        static bool IsLetterChar(UnicodeCategory category)
        {
            return category is
                UnicodeCategory.UppercaseLetter or
                UnicodeCategory.LowercaseLetter or
                UnicodeCategory.TitlecaseLetter or
                UnicodeCategory.ModifierLetter or
                UnicodeCategory.OtherLetter or
                UnicodeCategory.LetterNumber;
        }
    }
}
