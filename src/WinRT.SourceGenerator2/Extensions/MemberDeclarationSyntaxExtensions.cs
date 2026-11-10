// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WindowsRuntime.SourceGenerator;

/// <summary>
/// Extensions for member declaration syntax types.
/// </summary>
internal static class MemberDeclarationSyntaxExtensions
{
    extension(MemberDeclarationSyntax node)
    {
        /// <summary>
        /// Gets whether the input member declaration is partial.
        /// </summary>
        public bool IsPartial => node.Modifiers.Any(SyntaxKind.PartialKeyword);

        /// <summary>
        /// Gets whether the input member declaration is partial and
        /// all of its parent type declarations are also partial.
        /// </summary>
        public bool IsPartialAndWithinPartialTypeHierarchy
        {
            get
            {
                // If the target node is not partial, stop immediately
                if (!node.IsPartial)
                {
                    return false;
                }

                // Walk all parent type declarations, stop if any of them is not partial
                foreach (SyntaxNode ancestor in node.Ancestors())
                {
                    if (ancestor is BaseTypeDeclarationSyntax { IsPartial: false })
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}