// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using WinRT.SourceGenerator;

#nullable enable

namespace Generator;

[Generator]
public sealed class RcwReflectionFallbackGenerator : IIncrementalGenerator
{
    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Gather all PE references from the current compilation
        IncrementalValuesProvider<EquatablePortableExecutableReference> executableReferences =
            context.CompilationProvider
            .SelectMany(static (compilation, token) =>
        {
            var executableReferences = ImmutableArray.CreateBuilder<EquatablePortableExecutableReference>();

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
            return compilation.Options.OutputKind is OutputKind.ConsoleApplication or OutputKind.WindowsApplication or OutputKind.WindowsRuntimeApplication;
        });

        // Get whether the generator is explicitly set as opt-in
        IncrementalValueProvider<bool> isGeneratorForceOptIn = context.AnalyzerConfigOptionsProvider.Select(static (options, token) =>
        {
            return options.GetCsWinRTRcwFactoryFallbackGeneratorForceOptIn();
        });

        // Get whether the generator should actually run or not
        IncrementalValueProvider<bool> isGeneratorEnabled = isOutputTypeExe.Combine(isGeneratorForceOptIn).Select(static (flags, token) => flags.Left || flags.Right);

        // Bypass all items if the flag is not set
        IncrementalValuesProvider<(EquatablePortableExecutableReference Value, bool)> enabledExecutableReferences =
            executableReferences
            .Combine(isGeneratorEnabled)
            .Where(static item => item.Right);

        // Get all the names of the projected types to root
        IncrementalValuesProvider<EquatableArray<string>> executableTypeNames = enabledExecutableReferences.Select(static (executableReference, token) =>
        {
            Compilation compilation = executableReference.Value.GetCompilationUnsafe();

            // We only care about resolved assembly symbols (this should always be the case anyway)
            if (compilation.GetAssemblyOrModuleSymbol(executableReference.Value.Reference) is not IAssemblySymbol assemblySymbol)
            {
                return EquatableArray<string>.FromImmutableArray(ImmutableArray<string>.Empty);
            }

            // If the assembly is not an old projections assembly, we have nothing to do
            if (!IsOldProjectionAssembly(assemblySymbol))
            {
                return EquatableArray<string>.FromImmutableArray(ImmutableArray<string>.Empty);
            }

            token.ThrowIfCancellationRequested();

            ITypeSymbol attributeSymbol = compilation.GetTypeByMetadataName("System.Attribute")!;
            ITypeSymbol windowsRuntimeTypeAttributeSymbol = compilation.GetTypeByMetadataName("WinRT.WindowsRuntimeTypeAttribute")!;

            ImmutableArray<string>.Builder executableTypeNames = ImmutableArray.CreateBuilder<string>();

            // Process all type symbols in the current assembly
            foreach (INamedTypeSymbol typeSymbol in VisitNamedTypeSymbolsExceptABI(assemblySymbol))
            {
                token.ThrowIfCancellationRequested();

                // We only care about public or internal classes
                if (typeSymbol is not { TypeKind: TypeKind.Class, DeclaredAccessibility: Accessibility.Public or Accessibility.Internal })
                {
                    continue;
                }

                // Ignore static types (we only care about actual RCW types we can instantiate)
                if (typeSymbol.IsStatic)
                {
                    continue;
                }

                // Ignore attribute types (they're never instantiated like normal RCWs)
                if (IsDerivedFromType(typeSymbol, attributeSymbol))
                {
                    continue;
                }

                // If the type is not a generated projected type, do nothing
                if (!GeneratorHelper.HasAttributeWithType(typeSymbol, windowsRuntimeTypeAttributeSymbol))
                {
                    continue;
                }

                // Double check we can in fact access this type (or we can't reference it)
                if (!compilation.IsSymbolAccessibleWithin(typeSymbol, compilation.Assembly))
                {
                    continue;
                }

                executableTypeNames.Add(typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }

            token.ThrowIfCancellationRequested();

            return EquatableArray<string>.FromImmutableArray(executableTypeNames.ToImmutable());
        });

        // Combine all names into a single sequence
        IncrementalValueProvider<ImmutableArray<string>> projectedTypeNames =
            executableTypeNames
            .Where(static names => !names.IsEmpty)
            .SelectMany(static (executableTypeNames, token) => executableTypeNames.AsImmutableArray())
            .Collect();

        // Generate the [DynamicDependency] attributes
        context.RegisterImplementationSourceOutput(projectedTypeNames, static (context, projectedTypeNames) =>
        {
            if (projectedTypeNames.IsEmpty)
            {
                return;
            }

            StringBuilder builder = new();

            builder.AppendLine("""
                // <auto-generated/>
                #pragma warning disable

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
                builder.Append("        [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicConstructors, typeof(");
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
        if (assemblySymbol.Modules.First() is not { ReferencedAssemblies: { Length: > 0 } dependentAssemblies })
        {
            return false;
        }

        // Scan all dependent assemblies to look for CsWinRT with version < 2.0.8
        foreach (AssemblyIdentity assemblyIdentity in dependentAssemblies)
        {
            if (assemblyIdentity.Name == "WinRT.Runtime")
            {
                return assemblyIdentity.Version < new Version(2, 0, 8) && 
                    assemblyIdentity.Version != new Version(0, 0, 0, 0);
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
    /// Checks whether a given type is derived from a specified type.
    /// </summary>
    /// <param name="typeSymbol">The input <see cref="ITypeSymbol"/> instance to check.</param>
    /// <param name="baseTypeSymbol">The base type to look for.</param>
    /// <returns>Whether <paramref name="typeSymbol"/> derives from <paramref name="baseTypeSymbol"/>.</returns>
    private static bool IsDerivedFromType(ITypeSymbol typeSymbol, ITypeSymbol baseTypeSymbol)
    {
        for (ITypeSymbol? currentSymbol = typeSymbol.BaseType;
             currentSymbol is { SpecialType: not SpecialType.System_Object };
             currentSymbol = currentSymbol.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(currentSymbol, baseTypeSymbol))
            {
                return true;
            }
        }

        return false;
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
            if (Compilation.TryGetTarget(out Compilation? compilation))
            {
                return compilation;
            }

            throw new InvalidOperationException("No compilation object is available.");
        }

        /// <inheritdoc/>
        public bool Equals(EquatablePortableExecutableReference other)
        {
            if (other is null)
            {
                return false;
            }

            return other.Reference.GetMetadataId() == Reference.GetMetadataId();
        }
    }
}
