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
    /// <param name="symbol">The input <see cref="ISymbol"/> instance.</param>
    extension(ISymbol symbol)
    {
        /// <summary>
        /// Gets the fully qualified name for a given symbol.
        /// </summary>
        /// <returns>The fully qualified name for <paramref name="symbol"/>.</returns>
        public string GetFullyQualifiedName()
        {
            return symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        /// <summary>
        /// Gets the fully qualified name for a given symbol, including nullability annotations
        /// </summary>
        /// <returns>The fully qualified name for <paramref name="symbol"/>.</returns>
        public string GetFullyQualifiedNameWithNullabilityAnnotations()
        {
            return symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier));
        }

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

        /// <summary>
        /// Checks whether a given symbol is accessible from the assembly of a given compilation (including eg. through nested types).
        /// </summary>
        /// <param name="compilation">The <see cref="Compilation"/> instance currently in use.</param>
        /// <returns>Whether <paramref name="symbol"/> is accessible from the assembly for <paramref name="compilation"/>.</returns>
        public bool IsAccessibleFromCompilationAssembly(Compilation compilation)
        {
            return compilation.IsSymbolAccessibleWithin(symbol, compilation.Assembly);
        }
    }
}