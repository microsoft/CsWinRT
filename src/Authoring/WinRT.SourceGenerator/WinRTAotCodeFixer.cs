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
        private static ImmutableArray<DiagnosticDescriptor> _supportedDiagnostics = ImmutableArray.Create(WinRTRules.ClassNotAotCompatibleWarning, WinRTRules.ClassNotAotCompatibleInfo);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => _supportedDiagnostics;

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(static context =>
            {
                if (!context.Options.AnalyzerConfigOptionsProvider.IsCsWinRTAotOptimizerEnabled())
                {
                    return;
                }

                bool isComponentProject = context.Options.AnalyzerConfigOptionsProvider.IsCsWinRTComponent();
                var winrtTypeAttribute = context.Compilation.GetTypeByMetadataName("WinRT.WindowsRuntimeTypeAttribute");
                var winrtExposedTypeAttribute = context.Compilation.GetTypeByMetadataName("WinRT.WinRTExposedTypeAttribute");
                if (winrtTypeAttribute is null || winrtExposedTypeAttribute is null)
                {
                    return;
                }

                var typeMapper = new TypeMapper(context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.GetUIXamlProjectionsMode());
                var csWinRTAotWarningLevel = context.Options.AnalyzerConfigOptionsProvider.GetCsWinRTAotWarningLevel();

                context.RegisterSymbolAction(context =>
                {
                    // Filter to classes that can be passed as objects.
                    if (context.Symbol is INamedTypeSymbol namedType &&
                        namedType.TypeKind == TypeKind.Class &&
                        !namedType.IsAbstract &&
                        !namedType.IsStatic)
                    {
                        // Make sure this is a class that we would generate the WinRTExposedType attribute on
                        // and that it isn't already partial.
                        if (!GeneratorHelper.IsPartial(namedType) &&
                            !GeneratorHelper.IsWinRTType(namedType, winrtTypeAttribute, typeMapper, isComponentProject, context.Compilation.Assembly) &&
                            !GeneratorHelper.HasNonInstantiatedWinRTGeneric(namedType, typeMapper) &&
                            !GeneratorHelper.HasAttributeWithType(namedType, winrtExposedTypeAttribute))
                        {
                            foreach (var iface in namedType.AllInterfaces)
                            {
                                if (GeneratorHelper.IsWinRTType(iface, winrtTypeAttribute, typeMapper, isComponentProject, context.Compilation.Assembly))
                                {
                                    // Based on the warning level, emit as a warning or as an info.
                                    var diagnosticDescriptor = (csWinRTAotWarningLevel == 2 || 
                                                                (csWinRTAotWarningLevel == 1 && !GeneratorHelper.IsCustomMappedType(iface, typeMapper))) ? 
                                        WinRTRules.ClassNotAotCompatibleWarning : WinRTRules.ClassNotAotCompatibleInfo;
                                    context.ReportDiagnostic(Diagnostic.Create(diagnosticDescriptor, namedType.Locations[0], namedType.Name));
                                    return;
                                }
                            }
                        }
                    }
                }, SymbolKind.NamedType);
            });
        }
    }

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(WinRTAotCodeFixer)), Shared]
    public sealed class WinRTAotCodeFixer : CodeFixProvider
    {
        private const string title = "Make type partial";

        private static ImmutableArray<string> _fixableDiagnosticIds = ImmutableArray.Create(WinRTRules.ClassNotAotCompatibleWarning.Id);

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
