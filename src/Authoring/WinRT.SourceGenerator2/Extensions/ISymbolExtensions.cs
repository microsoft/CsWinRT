// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

#pragma warning disable CS1734

namespace WindowsRuntime.SourceGenerator;

/// <summary>
/// Extensions for <see cref="ISymbol"/>.
/// </summary>
internal static class ISymbolExtensions
{
    extension(ISymbol symbol)
    {
        /// <summary>
        /// Checks whether a type has an attribute with a specified type.
        /// </summary>
        /// <param name="typeSymbol">The <see cref="ITypeSymbol"/> instance for the attribute type to look for.</param>
        /// <returns>Whether or not <paramref name="symbol"/> has an attribute with the specified name.</returns>
        public bool HasAttributeWithType(ITypeSymbol typeSymbol)
        {
            return TryGetAttributeWithType(symbol, typeSymbol, out _);
        }

        /// <summary>
        /// Tries to get an attribute with the specified type.
        /// </summary>
        /// <param name="typeSymbol">The <see cref="ITypeSymbol"/> instance for the attribute type to look for.</param>
        /// <param name="attributeData">The resulting attribute, if it was found.</param>
        /// <returns>Whether or not <paramref name="symbol"/> has an attribute with the specified name.</returns>
        public bool TryGetAttributeWithType(ITypeSymbol typeSymbol, [NotNullWhen(true)] out AttributeData? attributeData)
        {
            foreach (AttributeData attribute in symbol.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, typeSymbol))
                {
                    attributeData = attribute;

                    return true;
                }
            }

            attributeData = null;

            return false;
        }
    }
}