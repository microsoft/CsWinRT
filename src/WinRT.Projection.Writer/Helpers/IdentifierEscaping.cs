// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Text;
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
    /// Returns <paramref name="identifier"/> prefixed with <c>@</c> if it is a reserved C# keyword;
    /// otherwise returns it unchanged.
    /// </summary>
    /// <param name="identifier">The identifier to escape.</param>
    /// <returns>The escaped identifier.</returns>
    public static string EscapeIdentifier(string identifier)
    {
        return CSharpKeywords.IsKeyword(identifier) ? "@" + identifier : identifier;
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
    public static IndentedTextWriterCallback WriteEscapedIdentifier(string identifier)
    {
        return writer => WriteEscapedIdentifier(writer, identifier);
    }

    /// <summary>
    /// Escapes an assembly name into a valid C# identifier, matching what the CsWinRT source generator does
    /// when it emits the <c>ABI.&lt;AssemblyName&gt;</c> namespace holding an assembly's <c>ManagedExports</c>
    /// type. The two have to agree: a component's generated activation entry point forwards to
    /// <c>ABI.&lt;AssemblyName&gt;.ManagedExports</c> in <c>WinRT.Component.dll</c> by name.
    /// </summary>
    /// <param name="value">The assembly name to escape.</param>
    /// <returns>The escaped identifier name.</returns>
    public static string EscapeAssemblyName(string value)
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

        return CSharpKeywords.IsKeyword(escapedValue) ? $"_{escapedValue}" : escapedValue;

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
