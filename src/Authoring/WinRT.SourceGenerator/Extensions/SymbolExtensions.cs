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
}
