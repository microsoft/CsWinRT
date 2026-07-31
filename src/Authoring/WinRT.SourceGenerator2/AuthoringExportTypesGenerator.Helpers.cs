// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis;
using WindowsRuntime.SourceGenerator.Models;

namespace WindowsRuntime.SourceGenerator;

/// <inheritdoc cref="AuthoringExportTypesGenerator"/>
public partial class AuthoringExportTypesGenerator
{
    /// <summary>
    /// Helper methods for <see cref="AuthoringExportTypesGenerator"/>.
    /// </summary>
    private static class Helpers
    {
        /// <summary>
        /// Tries to get the name of a dependent Windows Runtime component from a given assembly.
        /// </summary>
        /// <param name="assemblySymbol">The assembly symbol to analyze.</param>
        /// <param name="compilation">The <see cref="Compilation"/> instance to use.</param>
        /// <param name="token">The <see cref="CancellationToken"/> instance to use.</param>
        /// <param name="name">The resulting type name, if found.</param>
        /// <returns>Whether a type name was found.</returns>
        public static bool TryGetDependentAssemblyExportsTypeName(
            IAssemblySymbol assemblySymbol,
            Compilation compilation,
            CancellationToken token,
            [NotNullWhen(true)] out string? name)
        {
            // Get the attribute to lookup to find the target type to use
            INamedTypeSymbol winRTAssemblyExportsTypeAttributeSymbol = compilation.GetTypeByMetadataName("WindowsRuntime.InteropServices.WindowsRuntimeComponentAssemblyExportsTypeAttribute")!;

            // Make sure the assembly does have the attribute on it
            if (!assemblySymbol.TryGetAttributeWithType(winRTAssemblyExportsTypeAttributeSymbol, out AttributeData? attributeData))
            {
                name = null;

                return false;
            }

            token.ThrowIfCancellationRequested();

            // Sanity check: we should have a valid type in the annotation
            if (attributeData.ConstructorArguments is not [{ Kind: TypedConstantKind.Type, Value: INamedTypeSymbol assemblyExportsTypeSymbol }])
            {
                name = null;

                return false;
            }

            token.ThrowIfCancellationRequested();

            // Other sanity check: this type should be accessible from this compilation
            if (!assemblyExportsTypeSymbol.IsAccessibleFromCompilationAssembly(compilation))
            {
                name = null;

                return false;
            }

            token.ThrowIfCancellationRequested();

            name = assemblyExportsTypeSymbol.ToDisplayString();

            return true;
        }

        /// <summary>
        /// Discovers all user-authored <c>[WindowsRuntimeActivationFactory]</c> factories in the compilation.
        /// </summary>
        /// <param name="compilation">The <see cref="Compilation"/> instance to use.</param>
        /// <param name="token">The <see cref="CancellationToken"/> instance to use.</param>
        /// <returns>The discovered activation factories (empty if none or the attribute is unavailable).</returns>
        public static EquatableArray<AuthoringActivationFactoryInfo> GetActivationFactories(Compilation compilation, CancellationToken token)
        {
            INamedTypeSymbol? attributeSymbol = compilation.GetTypeByMetadataName("WindowsRuntime.InteropServices.WindowsRuntimeActivationFactoryAttribute");
            INamedTypeSymbol? implementableAttributeSymbol = compilation.GetTypeByMetadataName("WindowsRuntime.WindowsRuntimeImplementableClassAttribute");
            INamedTypeSymbol? implementableFactoryAttributeSymbol = compilation.GetTypeByMetadataName("WindowsRuntime.WindowsRuntimeImplementableClassFactoryAttribute");

            if (attributeSymbol is null || implementableAttributeSymbol is null || implementableFactoryAttributeSymbol is null)
            {
                return ImmutableArray<AuthoringActivationFactoryInfo>.Empty;
            }

            ImmutableArray<AuthoringActivationFactoryInfo>.Builder builder = ImmutableArray.CreateBuilder<AuthoringActivationFactoryInfo>();

            foreach (INamedTypeSymbol type in EnumerateTypes(compilation.Assembly.GlobalNamespace, token))
            {
                if (type.IsAbstract || type.IsStatic || !type.TryGetAttributeWithType(attributeSymbol, out AttributeData? attributeData))
                {
                    continue;
                }

                // '[WindowsRuntimeActivationFactory(typeof(<runtime class impl>))]'
                if (attributeData.ConstructorArguments is not [{ Kind: TypedConstantKind.Type, Value: INamedTypeSymbol runtimeClassType }])
                {
                    continue;
                }

                // Prefer the factory's own generated base, so statics-only runtime classes (which have no
                // instance type for the attribute to point at) still resolve correctly.
                INamedTypeSymbol? factoryBase = GetImplementableBase(type, implementableFactoryAttributeSymbol);

                INamedTypeSymbol? implementedClass =
                    GetImplementedRuntimeClass(factoryBase, implementableFactoryAttributeSymbol) ??
                    GetImplementedRuntimeClass(GetImplementableBase(runtimeClassType, implementableAttributeSymbol), implementableAttributeSymbol);

                // The factory must extend a generated factory base: that is what supplies the conversion to a
                // COM Callable Wrapper, which cannot be done from this compilation.
                if (implementedClass is null || factoryBase is null)
                {
                    continue;
                }

                builder.Add(new AuthoringActivationFactoryInfo(
                    RuntimeClassName: implementedClass.ToDisplayString(),
                    FactoryTypeName: type.ToDisplayString(),
                    FactoryBaseTypeName: factoryBase.ToDisplayString()));
            }

            return builder.ToImmutable();
        }

