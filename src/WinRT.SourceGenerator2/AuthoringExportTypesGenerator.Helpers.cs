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

            if (context.TargetSymbol is not INamedTypeSymbol type || context.Attributes is not [AttributeData attribute])
            {
                return null;
            }

            ImplementableClassLookup implementableClasses = new();

            // The factory must extend a generated factory base: that is what supplies the conversion to a COM Callable
            // Wrapper, which cannot be done from this compilation. The class it activates is read from that base, so
            // statics-only runtime classes (which have no instance base) resolve too. Invalid factories are reported by
            // the analyzer, so they are skipped here to avoid follow-up compiler errors in the generated code.
            return ActivationFactoryValidation.GetImplementedFactoryRuntimeClassName(
                type,
                attribute,
                context.SemanticModel.Compilation,
                implementableClasses,
                out INamedTypeSymbol? factoryBase) is string runtimeClassName
                ? new AuthoringActivationFactoryInfo(
                    RuntimeClassName: runtimeClassName,
                    FactoryTypeName: type.ToDisplayString(),
                    FactoryBaseTypeName: factoryBase!.ToDisplayString())
                : null;
        }

        /// <summary>
        /// Gets the Windows Runtime class implemented by a type annotated with <c>[GeneratedWindowsRuntimeActivationFactory]</c>.
        /// </summary>
        /// <param name="context">The <see cref="GeneratorAttributeSyntaxContext"/> for the annotated type.</param>
        /// <param name="token">The <see cref="CancellationToken"/> instance to use.</param>
        /// <returns>
        /// The implementation, or <see langword="null"/> if CsWinRT cannot generate its activation factory (which the analyzer
        /// reports, so an invalid request does not count against a valid one for the same class either, like in the analyzer).
        /// </returns>
        public static AuthoringImplementationInfo? GetImplementation(GeneratorAttributeSyntaxContext context, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            if (context.TargetSymbol is not INamedTypeSymbol type)
            {
                return null;
            }

            ImplementableClassLookup implementableClasses = new();

            if (ActivationFactoryValidation.GetInvalidGeneratedFactoryReason(
                type,
                context.SemanticModel.Compilation,
                implementableClasses,
                out string? runtimeClassName,
                out INamedTypeSymbol? implementableBase,
                out INamedTypeSymbol? factoryBase) is not null)
            {
                return null;
            }

            token.ThrowIfCancellationRequested();

            return new(
                RuntimeClassName: runtimeClassName!,
                ImplementationTypeName: type.ToDisplayString(),
                ImplementableBaseTypeName: implementableBase!.ToDisplayString(),
                FactoryBaseTypeName: factoryBase!.ToDisplayString());
        }

        /// <summary>
        /// Gets the activation factories for the current compilation: the ones the author declared, plus the ones CsWinRT
        /// generates for implementations annotated with <c>[GeneratedWindowsRuntimeActivationFactory]</c>.
        /// </summary>
        /// <param name="declaredFactories">The activation factories the author declared.</param>
        /// <param name="implementations">The implementations annotated with <c>[GeneratedWindowsRuntimeActivationFactory]</c>.</param>
        /// <param name="assemblyName">The name of the current assembly.</param>
        /// <returns>The activation factories, ordered by runtime class name.</returns>
        public static EquatableArray<AuthoringActivationFactoryInfo> GetActivationFactories(
            ImmutableArray<AuthoringActivationFactoryInfo> declaredFactories,
            ImmutableArray<AuthoringImplementationInfo> implementations,
            string assemblyName)
        {
            string factoryNamespace = $"ABI.{assemblyName.EscapeIdentifierName()}";

            // A partial type with the attribute on several declarations is discovered once per declaration
            IEnumerable<AuthoringActivationFactoryInfo> generatedFactories = implementations
                .Distinct()
                .Select(implementation => new AuthoringActivationFactoryInfo(
                    RuntimeClassName: implementation.RuntimeClassName,
                    FactoryTypeName: "", // Named below, once it is known which names are taken
                    FactoryBaseTypeName: implementation.FactoryBaseTypeName,
                    GeneratedForImplementationTypeName: implementation.ImplementationTypeName,
                    ImplementableBaseTypeName: implementation.ImplementableBaseTypeName));

            // A Windows Runtime class can only have one activation factory, whether declared or generated, so a class with
            // several is skipped altogether (the analyzer reports all of them, so this just avoids follow-up compiler errors).
            // Discovery order follows syntax trees, so sort to keep the generated code (and the names below) stable.
            List<AuthoringActivationFactoryInfo> factories =
            [
                .. declaredFactories
                    .Distinct()
                    .Concat(generatedFactories)
                    .GroupBy(static factory => factory.RuntimeClassName, StringComparer.Ordinal)
                    .Where(static group => group.Take(2).Count() == 1)
                    .Select(static group => group.First())
                    .OrderBy(static factory => factory.RuntimeClassName, StringComparer.Ordinal)
            ];

            // Names are derived from type names with every separator replaced by '_', so different types can map to the same
            // name. Those are rare enough that just appending a counter keeps the names readable, and stable given the order.
            HashSet<string> fieldNames = new(StringComparer.Ordinal);
            HashSet<string> generatedTypeNames = new(StringComparer.Ordinal);

            ImmutableArray<AuthoringActivationFactoryInfo>.Builder builder = ImmutableArray.CreateBuilder<AuthoringActivationFactoryInfo>(factories.Count);

            foreach (AuthoringActivationFactoryInfo factory in factories)
            {
                AuthoringActivationFactoryInfo uniqueFactory = factory with
                {
                    FieldName = GetUniqueName(fieldNames, $"_activationFactory_{factory.RuntimeClassName.EscapeIdentifierName()}", suffix: "")
                };

                // The '@' escaping keywords is dropped, as the name has a suffix (so it cannot be a keyword anyway)
                if (factory.GeneratedForImplementationTypeName is string implementationTypeName)
                {
                    string typeName = implementationTypeName.Replace("@", "").EscapeIdentifierName();

                    uniqueFactory = uniqueFactory with
                    {
                        FactoryTypeName = $"{factoryNamespace}.{GetUniqueName(generatedTypeNames, typeName, suffix: "ActivationFactory")}"
                    };
                }

                builder.Add(uniqueFactory);
            }

            return builder.MoveToImmutable();
        }

        /// <summary>
        /// Gets a name not used yet, by appending a counter to a base name if needed, and records it as used.
        /// </summary>
        /// <param name="usedNames">The names already used.</param>
        /// <param name="baseName">The base name.</param>
        /// <param name="suffix">The suffix to append after the counter.</param>
        /// <returns>The unique name.</returns>
        private static string GetUniqueName(HashSet<string> usedNames, string baseName, string suffix)
        {
            string name = $"{baseName}{suffix}";

            for (int i = 2; !usedNames.Add(name); i++)
            {
                name = $"{baseName}_{i}{suffix}";
            }

            return name;
        }
    }
}
