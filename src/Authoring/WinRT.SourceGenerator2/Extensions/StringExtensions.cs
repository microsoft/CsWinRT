// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CSharp;

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

            // Optimization: we can just return the name as is if it's already a valid identifier. Generally speaking this
            // should always be the case for assembly names (except if they have one or more parts separated by a '.').
            if (SyntaxFacts.IsValidIdentifier(value))
            {
                return value;
            }

            DefaultInterpolatedStringHandler handler = new(literalLength: value.Length, formattedCount: 0);

            // Build the escaped name by just replacing all invalid characters with '_'
            foreach (char c in value)
            {
                handler.AppendFormatted(SyntaxFacts.IsIdentifierPartCharacter(c) ? c : '_');
            }

            return handler.ToStringAndClear();
        }
    }
}