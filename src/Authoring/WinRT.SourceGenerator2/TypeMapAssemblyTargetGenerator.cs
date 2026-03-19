// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
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

        // Get whether the project has CsWinRTUseWindowsUIXamlProjections set
        IncrementalValueProvider<bool> useWindowsUIXamlProjections = context.AnalyzerConfigOptionsProvider.Select(static (options, token) =>
        {
            return options.GlobalOptions.GetCsWinRTUseWindowsUIXamlProjections();
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

        // Combine all matching assembly names and filter out the Windows SDK ones
        IncrementalValueProvider<ImmutableArray<string>> filteredAssemblyNames =
            assemblyNames
            .Where(static name => name is not null and not "Microsoft.Windows.SDK.NET" and not "Microsoft.Windows.UI.Xaml")
            .Collect()!;

        // Sort the assembly names
        IncrementalValueProvider<EquatableArray<string>> sortedAssemblyNames =
           filteredAssemblyNames
           .Select(static (names, token) => names.Sort(StringComparer.Ordinal).AsEquatableArray());

        // Whether the merged projection will be generated
        IncrementalValueProvider<bool> hasMergedProjection =
            filteredAssemblyNames
            .Select(static (assemblyNames, token) => !assemblyNames.IsDefaultOrEmpty);

        // Generate the attributes for all matching assemblies
        context.RegisterImplementationSourceOutput(sortedAssemblyNames, Execute.EmitPrivateProjectionsTypeMapAssemblyTargetAttributes);

        // Also generate the '[TypeMapAssemblyTarget]' entry for the default items
        context.RegisterImplementationSourceOutput(isGeneratorEnabled, Execute.EmitDefaultTypeMapAssemblyTargetAttributes);

        // Also generate the '[TypeMapAssemblyTarget]' entry for the merged projection
        context.RegisterImplementationSourceOutput(hasMergedProjection, Execute.EmitMergedProjectionTypeMapAssemblyTargetAttributes);

        // Get whether the xaml type map attributes should be emitted.
        IncrementalValueProvider<bool> isXamlGeneratorEnabled =
            isGeneratorEnabled
            .Combine(useWindowsUIXamlProjections)
            .Select(static (flags, token) => flags.Left && flags.Right);

        // Also generate the '[TypeMapAssemblyTarget]' entry for the xaml projection based on isXamlGeneratorEnabled.
        context.RegisterImplementationSourceOutput(isXamlGeneratorEnabled, Execute.EmitXamlTypeMapAssemblyTargetAttributes);
    }
}