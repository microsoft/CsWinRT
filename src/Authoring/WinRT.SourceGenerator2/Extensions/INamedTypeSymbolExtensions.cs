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
    }
}
