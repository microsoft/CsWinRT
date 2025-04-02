// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// Ported from 'MissingAttributeCodeFixer' in ComputeSharp (https://github.com/Sergio0694/ComputeSharp).
// Licensed under the MIT License (MIT) (see: https://github.com/Sergio0694/ComputeSharp?tab=MIT-1-ov-file).
// Source: https://github.com/Sergio0694/ComputeSharp/blob/main/src/ComputeSharp.CodeFixing/MissingAttributeCodeFixer.cs.

#if ROSLYN_4_12_0_OR_GREATER

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

#nullable enable

namespace Generator;

/// <summary>
/// A code fixer that adds the annotations for <see cref="RuntimeClassCastAnalyzer"/>.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp)]
[Shared]
public sealed class RuntimeClassCastCodeFixer : CodeFixProvider
{
    /// <inheritdoc/>
    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } = ["CsWinRT1034", "CsWinRT1035"];

    /// <inheritdoc/>
    public sealed override Microsoft.CodeAnalysis.CodeFixes.FixAllProvider? GetFixAllProvider()
    {
        return new FixAllProvider(this);
    }

    /// <inheritdoc/>
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        Diagnostic diagnostic = context.Diagnostics[0];
        TextSpan diagnosticSpan = context.Span;

        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        // Retrieve the property passed by the analyzer
        if (!diagnostic.Properties.TryGetValue(RuntimeClassCastAnalyzer.WindowsRuntimeTypeId, out string? windowsRuntimeTypeId))
        {
            return;
        }

        // Get the member declaration from the target diagnostic
        if (TryGetTargetNode(root, diagnosticSpan, out SyntaxNode? targetNode))
        {
            // Register the code fix to update the return type to be Task instead
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Add '[DynamicWindowsRuntimeCast]' attribute",
                    createChangedDocument: token => AddMissingAttributeAsync(context.Document, root, targetNode, windowsRuntimeTypeId, token),
                    equivalenceKey: "Add '[DynamicWindowsRuntimeCast]' attribute"),
                diagnostic);
        }
    }

    /// <summary>
    /// Tries to resolve the target syntax node to edit.
    /// </summary>
    /// <param name="root">The root of the document to edit.</param>
    /// <param name="span">The target span for the node to retrieve.</param>
    /// <param name="result">The resulting node to edit, if found.</param>
    /// <returns>Whether or not <paramref name="result"/> was retrieved correctly.</returns>
    private static bool TryGetTargetNode([NotNullWhen(true)] SyntaxNode? root, TextSpan span, [NotNullWhen(true)] out SyntaxNode? result)
    {
        result = root?.FindNode(span).FirstAncestorOrSelf<SyntaxNode>(static n =>
            n.IsKind(SyntaxKind.FieldDeclaration) ||
            n.IsKind(SyntaxKind.MethodDeclaration) ||
            n.IsKind(SyntaxKind.GetAccessorDeclaration) ||
            n.IsKind(SyntaxKind.SetAccessorDeclaration) ||
            n.IsKind(SyntaxKind.InitAccessorDeclaration) ||
            n.IsKind(SyntaxKind.AddAccessorDeclaration) ||
            n.IsKind(SyntaxKind.RemoveAccessorDeclaration));

        return result is not null;
    }

    /// <summary>
    /// Applies the code fix to add the missing attribute to a target type.
    /// </summary>
    /// <param name="document">The original document being fixed.</param>
    /// <param name="root">The original tree root belonging to the current document.</param>
    /// <param name="targetNode">The <see cref="SyntaxNode"/> to update.</param>
    /// <param name="windowsRuntimeTypeId">The id of the type symbols to target.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>An updated document with the applied code fix, and the return type of the method being <see cref="Task"/>.</returns>
    private async Task<Document> AddMissingAttributeAsync(
        Document document,
        SyntaxNode root,
        SyntaxNode targetNode,
        string? windowsRuntimeTypeId,
        CancellationToken cancellationToken)
    {
        // Get the compilation (bail if it's not available)
        if (await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false) is not { Compilation: Compilation compilation })
        {
            return document;
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Get the new member declaration
        SyntaxNode updatedMemberDeclaration = AddMissingAttribute(
            document,
            compilation,
            targetNode,
            windowsRuntimeTypeId);

        // Replace the node in the document tree
        return document.WithSyntaxRoot(root.ReplaceNode(targetNode, updatedMemberDeclaration));
    }

    /// <summary>
    /// Applies the code fix to add the missing attribute to a target type.
    /// </summary>
    /// <param name="document">The original document being fixed.</param>
    /// <param name="targetNode">The <see cref="SyntaxNode"/> to update.</param>
    /// <param name="windowsRuntimeTypeId">The id of the type symbols to target.</param>
    /// <returns>An updated document with the applied code fix.</returns>
    private SyntaxNode AddMissingAttribute(
        Document document,
        Compilation compilation,
        SyntaxNode targetNode,
        string? windowsRuntimeTypeId)
    {
        // Bail if we can't resolve the target attribute symbol (this should really never happen)
        if (compilation.GetTypeByMetadataName("WinRT.DynamicWindowsRuntimeCastAttribute") is not INamedTypeSymbol attributeSymbol)
        {
            return targetNode;
        }

        // Also bail if we can't resolve the target type symbol
        if (windowsRuntimeTypeId is null || compilation.GetTypeByMetadataName(windowsRuntimeTypeId) is not INamedTypeSymbol windowsRuntimeTypeSymbol)
        {
            return targetNode;
        }

        SyntaxGenerator syntaxGenerator = SyntaxGenerator.GetGenerator(document);

        // Create the attribute syntax for the new attribute. Also annotate it
        // to automatically add using directives to the document, if needed.
        // Then create the attribute syntax and insert it at the right position.
        SyntaxNode attributeTypeSyntax = syntaxGenerator.TypeExpression(attributeSymbol).WithAdditionalAnnotations(Simplifier.AddImportsAnnotation);
        SyntaxNode targetTypeSyntax = syntaxGenerator.TypeExpression(windowsRuntimeTypeSymbol).WithAdditionalAnnotations(Simplifier.AddImportsAnnotation);
        SyntaxNode attributeArgumentSyntax = syntaxGenerator.AttributeArgument(syntaxGenerator.TypeOfExpression(targetTypeSyntax));
        SyntaxNode attributeSyntax = syntaxGenerator.Attribute(attributeTypeSyntax, [attributeArgumentSyntax]);
        SyntaxNode updatedMemberDeclarationSyntax = syntaxGenerator.AddAttributes(targetNode, attributeSyntax);

        // Replace the node in the syntax tree
        return updatedMemberDeclarationSyntax;
    }

    /// <summary>
    /// A custom <see cref="FixAllProvider"/> with the logic from <see cref="RuntimeClassCastCodeFixer"/>.
    /// </summary>
    /// <param name="codeFixer">The owning <see cref="RuntimeClassCastCodeFixer"/> instance.</param>
    private sealed class FixAllProvider(RuntimeClassCastCodeFixer codeFixer) : DocumentBasedFixAllProvider
    {
        /// <inheritdoc/>
        protected override async Task<Document?> FixAllAsync(FixAllContext fixAllContext, Document document, ImmutableArray<Diagnostic> diagnostics)
        {
            // Get the document root (this should always succeed)
            if (await document.GetSyntaxRootAsync(fixAllContext.CancellationToken).ConfigureAwait(false) is not SyntaxNode root)
            {
                return document;
            }

            fixAllContext.CancellationToken.ThrowIfCancellationRequested();

            // Get the compilation (bail if it's not available)
            if (await document.GetSemanticModelAsync(fixAllContext.CancellationToken).ConfigureAwait(false) is not { Compilation: Compilation compilation })
            {
                return document;
            }

            fixAllContext.CancellationToken.ThrowIfCancellationRequested();

            SyntaxEditor syntaxEditor = new(root, fixAllContext.Solution.Services);

            foreach (Diagnostic diagnostic in diagnostics)
            {
                fixAllContext.CancellationToken.ThrowIfCancellationRequested();

                // Get the current node to annotate
                if (!TryGetTargetNode(root, diagnostic.Location.SourceSpan, out SyntaxNode? targetNode))
                {
                    continue;
                }

                // Retrieve the property passed by the analyzer
                if (!diagnostic.Properties.TryGetValue(RuntimeClassCastAnalyzer.WindowsRuntimeTypeId, out string? windowsRuntimeTypeId))
                {
                    continue;
                }

                // Replace the node via the editor (needs a lambda to ensure multiple fixes are combined correctly)
                syntaxEditor.ReplaceNode(targetNode, (targetNode, _) =>
                {
                    return codeFixer.AddMissingAttribute(
                        document,
                        compilation,
                        targetNode,
                        windowsRuntimeTypeId);
                });
            }

            return document.WithSyntaxRoot(syntaxEditor.GetChangedRoot());
        }
    }
}

#endif
