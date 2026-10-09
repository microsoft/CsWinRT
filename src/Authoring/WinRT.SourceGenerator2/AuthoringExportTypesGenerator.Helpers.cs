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

            if (attributeSymbol is null)
            {
                return ImmutableArray<AuthoringActivationFactoryInfo>.Empty;
            }

            ImplementableClassLookup implementableClasses = new();

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
                    if (attributeData.ConstructorArguments is not [{ Kind: TypedConstantKind.Type, Value: INamedTypeSymbol }])
                    {
                        continue;
                    }

                    // The factory must extend a generated factory base: that is what supplies the conversion to a
                    // COM Callable Wrapper, which cannot be done from this compilation. The class it activates is
                    // read from that base, so statics-only runtime classes (which have no instance base) resolve too.
                    INamedTypeSymbol? factoryBase = GetImplementableBase(type, implementableClasses.GetFactoryRuntimeClassName);

                    if (factoryBase is null || implementableClasses.GetFactoryRuntimeClassName(factoryBase) is not string runtimeClassName)
                    {
                        continue;
                    }

                    _ = declaredRuntimeClasses.Add(runtimeClassName);

                    builder.Add(new AuthoringActivationFactoryInfo(
                        RuntimeClassName: runtimeClassName,
                        FactoryTypeName: type.ToDisplayString(),
                        FactoryBaseTypeName: factoryBase.ToDisplayString()));

                    continue;
                }

                INamedTypeSymbol? implementableBase = GetImplementableBase(type, implementableClasses.GetInstanceRuntimeClassName);

                // A generic type cannot be a Windows Runtime class, so there is nothing to activate for it (and
                // naming a factory after it would not even produce valid code).
                if (IsGenericOrNestedInGeneric(type))
                {
                    continue;
                }

                if (implementableBase is not null && implementableClasses.GetInstanceRuntimeClassName(implementableBase) is string implementedRuntimeClassName)
                {
                    implementations.Add((type, implementableBase, implementedRuntimeClassName));
                }
            }

            foreach (AuthoringActivationFactoryInfo generated in GetGeneratedActivationFactories(
                compilation, implementations, declaredRuntimeClasses, implementableClasses, token))
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
        /// <param name="implementableClasses">The lookup for the generated bases.</param>
        /// <param name="token">The <see cref="CancellationToken"/> instance to use.</param>
        /// <returns>The activation factories to generate.</returns>
        private static IEnumerable<AuthoringActivationFactoryInfo> GetGeneratedActivationFactories(
            Compilation compilation,
            List<(INamedTypeSymbol Implementation, INamedTypeSymbol ImplementableBase, string RuntimeClassName)> implementations,
            HashSet<string> declaredRuntimeClasses,
            ImplementableClassLookup implementableClasses,
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

                if (GetGeneratedFactoryBase(compilation, implementableBase, implementableClasses) is not INamedTypeSymbol factoryBase)
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
                    GeneratedForImplementationTypeName: implementation.ToDisplayString(),
                    ImplementableBaseTypeName: implementableBase.ToDisplayString());
            }
        }

        /// <summary>
        /// Finds the generated factory base for a runtime class, if CsWinRT can supply that factory itself.
        /// </summary>
        /// <param name="compilation">The <see cref="Compilation"/> instance to use.</param>
        /// <param name="implementableBase">The generated abstract base class the implementation extends.</param>
        /// <param name="implementableClasses">The lookup for the generated bases.</param>
        /// <returns>
        /// The generated factory base, or <see langword="null"/> if the class has none, or if activating it takes
        /// more than a parameterless constructor (i.e. it has factory methods with arguments, or statics, whose
        /// members only the author can implement).
        /// </returns>
        private static INamedTypeSymbol? GetGeneratedFactoryBase(
            Compilation compilation,
            INamedTypeSymbol implementableBase,
            ImplementableClassLookup implementableClasses)
        {
            // The factory base sits next to the class base it activates, under a reserved name
            string factoryBaseName = $"{implementableBase.ContainingNamespace.ToDisplayString()}.{implementableBase.Name}ActivationFactory";

            return compilation.GetTypeByMetadataName(factoryBaseName) is INamedTypeSymbol factoryBase &&
                   implementableClasses.HasDefaultActivationOnly(factoryBase)
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
        /// Finds the nearest generated abstract base class that a type derives from.
        /// </summary>
        /// <param name="type">The type whose base classes to inspect.</param>
        /// <param name="getRuntimeClassName">Gets the Windows Runtime class a type is a generated base of the wanted kind for, if any.</param>
        /// <returns>The generated base class, or <see langword="null"/> if there is none.</returns>
        private static INamedTypeSymbol? GetImplementableBase(INamedTypeSymbol? type, Func<INamedTypeSymbol, string?> getRuntimeClassName)
        {
            for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
            {
                if (getRuntimeClassName(current) is not null)
                {
                    return current;
                }
            }

            return null;
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

        /// <summary>
        /// Reads the implementable bases that referenced reference projections built with <c>CsWinRTImplementWinMDTypes</c>
        /// declare, from their <c>[WindowsRuntimeReferenceAssemblyMetadata]</c> entries.
        /// </summary>
        /// <remarks>
        /// The bases carry no marker: each reference projection records the Windows Runtime classes they stand for, and
        /// each base has a well-known name derived from its class (<c>ABI.&lt;Namespace&gt;.&lt;Class&gt;</c> for the
        /// instance base and <c>ABI.&lt;Namespace&gt;.&lt;Class&gt;ActivationFactory</c> for the activation factory
        /// base). The keys and names must match the ones the projection writer emits.
        /// </remarks>
        private sealed class ImplementableClassLookup
        {
            /// <summary>
            /// The fully qualified name of the key/value metadata attribute.
            /// </summary>
            private const string MetadataAttributeName = "WindowsRuntime.InteropServices.WindowsRuntimeReferenceAssemblyMetadataAttribute";

            /// <summary>
            /// The key for a Windows Runtime class whose instance base the reference projection declares.
            /// </summary>
            private const string ImplementableClassKey = "CsWinRT.ImplementableClass.v1";

            /// <summary>
            /// The key for a Windows Runtime class whose activation factory base the reference projection declares.
            /// </summary>
            private const string ImplementableClassFactoryKey = "CsWinRT.ImplementableClassFactory.v1";

            /// <summary>
            /// The key for a Windows Runtime class whose activation factory base only declares <c>ActivateInstance</c>.
            /// </summary>
            private const string ImplementableClassDefaultActivationOnlyKey = "CsWinRT.ImplementableClassDefaultActivationOnly.v1";

            /// <summary>
            /// The implementable bases declared by each assembly inspected so far.
            /// </summary>
            private readonly Dictionary<IAssemblySymbol, AssemblyInfo> _assemblies = new(SymbolEqualityComparer.Default);

            /// <summary>
            /// Gets the Windows Runtime class a type is the generated instance base for.
            /// </summary>
            /// <param name="type">The type to inspect.</param>
            /// <returns>The fully qualified Windows Runtime class name, or <see langword="null"/> if <paramref name="type"/> is not an instance base.</returns>
            public string? GetInstanceRuntimeClassName(INamedTypeSymbol type)
            {
                return TryGetFullName(type, out string? fullName) && GetAssemblyInfo(type.ContainingAssembly).InstanceBases.TryGetValue(fullName, out string? runtimeClassName)
                    ? runtimeClassName
                    : null;
            }

            /// <summary>
            /// Gets the Windows Runtime class a type is the generated activation factory base for.
            /// </summary>
            /// <param name="type">The type to inspect.</param>
            /// <returns>The fully qualified Windows Runtime class name, or <see langword="null"/> if <paramref name="type"/> is not an activation factory base.</returns>
            public string? GetFactoryRuntimeClassName(INamedTypeSymbol type)
            {
                return TryGetFullName(type, out string? fullName) && GetAssemblyInfo(type.ContainingAssembly).FactoryBases.TryGetValue(fullName, out string? runtimeClassName)
                    ? runtimeClassName
                    : null;
            }

            /// <summary>
            /// Gets whether a generated activation factory base only declares <c>ActivateInstance</c>, i.e. its class has
            /// no constructors taking arguments and no statics, so CsWinRT can implement the factory itself.
            /// </summary>
            /// <param name="factoryBase">The activation factory base to inspect.</param>
            /// <returns>Whether <paramref name="factoryBase"/> only declares <c>ActivateInstance</c>.</returns>
            public bool HasDefaultActivationOnly(INamedTypeSymbol factoryBase)
            {
                return GetFactoryRuntimeClassName(factoryBase) is string runtimeClassName &&
                       GetAssemblyInfo(factoryBase.ContainingAssembly).DefaultActivationOnly.Contains(runtimeClassName);
            }

            /// <summary>
            /// Gets the full metadata name of a top-level type in a named namespace (where all generated bases live).
            /// </summary>
            private static bool TryGetFullName(INamedTypeSymbol type, [NotNullWhen(true)] out string? fullName)
            {
                if (type.ContainingType is not null || type.ContainingNamespace is not { IsGlobalNamespace: false } containingNamespace || type.ContainingAssembly is null)
                {
                    fullName = null;

                    return false;
                }

                fullName = $"{containingNamespace.ToDisplayString()}.{type.MetadataName}";

                return true;
            }

            /// <summary>
            /// Gets the implementable bases declared by an assembly, reading them on first use.
            /// </summary>
            private AssemblyInfo GetAssemblyInfo(IAssemblySymbol assembly)
            {
                if (_assemblies.TryGetValue(assembly, out AssemblyInfo? info))
                {
                    return info;
                }

                info = new AssemblyInfo();

                foreach (AttributeData attribute in assembly.GetAttributes())
                {
                    if (attribute.AttributeClass?.ToDisplayString() != MetadataAttributeName ||
                        attribute.ConstructorArguments is not [{ Value: string key }, { Value: string runtimeClassName }])
                    {
                        continue;
                    }

                    if (key == ImplementableClassKey)
                    {
                        info.InstanceBases[$"ABI.{runtimeClassName}"] = runtimeClassName;
                    }
                    else if (key == ImplementableClassFactoryKey)
                    {
                        info.FactoryBases[$"ABI.{runtimeClassName}ActivationFactory"] = runtimeClassName;
                    }
                    else if (key == ImplementableClassDefaultActivationOnlyKey)
                    {
                        _ = info.DefaultActivationOnly.Add(runtimeClassName);
                    }
                }

                _assemblies.Add(assembly, info);

                return info;
            }

            /// <summary>
            /// The implementable bases declared by an assembly.
            /// </summary>
            private sealed class AssemblyInfo
            {
                /// <summary>
                /// Gets the full names of the instance bases, mapped to the Windows Runtime class each stands for.
                /// </summary>
                public Dictionary<string, string> InstanceBases { get; } = new(StringComparer.Ordinal);

                /// <summary>
                /// Gets the full names of the activation factory bases, mapped to the Windows Runtime class each activates.
                /// </summary>
                public Dictionary<string, string> FactoryBases { get; } = new(StringComparer.Ordinal);

                /// <summary>
                /// Gets the Windows Runtime classes whose activation factory base only declares <c>ActivateInstance</c>.
                /// </summary>
                public HashSet<string> DefaultActivationOnly { get; } = new(StringComparer.Ordinal);
            }
        }
    }
}