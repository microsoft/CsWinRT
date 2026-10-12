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
        /// Gets the activation factory an author declared with <c>[WindowsRuntimeActivationFactory]</c>.
        /// </summary>
        /// <param name="context">The <see cref="GeneratorAttributeSyntaxContext"/> for the annotated type.</param>
        /// <param name="token">The <see cref="CancellationToken"/> instance to use.</param>
        /// <returns>The declared activation factory, or <see langword="null"/> if the annotated type is not a valid one.</returns>
        public static AuthoringActivationFactoryInfo? GetDeclaredActivationFactory(GeneratorAttributeSyntaxContext context, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            if (context.TargetSymbol is not INamedTypeSymbol { IsAbstract: false, IsStatic: false, TypeKind: TypeKind.Class } type)
            {
                return null;
            }

            // '[WindowsRuntimeActivationFactory(typeof(<runtime class impl>))]'
            if (context.Attributes is not [{ ConstructorArguments: [{ Kind: TypedConstantKind.Type, Value: INamedTypeSymbol }] }])
            {
                return null;
            }

            ImplementableClassLookup implementableClasses = new();

            // The factory must extend a generated factory base: that is what supplies the conversion to a COM Callable
            // Wrapper, which cannot be done from this compilation. The class it activates is read from that base, so
            // statics-only runtime classes (which have no instance base) resolve too.
            INamedTypeSymbol? factoryBase = GetImplementableBase(type, implementableClasses.GetFactoryRuntimeClassName);

            return factoryBase is not null && implementableClasses.GetFactoryRuntimeClassName(factoryBase) is string runtimeClassName
                ? new AuthoringActivationFactoryInfo(
                    RuntimeClassName: runtimeClassName,
                    FactoryTypeName: type.ToDisplayString(),
                    FactoryBaseTypeName: factoryBase.ToDisplayString())
                : null;
        }

        /// <summary>
        /// Gets the Windows Runtime class implemented by a candidate type, if it derives from a generated instance base.
        /// </summary>
        /// <param name="context">The <see cref="GeneratorSyntaxContext"/> for the candidate type.</param>
        /// <param name="token">The <see cref="CancellationToken"/> instance to use.</param>
        /// <returns>The implementation, or <see langword="null"/> if the type is not one.</returns>
        public static AuthoringImplementationInfo? GetImplementation(GeneratorSyntaxContext context, CancellationToken token)
        {
            if (context.SemanticModel.GetDeclaredSymbol(context.Node, token) is not INamedTypeSymbol { IsAbstract: false, IsStatic: false, TypeKind: TypeKind.Class } type)
            {
                return null;
            }

            // A generic type cannot be a Windows Runtime class, so there is nothing to activate for it (and naming a
            // factory after it would not even produce valid code).
            if (type.IsGenericOrNestedInGenericType)
            {
                return null;
            }

            ImplementableClassLookup implementableClasses = new();

            if (GetImplementableBase(type, implementableClasses.GetInstanceRuntimeClassName) is not INamedTypeSymbol implementableBase ||
                implementableClasses.GetInstanceRuntimeClassName(implementableBase) is not string runtimeClassName)
            {
                return null;
            }

            token.ThrowIfCancellationRequested();

            return new(
                RuntimeClassName: runtimeClassName,
                ImplementationTypeName: type.ToDisplayString(),
                ImplementableBaseTypeName: implementableBase.ToDisplayString(),
                FactoryBaseTypeName: GetGeneratedFactoryBase(context.SemanticModel.Compilation, type, implementableBase, implementableClasses)?.ToDisplayString());
        }

        /// <summary>
        /// Gets the activation factories for the current compilation: the ones the author declared, plus the ones CsWinRT
        /// supplies for implementations whose runtime class has no declared factory.
        /// </summary>
        /// <param name="declaredFactories">The activation factories the author declared.</param>
        /// <param name="implementations">The implementations of Windows Runtime classes declared in existing metadata.</param>
        /// <param name="assemblyName">The name of the current assembly.</param>
        /// <returns>The activation factories, ordered by runtime class name.</returns>
        public static EquatableArray<AuthoringActivationFactoryInfo> GetActivationFactories(
            ImmutableArray<AuthoringActivationFactoryInfo> declaredFactories,
            ImmutableArray<AuthoringImplementationInfo> implementations,
            string assemblyName)
        {
            ImmutableArray<AuthoringActivationFactoryInfo>.Builder builder = ImmutableArray.CreateBuilder<AuthoringActivationFactoryInfo>();

            // Runtime classes the author declared a factory for. Those always win: the factory may implement additional
            // interop interfaces that need to be on its vtable, which cannot be inferred from here.
            HashSet<string> declaredRuntimeClasses = new(StringComparer.Ordinal);

            foreach (AuthoringActivationFactoryInfo factory in declaredFactories)
            {
                _ = declaredRuntimeClasses.Add(factory.RuntimeClassName);

                builder.Add(factory);
            }

            string factoryNamespace = $"ABI.{assemblyName.EscapeIdentifierName()}";

            // A partial type with a base list on several declarations is discovered once per declaration
            foreach (IGrouping<string, AuthoringImplementationInfo> group in implementations
                .Distinct()
                .GroupBy(static implementation => implementation.RuntimeClassName, StringComparer.Ordinal))
            {
                if (declaredRuntimeClasses.Contains(group.Key))
                {
                    continue;
                }

                // Several implementations of the same runtime class give no basis for picking the one to activate,
                // so the author has to say which by declaring the factory themselves.
                if (group.Take(2).Count() != 1 || group.First() is not { FactoryBaseTypeName: string factoryBaseTypeName } implementation)
                {
                    continue;
                }

                builder.Add(new(
                    RuntimeClassName: implementation.RuntimeClassName,
                    FactoryTypeName: $"{factoryNamespace}.{implementation.ImplementationTypeName.Replace('.', '_')}ActivationFactory",
                    FactoryBaseTypeName: factoryBaseTypeName,
                    GeneratedForImplementationTypeName: implementation.ImplementationTypeName,
                    ImplementableBaseTypeName: implementation.ImplementableBaseTypeName));
            }

            // Discovery order follows syntax trees, so sort to keep the generated code stable
            builder.Sort(static (left, right) => StringComparer.Ordinal.Compare(left.RuntimeClassName, right.RuntimeClassName));

            return builder.ToImmutable();
        }

        /// <summary>
        /// Finds the generated factory base for an implementation, if CsWinRT can supply that factory itself.
        /// </summary>
        /// <param name="compilation">The <see cref="Compilation"/> instance to use.</param>
        /// <param name="implementation">The implementation the generated factory would construct.</param>
        /// <param name="implementableBase">The generated abstract base class the implementation extends.</param>
        /// <param name="implementableClasses">The lookup for the generated bases.</param>
        /// <returns>
        /// The generated factory base, or <see langword="null"/> if the class has none, if activating it takes more than
        /// a parameterless constructor (i.e. it has factory methods with arguments, or statics, whose members only the
        /// author can implement), or if the generated factory could not construct the implementation.
        /// </returns>
        private static INamedTypeSymbol? GetGeneratedFactoryBase(
            Compilation compilation,
            INamedTypeSymbol implementation,
            INamedTypeSymbol implementableBase,
            ImplementableClassLookup implementableClasses)
        {
            // The generated factory just constructs the implementation, so both it and a parameterless constructor have
            // to be reachable from the generated code.
            if (!compilation.IsSymbolAccessibleWithin(implementation, compilation.Assembly) ||
                !implementation.InstanceConstructors.Any(constructor => constructor.Parameters.IsEmpty && compilation.IsSymbolAccessibleWithin(constructor, compilation.Assembly)))
            {
                return null;
            }

            // The factory base sits next to the class base it activates, in the same assembly, under a reserved name
            string factoryBaseName = $"{implementableBase.ContainingNamespace.ToDisplayString()}.{implementableBase.Name}ActivationFactory";

            return implementableBase.ContainingAssembly.GetTypeByMetadataName(factoryBaseName) is INamedTypeSymbol factoryBase &&
                   implementableClasses.HasDefaultActivationOnly(factoryBase)
                ? factoryBase
                : null;
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
    }
}
