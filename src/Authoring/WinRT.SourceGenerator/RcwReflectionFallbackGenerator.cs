// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using WinRT.SourceGenerator;

namespace Generator;

[Generator]
public sealed class RcwReflectionFallbackGenerator : IIncrementalGenerator
{
    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Get all the names of the projected types to root
        IncrementalValueProvider<EquatableArray<string>> projectedTypeNames = context.CompilationProvider.Select(static (compilation, token) =>
        {
            ITypeSymbol windowsRuntimeTypeAttributeSymbol = compilation.GetTypeByMetadataName("WinRT.WindowsRuntimeTypeAttribute");

            ImmutableArray<string>.Builder projectedTypeNames = ImmutableArray.CreateBuilder<string>();

            foreach (MetadataReference metadataReference in compilation.References)
            {
                // We only care about resolved assembly symbols (this should always be the case anyway)
                if (compilation.GetAssemblyOrModuleSymbol(metadataReference) is not IAssemblySymbol assemblySymbol)
                {
                    continue;
                }

                // If the assembly is not an old projections assembly, we have nothing to do
                if (!IsOldProjectionAssembly(assemblySymbol))
                {
                    continue;
                }

                // Process all type symbols in the current assembly
                foreach (INamedTypeSymbol typeSymbol in VisitNamedTypeSymbolsExceptABI(assemblySymbol))
                {
                    // We only care about public or internal classes
                    if (typeSymbol is not { TypeKind: TypeKind.Class, DeclaredAccessibility: Accessibility.Public or Accessibility.Internal })
                    {
                        continue;
                    } 

                    // If the type is not a generated projected type, do nothing
                    if (ContainsAttributeWithType(typeSymbol, windowsRuntimeTypeAttributeSymbol))
                    {
                        continue;
                    }

                    // Double check we can in fact access this type (or we can't reference it)
                    if (!compilation.IsSymbolAccessibleWithin(typeSymbol, compilation.Assembly))
                    {
                        continue;
                    }

                    projectedTypeNames.Add(typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                }
            }

            return EquatableArray<string>.FromImmutableArray(projectedTypeNames.ToImmutable());
        });

        // Generate the [DynamicDependency] attributes
        context.RegisterImplementationSourceOutput(projectedTypeNames, static (context, projectedTypeNames) =>
        {
            if (projectedTypeNames.IsEmpty)
            {
                return;
            }

            StringBuilder builder = new();

            builder.AppendLine("""
                namespace WinRT
                {
                    using global::System.Runtime.CompilerServices;
                    using global::System.Diagnostics.CodeAnalysis;

                    /// <summary>
                    /// Roots RCW types for assemblies referencing old projections.
                    /// It is recommended to update those, to get binary size savings.
                    /// </summary>
                    internal static class RcwFallbackInitializer
                    {
                        /// <summary>
                        /// Roots all dependent RCW types.
                        /// </summary>
                        [ModuleInitializer]                    
                """);

            foreach (string projectedTypeName in projectedTypeNames)
            {
                builder.AppendLine("[DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicConstructors, typeof(");
                builder.Append(projectedTypeName);
                builder.AppendLine("))]");
            }

            builder.Append("""
                public static void InitializeRcwFallback()
                        {
                        }
                    }
                }
                """);

            context.AddSource("RcwFallbackInitializer.g.cs", builder.ToString());
        });
    }

    /// <summary>
    /// Checks whether an assembly contains old projections.
    /// </summary>
    /// <param name="assemblySymbol">The assembly to inspect.</param>
    /// <returns>Whether <paramref name="assemblySymbol"/> contains old projections.</returns>
    private static bool IsOldProjectionAssembly(IAssemblySymbol assemblySymbol)
    {
        // We only care about assemblies that have some dependent assemblies
        if (assemblySymbol.ContainingModule is not { ReferencedAssemblies: { Length: > 0 } dependentAssemblies })
        {
            return false;
        }

        // Scan all dependent assemblies to look for CsWinRT with version < 2.0.8
        foreach (AssemblyIdentity assemblyIdentity in dependentAssemblies)
        {
            if (assemblyIdentity.Name == "WinRT.Runtime")
            {
                return assemblyIdentity.Version < new Version(2, 0, 8);
            }
        }

        // This assembly is not a projection assembly
        return false;
    }

    /// <summary>
    /// Visits all named type symbols in a given assembly, except for ABI types.
    /// </summary>
    /// <param name="assemblySymbol">The assembly to inspect.</param>
    /// <returns>All named type symbols in <paramref name="assemblySymbol"/>, except for ABI types.</returns>
    private static IEnumerable<INamedTypeSymbol> VisitNamedTypeSymbolsExceptABI(IAssemblySymbol assemblySymbol)
    {
        static IEnumerable<INamedTypeSymbol> Visit(INamespaceOrTypeSymbol symbol)
        {
            foreach (ISymbol memberSymbol in symbol.GetMembers())
            {
                // Visit the current symbol if it's a type symbol
                if (memberSymbol is INamedTypeSymbol typeSymbol)
                {
                    yield return typeSymbol;
                }
                else if (memberSymbol is INamespaceSymbol { Name: not ("ABI" or "WinRT") } namespaceSymbol)
                {
                    // If the symbol is a namespace, also recurse (ignore the ABI namespaces)
                    foreach (INamedTypeSymbol nestedTypeSymbol in Visit(namespaceSymbol))
                    {
                        yield return nestedTypeSymbol;
                    }
                }
            }
        }

        return Visit(assemblySymbol.GlobalNamespace);
    }

    /// <summary>
    /// Checks whether a given type symbol has an attribute with a specified type.
    /// </summary>
    /// <param name="typeSymbol">The type to check.</param>
    /// <param name="attributeTypeSymbol">The attribute to look for.</param>
    /// <returns>Whether <paramref name="typeSymbol"/> has an attribute with type <paramref name="attributeTypeSymbol"/>.</returns>
    private static bool ContainsAttributeWithType(ITypeSymbol typeSymbol, ITypeSymbol attributeTypeSymbol)
    {
        foreach (AttributeData attributeData in typeSymbol.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, attributeTypeSymbol))
            {
                return true;
            }
        }

        return false;
    }
}
