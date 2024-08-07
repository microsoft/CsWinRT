﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using WinRT.SourceGenerator;

#nullable enable

namespace Generator;

[Generator]
public sealed class MergeReferencedActivationFactoriesGenerator : IIncrementalGenerator
{
    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Get whether the generator is enabled
        IncrementalValueProvider<bool> isGeneratorEnabled = context.AnalyzerConfigOptionsProvider.Select(static (options, token) =>
        {
            return options.GetCsWinRTMergeReferencedActivationFactories();
        });

        // Get the fully qualified type names of all assembly exports types to merge
        IncrementalValueProvider<EquatableArray<string>> assemblyExportsTypeNames =
            context.CompilationProvider
            .Combine(isGeneratorEnabled)
            .Select(static (item, token) =>
            {
                // Immediately bail if the generator is disabled
                if (!item.Right)
                {
                    return new EquatableArray<string>(ImmutableArray<string>.Empty);
                }

                ImmutableArray<string>.Builder builder = ImmutableArray.CreateBuilder<string>();

                foreach (MetadataReference metadataReference in item.Left.References)
                {
                    token.ThrowIfCancellationRequested();

                    if (item.Left.GetAssemblyOrModuleSymbol(metadataReference) is not IAssemblySymbol assemblySymbol)
                    {
                        continue;
                    }

                    token.ThrowIfCancellationRequested();

                    // Add the type name if the assembly is a WinRT component
                    if (TryGetDependentAssemblyExportsTypeName(
                        assemblySymbol,
                        item.Left,
                        token,
                        out string? name))
                    {
                        builder.Add(name);
                    }
                }

                token.ThrowIfCancellationRequested();

                return new EquatableArray<string>(builder.ToImmutable());
            });

        // Generate the chaining helper
        context.RegisterImplementationSourceOutput(assemblyExportsTypeNames, static (context, assemblyExportsTypeNames) =>
        {
            if (assemblyExportsTypeNames.IsEmpty)
            {
                return;
            }

            StringBuilder builder = new();

            builder.AppendLine("""
                // <auto-generated/>
                #pragma warning disable

                namespace WinRT
                {
                    using global::System;

                    /// <inheritdoc cref="Module"/>
                    partial class Module
                    {
                        /// <summary>
                        /// Tries to retrieve the activation factory from all dependent WinRT components.
                        /// </summary>
                        /// <param name="fullyQualifiedTypeName">The marshalled fully qualified type name of the activation factory to retrieve.</param>
                        /// <returns>The pointer to the activation factory that corresponds with the class specified by <paramref name="fullyQualifiedTypeName"/>.</returns>
                        internal static unsafe IntPtr TryGetDependentActivationFactory(ReadOnlySpan<char> fullyQualifiedTypeName)
                        {
                            IntPtr obj;

                """);

            foreach (string assemblyExportsTypeName in assemblyExportsTypeNames)
            {
                builder.AppendLine($$"""
                                obj = global::{{assemblyExportsTypeName}}.GetActivationFactory(fullyQualifiedTypeName);

                                if ((void*)obj is not null)
                                {
                                    return obj;
                                }

                    """);
            }

            builder.AppendLine("""
                            return default;
                        }
                    }
                }
                """);

            context.AddSource("ChainedExports.g.cs", builder.ToString());
        });
    }

    /// <summary>
    /// Tries to get the name of a dependent WinRT component from a given assembly.
    /// </summary>
    /// <param name="assemblySymbol">The assembly symbol to analyze.</param>
    /// <param name="compilation">The <see cref="Compilation"/> instance to use.</param>
    /// <param name="token">The <see cref="CancellationToken"/> instance to use.</param>
    /// <param name="name">The resulting type name, if found.</param>
    /// <returns>Whether a type name was found.</returns>
    internal static bool TryGetDependentAssemblyExportsTypeName(
        IAssemblySymbol assemblySymbol,
        Compilation compilation,
        CancellationToken token,
        [NotNullWhen(true)] out string? name)
    {
        // Get the attribute to lookup to find the target type to use
        INamedTypeSymbol winRTAssemblyExportsTypeAttributeSymbol = compilation.GetTypeByMetadataName("WinRT.WinRTAssemblyExportsTypeAttribute")!;

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
}
