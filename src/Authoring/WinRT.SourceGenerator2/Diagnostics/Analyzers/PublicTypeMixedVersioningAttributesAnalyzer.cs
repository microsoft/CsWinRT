// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WindowsRuntime.SourceGenerator.Diagnostics;

/// <summary>
/// A diagnostic analyzer that reports when a public authored type has both a <c>[ContractVersion]</c>
/// and a <c>[Version]</c> attribute applied. Public types in a Windows Runtime component should
/// use only one of these two versioning schemes, applied consistently across the public API surface.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PublicTypeMixedVersioningAttributesAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [DiagnosticDescriptors.PublicTypeMixedVersioningAttributes];

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

            // Get the '[Version]' symbol
            if (context.Compilation.GetTypeByMetadataName("Windows.Foundation.Metadata.VersionAttribute") is not { } versionAttributeType)
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

                // Skip API contract types: those use '[ContractVersion]' with the version-only constructors
                // to declare their own contract version, which is a different scenario from this analyzer.
                if (apiContractAttributeType is not null &&
                    typeSymbol is { TypeKind: TypeKind.Enum } &&
                    typeSymbol.HasAttributeWithType(apiContractAttributeType))
                {
                    return;
                }

                // Only report if both '[ContractVersion]' and '[Version]' are applied to the type
                if (!typeSymbol.HasAttributeWithType(contractVersionAttributeType) ||
                    !typeSymbol.HasAttributeWithType(versionAttributeType))
                {
                    return;
                }

                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.PublicTypeMixedVersioningAttributes,
                    typeSymbol.Locations.FirstOrDefault(),
                    typeSymbol));
            }, SymbolKind.NamedType);
        });
    }
}
