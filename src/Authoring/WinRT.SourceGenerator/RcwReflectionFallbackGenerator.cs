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

        // Get whether the generator is explicitly set as opt-out
        IncrementalValueProvider<bool> isGeneratorForceOptOut = context.AnalyzerConfigOptionsProvider.Select(static (options, token) =>
        {
            return options.GetCsWinRTRcwFactoryFallbackGeneratorForceOptOut();
        });

        IncrementalValueProvider<bool> csWinRTAotWarningEnabled = context.AnalyzerConfigOptionsProvider.Select(static (options, token) =>
        {
            return options.GetCsWinRTAotWarningLevel() >= 1;
        });

        // Get whether the generator should actually run or not
        IncrementalValueProvider<bool> isGeneratorEnabled =
            isOutputTypeExe
            .Combine(isGeneratorForceOptIn)
            .Combine(isGeneratorForceOptOut)
            .Select(static (flags, token) => (flags.Left.Left || flags.Left.Right) && !flags.Right);

        // Bypass all items if the flag is not set
        IncrementalValuesProvider<(EquatablePortableExecutableReference Value, bool)> enabledExecutableReferences =
            executableReferences
            .Combine(isGeneratorEnabled)
            .Where(static item => item.Right);

        // Get all the names of the projected types to root
        IncrementalValuesProvider<EquatableArray<RcwReflectionFallbackType>> executableTypeNames = enabledExecutableReferences.Select(static (executableReference, token) =>
        {
            Compilation compilation = executableReference.Value.GetCompilationUnsafe();

            // We only care about resolved assembly symbols (this should always be the case anyway)
            if (compilation.GetAssemblyOrModuleSymbol(executableReference.Value.Reference) is not IAssemblySymbol assemblySymbol)
            {
                return EquatableArray<RcwReflectionFallbackType>.FromImmutableArray(ImmutableArray<RcwReflectionFallbackType>.Empty);
            }

            // If the assembly is not an old projections assembly, we have nothing to do
            if (!GeneratorHelper.IsOldProjectionAssembly(assemblySymbol))
            {
                return EquatableArray<RcwReflectionFallbackType>.FromImmutableArray(ImmutableArray<RcwReflectionFallbackType>.Empty);
            }

            token.ThrowIfCancellationRequested();

            ITypeSymbol attributeSymbol = compilation.GetTypeByMetadataName("System.Attribute")!;
            ITypeSymbol windowsRuntimeTypeAttributeSymbol = compilation.GetTypeByMetadataName("WinRT.WindowsRuntimeTypeAttribute")!;

            ImmutableArray<RcwReflectionFallbackType>.Builder executableTypeNames = ImmutableArray.CreateBuilder<RcwReflectionFallbackType>();

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

                var typeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                // These types are in the existing WinUI projection, but have been moved to the Windows SDK projection.
                // So if we see those, we want to ignore them.
                if (typeName == "global::Windows.UI.Text.ContentLinkInfo" ||
                    typeName == "global::Windows.UI.Text.RichEditTextDocument" ||
                    typeName == "global::Windows.UI.Text.RichEditTextRange")
                {
                    continue;
                }

                // Check if we are able to resolve the type using GetTypeByMetadataName.  If not,
                // it typically indicates there are multiple definitions of this type in the references
                // and us emitting a dependency on this type would cause compiler error.  So emit
                // a warning instead.
                bool hasMultipleDefinitions = compilation.GetTypeByMetadataName(typeSymbol.MetadataName) is null;
                executableTypeNames.Add(new RcwReflectionFallbackType(typeName, hasMultipleDefinitions));
            }

            token.ThrowIfCancellationRequested();

            return EquatableArray<RcwReflectionFallbackType>.FromImmutableArray(executableTypeNames.ToImmutable());
        });

        // Combine all names into a single sequence
        IncrementalValueProvider<(ImmutableArray<RcwReflectionFallbackType>, bool)> projectedTypeNamesAndAotWarningEnabled =
            executableTypeNames
            .Where(static names => !names.IsEmpty)
            .SelectMany(static (executableTypeNames, token) => executableTypeNames.AsImmutableArray())
            .Collect()
            .Combine(csWinRTAotWarningEnabled);

        // Generate the [DynamicDependency] attributes
        context.RegisterImplementationSourceOutput(projectedTypeNamesAndAotWarningEnabled, static (SourceProductionContext context, (ImmutableArray<RcwReflectionFallbackType> projectedTypeNames, bool csWinRTAotWarningEnabled) value) =>
        {
            if (value.projectedTypeNames.IsEmpty)
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

            bool emittedDynamicDependency = false;
            foreach (RcwReflectionFallbackType projectedTypeName in value.projectedTypeNames)
            {
                // If there are multiple definitions of the type, emitting a dependency would result in a compiler error.
                // So instead, emit a diagnostic for it.
                if (projectedTypeName.HasMultipleDefinitions)
                {
                    var diagnosticDescriptor = value.csWinRTAotWarningEnabled ?
                        WinRTRules.ClassNotAotCompatibleOldProjectionMultipleInstancesWarning : WinRTRules.ClassNotAotCompatibleOldProjectionMultipleInstancesInfo;
                    // We have no location to emit the diagnostic as this is just a reference we detect.
                    context.ReportDiagnostic(Diagnostic.Create(diagnosticDescriptor, null, projectedTypeName.TypeName));
                }
                else
                {
                    emittedDynamicDependency = true;
                    builder.Append("        [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicConstructors, typeof(");
                    builder.Append(projectedTypeName.TypeName);
                    builder.AppendLine("))]");
                }
            }

            builder.Append("""
                        public static void InitializeRcwFallback()
                        {
                        }
                    }
                }
                """);

            if (emittedDynamicDependency)
            {
                context.AddSource("RcwFallbackInitializer.g.cs", builder.ToString());
            }
        });
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

    internal readonly record struct RcwReflectionFallbackType(string TypeName, bool HasMultipleDefinitions);
}
