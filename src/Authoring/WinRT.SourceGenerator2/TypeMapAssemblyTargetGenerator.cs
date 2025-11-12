// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace WindowsRuntime.SourceGenerator;

/// <summary>
/// A generator to emit <see cref="System.Runtime.InteropServices.TypeMapAssemblyTargetAttribute{TTypeMapGroup}"/> entries in publishing projects.
/// </summary>
[Generator]
public sealed class TypeMapAssemblyTargetGenerator : IIncrementalGenerator
{
    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Gather all PE references from the current compilation
        IncrementalValuesProvider<EquatablePortableExecutableReference> executableReferences =
            context.CompilationProvider
            .SelectMany(static (compilation, token) =>
            {
                ImmutableArray<EquatablePortableExecutableReference>.Builder executableReferences = ImmutableArray.CreateBuilder<EquatablePortableExecutableReference>();

                foreach (MetadataReference metadataReference in compilation.References)
                {
                    // We are only interested in PE references (not project references)
                    if (metadataReference is not PortableExecutableReference executableReference)
                    {
                        continue;
                    }

                    executableReferences.Add(new EquatablePortableExecutableReference(executableReference, compilation));
                }

                return executableReferences.ToImmutable();
            });

        // Get whether the current project is an .exe
        IncrementalValueProvider<bool> isOutputTypeExe = context.CompilationProvider.Select(static (compilation, token) =>
        {
            return compilation.Options.OutputKind is OutputKind.ConsoleApplication or OutputKind.WindowsApplication;
        });

        // Get whether the current project is a library
        IncrementalValueProvider<bool> isOutputTypeLibrary = context.CompilationProvider.Select(static (compilation, token) =>
        {
            return compilation.Options.OutputKind is OutputKind.DynamicallyLinkedLibrary;
        });

        // Get whether the project is being published with Native AOT
        IncrementalValueProvider<bool> isPublishAot = context.AnalyzerConfigOptionsProvider.Select(static (options, token) =>
        {
            return options.GlobalOptions.GetPublishAot();
        });

        // Get whether the current project is a library published with Native AOT
        IncrementalValueProvider<bool> isPublishAotLibrary =
            isOutputTypeLibrary
            .Combine(isPublishAot)
            .Select(static (flags, token) => flags.Left && flags.Right);

        // Get whether the generator should actually run or not
        IncrementalValueProvider<bool> isGeneratorEnabled =
            isOutputTypeExe
            .Combine(isPublishAotLibrary)
            .Select(static (flags, token) => flags.Left || flags.Right);

        // Bypass all items if the flag is not set
        IncrementalValuesProvider<(EquatablePortableExecutableReference Value, bool)> enabledExecutableReferences =
            executableReferences
            .Combine(isGeneratorEnabled)
            .Where(static item => item.Right);

        // Get all the names of assemblies with '[WindowsRuntimeReferenceAssembly]'
        IncrementalValuesProvider<string?> assemblyNames = enabledExecutableReferences.Select(static (executableReference, token) =>
        {
            Compilation compilation = executableReference.Value.GetCompilationUnsafe();

            token.ThrowIfCancellationRequested();

            // We only care about resolved assembly symbols (this should always be the case anyway)
            if (compilation.GetAssemblyOrModuleSymbol(executableReference.Value.Reference) is not IAssemblySymbol assemblySymbol)
            {
                return null;
            }

            token.ThrowIfCancellationRequested();

            ITypeSymbol attributeSymbol = compilation.GetTypeByMetadataName("WindowsRuntime.InteropServices.WindowsRuntimeReferenceAssemblyAttribute")!;

            token.ThrowIfCancellationRequested();

            return assemblySymbol.HasAttributeWithType(attributeSymbol) ? assemblySymbol.Identity.Name : null;
        });

        // Combine all matching assembly names
        IncrementalValueProvider<ImmutableArray<string>> filteredAssemblyNames =
            assemblyNames
            .Where(static name => name is not null)
            .Collect()!;

