// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// Ported from 'MissingAttributeCodeFixer' in ComputeSharp (https://github.com/Sergio0694/ComputeSharp).
// Licensed under the MIT License (MIT) (see: https://github.com/Sergio0694/ComputeSharp?tab=MIT-1-ov-file).
// Source: https://github.com/Sergio0694/ComputeSharp/blob/main/src/ComputeSharp.CodeFixing/MissingAttributeCodeFixer.cs.

#if ROSLYN_4_12_0_OR_GREATER

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

        SemanticModel? semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        Compilation compilation = semanticModel!.Compilation;

        // Retrieve the property passed by the analyzer
        if (!diagnostic.Properties.TryGetValue(RuntimeClassCastAnalyzer.WindowsRuntimeTypeId, out string? windowsRuntimeTypeId))
        {
            return;
        }

        // Get the member declaration from the target diagnostic
        if (root?.FindNode(diagnosticSpan).FirstAncestorOrSelf<MemberDeclarationSyntax>(static n => n.IsKind(SyntaxKind.FieldDeclaration) || n.IsKind(SyntaxKind.MethodDeclaration)) is { } memberDeclaration)
        {
            // Register the code fix to update the return type to be Task instead
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Add '[DynamicWindowsRuntimeCast]' attribute",
                    createChangedDocument: token => Task.FromResult(AddMissingAttribute(context.Document, compilation, root, memberDeclaration, windowsRuntimeTypeId, token)),
                    equivalenceKey: "Add '[DynamicWindowsRuntimeCast]' attribute"),
                diagnostic);
        }
    }

    /// <summary>
    /// Applies the code fix to add the missing attribute to a target type.
    /// </summary>
    /// <param name="document">The original document being fixed.</param>
    /// <param name="root">The original tree root belonging to the current document.</param>
    /// <param name="memberDeclaration">The <see cref="MemberDeclarationSyntax"/> to update.</param>
    /// <param name="windowsRuntimeTypeId">The id of the type symbols to target.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>An updated document with the applied code fix, and the return type of the method being <see cref="Task"/>.</returns>
    private Document AddMissingAttribute(
        Document document,
        Compilation compilation,
        SyntaxNode root,
        MemberDeclarationSyntax memberDeclaration,
        string? windowsRuntimeTypeId,
        CancellationToken cancellationToken)
    {
        // Get the new member declaration
        SyntaxNode updatedMemberDeclaration = AddMissingAttribute(
            document,
            compilation,
            memberDeclaration,
            windowsRuntimeTypeId,
            cancellationToken);

        // Replace the node in the document tree
        return document.WithSyntaxRoot(root.ReplaceNode(memberDeclaration, updatedMemberDeclaration));
    }

    /// <summary>
    /// Applies the code fix to add the missing attribute to a target type.
    /// </summary>
    /// <param name="document">The original document being fixed.</param>
    /// <param name="memberDeclaration">The <see cref="MemberDeclarationSyntax"/> to update.</param>
    /// <param name="windowsRuntimeTypeId">The id of the type symbols to target.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>An updated document with the applied code fix, and the return type of the method being <see cref="Task"/>.</returns>
    private SyntaxNode AddMissingAttribute(
        Document document,
        Compilation compilation,
        MemberDeclarationSyntax memberDeclaration,
        string? windowsRuntimeTypeId,
        CancellationToken cancellationToken)
    {
        // Bail if we can't resolve the target attribute symbol (this should really never happen)
        if (compilation.GetTypeByMetadataName("WinRT.DynamicWindowsRuntimeCastAttribute") is not INamedTypeSymbol attributeSymbol)
        {
            return memberDeclaration;
        }

        // Also bail if we can't resolve the target type symbol
        if (windowsRuntimeTypeId is null || compilation.GetTypeByMetadataName(windowsRuntimeTypeId) is not INamedTypeSymbol windowsRuntimeTypeSymbol)
        {
            return memberDeclaration;
        }

        SyntaxGenerator syntaxGenerator = SyntaxGenerator.GetGenerator(document);

        // Create the attribute syntax for the new attribute. Also annotate it
        // to automatically add using directives to the document, if needed.
        // Then create the attribute syntax and insert it at the right position.
        SyntaxNode attributeTypeSyntax = syntaxGenerator.TypeExpression(attributeSymbol).WithAdditionalAnnotations(Simplifier.AddImportsAnnotation);
        SyntaxNode targetTypeSyntax = syntaxGenerator.TypeExpression(windowsRuntimeTypeSymbol).WithAdditionalAnnotations(Simplifier.AddImportsAnnotation);
        SyntaxNode attributeArgumentSyntax = syntaxGenerator.AttributeArgument(syntaxGenerator.TypeOfExpression(targetTypeSyntax));
        SyntaxNode attributeSyntax = syntaxGenerator.Attribute(attributeTypeSyntax, [attributeArgumentSyntax]);
        SyntaxNode updatedMemberDeclarationSyntax = syntaxGenerator.AddAttributes(memberDeclaration, attributeSyntax);

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

            var semanticModel = await document.GetSemanticModelAsync(fixAllContext.CancellationToken).ConfigureAwait(false);
            var compilation = semanticModel!.Compilation;

            SyntaxEditor syntaxEditor = new(root, fixAllContext.Solution.Services);

            foreach (Diagnostic diagnostic in diagnostics)
            {
                // Get the current node to annotate
                if (root.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<MemberDeclarationSyntax>(static n => n.IsKind(SyntaxKind.FieldDeclaration) || n.IsKind(SyntaxKind.MethodDeclaration)) is not { } memberDeclaration)
                {
                    continue;
                }

                // Retrieve the property passed by the analyzer
                if (!diagnostic.Properties.TryGetValue(RuntimeClassCastAnalyzer.WindowsRuntimeTypeId, out string? windowsRuntimeTypeId))
                {
                    continue;
                }

                // Replace the node via the editor
                syntaxEditor.ReplaceNode(memberDeclaration, (memberDeclaration, _) =>
                {
                    // Get the syntax node with the updated declaration
                    return codeFixer.AddMissingAttribute(
                        document,
                        compilation,
                        (MemberDeclarationSyntax)memberDeclaration,
                        windowsRuntimeTypeId,
                        fixAllContext.CancellationToken);
                });
            }

            return document.WithSyntaxRoot(syntaxEditor.GetChangedRoot());
        }
    }
}

#endif
