// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WindowsRuntime.SourceGenerator.Diagnostics;

/// <summary>
/// A diagnostic analyzer that reports when a public authored type is missing version metadata
/// (i.e. is missing both a <c>[ContractVersion]</c> and a <c>[Version]</c> attribute).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PublicTypeRequiresVersioningAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [DiagnosticDescriptors.PublicTypeMissingVersioning];

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

            // Get the '[Version]' symbol (used to also accept '[Version]' as valid version metadata,
            // as an alternative to '[ContractVersion]' for declaring the version of a public type).
            INamedTypeSymbol? versionAttributeType = context.Compilation.GetTypeByMetadataName("Windows.Foundation.Metadata.VersionAttribute");

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
                // specific constructor used is reported by the other 'ContractVersion' analyzers).
                if (typeSymbol.HasAttributeWithType(contractVersionAttributeType))
                {
                    return;
                }

                // Skip if any '[Version]' attribute is applied: '[Version]' is an alternative versioning
                // metadata that also satisfies the requirement of declaring a version for the type.
                if (versionAttributeType is not null && typeSymbol.HasAttributeWithType(versionAttributeType))
                {
                    return;
                }

                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.PublicTypeMissingVersioning,
                    typeSymbol.Locations.FirstOrDefault(),
                    typeSymbol));
            }, SymbolKind.NamedType);
        });
    }
}
