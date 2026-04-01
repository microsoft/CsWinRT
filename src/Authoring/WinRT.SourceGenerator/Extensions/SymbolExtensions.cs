using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;

#nullable enable

namespace Generator;

/// <summary>
/// Extensions for symbol types.
/// </summary>
internal static class SymbolExtensions
{
    /// <summary>
    /// Checks whether a given type symbol is publicly accessible (ie. it's public and not nested in any non public type).
    /// </summary>
    /// <param name="type">The type symbol to check for public accessibility.</param>
    /// <returns>Whether <paramref name="type"/> is publicly accessible.</returns>
    public static bool IsPubliclyAccessible(this ITypeSymbol type)
    {
        for (ITypeSymbol? currentType = type; currentType is not null; currentType = currentType.ContainingType)
        {
            // If any type in the type hierarchy is not public, the type is not public.
            // This makes sure to detect public types nested into eg. a private type.
            if (currentType.DeclaredAccessibility is not Accessibility.Public)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks whether a given symbol is an explicit interface implementation of a member of an internal interface (or more than one).
    /// </summary>
    /// <param name="symbol">The input member symbol to check.</param>
    /// <returns>Whether <paramref name="symbol"/> is an explicit interface implementation of internal interfaces.</returns>
    public static bool IsExplicitInterfaceImplementationOfInternalInterfaces(this ISymbol symbol)
    {
        static bool IsAnyContainingTypePublic(IEnumerable<ISymbol> symbols)
        {
            return symbols.Any(static symbol => symbol.ContainingType!.IsPubliclyAccessible());
        }

        return symbol switch
        {
            IMethodSymbol { ExplicitInterfaceImplementations: { Length: > 0 } methods } => !IsAnyContainingTypePublic(methods),
            IPropertySymbol { ExplicitInterfaceImplementations: { Length: > 0 } properties } => !IsAnyContainingTypePublic(properties),
            IEventSymbol { ExplicitInterfaceImplementations: { Length: > 0 } events } => !IsAnyContainingTypePublic(events),
            _ => false
        };
    }

    /// <summary>
    /// Checks whether a type has an attribute with a specified type.
    /// </summary>
    /// <param name="symbol">The input <see cref="ISymbol"/> instance to check.</param>
    /// <param name="typeSymbol">The <see cref="ITypeSymbol"/> instance for the attribute type to look for.</param>
    /// <returns>Whether or not <paramref name="symbol"/> has an attribute with the specified name.</returns>
    public static bool HasAttributeWithType(this ISymbol symbol, ITypeSymbol typeSymbol)
    {
        return TryGetAttributeWithType(symbol, typeSymbol, out _);
    }

    /// <summary>
    /// Tries to get an attribute with the specified type.
    /// </summary>
    /// <param name="symbol">The input <see cref="ISymbol"/> instance to check.</param>
    /// <param name="typeSymbol">The <see cref="ITypeSymbol"/> instance for the attribute type to look for.</param>
    /// <param name="attributeData">The resulting attribute, if it was found.</param>
    /// <returns>Whether or not <paramref name="symbol"/> has an attribute with the specified name.</returns>
    public static bool TryGetAttributeWithType(this ISymbol symbol, ITypeSymbol typeSymbol, [NotNullWhen(true)] out AttributeData? attributeData)
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
    /// Enumerates all attributes with the specified type.
    /// </summary>
    /// <param name="symbol">The input <see cref="ISymbol"/> instance to check.</param>
    /// <param name="typeSymbol">The <see cref="ITypeSymbol"/> instance for the attribute type to look for.</param>
    /// <returns>The matching attributes.</returns>
    public static IEnumerable<AttributeData> EnumerateAttributesWithType(this ISymbol symbol, ITypeSymbol typeSymbol)
    {
        foreach (AttributeData attribute in symbol.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, typeSymbol))
            {
                yield return attribute;
            }
        }
    }

    /// <summary>
    /// Checks whether a given symbol is accessible from the assembly of a given compilation (including eg. through nested types).
    /// </summary>
    /// <param name="symbol">The input <see cref="ISymbol"/> instance.</param>
    /// <param name="compilation">The <see cref="Compilation"/> instance currently in use.</param>
    /// <returns>Whether <paramref name="symbol"/> is accessible from the assembly for <paramref name="compilation"/>.</returns>
    public static bool IsAccessibleFromCompilationAssembly(this ISymbol symbol, Compilation compilation)
    {
        return compilation.IsSymbolAccessibleWithin(symbol, compilation.Assembly);
    }

    /// <summary>
    /// Checks whether or not a given <see cref="ITypeSymbol"/> inherits from a specified type.
    /// </summary>
    /// <param name="typeSymbol">The target <see cref="ITypeSymbol"/> instance to check.</param>
    /// <param name="baseTypeSymbol">The <see cref="ITypeSymbol"/> instane to check for inheritance from.</param>
    /// <returns>Whether or not <paramref name="typeSymbol"/> inherits from <paramref name="baseTypeSymbol"/>.</returns>
    public static bool InheritsFromType(this ITypeSymbol typeSymbol, ITypeSymbol baseTypeSymbol)
    {
        INamedTypeSymbol? currentBaseTypeSymbol = typeSymbol.BaseType;

        while (currentBaseTypeSymbol is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(currentBaseTypeSymbol, baseTypeSymbol))
            {
                return true;
            }

            currentBaseTypeSymbol = currentBaseTypeSymbol.BaseType;
        }

        return false;
    }
}
