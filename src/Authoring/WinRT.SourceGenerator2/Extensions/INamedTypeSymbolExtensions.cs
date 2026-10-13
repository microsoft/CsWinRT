// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
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

        /// <summary>
        /// Gets why code generated into another file of the same assembly cannot instantiate the type with <c>new T()</c>.
        /// </summary>
        /// <param name="compilation">The current <see cref="Compilation"/>.</param>
        /// <returns>The reason, or <see langword="null"/> if generated code can instantiate the type.</returns>
        /// <remarks>
        /// This does not check whether the type is abstract or static, which callers report with more specific messages.
        /// </remarks>
        public string? GetNotInstantiableFromGeneratedCodeReason(Compilation compilation)
        {
            if (symbol.IsGenericOrNestedInGenericType)
            {
                return "it is generic, or nested in a generic type";
            }

            for (INamedTypeSymbol? current = symbol; current is not null; current = current.ContainingType)
            {
                if (current.IsFileLocal)
                {
                    return "it is a file-local type, or nested in one";
                }
            }

            if (!compilation.IsSymbolAccessibleWithin(symbol, compilation.Assembly))
            {
                return "it is not accessible from the rest of its assembly";
            }

            if (symbol.InstanceConstructors.FirstOrDefault(constructor =>
                constructor.Parameters.IsEmpty &&
                compilation.IsSymbolAccessibleWithin(constructor, compilation.Assembly)) is not IMethodSymbol constructor)
            {
                return "it has no parameterless constructor accessible from the rest of its assembly";
            }

            // 'new T()' does not compile if 'T' has required members that the constructor does not declare it sets
            return symbol.HasRequiredMembers && !constructor.GetAttributes().Any(static attribute =>
                attribute.AttributeClass?.ToDisplayString() == "System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute")
                ? "it has required members, which its parameterless constructor does not set (as declared with '[SetsRequiredMembers]')"
                : null;
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
