// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace WinRT.SourceGenerator
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(WinRTAotCodeFixer)), Shared]
    public sealed class WinRTAotCodeFixer : CodeFixProvider
    {
        private const string title = "Make type partial";

        private static ImmutableArray<string> _fixableDiagnosticIds = ImmutableArray.Create(WinRTRules.ClassNotAotCompatible.Id);

        public override ImmutableArray<string> FixableDiagnosticIds => _fixableDiagnosticIds;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root is null)
                return;

            var node = root.FindNode(context.Span);
            if (node is null)
                return;

            var declaration = node.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            if (declaration is null)
                return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    ct => MakeTypePartial(context.Document, declaration, ct),
                    nameof(WinRTAotCodeFixer)),
                context.Diagnostics);
        }

        private static async Task<Document> MakeTypePartial(Document document, ClassDeclarationSyntax @class, CancellationToken token)
        {
            var newClass = @class.AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword));

            var oldRoot = await document.GetSyntaxRootAsync(token).ConfigureAwait(false);
            if (oldRoot is null)
                return document;

            var newRoot = oldRoot.ReplaceNode(@class, newClass);
            return document.WithSyntaxRoot(newRoot);
        }

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;
    }
}
