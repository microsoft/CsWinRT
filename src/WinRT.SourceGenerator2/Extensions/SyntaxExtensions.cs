// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace WindowsRuntime.SourceGenerator;

/// <summary>
/// Extensions for syntax types.
/// </summary>
internal static class SyntaxExtensions
{
    extension(SyntaxNode node)
    {
        /// <summary>
        /// Determines if <see cref="SyntaxNode"/> is of any of the specified kinds.
        /// </summary>
        /// <param name="kinds">The syntax kinds to test for.</param>
        /// <returns>Whether the input node is of any of the specified kinds.</returns>
        public bool IsAnyKind(params ReadOnlySpan<SyntaxKind> kinds)
        {
            foreach (SyntaxKind kind in kinds)
            {
                if (node.IsKind(kind))
                {
                    return true;
                }
            }

            return false;
        }
    }

    extension(SyntaxTokenList list)
    {
        /// <summary>
        /// Tests whether a list contains any token of particular kinds.
        /// </summary>
        /// <param name="kinds">The syntax kinds to test for.</param>
        /// <returns>Whether the input list contains any of the specified kinds.</returns>
        public bool ContainsAny(params ReadOnlySpan<SyntaxKind> kinds)
        {
            foreach (SyntaxKind kind in kinds)
            {
                if (list.IndexOf(kind) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}