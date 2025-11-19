// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace WindowsRuntime.SourceGenerator;

/// <summary>
/// A generator to emit <see cref="System.Runtime.InteropServices.TypeMapAssemblyTargetAttribute{TTypeMapGroup}"/> entries in publishing projects.
/// </summary>
[Generator]
public sealed partial class TypeMapAssemblyTargetGenerator : IIncrementalGenerator
{
    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
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

        // Gather all PE references from the current compilation
        IncrementalValuesProvider<EquatablePortableExecutableReference> executableReferences =
            context.CompilationProvider
            .Combine(isGeneratorEnabled)
            .SelectMany(Execute.GetAllPortableExecutableReferences);

        // Get all the names of assemblies with '[WindowsRuntimeReferenceAssembly]'
        IncrementalValuesProvider<string?> assemblyNames = executableReferences.Select(Execute.GetAssemblyNameIfWindowsRuntimeReferenceAssembly);

        // Combine all matching assembly names
        IncrementalValueProvider<ImmutableArray<string>> filteredAssemblyNames =
            assemblyNames
            .Where(static name => name is not null)
            .Collect()!;

        // Sort the assembly names
        IncrementalValueProvider<EquatableArray<string>> sortedAssemblyNames =
           filteredAssemblyNames
           .Select(static (names, token) => names.Sort(StringComparer.Ordinal).AsEquatableArray());

        // Generate the attributes for all matching assemblies
        context.RegisterImplementationSourceOutput(sortedAssemblyNames, Execute.EmitPrivateProjectionsTypeMapAssemblyTargetAttributes);

        // Also generate the '[TypeMapAssemblyTarget]' entry for the default items
        context.RegisterImplementationSourceOutput(isGeneratorEnabled, Execute.EmitDefaultTypeMapAssemblyTargetAttributes);
    }
}