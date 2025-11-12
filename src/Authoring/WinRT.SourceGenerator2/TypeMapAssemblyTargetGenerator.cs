// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
        // Gather all PE references from the current compilation
        IncrementalValuesProvider<EquatablePortableExecutableReference> executableReferences =
            context.CompilationProvider
            .SelectMany(Execute.GetAllPortableExecutableReferences);

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
        IncrementalValuesProvider<string?> assemblyNames = enabledExecutableReferences.Select(Execute.GetAssemblyNameIfWindowsRuntimeReferenceAssembly);

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
        context.RegisterImplementationSourceOutput(sortedAssemblyNames, Execute.EmitPrivateProjectionsTypeMapAssemblyTargetAttributes);

        // Also generate the '[TypeMapAssemblyTarget]' entry for the default items
        context.RegisterImplementationSourceOutput(isGeneratorEnabled, Execute.EmitDefaultTypeMapAssemblyTargetAttributes);
    }
}
