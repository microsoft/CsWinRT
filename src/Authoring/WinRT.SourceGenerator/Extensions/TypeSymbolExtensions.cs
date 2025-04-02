// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// Ported from 'ITypeSymbolExtensions' in ComputeSharp (https://github.com/Sergio0694/ComputeSharp).
// Licensed under the MIT License (MIT) (see: https://github.com/Sergio0694/ComputeSharp?tab=MIT-1-ov-file).
// Source: https://github.com/Sergio0694/ComputeSharp/blob/main/src/ComputeSharp.SourceGeneration/Extensions/ITypeSymbolExtensions.cs.

using System;
using Microsoft.CodeAnalysis;

#nullable enable

namespace Generator;

/// <summary>
/// Extensions for type symbols.
/// </summary>
internal static class TypeSymbolExtensions
{
    /// <summary>
    /// Thread-local writer to build metadata names.
    /// </summary>
    [ThreadStatic]
    private static ArrayBufferWriter<char>? Writer;

    /// <summary>
    /// Gets the fully qualified metadata name for a given <see cref="ITypeSymbol"/> instance.
    /// </summary>
    /// <param name="symbol">The input <see cref="ITypeSymbol"/> instance.</param>
    /// <returns>The fully qualified metadata name for <paramref name="symbol"/>.</returns>
    public static string GetFullyQualifiedMetadataName(this ITypeSymbol symbol)
    {
        ArrayBufferWriter<char> writer = Writer ??= [];

        symbol.AppendFullyQualifiedMetadataName(writer);

        return writer.WrittenSpan.ToString();
    }

    /// <summary>
    /// Appends the fully qualified metadata name for a given symbol to a target builder.
    /// </summary>
    /// <param name="symbol">The input <see cref="ITypeSymbol"/> instance.</param>
    /// <param name="builder">The target <see cref="ArrayBufferWriter{T}"/> instance.</param>
    public static void AppendFullyQualifiedMetadataName(this ITypeSymbol symbol, ArrayBufferWriter<char> builder)
    {
        static void BuildFrom(ISymbol? symbol, ArrayBufferWriter<char> builder)
        {
            switch (symbol)
            {
                // Namespaces that are nested also append a leading '.'
                case INamespaceSymbol { ContainingNamespace.IsGlobalNamespace: false }:
                    BuildFrom(symbol.ContainingNamespace, builder);
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
                    BuildFrom(namespaceSymbol, builder);
                    builder.Add('.');
                    builder.AddRange(symbol.MetadataName.AsSpan());
                    break;

                // Nested types append a leading '+'
                case ITypeSymbol { ContainingSymbol: ITypeSymbol typeSymbol }:
                    BuildFrom(typeSymbol, builder);
                    builder.Add('+');
                    builder.AddRange(symbol.MetadataName.AsSpan());
                    break;
                default:
                    break;
            }
        }

        BuildFrom(symbol, builder);
    }
}
