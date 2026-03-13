// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WindowsRuntime.SourceGenerator.Diagnostics;

/// <summary>
/// A diagnostic analyzer that validates the arguments of <c>[GeneratedCustomPropertyProvider]</c> when
/// using the constructor taking explicit property names and indexer types.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class GeneratedCustomPropertyProviderAttributeArgumentAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [
        DiagnosticDescriptors.GeneratedCustomPropertyProviderNullPropertyName,
        DiagnosticDescriptors.GeneratedCustomPropertyProviderNullIndexerType,
        DiagnosticDescriptors.GeneratedCustomPropertyProviderPropertyNameNotFound,
        DiagnosticDescriptors.GeneratedCustomPropertyProviderIndexerTypeNotFound,
        DiagnosticDescriptors.GeneratedCustomPropertyProviderStaticIndexer];

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

                // Get the attribute instance, if present
                if (!typeSymbol.TryGetAttributeWithType(attributeType, out AttributeData? attribute))
                {
                    return;
                }

                // Only validate when using the constructor with explicit property names and indexer types
                if (attribute.ConstructorArguments is not [
                    { Kind: TypedConstantKind.Array, Values: var typedPropertyNames },
                    { Kind: TypedConstantKind.Array, Values: var typedIndexerTypes }])
                {
                    return;
                }

                // Validate all property name arguments
                foreach (TypedConstant typedName in typedPropertyNames)
                {
                    // Check that we have a valid 'string' value
                    if (typedName.Value is not string propertyName)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.GeneratedCustomPropertyProviderNullPropertyName,
                            typeSymbol.Locations.FirstOrDefault(),
                            typeSymbol));

                        continue;
                    }

                    // Check whether any public, non-override, non-partial-impl, non-indexer property has this name
                    if (!HasAccessiblePropertyWithName(typeSymbol, propertyName))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.GeneratedCustomPropertyProviderPropertyNameNotFound,
                            typeSymbol.Locations.FirstOrDefault(),
                            propertyName,
                            typeSymbol));
                    }
                }

                // Validate all indexer type arguments
                foreach (TypedConstant typedType in typedIndexerTypes)
                {
                    // Check that we have a valid 'Type' value (the parameter type of the target indexer)
                    if (typedType.Value is not ITypeSymbol indexerType)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.GeneratedCustomPropertyProviderNullIndexerType,
                            typeSymbol.Locations.FirstOrDefault(),
                            typeSymbol));

                        continue;
                    }

                    // Check whether there's a static indexer with a matching parameter type (which wouldn't be usable)
                    if (HasStaticIndexerWithParameterType(typeSymbol, indexerType))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.GeneratedCustomPropertyProviderStaticIndexer,
                            typeSymbol.Locations.FirstOrDefault(),
                            indexerType,
                            typeSymbol));
                    }
                    else if (!HasAccessibleIndexerWithParameterType(typeSymbol, indexerType))
                    {
                        // No matching instance or static indexer at all
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.GeneratedCustomPropertyProviderIndexerTypeNotFound,
                            typeSymbol.Locations.FirstOrDefault(),
                            indexerType,
                            typeSymbol));
                    }
                }
            }, SymbolKind.NamedType);
        });
    }

    /// <summary>
    /// Checks whether a type has an accessible (public, non-override, non-indexer) property with a given name.
    /// </summary>
    /// <param name="typeSymbol">The type to inspect.</param>
    /// <param name="propertyName">The property name to look for.</param>
    /// <returns>Whether a matching property exists.</returns>
    private static bool HasAccessiblePropertyWithName(INamedTypeSymbol typeSymbol, string propertyName)
    {
        foreach (ISymbol symbol in typeSymbol.EnumerateAllMembers())
        {
            // Filter to public properties that we might care about (we ignore indexers here)
            if (symbol is not IPropertySymbol { DeclaredAccessibility: Accessibility.Public, IsOverride: false, PartialDefinitionPart: null, IsIndexer: false } property)
            {
                continue;
            }

            if (property.Name.Equals(propertyName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks whether a type has an accessible (public, non-override, non-static, single-parameter) indexer
    /// with a parameter matching a given type.
    /// </summary>
    /// <param name="typeSymbol">The type to inspect.</param>
    /// <param name="indexerType">The indexer parameter type to look for.</param>
    /// <returns>Whether a matching indexer exists.</returns>
    private static bool HasAccessibleIndexerWithParameterType(INamedTypeSymbol typeSymbol, ITypeSymbol indexerType)
    {
        foreach (ISymbol symbol in typeSymbol.EnumerateAllMembers())
        {
            // Same filtering as above, but we also exclude static members, and we only look for indexers
            if (symbol is not IPropertySymbol { DeclaredAccessibility: Accessibility.Public, IsOverride: false, PartialDefinitionPart: null, IsIndexer: true, IsStatic: false } property)
            {
                continue;
            }

            // Check that we have a single parameter, that matches theone we're looking for
            if (property.Parameters is [{ } parameter] && SymbolEqualityComparer.Default.Equals(parameter.Type, indexerType))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks whether a type has a static indexer with a parameter matching a given type.
    /// This is used to provide a more specific diagnostic when the indexer exists but is static.
    /// </summary>
    /// <param name="typeSymbol">The type to inspect.</param>
    /// <param name="indexerType">The indexer parameter type to look for.</param>
    /// <returns>Whether a matching static indexer exists.</returns>
    private static bool HasStaticIndexerWithParameterType(INamedTypeSymbol typeSymbol, ITypeSymbol indexerType)
    {
        foreach (ISymbol symbol in typeSymbol.EnumerateAllMembers())
        {
            // Same filtering as above, but this time including static properties
            if (symbol is not IPropertySymbol { DeclaredAccessibility: Accessibility.Public, IsOverride: false, PartialDefinitionPart: null, IsIndexer: true, IsStatic: true } property)
            {
                continue;
            }

            // Validate the parameter type (same as above)
            if (property.Parameters is [{ } parameter] && SymbolEqualityComparer.Default.Equals(parameter.Type, indexerType))
            {
                return true;
            }
        }

        return false;
    }
}
