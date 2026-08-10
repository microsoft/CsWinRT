// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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

            // Runtime classes the author declared a factory for. Those always win: the factory may implement
            // additional interop interfaces that need to be on its vtable, which cannot be inferred from here.
            HashSet<string> declaredRuntimeClasses = new(StringComparer.Ordinal);

            // Implementations of Windows Runtime classes declared in existing metadata, which may need a
            // factory generated for them. Resolved after the walk, once all declared factories are known.
            List<(INamedTypeSymbol Implementation, INamedTypeSymbol ImplementableBase, string RuntimeClassName)> implementations = [];

            foreach (INamedTypeSymbol type in EnumerateTypes(compilation.Assembly.GlobalNamespace, token))
            {
                if (type.IsAbstract || type.IsStatic || type.TypeKind != TypeKind.Class)
                {
                    continue;
                }

                if (type.TryGetAttributeWithType(attributeSymbol, out AttributeData? attributeData))
                {
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

                    string runtimeClassName = implementedClass.ToDisplayString();

                    _ = declaredRuntimeClasses.Add(runtimeClassName);

                    builder.Add(new AuthoringActivationFactoryInfo(
                        RuntimeClassName: runtimeClassName,
                        FactoryTypeName: type.ToDisplayString(),
                        FactoryBaseTypeName: factoryBase.ToDisplayString()));

                    continue;
                }

                INamedTypeSymbol? implementableBase = GetImplementableBase(type, implementableAttributeSymbol);

                // A generic type cannot be a Windows Runtime class, so there is nothing to activate for it (and
                // naming a factory after it would not even produce valid code).
                if (IsGenericOrNestedInGeneric(type))
                {
                    continue;
                }

                if (GetImplementedRuntimeClass(implementableBase, implementableAttributeSymbol) is INamedTypeSymbol implementedRuntimeClass)
                {
                    implementations.Add((type, implementableBase!, implementedRuntimeClass.ToDisplayString()));
                }
            }

            foreach (AuthoringActivationFactoryInfo generated in GetGeneratedActivationFactories(
                compilation, implementations, declaredRuntimeClasses, implementableFactoryAttributeSymbol, token))
            {
                builder.Add(generated);
            }

            return builder.ToImmutable();
        }

        /// <summary>
        /// Determines which implementations of Windows Runtime classes need CsWinRT to supply their activation
        /// factory, and describes the factory to generate for each.
        /// </summary>
        /// <param name="compilation">The <see cref="Compilation"/> instance to use.</param>
        /// <param name="implementations">The candidate implementations, with the generated base each extends.</param>
        /// <param name="declaredRuntimeClasses">The runtime classes the author already declared a factory for.</param>
        /// <param name="implementableFactoryAttributeSymbol">The marker attribute on generated factory bases.</param>
        /// <param name="token">The <see cref="CancellationToken"/> instance to use.</param>
        /// <returns>The activation factories to generate.</returns>
        private static IEnumerable<AuthoringActivationFactoryInfo> GetGeneratedActivationFactories(
            Compilation compilation,
            List<(INamedTypeSymbol Implementation, INamedTypeSymbol ImplementableBase, string RuntimeClassName)> implementations,
            HashSet<string> declaredRuntimeClasses,
            INamedTypeSymbol implementableFactoryAttributeSymbol,
            CancellationToken token)
        {
            string factoryNamespace = $"ABI.{compilation.Assembly.Name.EscapeIdentifierName()}";

            foreach (IGrouping<string, (INamedTypeSymbol Implementation, INamedTypeSymbol ImplementableBase, string RuntimeClassName)> group in
                implementations.GroupBy(static candidate => candidate.RuntimeClassName, StringComparer.Ordinal))
            {
                token.ThrowIfCancellationRequested();

                if (declaredRuntimeClasses.Contains(group.Key))
                {
                    continue;
                }

                // Several implementations of the same runtime class give no basis for picking the one to activate,
                // so the author has to say which by declaring the factory themselves.
                if (group.Take(2).Count() != 1)
                {
                    continue;
                }

                (INamedTypeSymbol implementation, INamedTypeSymbol implementableBase, string runtimeClassName) = group.First();

                if (GetGeneratedFactoryBase(compilation, implementableBase, implementableFactoryAttributeSymbol) is not INamedTypeSymbol factoryBase)
                {
                    continue;
                }

                // The generated factory just constructs the implementation, so both it and a parameterless
                // constructor have to be reachable from the generated code.
                if (!compilation.IsSymbolAccessibleWithin(implementation, compilation.Assembly))
                {
                    continue;
                }

                if (!implementation.InstanceConstructors.Any(constructor =>
                        constructor.Parameters.IsEmpty &&
                        compilation.IsSymbolAccessibleWithin(constructor, compilation.Assembly)))
                {
                    continue;
                }

                yield return new AuthoringActivationFactoryInfo(
                    RuntimeClassName: runtimeClassName,
                    FactoryTypeName: $"{factoryNamespace}.{implementation.ToDisplayString().Replace('.', '_')}ActivationFactory",
                    FactoryBaseTypeName: factoryBase.ToDisplayString(),
                    GeneratedForImplementationTypeName: implementation.ToDisplayString());
            }
        }

        /// <summary>
        /// Finds the generated factory base for a runtime class, if CsWinRT can supply that factory itself.
        /// </summary>
        /// <param name="compilation">The <see cref="Compilation"/> instance to use.</param>
        /// <param name="implementableBase">The generated abstract base class the implementation extends.</param>
        /// <param name="implementableFactoryAttributeSymbol">The marker attribute on generated factory bases.</param>
        /// <returns>
        /// The generated factory base, or <see langword="null"/> if the class has none, or if activating it takes
        /// more than the parameterless <c>ActivateInstance</c> (i.e. it has factory, statics or composable
        /// interfaces, whose members only the author can implement).
        /// </returns>
        private static INamedTypeSymbol? GetGeneratedFactoryBase(
            Compilation compilation,
            INamedTypeSymbol implementableBase,
            INamedTypeSymbol implementableFactoryAttributeSymbol)
        {
            // The factory base sits next to the class base it activates, under a reserved name
            string factoryBaseName = $"{implementableBase.ContainingNamespace.ToDisplayString()}.{implementableBase.Name}ActivationFactory";

            return compilation.GetTypeByMetadataName(factoryBaseName) is INamedTypeSymbol factoryBase &&
                   factoryBase.TryGetAttributeWithType(implementableFactoryAttributeSymbol, out AttributeData? attributeData) &&
                   attributeData.NamedArguments.Any(static argument => argument is { Key: "HasDefaultActivationOnly", Value.Value: true })
                ? factoryBase
                : null;
        }

        /// <summary>
        /// Returns whether a type is generic, or is nested (at any depth) in a generic type.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <returns>Whether the type carries any type parameters.</returns>
        private static bool IsGenericOrNestedInGeneric(INamedTypeSymbol type)
        {
            for (INamedTypeSymbol? current = type; current is not null; current = current.ContainingType)
            {
                if (current.Arity > 0)
                {
                    return true;
                }
            }

            return false;
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