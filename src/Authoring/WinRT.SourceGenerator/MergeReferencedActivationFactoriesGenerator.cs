﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

#nullable enable

namespace Generator;

[Generator]
public sealed class MergeReferencedActivationFactoriesGenerator : IIncrementalGenerator
{
    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Gather all valid metadata references from the current compilation
        IncrementalValuesProvider<EquatableMetadataReference> metadataReferences =
            context.CompilationProvider
            .SelectMany(static (compilation, token) =>
        {
            var metadataReferences = ImmutableArray.CreateBuilder<EquatableMetadataReference>();

            foreach (MetadataReference metadataReference in compilation.References)
            {
                // We are only interested in PE references or project references
                if (metadataReference is not (PortableExecutableReference or CompilationReference))
                {
                    continue;
                }

                metadataReferences.Add(new EquatableMetadataReference(metadataReference, compilation));
            }

            return metadataReferences.ToImmutable();
        });

        // Get whether the generator is enabled
        IncrementalValueProvider<bool> isGeneratorEnabled = context.AnalyzerConfigOptionsProvider.Select(static (options, token) =>
        {
            return options.GetCsWinRTMergeReferencedActivationFactories();
        });

        // Bypass all items if the flag is not set
        IncrementalValuesProvider<(EquatableMetadataReference Value, bool)> enabledMetadataReferences =
            metadataReferences
            .Combine(isGeneratorEnabled)
            .Where(static item => item.Right);

        // Get the fully qualified type names of all assembly exports types to merge
        IncrementalValueProvider<ImmutableArray<string>> assemblyExportsTypeNames =
            enabledMetadataReferences
            .Select(static (executableReference, token) =>
            {
                Compilation compilation = executableReference.Value.GetCompilationUnsafe();

                // We only care about resolved assembly symbols (this should always be the case anyway)
                if (compilation.GetAssemblyOrModuleSymbol(executableReference.Value.Reference) is not IAssemblySymbol assemblySymbol)
                {
                    return null;
                }

                token.ThrowIfCancellationRequested();

                // Get the attribute to lookup to find the target type to use
                INamedTypeSymbol winRTAssemblyExportsTypeAttributeSymbol = compilation.GetTypeByMetadataName("WinRT.WinRTAssemblyExportsTypeAttribute")!;

                // Make sure the assembly does have the attribute on it
                if (!assemblySymbol.TryGetAttributeWithType(winRTAssemblyExportsTypeAttributeSymbol, out AttributeData? attributeData))
                {
                    return null;
                }

                token.ThrowIfCancellationRequested();

                // Sanity check: we should have a valid type in the annotation
                if (attributeData.ConstructorArguments is not [{ Kind: TypedConstantKind.Type, Type: INamedTypeSymbol assemblyExportsTypeSymbol }])
                {
                    return null;
                }

                // Other sanity check: this type should be accessible from this compilation
                if (!assemblyExportsTypeSymbol.IsAccessibleFromCompilationAssembly(compilation))
                {
                    return null;
                }

                token.ThrowIfCancellationRequested();

                return assemblyExportsTypeSymbol.ToDisplayString();
            })
            .Where(static name => name is not null)
            .Collect()!;

        // Generate the chaining helper
        context.RegisterImplementationSourceOutput(assemblyExportsTypeNames, static (context, assemblyExportsTypeNames) =>
        {
            if (assemblyExportsTypeNames.IsEmpty)
            {
                context.AddSource("ChainedExports.g.cs", """
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
                            internal static IntPtr TryGetDependentActivationFactory(ReadOnlySpan<char> fullyQualifiedTypeName)
                            {
                                return default;
                            }
                        }
                    }
                    """);
            }
            else
            {
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
                            internal static IntPtr TryGetDependentActivationFactory(ReadOnlySpan<char> fullyQualifiedTypeName)
                            {                  
                    """);

                foreach (string assemblyExportsTypeName in assemblyExportsTypeNames)
                {
                    builder.AppendLine($$"""
                                    IntPtr obj = global::{{assemblyExportsTypeName}}.GetActivationFactory(fullyQualifiedTypeName);

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

            }
        });
    }

    /// <summary>
    /// An equatable <see cref="MetadataReference"/> type that weakly references a <see cref="Microsoft.CodeAnalysis.Compilation"/> object.
    /// </summary>
    /// <param name="reference">The <see cref="MetadataReference"/> object to wrap.</param>
    /// <param name="compilation">The <see cref="Microsoft.CodeAnalysis.Compilation"/> instance where <paramref name="reference"/> comes from.</param>
    public sealed class EquatableMetadataReference(MetadataReference reference, Compilation compilation) : IEquatable<EquatableMetadataReference>
    {
        /// <summary>
        /// A weak reference to the <see cref="Microsoft.CodeAnalysis.Compilation"/> object owning <see cref="Reference"/>.
        /// </summary>
        private readonly WeakReference<Compilation> Compilation = new(compilation);

        /// <summary>
        /// Gets the <see cref="MetadataReference"/> object for this instance.
        /// </summary>
        public MetadataReference Reference { get; } = reference;

        /// <summary>
        /// Gets the <see cref="Microsoft.CodeAnalysis.Compilation"/> object for <see cref="Reference"/>.
        /// </summary>
        /// <returns>The <see cref="Microsoft.CodeAnalysis.Compilation"/> object for <see cref="Reference"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the <see cref="Microsoft.CodeAnalysis.Compilation"/> object has been collected.</exception>
        /// <remarks>
        /// This method should only be used from incremental steps immediately following a change in the metadata reference
        /// being used, as that would guarantee that that <see cref="Microsoft.CodeAnalysis.Compilation"/> object would be alive.
        /// </remarks>
        public Compilation GetCompilationUnsafe()
        {
            if (Compilation.TryGetTarget(out Compilation? compilation))
            {
                return compilation;
            }

            throw new InvalidOperationException("No compilation object is available.");
        }

        /// <inheritdoc/>
        public bool Equals(EquatableMetadataReference other)
        {
            if (other is null)
            {
                return false;
            }

            if (Reference is PortableExecutableReference thisExecutionReference)
            {
                return
                    other.Reference is PortableExecutableReference otherExecutionReference &&
                    thisExecutionReference.GetMetadataId() == otherExecutionReference.GetMetadataId();
            }

            if (Reference is CompilationReference thisCompilationReference)
            {
                return
                    other.Reference is CompilationReference otherCompilationReference &&
                    ReferenceEquals(thisCompilationReference, otherCompilationReference);
            }

            throw new InvalidOperationException("Invalid metadata reference type in the current instance.");
        }
    }
}
