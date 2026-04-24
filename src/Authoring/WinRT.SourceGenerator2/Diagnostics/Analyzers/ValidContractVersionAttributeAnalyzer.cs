// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

                    if (isVersionOnlyConstructor)
                    {
                        // The version-only constructors must be applied to API contract types
                        if (!isApiContractType)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                DiagnosticDescriptors.ContractVersionAttributeRequiresApiContractTarget,
                                GetAttributeLocation(attribute, context.CancellationToken) ?? typeSymbol.Locations.FirstOrDefault(),
                                typeSymbol));
                        }
                    }
                    else if (isContractTypeConstructor)
                    {
                        // The contract-type constructor must NOT be applied to API contract types
                        if (isApiContractType)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                DiagnosticDescriptors.ContractVersionAttributeNotAllowedOnApiContractTarget,
                                GetAttributeLocation(attribute, context.CancellationToken) ?? typeSymbol.Locations.FirstOrDefault(),
                                typeSymbol));
                        }

                        // The contract type argument must be a valid API contract type
                        if (attribute.ConstructorArguments is [{ Value: INamedTypeSymbol contractTypeArgument }, ..] &&
                            !IsApiContractType(contractTypeArgument, apiContractAttributeType))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                DiagnosticDescriptors.ContractVersionAttributeInvalidContractTypeArgument,
                                GetAttributeArgumentLocation(attribute, argumentIndex: 0, context.CancellationToken)
                                    ?? GetAttributeLocation(attribute, context.CancellationToken)
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

    /// <summary>
    /// Gets the location of the syntax node where an attribute is applied.
    /// </summary>
    /// <param name="attribute">The attribute to locate.</param>
    /// <param name="cancellationToken">The cancellation token to use.</param>
    /// <returns>The location of the attribute application, or <see langword="null"/> if it cannot be determined.</returns>
    private static Location? GetAttributeLocation(AttributeData attribute, CancellationToken cancellationToken)
    {
        return attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation();
    }

    /// <summary>
    /// Gets the location of a specific positional argument of an attribute application.
    /// </summary>
    /// <param name="attribute">The attribute to locate.</param>
    /// <param name="argumentIndex">The index of the positional argument.</param>
    /// <param name="cancellationToken">The cancellation token to use.</param>
    /// <returns>The location of the argument, or <see langword="null"/> if it cannot be determined.</returns>
    private static Location? GetAttributeArgumentLocation(AttributeData attribute, int argumentIndex, CancellationToken cancellationToken)
    {
        return attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken) is AttributeSyntax { ArgumentList.Arguments: { } arguments } && argumentIndex < arguments.Count
            ? arguments[argumentIndex].GetLocation()
            : null;
    }
}
