// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WindowsRuntime.SourceGenerator.Models;

#pragma warning disable IDE0046

namespace WindowsRuntime.SourceGenerator;

/// <inheritdoc cref="CustomPropertyProviderGenerator"/>
public partial class CustomPropertyProviderGenerator
{
    /// <summary>
    /// Generation methods for <see cref="CustomPropertyProviderGenerator"/>.
    /// </summary>
    private static class Execute
    {
        /// <summary>
        /// Checks whether a target node needs the <c>ICustomPropertyProvider</c> implementation.
        /// </summary>
        /// <param name="node">The target <see cref="SyntaxNode"/> instance to check.</param>
        /// <param name="token">The cancellation token for the operation.</param>
        /// <returns>Whether <paramref name="node"/> is a valid target for the <c>ICustomPropertyProvider</c> implementation.</returns>
        [SuppressMessage("Style", "IDE0060", Justification = "The cancellation token is supplied by Roslyn.")]
        public static bool IsTargetNodeValid(SyntaxNode node, CancellationToken token)
        {
            // We only care about class and struct types, all other types are not valid targets
            if (!node.IsAnyKind(SyntaxKind.ClassDeclaration, SyntaxKind.RecordDeclaration, SyntaxKind.StructDeclaration, SyntaxKind.RecordStructDeclaration))
            {
                return false;
            }

            // If the type is static, abstract, or 'ref', we cannot implement 'ICustomPropertyProvider' on it
            if (((MemberDeclarationSyntax)node).Modifiers.ContainsAny(SyntaxKind.StaticKeyword, SyntaxKind.AbstractKeyword, SyntaxKind.RefKeyword))
            {
                return false;
            }

            // We can only generated the 'ICustomPropertyProvider' implementation if the type is 'partial'.
            // Additionally, all parent type declarations must also be 'partial', for generation to work.
            if (!((MemberDeclarationSyntax)node).IsPartialAndWithinPartialTypeHierarchy)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Tries to get the <see cref="CustomPropertyProviderInfo"/> instance for a given annotated symbol.
        /// </summary>
        /// <param name="context">The <see cref="GeneratorAttributeSyntaxContextWithOptions"/> value to use.</param>
        /// <param name="token">The cancellation token for the operation.</param>
        /// <returns>The resulting <see cref="CustomPropertyProviderInfo"/> instance, if processed successfully.</returns>
        public static CustomPropertyProviderInfo? GetCustomPropertyProviderInfo(GeneratorAttributeSyntaxContextWithOptions context, CancellationToken token)
        {
            bool useWindowsUIXamlProjections = context.GlobalOptions.GetBooleanProperty("CsWinRTUseWindowsUIXamlProjections");

            token.ThrowIfCancellationRequested();

            // Make sure that the target interface types are available. This is mostly because when UWP XAML projections
            // are not used, the target project must be referencing the WinUI package to get the right interface type.
            // If we can't find it, we just stop here. A separate diagnostic analyzer will emit the right diagnostic.
            if ((useWindowsUIXamlProjections && context.SemanticModel.Compilation.GetTypeByMetadataName("Windows.UI.Xaml.Data.ICustomPropertyProvider") is null) ||
                (!useWindowsUIXamlProjections && context.SemanticModel.Compilation.GetTypeByMetadataName("Microsoft.UI.Xaml.Data.ICustomPropertyProvider") is null))
            {
                return null;
            }

            token.ThrowIfCancellationRequested();

            // Ensure we have a valid named type symbol for the annotated type
            if (context.TargetSymbol is not INamedTypeSymbol typeSymbol)
            {
                return null;
            }

            // Get the type hierarchy (needed to correctly generate sources for nested types too)
            HierarchyInfo typeHierarchy = HierarchyInfo.From(typeSymbol);

            token.ThrowIfCancellationRequested();

            // Gather all custom properties, depending on how the attribute was used
            EquatableArray<CustomPropertyInfo> customProperties = GetCustomPropertyInfo(typeSymbol, context.Attributes[0], token);

            token.ThrowIfCancellationRequested();

            return new(
                TypeHierarchy: typeHierarchy,
                CustomProperties: customProperties,
                UseWindowsUIXamlProjections: useWindowsUIXamlProjections);
        }

        public static void WriteCustomPropertyProviderImplementations(SourceProductionContext context, CustomPropertyProviderInfo info)
        {
        }

        /// <summary>
        /// Gets the <see cref="CustomPropertyInfo"/> values for all applicable properties of a target type.
        /// </summary>
        /// <param name="typeSymbol">The annotated type.</param>
        /// <param name="attribute">The attribute to trigger generation.</param>
        /// <param name="token">The cancellation token for the operation.</param>
        /// <returns>The resulting <see cref="CustomPropertyInfo"/> values for <paramref name="typeSymbol"/>.</returns>
        private static EquatableArray<CustomPropertyInfo> GetCustomPropertyInfo(INamedTypeSymbol typeSymbol, AttributeData attribute, CancellationToken token)
        {
            string?[]? propertyNames = null;
            ITypeSymbol?[]? indexerTypes = null;

            token.ThrowIfCancellationRequested();

            // If using the attribute constructor taking explicit property names and indexer
            // types, get those names to filter the properties. We'll validate them later.
            if (attribute.ConstructorArguments is [
                { Kind: TypedConstantKind.Array, Values: var typedPropertyNames },
                { Kind: TypedConstantKind.Array, Values: var typedIndexerTypes }])
            {
                propertyNames = [.. typedPropertyNames.Select(tc => tc.Value as string)];
                indexerTypes = [.. typedIndexerTypes.Select(tc => tc.Value as ITypeSymbol)];
            }

            token.ThrowIfCancellationRequested();

            using PooledArrayBuilder<CustomPropertyInfo> customPropertyInfo = new();

            // Enumerate all members of the annotated type to discover all properties
            foreach (ISymbol symbol in typeSymbol.EnumerateAllMembers())
            {
                token.ThrowIfCancellationRequested();

                // Only gather public properties, and ignore overrides (we'll find the base definition instead).
                // We also ignore partial property implementations, as we only care about the partial definitions.
                if (symbol is not IPropertySymbol { DeclaredAccessibility: Accessibility.Public, IsOverride: false, PartialDefinitionPart: null } propertySymbol)
                {
                    continue;
                }

                // We can only support indexers with a single parameter.
                // If there's more, an analyzer will emit a warning.
                if (propertySymbol.Parameters.Length > 1)
                {
                    continue;
                }

                // Ignore the current property if we have explicit filters and the property doesn't match
                if ((propertySymbol.IsIndexer && indexerTypes?.Contains(propertySymbol.Parameters[0].Type, SymbolEqualityComparer.Default) is false) ||
                    (!propertySymbol.IsIndexer && propertyNames?.Contains(propertySymbol.Name, StringComparer.Ordinal) is false))
                {
                    continue;
                }

                // Gather all the info for the current property
                customPropertyInfo.Add(new CustomPropertyInfo(
                    Name: propertySymbol.Name,
                    FullyQualifiedTypeName: propertySymbol.Type.GetFullyQualifiedNameWithNullabilityAnnotations(),
                    FullyQualifiedIndexerTypeName: propertySymbol.Parameters.FirstOrDefault()?.GetFullyQualifiedNameWithNullabilityAnnotations(),
                    CanRead: propertySymbol.GetMethod is { DeclaredAccessibility: Accessibility.Public },
                    CanWrite: propertySymbol.SetMethod is { DeclaredAccessibility: Accessibility.Public },
                    IsStatic: propertySymbol.IsStatic));
            }

            token.ThrowIfCancellationRequested();

            return customPropertyInfo.ToImmutable();
        }
    }
}

internal sealed record CustomPropertyInfo(
    string Name,
    string FullyQualifiedTypeName,
    string? FullyQualifiedIndexerTypeName,
    bool CanRead,
    bool CanWrite,
    bool IsStatic);

internal sealed record CustomPropertyProviderInfo(
    HierarchyInfo TypeHierarchy,
    EquatableArray<CustomPropertyInfo> CustomProperties,
    bool UseWindowsUIXamlProjections);