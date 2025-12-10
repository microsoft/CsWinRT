// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

#pragma warning disable CS1734, IDE0046

namespace WindowsRuntime.SourceGenerator;

/// <summary>
/// Extensions for <see cref="ITypeSymbol"/>.
/// </summary>
internal static class ITypeSymbolExtensions
{
    extension(ITypeSymbol symbol)
    {
        /// <summary>
        /// Gets a value indicating whether the given <see cref="ITypeSymbol"/> can be boxed.
        /// </summary>
        public bool CanBeBoxed
        {
            get
            {
                // Byref-like types can't be boxed, and same for all kinds of pointers
                if (symbol.IsRefLikeType || symbol.TypeKind is TypeKind.Pointer or TypeKind.FunctionPointer)
                {
                    return false;
                }

                // Type parameters with 'allows ref struct' also can't be boxed
                if (symbol is ITypeParameterSymbol { AllowsRefLikeType: true })
                {
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Enumerates all members of a given <see cref="ITypeSymbol"/> instance, including inherited ones.
        /// </summary>
        /// <returns>The sequence of all member symbols for <paramref name="symbol"/>.</returns>
        public IEnumerable<ISymbol> EnumerateAllMembers()
        {
            for (ITypeSymbol? currentSymbol = symbol;
                currentSymbol is not (null or { SpecialType: SpecialType.System_ValueType or SpecialType.System_Object });
                currentSymbol = currentSymbol.BaseType)
            {
                foreach (ISymbol currentMember in currentSymbol.GetMembers())
                {
                    yield return currentMember;
                }
            }
        }

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