        /// <summary>
        /// Finds the generated abstract base class that a type derives from, identified by a marker attribute.
        /// </summary>
        /// <param name="type">The type whose base classes to inspect.</param>
        /// <param name="markerAttributeSymbol">The marker attribute identifying the generated base.</param>
        /// <returns>The generated base class, or <see langword="null"/> if there is none.</returns>
        private static INamedTypeSymbol? GetImplementableBase(INamedTypeSymbol? type, INamedTypeSymbol markerAttributeSymbol)
        {
            for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
            {
                if (current.TryGetAttributeWithType(markerAttributeSymbol, out _))
                {
                    return current;
                }
            }

            return null;
        }

        /// <summary>
        /// Reads the Windows Runtime class recorded on a generated abstract base class.
        /// </summary>
        /// <param name="implementableBase">The generated base class, if any.</param>
        /// <param name="markerAttributeSymbol">The marker attribute carrying the class.</param>
        /// <returns>The projected Windows Runtime class type, or <see langword="null"/> if there is none.</returns>
        private static INamedTypeSymbol? GetImplementedRuntimeClass(INamedTypeSymbol? implementableBase, INamedTypeSymbol markerAttributeSymbol)
        {
            return implementableBase is not null &&
                   implementableBase.TryGetAttributeWithType(markerAttributeSymbol, out AttributeData? attributeData) &&
                   attributeData.ConstructorArguments is [{ Kind: TypedConstantKind.Type, Value: INamedTypeSymbol runtimeClassType }]
                ? runtimeClassType
                : null;
        }

        /// <summary>
        /// Enumerates all named types (including nested types) declared under a namespace.
        /// </summary>
        /// <param name="namespaceSymbol">The root namespace to enumerate.</param>
        /// <param name="token">The <see cref="CancellationToken"/> instance to use.</param>
        /// <returns>All named types under <paramref name="namespaceSymbol"/>.</returns>
        private static IEnumerable<INamedTypeSymbol> EnumerateTypes(INamespaceSymbol namespaceSymbol, CancellationToken token)
        {
            foreach (INamespaceOrTypeSymbol member in namespaceSymbol.GetMembers())
            {
                token.ThrowIfCancellationRequested();

                if (member is INamespaceSymbol nestedNamespace)
                {
                    foreach (INamedTypeSymbol nestedType in EnumerateTypes(nestedNamespace, token))
                    {
                        yield return nestedType;
                    }
                }
                else if (member is INamedTypeSymbol type)
                {
                    yield return type;

                    foreach (INamedTypeSymbol nestedType in EnumerateNestedTypes(type, token))
                    {
                        yield return nestedType;
                    }
                }
            }
        }

        /// <summary>
        /// Enumerates all nested types of a given type, recursively.
        /// </summary>
        /// <param name="type">The type whose nested types to enumerate.</param>
        /// <param name="token">The <see cref="CancellationToken"/> instance to use.</param>
        /// <returns>All nested types of <paramref name="type"/>.</returns>
        private static IEnumerable<INamedTypeSymbol> EnumerateNestedTypes(INamedTypeSymbol type, CancellationToken token)
        {
            foreach (INamedTypeSymbol nestedType in type.GetTypeMembers())
            {
                token.ThrowIfCancellationRequested();

                yield return nestedType;

                foreach (INamedTypeSymbol deeplyNestedType in EnumerateNestedTypes(nestedType, token))
                {
                    yield return deeplyNestedType;
                }
            }
        }
    }
}