        // Sort the assembly names
        IncrementalValueProvider<EquatableArray<string>> sortedAssemblyNames =
           filteredAssemblyNames
           .Select(static (names, token) => names.Sort().AsEquatableArray());

        // Generate the attributes for all matching assemblies
        context.RegisterImplementationSourceOutput(sortedAssemblyNames, static (context, assemblyNames) =>
        {
            if (assemblyNames.IsEmpty)
            {
                return;
            }

            StringBuilder builder = new();

            _ = builder.AppendLine($"""
                // <auto-generated/>
                #pragma warning disable

                """);

            // Add a '[TypeMapAssemblyTarget]' entry for each assembly
            foreach (string assemblyName in assemblyNames)
            {
                _ = builder.AppendLine($"""
                    [assembly: global::System.Runtime.InteropServices.TypeMapAssemblyTarget<global::WindowsRuntime.InteropServices.WindowsRuntimeComWrappersTypeMapGroup>("{assemblyName}")]
                    """);
            }

            context.AddSource("TypeMapAssemblyTarget.PrivateProjections.g.cs", builder.ToString());
        });

        // Also generate the '[TypeMapAssemblyTarget]' entry for the default items
        context.RegisterImplementationSourceOutput(isGeneratorEnabled, static (context, isEnabled) =>
        {
            if (!isEnabled)
            {
                return;
            }

            const string source = """
                // <auto-generated/>
                #pragma warning disable

                [assembly: global::System.Runtime.InteropServices.TypeMapAssemblyTarget<global::WindowsRuntime.InteropServices.WindowsRuntimeComWrappersTypeMapGroup>("WinRT.Interop")]
                //[assembly: global::System.Runtime.InteropServices.TypeMapAssemblyTarget<global::WindowsRuntime.InteropServices.WindowsRuntimeComWrappersTypeMapGroup>("WinRT.Projection")]
                [assembly: global::System.Runtime.InteropServices.TypeMapAssemblyTarget<global::WindowsRuntime.InteropServices.WindowsRuntimeComWrappersTypeMapGroup>("WinRT.Runtime2")]
                """;

            context.AddSource("TypeMapAssemblyTarget.g.cs", source);
        });
    }

    /// <summary>
    /// An equatable <see cref="PortableExecutableReference"/> type that weakly references a <see cref="Microsoft.CodeAnalysis.Compilation"/> object.
    /// </summary>
    /// <param name="executableReference">The <see cref="PortableExecutableReference"/> object to wrap.</param>
    /// <param name="compilation">The <see cref="Microsoft.CodeAnalysis.Compilation"/> instance where <paramref name="executableReference"/> comes from.</param>
    public sealed class EquatablePortableExecutableReference(
        PortableExecutableReference executableReference,
        Compilation compilation) : IEquatable<EquatablePortableExecutableReference>
    {
        /// <summary>
        /// A weak reference to the <see cref="Microsoft.CodeAnalysis.Compilation"/> object owning <see cref="Reference"/>.
        /// </summary>
        private readonly WeakReference<Compilation> Compilation = new(compilation);

        /// <summary>
        /// Gets the <see cref="PortableExecutableReference"/> object for this instance.
        /// </summary>
        public PortableExecutableReference Reference { get; } = executableReference;

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
            return Compilation.TryGetTarget(out Compilation? compilation)
                ? compilation
                : throw new InvalidOperationException("No compilation object is available.");
        }

        /// <inheritdoc/>
        public override bool Equals(object? other)
        {
            return Equals(other as EquatablePortableExecutableReference);
        }

        /// <inheritdoc/>
        public bool Equals([NotNullWhen(true)] EquatablePortableExecutableReference? other)
        {
            return other?.Reference.GetMetadataId() == Reference.GetMetadataId();
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Reference.GetMetadataId().GetHashCode();
        }
    }
}
