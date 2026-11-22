// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WindowsRuntime.Migration;

/// <summary>
/// Offers the CsWinRT 3.0 delegate mapping for missing legacy typed event handlers.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class TypedEventHandlerCodeFixProvider : CodeFixProvider
{
    /// <summary>
    /// The stable key for the delegate replacement action.
    /// </summary>
    private const string EquivalenceKey = "CsWinRT.MigrateTypedEventHandler";

    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ["CS0234", "CS0246"];

    /// <inheritdoc/>
    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    /// <inheritdoc/>
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        Document document = context.Document;

        if (document.Project.ParseOptions is not CSharpParseOptions options ||
            !options.PreprocessorSymbolNames.Contains("CSWINRT3_0"))
        {
            return;
        }

        SyntaxNode? root = await document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        if (root is null || LegacyAttributeMigration.IsGenerated(
            root.SyntaxTree, document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider, context.CancellationToken))
        {
            return;
        }

        GenericNameSyntax? name = root.FindToken(context.Span.Start).Parent?
            .AncestorsAndSelf().OfType<GenericNameSyntax>().FirstOrDefault();

        if (name is null ||
            name.Identifier.ValueText != "TypedEventHandler" ||
            name.TypeArgumentList.Arguments.Count != 2 ||
            !name.Identifier.Span.IntersectsWith(context.Span))
        {
            return;
        }

        SemanticModel? model = await document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

        if (model is null)
        {
            return;
        }

        INamedTypeSymbol? replacementType = model.Compilation.GetTypeByMetadataName("System.EventHandler`2");

        if (replacementType is not { TypeKind: TypeKind.Delegate } ||
            !SymbolEqualityComparer.Default.Equals(
                replacementType.ContainingAssembly,
                model.Compilation.GetSpecialType(SpecialType.System_Object).ContainingAssembly))
        {
            return;
        }

        NameSyntax original = name.Parent switch
        {
            QualifiedNameSyntax qualified when qualified.Right == name => qualified,
            AliasQualifiedNameSyntax aliasQualified when aliasQualified.Name == name => aliasQualified,
            _ => name
        };

        SymbolInfo symbolInfo = model.GetSymbolInfo(original, context.CancellationToken);

        // A bound legacy type indicates an incompatible dependency, not a missing 3.0 projection name
        if (symbolInfo.Symbol is not null || !symbolInfo.CandidateSymbols.IsEmpty)
        {
            return;
        }

        if (original != name)
        {
            ISymbol? qualifier = original switch
            {
                QualifiedNameSyntax qualifiedName => model.GetSymbolInfo(
                    qualifiedName.Left, context.CancellationToken).Symbol,
                AliasQualifiedNameSyntax aliasQualifiedName => model.GetAliasInfo(
                    aliasQualifiedName.Alias, context.CancellationToken)?.Target,
                _ => null
            };

            if (qualifier is IAliasSymbol alias)
            {
                qualifier = alias.Target;
            }

            if (qualifier is not INamespaceSymbol namespaceSymbol ||
                namespaceSymbol.ToDisplayString() != "Windows.Foundation")
            {
                return;
            }
        }
        else if (!model.GetImportScopes(name.SpanStart, context.CancellationToken)
                     .SelectMany(static scope => scope.Imports)
                     .Any(static import => import.NamespaceOrType is INamespaceSymbol namespaceSymbol &&
                         namespaceSymbol.ToDisplayString() == "Windows.Foundation") ||
                 !model.LookupNamespacesAndTypes(name.SpanStart, name: "TypedEventHandler").IsEmpty)
        {
            return;
        }

        GenericNameSyntax newName = SyntaxFactory.GenericName(
            SyntaxFactory.Identifier("EventHandler"),
            name.TypeArgumentList);
        NameSyntax replacement = SyntaxFactory.QualifiedName(
            SyntaxFactory.ParseName("global::System"),
            newName).WithTriviaFrom(original);

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Migrate to System.EventHandler<TSender, TEventArgs>",
                createChangedDocument: cancellationToken => Task.FromResult(
                    document.WithSyntaxRoot(root.ReplaceNode(original, replacement))),
                equivalenceKey: EquivalenceKey),
            context.Diagnostics);
    }
}
