// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.CodeAnalysis;

#pragma warning disable CS1734

namespace WindowsRuntime.SourceGenerator;

/// <summary>
/// Extensions for <see cref="ITypeSymbol"/>.
/// </summary>
internal static class ITypeSymbolExtensions
{
    extension(ITypeSymbol symbol)
    {
        /// <summary>
        /// Gets the fully qualified metadata name for a given <see cref="ITypeSymbol"/> instance.
        /// </summary>
        /// <returns>The fully qualified metadata name for <paramref name="symbol"/>.</returns>
        public string GetFullyQualifiedMetadataName()
        {
            using PooledArrayBuilder<char> builder = new();

            symbol.AppendFullyQualifiedMetadataName(in builder);

            return builder.ToString();
        }

        /// <summary>
        /// Appends the fully qualified metadata name for a given symbol to a target builder.
        /// </summary>
        /// <param name="builder">The target <see cref="PooledArrayBuilder{T}"/> instance.</param>
        public void AppendFullyQualifiedMetadataName(ref readonly PooledArrayBuilder<char> builder)
        {
            static void BuildFrom(ISymbol? symbol, ref readonly PooledArrayBuilder<char> builder)
            {
                switch (symbol)
                {
                    // Namespaces that are nested also append a leading '.'
                    case INamespaceSymbol { ContainingNamespace.IsGlobalNamespace: false }:
                        BuildFrom(symbol.ContainingNamespace, in builder);
                        builder.Add('.');
                        builder.AddRange(symbol.MetadataName.AsSpan());
                        break;

                    // Other namespaces (ie. the one right before global) skip the leading '.'
                    case INamespaceSymbol { IsGlobalNamespace: false }:
                        builder.AddRange(symbol.MetadataName.AsSpan());
                        break;

                    // Types with no namespace just have their metadata name directly written
                    case ITypeSymbol { ContainingSymbol: INamespaceSymbol { IsGlobalNamespace: true } }:
                        builder.AddRange(symbol.MetadataName.AsSpan());
                        break;

                    // Types with a containing non-global namespace also append a leading '.'
                    case ITypeSymbol { ContainingSymbol: INamespaceSymbol namespaceSymbol }:
                        BuildFrom(namespaceSymbol, in builder);
                        builder.Add('.');
                        builder.AddRange(symbol.MetadataName.AsSpan());
                        break;

                    // Nested types append a leading '+'
                    case ITypeSymbol { ContainingSymbol: ITypeSymbol typeSymbol }:
                        BuildFrom(typeSymbol, in builder);
                        builder.Add('+');
                        builder.AddRange(symbol.MetadataName.AsSpan());
                        break;
                    default:
                        break;
                }
            }

            BuildFrom(symbol, in builder);
        }
    }
}