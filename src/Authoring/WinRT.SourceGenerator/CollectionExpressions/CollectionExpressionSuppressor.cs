// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using WinRT.SourceGenerator;

#nullable enable

namespace Generator;

/// <summary>
/// A diagnostic suppressor to suppress collection expression warnings where needed for AOT compatibility in WinRT scenarios.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CollectionExpressionSuppressor : DiagnosticSuppressor
{
    /// <inheritdoc/>
    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } =
    [
        WinRTSuppressions.CollectionExpressionIDE0303,
        WinRTSuppressions.CollectionExpressionIDE0304,
        WinRTSuppressions.CollectionExpressionIDE0305
    ];

    /// <inheritdoc/>
    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        foreach (Diagnostic diagnostic in context.ReportedDiagnostics)
        {
            // Try to get the syntax node matching the location of the diagnostic
            if (diagnostic.Location.SourceTree?.GetRoot(context.CancellationToken).FindNode(diagnostic.Location.SourceSpan) is not CollectionExpressionSyntax syntaxNode)
            {
                continue;
            }

            // If the collection expression has no elements, we want to keep the diagnostic
            if (!syntaxNode.Elements.Any())
            {
                continue;
            }

            SemanticModel semanticModel = context.GetSemanticModel(syntaxNode.SyntaxTree);

            // Try to get the operation for the collection expression, skip if we can't
            if (semanticModel.GetOperation(syntaxNode, context.CancellationToken) is not ICollectionExpressionOperation operation)
            {
                continue;
            }

            // Try to suppress all supported diagnostics
            if (ShouldSuppressIDE0303OrIDE0304OrIDE0305(operation, semanticModel.Compilation))
            {
                SuppressionDescriptor descriptor = diagnostic.Id switch
                {
                    "IDE0303" => WinRTSuppressions.CollectionExpressionIDE0303,
                    "IDE0304" => WinRTSuppressions.CollectionExpressionIDE0304,
                    "IDE0305" => WinRTSuppressions.CollectionExpressionIDE0305,
                    _ => throw new Exception($"Unsupported diagnostic id: '{diagnostic.Id}'.")
                };

                context.ReportSuppression(Suppression.Create(descriptor, diagnostic));
            }
        }
    }

    /// <summary>
    /// Checks whether 'IDE0303', 'IDE0304', or 'IDE0305' should be suppressed for a given operation.
    /// </summary>
    private static bool ShouldSuppressIDE0303OrIDE0304OrIDE0305(ICollectionExpressionOperation operation, Compilation compilation)
    {
        // We only possibly suppress here for some interface types
        if (operation.Type is not INamedTypeSymbol { TypeKind: TypeKind.Interface } typeSymbol)
        {
            return false;
        }

        // Get the symbol for '[CollectionBuilder]', we need it for lookups
        if (compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.CollectionBuilderAttribute") is not { } collectionBuilderSymbol)
        {
            return false;
        }

        // We should only suppress when the target interface does not have '[CollectionBuilder]' on it.
        // If it does, then the CsWinRT analyzer will be able to analyze the body of the 'Create' method.
        return !GeneratorHelper.HasAttributeWithType(typeSymbol, collectionBuilderSymbol);
    }
}
