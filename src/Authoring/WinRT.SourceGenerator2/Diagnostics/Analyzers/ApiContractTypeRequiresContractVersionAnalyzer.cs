// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WindowsRuntime.SourceGenerator.Diagnostics;

/// <summary>
/// A diagnostic analyzer that validates that <c>[ApiContract]</c> enum types declare their contract version using <c>[ContractVersion]</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ApiContractTypeRequiresContractVersionAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [DiagnosticDescriptors.ApiContractTypeMissingContractVersion];

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
            if (context.Compilation.GetTypeByMetadataName("Windows.Foundation.Metadata.ApiContractAttribute") is not { } apiContractAttributeType)
            {
                return;
            }

            // Get the '[ContractVersion]' symbol
            if (context.Compilation.GetTypeByMetadataName("Windows.Foundation.Metadata.ContractVersionAttribute") is not { } contractVersionAttributeType)
            {
                return;
            }

            context.RegisterSymbolAction(context =>
            {
                // Only enum types can be valid API contract types
                if (context.Symbol is not INamedTypeSymbol { TypeKind: TypeKind.Enum } typeSymbol)
                {
                    return;
                }

                // Immediately bail if the type is not an API contract type
                if (!typeSymbol.HasAttributeWithType(apiContractAttributeType))
                {
                    return;
                }

                // Check whether any '[ContractVersion]' attribute using a version-only constructor is applied
                foreach (AttributeData attribute in typeSymbol.GetAttributes())
                {
                    if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, contractVersionAttributeType))
                    {
                        continue;
                    }

                    // The version-only constructors are '(uint)' and '(string, uint)'.
                    // The contract-type constructor is '(Type, uint)', which we want to ignore here.
                    if (attribute.AttributeConstructor?.Parameters is [{ Type.SpecialType: SpecialType.System_UInt32 or SpecialType.System_String }, ..])
                    {
                        return;
                    }
                }

                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ApiContractTypeMissingContractVersion,
                    typeSymbol.Locations.FirstOrDefault(),
                    typeSymbol));
            }, SymbolKind.NamedType);
        });
    }
}
