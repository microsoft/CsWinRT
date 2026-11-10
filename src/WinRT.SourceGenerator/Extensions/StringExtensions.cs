// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CSharp;

#pragma warning disable IDE0046

namespace WindowsRuntime.SourceGenerator;

/// <summary>
/// Extensions for <see cref="string"/>.
/// </summary>
internal static class StringExtensions
{
    /// <param name="value">The input <see cref="string"/> value.</param>
    extension(string value)
    {
        /// <summary>
        /// Escapes an identifier name.
        /// </summary>
        /// <returns>The escaped identifier name from the current value.</returns>
        public string EscapeIdentifierName()
        {
            // If the current value is empty, we just use an underscore for the identifier name. This
            // should generally never be the case, as this method is mostly used for assembly names.
            if (value is null or "")
            {
                return "_";
            }

            string escapedValue;

            // Optimization: we can just return the name as is if it's already a valid identifier. Generally speaking this
            // should always be the case for assembly names (except if they have one or more parts separated by a '.').
            if (SyntaxFacts.IsValidIdentifier(value))
            {
                escapedValue = value;
            }
            else
            {
                DefaultInterpolatedStringHandler handler = new(literalLength: value.Length, formattedCount: 0);

                // Add a leading '_' if the first character is not a valid start character for an identifier
                if (!SyntaxFacts.IsIdentifierStartCharacter(value[0]))
                {
                    handler.AppendFormatted('_');
                }

                // Build the escaped name by just replacing all invalid characters with '_'
                foreach (char c in value)
                {
                    handler.AppendFormatted(SyntaxFacts.IsIdentifierPartCharacter(c) ? c : '_');
                }

                escapedValue = handler.ToStringAndClear();
            }

            // If the resulting identifier is still not valid (eg. a keyword like 'class'),
            // adjust it so that the final result is actually a valid identifier we can use.
            if (SyntaxFacts.GetKeywordKind(escapedValue) is not SyntaxKind.None)
            {
                return $"_{escapedValue}";
            }

            return escapedValue;
        }
    }
}