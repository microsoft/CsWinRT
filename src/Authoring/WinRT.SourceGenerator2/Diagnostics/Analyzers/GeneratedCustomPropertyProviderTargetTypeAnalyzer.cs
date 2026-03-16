// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WindowsRuntime.SourceGenerator.Diagnostics;

/// <summary>
/// A diagnostic analyzer that validates target types for <c>[GeneratedCustomPropertyProvider]</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class GeneratedCustomPropertyProviderTargetTypeAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [
        DiagnosticDescriptors.GeneratedCustomPropertyProviderInvalidTargetType,
        DiagnosticDescriptors.GeneratedCustomPropertyProviderMissingPartialModifier];

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

            context.RegisterSymbolAction(context =>
            {
                // Only classes and structs can be targets of the attribute
                if (context.Symbol is not INamedTypeSymbol { TypeKind: TypeKind.Class or TypeKind.Struct } typeSymbol)
                {
                    return;
                }

                // Immediately bail if the type doesn't have the attribute
                if (!typeSymbol.HasAttributeWithType(attributeType))
                {
                    return;
                }

                // If the type is static, abstract, or 'ref', it isn't valid
                if (typeSymbol.IsAbstract || typeSymbol.IsStatic || typeSymbol.IsRefLikeType)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.GeneratedCustomPropertyProviderInvalidTargetType,
                        typeSymbol.Locations.FirstOrDefault(),
                        typeSymbol));
                }

                // Try to get a syntax reference for the symbol, to resolve the syntax node for it
                if (typeSymbol.DeclaringSyntaxReferences.FirstOrDefault() is SyntaxReference syntaxReference)
                {
                    SyntaxNode typeNode = syntaxReference.GetSyntax(context.CancellationToken);

                    // If there's no 'partial' modifier in the type hierarchy, the target type isn't valid
                    if (!((MemberDeclarationSyntax)typeNode).IsPartialAndWithinPartialTypeHierarchy)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.GeneratedCustomPropertyProviderMissingPartialModifier,
                            typeSymbol.Locations.FirstOrDefault(),
                            typeSymbol));
                    }
                }
            }, SymbolKind.NamedType);
        });
    }
}