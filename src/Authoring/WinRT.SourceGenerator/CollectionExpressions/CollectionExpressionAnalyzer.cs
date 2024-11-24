// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Operations;
using WinRT.SourceGenerator;

#nullable enable

namespace Generator;

/// <summary>
/// A diagnostic analyzer to warn for collection expression that are not AOT compatible in WinRT scenarios.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CollectionExpressionAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [WinRTRules.NonEmptyCollectionExpressionTargetingNonBuilderInterfaceType];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static context =>
        {
            // Get the symbol for '[CollectionBuilder]', we need it for lookups
            if (context.Compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.CollectionBuilderAttribute") is not { } collectionBuilderSymbol)
            {
                return;
            }

            context.RegisterOperationAction(context =>
            {
                ICollectionExpressionOperation operation = (ICollectionExpressionOperation)context.Operation;

                // We only possibly warn if the target type is an interface type
                if (operation.Type is not INamedTypeSymbol { TypeKind: TypeKind.Interface } typeSymbol)
                {
                    return;
                }

                // We can also skip all cases where the collection expression is empty, those are fine
                if (operation.Elements.IsEmpty)
                {
                    return;
                }

                // If the target interface type doesn't have '[CollectionBuilder]' on it, we should warn
                if (!GeneratorHelper.HasAttributeWithType(typeSymbol, collectionBuilderSymbol))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        WinRTRules.NonEmptyCollectionExpressionTargetingNonBuilderInterfaceType,
                        operation.Syntax.GetLocation(),
                        typeSymbol));
                }
            }, OperationKind.CollectionExpression);
        });
    }
}
