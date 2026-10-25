// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis;

namespace WindowsRuntime.SourceGenerator;

/// <summary>
/// Extensions for <see cref="INamedTypeSymbol"/>.
/// </summary>
internal static class INamedTypeSymbolExtensions
{
    extension(INamedTypeSymbol symbol)
    {
        /// <summary>
        /// Gets whether the type and all of its containing types are public.
        /// </summary>
        public bool IsPubliclyAccessible
        {
            get
            {
                for (INamedTypeSymbol? current = symbol; current is not null; current = current.ContainingType)
                {
                    if (current.DeclaredAccessibility is not Accessibility.Public)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

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
        /// Gets whether the type is generic, or is nested (at any depth) in a generic type.
        /// </summary>
        public bool IsGenericOrNestedInGenericType
        {
            get
            {
                for (INamedTypeSymbol? current = symbol; current is not null; current = current.ContainingType)
                {
                    if (current.Arity > 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets whether the type declares or inherits any required members.
        /// </summary>
        public bool HasRequiredMembers
        {
            get
            {
                for (INamedTypeSymbol? current = symbol; current is not null; current = current.BaseType)
                {
                    foreach (ISymbol member in current.GetMembers())
                    {
                        if (member is IPropertySymbol { IsRequired: true } or IFieldSymbol { IsRequired: true })
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }
    }
}