// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using Microsoft.CodeAnalysis;

namespace WindowsRuntime.SourceGenerator;

/// <summary>
/// Windows Runtime-specific extensions for Roslyn symbols.
/// </summary>
internal static class WindowsRuntimeExtensions
{
    extension(ISymbol symbol)
    {
        /// <summary>
        /// Gets whether a declaration is excluded from component metadata, directly or by its containing type.
        /// </summary>
        /// <param name="excludeAttributeType">The metadata exclusion marker, if available.</param>
        /// <returns>Whether the declaration is excluded from component metadata.</returns>
        public bool IsExcludedFromWindowsMetadata(INamedTypeSymbol? excludeAttributeType)
        {
            if (excludeAttributeType is null)
            {
                return false;
            }

            // Containment propagates exclusion; inheritance does not
            for (ISymbol? current = symbol; current is not null; current = current.ContainingType)
            {
                if (current.HasAttributeWithType(excludeAttributeType))
                {
                    return true;
                }
            }

            // Accessors also follow exclusions applied to their property or event
            return symbol is IMethodSymbol { AssociatedSymbol: { } associated } &&
                   associated.IsExcludedFromWindowsMetadata(excludeAttributeType);
        }
    }

    extension(INamedTypeSymbol symbol)
    {
        /// <summary>
        /// Gets whether the type is declared in the <c>ABI</c> namespace or one of its child namespaces.
        /// </summary>
        public bool IsInAbiNamespace
        {
            get
            {
                INamespaceSymbol? rootNamespace = null;

                for (INamespaceSymbol? current = symbol.ContainingNamespace; current is { IsGlobalNamespace: false }; current = current.ContainingNamespace)
                {
                    rootNamespace = current;
                }

                return rootNamespace?.Name == "ABI";
            }
        }

        /// <summary>
        /// Checks whether the type is declared in a reference projection, or carries the implementation-only runtime marker.
        /// </summary>
        /// <param name="referenceAssemblyAttributeType">The reference projection assembly marker, if available.</param>
        /// <param name="windowsRuntimeTypeAttributeType">The per-type Windows Runtime marker, if available.</param>
        /// <returns>Whether either projection marker is present.</returns>
        public bool IsProjectedWindowsRuntimeType(
            INamedTypeSymbol? referenceAssemblyAttributeType,
            INamedTypeSymbol? windowsRuntimeTypeAttributeType)
        {
            // Reference projections strip per-type markers; implementation projections retain them
            return (referenceAssemblyAttributeType is not null &&
                    symbol.ContainingAssembly is { } assembly &&
                    assembly.HasAttributeWithType(referenceAssemblyAttributeType)) ||
                   (windowsRuntimeTypeAttributeType is not null && symbol.HasAttributeWithType(windowsRuntimeTypeAttributeType));
        }

        /// <summary>
        /// Checks a default overload annotation, including one supplied by an implemented interface.
        /// </summary>
        /// <param name="method">The method to check for a default overload annotation.</param>
        /// <param name="defaultOverloadAttributeType">The default overload attribute type, if available.</param>
        /// <returns>Whether the method or its implemented interface member has a default overload annotation.</returns>
        public bool HasDefaultOverload(
            IMethodSymbol method,
            INamedTypeSymbol? defaultOverloadAttributeType)
        {
            if (defaultOverloadAttributeType is null)
            {
                return false;
            }

            if (method.HasAttributeWithType(defaultOverloadAttributeType))
            {
                return true;
            }

            // Interface annotations can select the default even when the implementation has no attribute
            foreach (INamedTypeSymbol iface in symbol.AllInterfaces)
            {
                foreach (IMethodSymbol candidate in iface.GetMembers(method.Name).OfType<IMethodSymbol>())
                {
                    if (SymbolEqualityComparer.Default.Equals(symbol.FindImplementationForInterfaceMember(candidate), method) &&
                        candidate.HasAttributeWithType(defaultOverloadAttributeType))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    extension(IMethodSymbol symbol)
    {
        /// <summary>
        /// Counts Windows Runtime input parameters, excluding output parameters.
        /// </summary>
        /// <returns>The number of input parameters used to group Windows Runtime overloads.</returns>
        public int GetWindowsRuntimeInputArity()
        {
            return symbol.Parameters.Count(static parameter => parameter.RefKind is not RefKind.Out);
        }
    }
}