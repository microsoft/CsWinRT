// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WindowsRuntime.SourceGenerator.Diagnostics;

/// <summary>
/// A diagnostic analyzer that validates applications of <c>[ContractVersion]</c> attributes.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ValidContractVersionAttributeAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [
        DiagnosticDescriptors.ContractVersionAttributeRequiresApiContractTarget,
        DiagnosticDescriptors.ContractVersionAttributeNotAllowedOnApiContractTarget,
        DiagnosticDescriptors.ContractVersionAttributeInvalidContractTypeArgument];

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

            // Get the '[ApiContract]' symbol
            if (context.Compilation.GetTypeByMetadataName("Windows.Foundation.Metadata.ApiContractAttribute") is not { } apiContractAttributeType)
            {
                return;
            }

            context.RegisterSymbolAction(context =>
            {
                if (context.Symbol is not INamedTypeSymbol typeSymbol)
                {
                    return;
                }

                bool isApiContractType = IsApiContractType(typeSymbol, apiContractAttributeType);

                foreach (AttributeData attribute in typeSymbol.GetAttributes())
                {
                    // We can have multiple '[ContractVersion]' uses, so we need to iterate and check each of them
                    if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, contractVersionAttributeType))
                    {
                        continue;
                    }

                    if (attribute.AttributeConstructor is not { } constructor)
                    {
                        continue;
                    }

                    ImmutableArray<IParameterSymbol> parameters = constructor.Parameters;

                    // Identify the constructor by its first parameter type:
                    //   - 'ContractVersionAttribute(uint)'
                    //   - 'ContractVersionAttribute(string, uint)'
                    //   - 'ContractVersionAttribute(Type, uint)'
                    bool isVersionOnlyConstructor = parameters is
                        [{ Type.SpecialType: SpecialType.System_UInt32 }] or
                        [{ Type.SpecialType: SpecialType.System_String }, _];

                    bool isContractTypeConstructor = parameters is
                        [{ Type: INamedTypeSymbol { MetadataName: "Type", ContainingNamespace.Name: "System" } }, _];

                    // The version-only constructors must be applied to API contract types
                    if (isVersionOnlyConstructor && !isApiContractType)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.ContractVersionAttributeRequiresApiContractTarget,
                            attribute.GetLocation(context.CancellationToken) ?? typeSymbol.Locations.FirstOrDefault(),
                            typeSymbol));
                    }
                    else if (isContractTypeConstructor)
                    {
                        // The contract-type constructor must not be applied to API contract types
                        if (isApiContractType)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                DiagnosticDescriptors.ContractVersionAttributeNotAllowedOnApiContractTarget,
                                attribute.GetLocation(context.CancellationToken) ?? typeSymbol.Locations.FirstOrDefault(),
                                typeSymbol));
                        }

                        // The contract type argument must be a valid API contract type
                        if (attribute.ConstructorArguments is [{ Value: INamedTypeSymbol contractTypeArgument }, ..] &&
                            !IsApiContractType(contractTypeArgument, apiContractAttributeType))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                DiagnosticDescriptors.ContractVersionAttributeInvalidContractTypeArgument,
                                attribute.GetArgumentLocation(argumentIndex: 0, context.CancellationToken)
                                    ?? attribute.GetLocation(context.CancellationToken)
                                    ?? typeSymbol.Locations.FirstOrDefault(),
                                typeSymbol,
                                contractTypeArgument));
                        }
                    }
                }
            }, SymbolKind.NamedType);
        });
    }

    /// <summary>
    /// Checks whether a type is a valid API contract type (an enum type annotated with <c>[ApiContract]</c>).
    /// </summary>
    /// <param name="typeSymbol">The type symbol to check.</param>
    /// <param name="apiContractAttributeType">The <c>[ApiContract]</c> attribute symbol.</param>
    /// <returns>Whether <paramref name="typeSymbol"/> is a valid API contract type.</returns>
    private static bool IsApiContractType(INamedTypeSymbol typeSymbol, INamedTypeSymbol apiContractAttributeType)
    {
        return typeSymbol is { TypeKind: TypeKind.Enum } && typeSymbol.HasAttributeWithType(apiContractAttributeType);
    }
}
