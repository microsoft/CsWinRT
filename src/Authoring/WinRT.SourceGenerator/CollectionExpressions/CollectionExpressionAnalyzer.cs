// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if ROSLYN_4_12_0_OR_GREATER

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
            // We only need to emit warnings if CsWinRT is in 'auto' mode
            if (!GeneratorExecutionContextHelper.IsCsWinRTAotOptimizerInAutoMode(context.Options.AnalyzerConfigOptionsProvider, context.Compilation))
            {
                return;
            }

            // Get the symbols for '[CollectionBuilder]', we need them for lookups. Note that we cannot just
            // use 'GetTypeByMetadataName' here, as it's possible for the attribute to exist across multiple
            // assemblies. This is the case if any referenced assemblies is using polyfills due to targeting
            // an older TFM that does not have the attribute. We still want to work correctly in those cases.
            // We can just use an array here, since in the vast majority of cases we only expect 1-2 items.
            ImmutableArray<INamedTypeSymbol> collectionBuilderSymbols = context.Compilation.GetTypesByMetadataName("System.Runtime.CompilerServices.CollectionBuilderAttribute");

            context.RegisterOperationAction(context =>
            {
                ICollectionExpressionOperation operation = (ICollectionExpressionOperation)context.Operation;

                // We only possibly warn if the target type is a generic interface type
                if (operation.Type is not INamedTypeSymbol { TypeKind: TypeKind.Interface, IsGenericType: true } typeSymbol)
                {
                    return;
                }

                // We can also skip all cases where the collection expression is empty, those are fine
                if (operation.Elements.IsEmpty)
                {
                    return;
                }

                // We can skip 'ICollection<T>' and 'IList<T>', as those guarantee to use 'List<T>'
                if (typeSymbol.ConstructedFrom.SpecialType is
                    SpecialType.System_Collections_Generic_ICollection_T or
                    SpecialType.System_Collections_Generic_IList_T)
                {
                    return;
                }

                // If the target interface type doesn't have '[CollectionBuilder]' on it, we should warn
                if (!GeneratorHelper.HasAttributeWithAnyType(typeSymbol, collectionBuilderSymbols))
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

#endif
