// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WindowsRuntime.SourceGenerator.Diagnostics;

/// <summary>
/// A diagnostic analyzer that validates when <c>[GeneratedCustomPropertyProvider]</c> is used but no interface is available.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class GeneratedCustomPropertyProviderNoAvailableInterfaceTypeAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [DiagnosticDescriptors.GeneratedCustomPropertyProviderNoAvailableInterfaceType];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static context =>
        {
            // Get the '[GeneratedCustomPropertyProvider]' symbol
            if (context.Compilation.GetTypeByMetadataName("WindowsRuntime.Xaml.GeneratedCustomPropertyProviderAttribute") is not { } attributeType)
            {
                return;
            }

            // Try to get any 'ICustomPropertyProvider' symbol
            INamedTypeSymbol? windowsUIXamlCustomPropertyProviderType = context.Compilation.GetTypeByMetadataName("Windows.UI.Xaml.Data.ICustomPropertyProvider");
            INamedTypeSymbol? microsoftUIXamlCustomPropertyProviderType = context.Compilation.GetTypeByMetadataName("Microsoft.UI.Xaml.Data.ICustomPropertyProvider");

            // If we have either of them, we'll never need to report any diagnostics
            if (windowsUIXamlCustomPropertyProviderType is not null || microsoftUIXamlCustomPropertyProviderType is not null)
            {
                return;
            }

            context.RegisterSymbolAction(context =>
            {
                // Only classes and structs can be targets of the attribute
                if (context.Symbol is not INamedTypeSymbol { TypeKind: TypeKind.Class or TypeKind.Struct } typeSymbol)
                {
                    return;
                }

                // Emit a diagnostic if the type has the attribute, as it can't be used now
                if (typeSymbol.HasAttributeWithType(attributeType))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.GeneratedCustomPropertyProviderNoAvailableInterfaceType,
                        typeSymbol.Locations.FirstOrDefault(),
                        typeSymbol));
                }
            }, SymbolKind.NamedType);
        });
    }
}