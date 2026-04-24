// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WindowsRuntime.SourceGenerator.Diagnostics;

/// <summary>
/// A diagnostic analyzer that reports when a public authored type is missing a <c>[ContractVersion]</c> attribute.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PublicTypeRequiresContractVersionAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [DiagnosticDescriptors.PublicTypeMissingContractVersion];

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

            // Get the '[ContractVersion]' symbol
            if (context.Compilation.GetTypeByMetadataName("Windows.Foundation.Metadata.ContractVersionAttribute") is not { } contractVersionAttributeType)
            {
                return;
            }

            // Get the '[ApiContract]' symbol (used to skip API contract types, which are validated by a different analyzer)
            INamedTypeSymbol? apiContractAttributeType = context.Compilation.GetTypeByMetadataName("Windows.Foundation.Metadata.ApiContractAttribute");

            context.RegisterSymbolAction(context =>
            {
                // Only consider top-level public types
                if (context.Symbol is not INamedTypeSymbol { DeclaredAccessibility: Accessibility.Public, ContainingType: null } typeSymbol)
                {
                    return;
                }

                // Skip API contract types: those are handled by 'ApiContractTypeRequiresContractVersionAnalyzer'
                if (apiContractAttributeType is not null &&
                    typeSymbol is { TypeKind: TypeKind.Enum } &&
                    typeSymbol.HasAttributeWithType(apiContractAttributeType))
                {
                    return;
                }

                // Skip if any '[ContractVersion]' attribute is already applied (validity of the
                // specific constructor used is reported by the other 'ContractVersion' analyzers)
                if (typeSymbol.HasAttributeWithType(contractVersionAttributeType))
                {
                    return;
                }

                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.PublicTypeMissingContractVersion,
                    typeSymbol.Locations.FirstOrDefault(),
                    typeSymbol));
            }, SymbolKind.NamedType);
        });
    }
}
