// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#pragma warning disable IDE0046

namespace WindowsRuntime.SourceGenerator;

/// <inheritdoc cref="CustomPropertyProviderGenerator"/>
public partial class CustomPropertyProviderGenerator
{
    /// <summary>
    /// Generation methods for <see cref="CustomPropertyProviderGenerator"/>.
    /// </summary>
    private static class Execute
    {
        /// <summary>
        /// Checks whether a target node needs the <c>ICustomPropertyProvider</c> implementation.
        /// </summary>
        /// <param name="node">The target <see cref="SyntaxNode"/> instance to check.</param>
        /// <param name="token">The cancellation token for the operation.</param>
        /// <returns>Whether <paramref name="node"/> is a valid target for the <c>ICustomPropertyProvider</c> implementation.</returns>
        [SuppressMessage("Style", "IDE0060", Justification = "The cancellation token is supplied by Roslyn.")]
        public static bool IsTargetNodeValid(SyntaxNode node, CancellationToken token)
        {
            // We only care about class and struct types, all other types are not valid targets
            if (!node.IsAnyKind(SyntaxKind.ClassDeclaration, SyntaxKind.RecordDeclaration, SyntaxKind.StructDeclaration, SyntaxKind.RecordStructDeclaration))
            {
                return false;
            }

            // If the type is static, abstract, or 'ref', we cannot implement 'ICustomPropertyProvider' on it
            if (((MemberDeclarationSyntax)node).Modifiers.ContainsAny(SyntaxKind.StaticKeyword, SyntaxKind.AbstractKeyword, SyntaxKind.RefKeyword))
            {
                return false;
            }

            // We can only generated the 'ICustomPropertyProvider' implementation if the type is 'partial'.
            // Additionally, all parent type declarations must also be 'partial', for generation to work.
            if (!((MemberDeclarationSyntax)node).IsPartialAndWithinPartialTypeHierarchy)
            {
                return false;
            }

            return true;
        }
    }
}