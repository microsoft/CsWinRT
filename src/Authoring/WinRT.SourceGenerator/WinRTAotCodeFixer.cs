// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace WinRT.SourceGenerator
{
    [DiagnosticAnalyzer(LanguageNames.CSharp), Shared]
    public sealed class WinRTAotDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        private static ImmutableArray<DiagnosticDescriptor> _supportedDiagnostics = ImmutableArray.Create(WinRTRules.ClassNotAotCompatible);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => _supportedDiagnostics;

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSymbolAction(static (c) => 
            {
                if (!c.Options.AnalyzerConfigOptionsProvider.IsCsWinRTAotOptimizerEnabled() || 
                    c.Options.AnalyzerConfigOptionsProvider.IsCsWinRTComponent())
                {
                    return;
                }

                // Filter to classes that can be passed as objects.
                if (c.Symbol is INamedTypeSymbol namedType && 
                    namedType.TypeKind == TypeKind.Class &&
                    !namedType.IsAbstract &&
                    !namedType.IsStatic)
                {
                    // Make sure this is a class that we would generate the WinRTExposedType attribute on
                    // and that it isn't already partial.
                    if (!GeneratorHelper.IsPartial(namedType) &&
                        !GeneratorHelper.IsWinRTType(namedType) &&
                        !GeneratorHelper.HasNonInstantiatedWinRTGeneric(namedType) &&
                        !GeneratorHelper.HasWinRTExposedTypeAttribute(namedType))
                    {
                        foreach (var iface in namedType.AllInterfaces)
                        {
                            if (GeneratorHelper.IsWinRTType(iface))
                            {
                                c.ReportDiagnostic(Diagnostic.Create(WinRTRules.ClassNotAotCompatible, namedType.Locations[0], namedType.Name));
                                return;
                            }
                        }
                    }
                }
            }, SymbolKind.NamedType);
        }
    }

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
