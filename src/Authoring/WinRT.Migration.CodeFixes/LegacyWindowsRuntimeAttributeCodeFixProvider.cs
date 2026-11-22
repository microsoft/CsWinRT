// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace WindowsRuntime.Migration;

/// <summary>
/// Migrates supported legacy attributes while retaining their exact legacy spelling when requested.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class LegacyWindowsRuntimeAttributeCodeFixProvider : CodeFixProvider
{
    /// <summary>The action for a 3.0-only source migration.</summary>
    private const string DirectKey = "CsWinRT.MigrateLegacyAttributes";

    /// <summary>The action retaining the 2.x source branch.</summary>
    private const string PreserveKey = "CsWinRT.MigrateLegacyAttributes.PreserveVersion2";

    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = [LegacyAttributeMigration.DiagnosticId];

    /// <inheritdoc/>
    public override FixAllProvider GetFixAllProvider()
    {
        return new AttributeFixAllProvider();
    }

    /// <inheritdoc/>
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        SemanticModel? model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

        if (root is null || model is null ||
            !LegacyAttributeMigration.IsVersion3(root.SyntaxTree) ||
            LegacyAttributeMigration.IsGenerated(root.SyntaxTree, context.Document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider, context.CancellationToken) ||
            root.FindNode(context.Span).FirstAncestorOrSelf<AttributeSyntax>() is not { } attribute ||
            !LegacyAttributeMigration.TryGetMigration(attribute, model, context.CancellationToken, out _, out _, out bool review) ||
            review || LegacyAttributeMigration.HasInteriorDirectives(attribute.Parent!))
        {
            return;
        }

        bool? preserve = GetPreservationMode(context.Document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GlobalOptions);

        if (preserve != false)
        {
            Register(preserveVersion2: true);
        }

        if (preserve != true)
        {
            Register(preserveVersion2: false);
        }

        void Register(bool preserveVersion2)
        {
            context.RegisterCodeFix(CodeAction.Create(
                title: preserveVersion2 ? "Migrate attribute and preserve CsWinRT 2.x" : "Migrate attribute for CsWinRT 3.0 only",
                createChangedDocument: token => MigrateAsync(context.Document, context.Diagnostics, preserveVersion2, token),
                equivalenceKey: preserveVersion2 ? PreserveKey : DirectKey), context.Diagnostics);
        }
    }

    /// <summary>Determines retained-target intent without inferring it from the active compilation alone.</summary>
    /// <param name="options">The evaluated project properties.</param>
    /// <returns>True for retained legacy targets, false for known 3.0-only targets, or null if unknown.</returns>
    internal static bool? GetPreservationMode(AnalyzerConfigOptions options)
    {
        return options.TryGetValue("build_property.CsWinRTMigrationTargetFrameworks", out string? frameworks) &&
            !string.IsNullOrWhiteSpace(frameworks)
            ? frameworks.Split([','], StringSplitOptions.RemoveEmptyEntries).Any(static framework =>
        {
            string value = framework.Trim();
            int windows = value.IndexOf("-windows", StringComparison.OrdinalIgnoreCase);
            int dot = value.IndexOf('.');

            return windows < 0 || dot < 3 || !int.TryParse(value.Substring(3, dot - 3), out int major) || major < 10 ||
                   !Version.TryParse(value.Substring(windows + 8), out Version? version) || version.Revision != 1;
        })
            : null;
    }

    /// <summary>Changes each attribute list once so Fix All cannot lose overlapping edits.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="diagnostics">The selected migration diagnostics.</param>
    /// <param name="preserveVersion2">Whether to retain the original 2.x branch.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>The migrated document.</returns>
    private static async Task<Document> MigrateAsync(
        Document document,
        ImmutableArray<Diagnostic> diagnostics,
        bool preserveVersion2,
        CancellationToken token)
    {
        SyntaxNode? root = await document.GetSyntaxRootAsync(token).ConfigureAwait(false);
        SemanticModel? model = await document.GetSemanticModelAsync(token).ConfigureAwait(false);

        if (root is null || model is null || !LegacyAttributeMigration.IsVersion3(root.SyntaxTree) ||
            LegacyAttributeMigration.IsGenerated(root.SyntaxTree, document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider, token) ||
            (!preserveVersion2 && GetPreservationMode(document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GlobalOptions) == true))
        {
            return document;
        }

        SourceText text = await document.GetTextAsync(token).ConfigureAwait(false);
        Dictionary<AttributeListSyntax, Dictionary<AttributeSyntax, string?>> lists = [];

        foreach (Diagnostic diagnostic in diagnostics)
        {
            token.ThrowIfCancellationRequested();

            if (diagnostic.Id != LegacyAttributeMigration.DiagnosticId ||
                root.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<AttributeSyntax>() is not { Parent: AttributeListSyntax list } attribute ||
                LegacyAttributeMigration.HasInteriorDirectives(list) ||
                !LegacyAttributeMigration.TryGetMigration(attribute, model, token, out _, out string? replacement, out bool review) ||
                review)
            {
                continue;
            }

            if (!lists.TryGetValue(list, out Dictionary<AttributeSyntax, string?>? selected))
            {
                lists.Add(list, selected = []);
            }

            selected[attribute] = replacement;
        }

        List<TextChange> changes = [];

        foreach (KeyValuePair<AttributeListSyntax, Dictionary<AttributeSyntax, string?>> pair in lists)
        {
            AttributeListSyntax list = pair.Key;
            AttributeListSyntax migrated = list;

            // Work backwards to retain stable indexes as selected attributes are removed
            for (int index = list.Attributes.Count - 1; index >= 0; index--)
            {
                if (!pair.Value.TryGetValue(list.Attributes[index], out string? replacement))
                {
                    continue;
                }

                AttributeSyntax current = migrated.Attributes[index];
                migrated = replacement is null
                    ? migrated.RemoveNode(current, SyntaxRemoveOptions.KeepExteriorTrivia)!
                    : migrated.ReplaceNode(current, current.WithName(SyntaxFactory.ParseName("global::" + replacement)));
            }

            string newList = migrated.Attributes.Count == 0 ? "" : migrated.ToString();
            string replacementText = preserveVersion2
                ? ConditionalText(text, list.Span, list.ToString(), newList)
                : newList;
            changes.Add(new TextChange(list.Span, replacementText));
        }

        if (changes.Count == 0)
        {
            return document;
        }

        Document changed = document.WithText(text.WithChanges(changes));
        return await CleanLegacyImportsAsync(changed, preserveVersion2, token).ConfigureAwait(false);
    }

    /// <summary>Emits balanced directives without moving existing surrounding directives or comments.</summary>
    /// <param name="text">The original source.</param>
    /// <param name="span">The replaced syntax span.</param>
    /// <param name="legacy">The original legacy syntax.</param>
    /// <param name="modern">The migrated syntax, or empty for removal.</param>
    /// <returns>The conditional source replacement.</returns>
    internal static string ConditionalText(SourceText text, TextSpan span, string legacy, string modern)
    {
        TextLine line = text.Lines.GetLineFromPosition(span.Start);
        string prefix = text.ToString(TextSpan.FromBounds(line.Start, span.Start));
        bool atLineStart = prefix.All(char.IsWhiteSpace);
        string indent = atLineStart ? prefix : "";
        string newline = text.ToString().Contains("\r\n") ? "\r\n" : "\n";
        StringBuilder result = new();

        if (!atLineStart)
        {
            _ = result.Append(newline);
        }

        _ = modern.Length != 0
            ? result.Append("#if CSWINRT3_0").Append(newline).Append(indent).Append(modern).Append(newline)
                .Append(indent).Append("#else").Append(newline)
            : result.Append("#if !CSWINRT3_0").Append(newline);

        return result.Append(indent).Append(legacy).Append(newline).Append(indent).Append("#endif").Append(newline).ToString();
    }

    /// <summary>Removes or guards dangling legacy imports only when no other compilation errors obscure their use.</summary>
    /// <param name="document">The migrated source.</param>
    /// <param name="preserveVersion2">Whether imports must remain available on 2.x.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>The document with any safely recognized dangling imports migrated.</returns>
    internal static async Task<Document> CleanLegacyImportsAsync(Document document, bool preserveVersion2, CancellationToken token)
    {
        SemanticModel model = (await document.GetSemanticModelAsync(token).ConfigureAwait(false))!;
        SyntaxNode root = (await document.GetSyntaxRootAsync(token).ConfigureAwait(false))!;
        UsingDirectiveSyntax[] imports = [.. root.DescendantNodes().OfType<UsingDirectiveSyntax>()
            .Where(static import => !LegacyAttributeMigration.HasInteriorDirectives(import) &&
                (import.Name?.ToString() is "WinRT" or "System.Runtime.InteropServices.WindowsRuntime" ||
                 import.Name?.ToString() is "System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArrayAttribute" or
                     "System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArrayAttribute" ||
                 import.Name?.ToString().StartsWith("WinRT.", StringComparison.Ordinal) == true))];
        Diagnostic[] errors = [.. model.GetDiagnostics(cancellationToken: token)
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)];

        if (errors.Length == 0 || errors.Any(error => !imports.Any(import => import.Span.Contains(error.Location.SourceSpan))))
        {
            return document;
        }

        SourceText text = await document.GetTextAsync(token).ConfigureAwait(false);
        return document.WithText(text.WithChanges(imports.Where(import => errors.Any(error => import.Span.Contains(error.Location.SourceSpan)))
            .Select(import => new TextChange(import.Span, preserveVersion2
                ? ConditionalText(text, import.Span, import.ToString(), "")
                : ""))));
    }

    /// <summary>Applies coherent attribute-list edits across the requested scope.</summary>
    private sealed class AttributeFixAllProvider : FixAllProvider
    {
        /// <inheritdoc/>
        public override Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
        {
            bool preserve = fixAllContext.CodeActionEquivalenceKey == PreserveKey;
            CodeAction action = CodeAction.Create(
                preserve ? "Migrate attributes and preserve CsWinRT 2.x" : "Migrate attributes for CsWinRT 3.0 only",
                async token =>
                {
                    Solution solution = fixAllContext.Solution;
                    IEnumerable<Document> documents = fixAllContext.Scope switch
                    {
                        FixAllScope.Document => [fixAllContext.Document!],
                        FixAllScope.Project => fixAllContext.Project.Documents,
                        FixAllScope.Solution => solution.Projects.SelectMany(static project => project.Documents),
                        FixAllScope.Custom or FixAllScope.ContainingMember or FixAllScope.ContainingType => [],
                        _ => []
                    };

                    foreach (Document document in documents)
                    {
                        token.ThrowIfCancellationRequested();
                        ImmutableArray<Diagnostic> diagnostics = await fixAllContext.GetDocumentDiagnosticsAsync(document).ConfigureAwait(false);
                        Document changed = await MigrateAsync(document, diagnostics, preserve, token).ConfigureAwait(false);
                        solution = solution.WithDocumentText(document.Id, await changed.GetTextAsync(token).ConfigureAwait(false));
                    }

                    return solution;
                },
                preserve ? PreserveKey : DirectKey);

            return Task.FromResult<CodeAction?>(action);
        }
    }
}
