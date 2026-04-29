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

        // Get whether the project is being built for a native consumer
        IncrementalValueProvider<bool> isBuildForNativeConsumer = context.AnalyzerConfigOptionsProvider.Select(static (options, token) =>
        {
            return options.GlobalOptions.GetBuildForNativeConsumer();
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
            .Combine(isBuildForNativeConsumer)
            .Select(static (flags, token) => flags.Left.Left || flags.Left.Right || flags.Right);

        // Gather all PE references from the current compilation
        IncrementalValuesProvider<EquatablePortableExecutableReference> executableReferences =
            context.CompilationProvider
            .Combine(isGeneratorEnabled)
            .SelectMany(Execute.GetAllPortableExecutableReferences);

        // Get all the names of assemblies with '[WindowsRuntimeReferenceAssembly]'
        IncrementalValuesProvider<string?> referenceAssemblyNames =
            executableReferences.Select(Execute.GetAssemblyNameIfWindowsRuntimeReferenceAssembly);

        // Get all the names of assemblies with '[WindowsRuntimeComponentAssembly]'
        IncrementalValuesProvider<string?> componentAssemblyNames =
            executableReferences.Select(Execute.GetAssemblyNameIfWindowsRuntimeComponentAssembly);

        // Collect component assembly names
        IncrementalValueProvider<ImmutableArray<string>> collectedComponentAssemblyNames =
            componentAssemblyNames
            .Where(static name => name is not null)
            .Select(static (name, _) => name!)
            .Collect();

        // Combine all matching assembly names (reference + component) and filter out the Windows SDK ones
        IncrementalValueProvider<ImmutableArray<string>> filteredAssemblyNames =
            referenceAssemblyNames
            .Where(static name => name is not null and not "Microsoft.Windows.SDK.NET" and not "Microsoft.Windows.UI.Xaml")
            .Select(static (name, _) => name!)
            .Collect()
            .Combine(collectedComponentAssemblyNames)
            .Select(static (pair, token) => pair.Left.AddRange(pair.Right));

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

        // Get whether the current project is a component
        IncrementalValueProvider<bool> isComponent = context.AnalyzerConfigOptionsProvider.Select(static (options, token) =>
        {
            return options.GlobalOptions.GetCsWinRTComponent();
        });

        // Whether any component assemblies are referenced, or the current project is itself a component
        IncrementalValueProvider<bool> hasComponentAssembly =
            collectedComponentAssemblyNames
            .Select(static (names, token) => !names.IsDefaultOrEmpty)
            .Combine(isComponent)
            .Select(static (pair, token) => pair.Left || pair.Right)
            .Combine(isGeneratorEnabled)
            .Select(static (pair, token) => pair.Left && pair.Right);

        // Also generate the '[TypeMapAssemblyTarget]' entry for WinRT.Component if any component assemblies exist
        context.RegisterImplementationSourceOutput(hasComponentAssembly, Execute.EmitComponentTypeMapAssemblyTargetAttributes);

        // Get whether the xaml type map attributes should be emitted.
        IncrementalValueProvider<bool> isXamlGeneratorEnabled =
            isGeneratorEnabled
            .Combine(useWindowsUIXamlProjections)
            .Select(static (flags, token) => flags.Left && flags.Right);

        // Also generate the '[TypeMapAssemblyTarget]' entry for the xaml projection based on isXamlGeneratorEnabled.
        context.RegisterImplementationSourceOutput(isXamlGeneratorEnabled, Execute.EmitXamlTypeMapAssemblyTargetAttributes);
    }
}