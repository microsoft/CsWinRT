using System.Collections.Generic;
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
}
