// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using WindowsRuntime.Migration;

namespace WindowsRuntime.SourceGenerator.Diagnostics;

/// <summary>
/// Reports legacy CsWinRT attributes only on the selected 3.0 source target.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LegacyWindowsRuntimeAttributeAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [DiagnosticDescriptors.LegacyWindowsRuntimeAttribute, DiagnosticDescriptors.LegacyWindowsRuntimeAttributeRequiresReview,
         DiagnosticDescriptors.LegacyArrayParameter];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static start =>
        {
            if (!start.Compilation.SyntaxTrees.Any(LegacyAttributeMigration.IsVersion3))
            {
                return;
            }

            HashSet<string> aliases = [.. start.Compilation.SyntaxTrees.SelectMany(tree =>
                tree.GetRoot(start.CancellationToken).DescendantNodes().OfType<UsingDirectiveSyntax>())
                .Where(static import => import.Alias is not null)
                .Select(static import => import.Alias!.Name.Identifier.ValueText)];

            start.RegisterSyntaxNodeAction(context =>
            {
                AttributeSyntax attribute = (AttributeSyntax)context.Node;
                string spelling = attribute.Name.GetLastToken().ValueText;

                if ((!LegacyAttributeMigration.IsCandidateName(spelling) && !aliases.Contains(spelling)) ||
                    !LegacyAttributeMigration.IsVersion3(attribute.SyntaxTree) ||
                    !LegacyAttributeMigration.TryGetMigration(attribute, context.SemanticModel, context.CancellationToken,
                        out string? name, out _, out bool review))
                {
                    return;
                }

                if (LegacyAttributeMigration.TryGetArraySpanName(attribute, context.SemanticModel, context.CancellationToken, out string? spanName))
                {
                    ParameterSyntax parameter = (ParameterSyntax)attribute.Parent!.Parent!;
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.LegacyArrayParameter,
                        attribute.GetLocation(),
                        parameter.Identifier.ValueText,
                        spanName == "System.ReadOnlySpan`1" ? "ReadOnlySpan<T>" : "Span<T>"));
                    return;
                }

                context.ReportDiagnostic(Diagnostic.Create(
                    review ? DiagnosticDescriptors.LegacyWindowsRuntimeAttributeRequiresReview : DiagnosticDescriptors.LegacyWindowsRuntimeAttribute,
                    attribute.GetLocation(),
                    name));
            }, SyntaxKind.Attribute);
        });
    }
}
