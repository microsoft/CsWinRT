// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WindowsRuntime.SourceGenerator.Diagnostics;

/// <summary>
/// A diagnostic analyzer that validates that <c>[ApiContract]</c> enum types do not define any enum cases.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ValidApiContractEnumTypeAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [DiagnosticDescriptors.ApiContractEnumWithCases];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static context =>
        {
            // This analyzer only applies to Windows Runtime component authoring scenarios
            if (!context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.GetCsWinRTComponent())
            {
                return;
            }

            // Get the '[ApiContract]' symbol
            if (context.Compilation.GetTypeByMetadataName("Windows.Foundation.Metadata.ApiContractAttribute") is not { } attributeType)
            {
                return;
            }

            context.RegisterSymbolAction(context =>
            {
                // Only enum types can be annotated with '[ApiContract]'
                if (context.Symbol is not INamedTypeSymbol { TypeKind: TypeKind.Enum } typeSymbol)
                {
                    return;
                }

                // Immediately bail if the type doesn't have the attribute
                if (!typeSymbol.HasAttributeWithType(attributeType))
                {
                    return;
                }

                // Warn if the enum defines any enum cases (i.e. any fields other than the implicit value field)
                if (typeSymbol.GetMembers().Any(static member => member is IFieldSymbol { IsConst: true }))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.ApiContractEnumWithCases,
                        typeSymbol.Locations.FirstOrDefault(),
                        typeSymbol));
                }
            }, SymbolKind.NamedType);
        });
    }
}
