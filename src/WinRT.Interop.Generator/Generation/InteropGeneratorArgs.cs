// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using WindowsRuntime.Generator.Attributes;
using WindowsRuntime.Generator.Generation;

namespace WindowsRuntime.InteropGenerator.Generation;

/// <summary>
/// Input parameters for <see cref="InteropGenerator"/>.
/// </summary>
internal sealed partial class InteropGeneratorArgs
{
    /// <summary>Gets the input .dll paths.</summary>
    [CommandLineArgumentName("--reference-assembly-paths")]
    public required string[] ReferenceAssemblyPaths { get; init; }

    /// <summary>Gets the path of the assembly that was built.</summary>
    [CommandLineArgumentName("--output-assembly-path")]
    public required string OutputAssemblyPath { get; init; }

    /// <summary>Gets the directory to use to place the generated assembly.</summary>
    [CommandLineArgumentName("--generated-assembly-directory")]
    public required string GeneratedAssemblyDirectory { get; init; }

    /// <summary>Gets whether to use <c>Windows.UI.Xaml</c> projections.</summary>
    [CommandLineArgumentName("--use-windows-ui-xaml-projections")]
    public required bool UseWindowsUIXamlProjections { get; init; }

    /// <summary>Gets whether to validate the assembly version of <c>WinRT.Runtime.dll</c>, to ensure it matches the generator.</summary>
    [CommandLineArgumentName("--validate-winrt-runtime-assembly-version")]
    public required bool ValidateWinRTRuntimeAssemblyVersion { get; init; }

    /// <summary>Gets whether to validate that any references to <c>WinRT.Runtime.dll</c> version 2 are present across any assemblies.</summary>
    [CommandLineArgumentName("--validate-winrt-runtime-dll-version-2-references")]
    public required bool ValidateWinRTRuntimeDllVersion2References { get; init; }

    /// <summary>Gets whether to enable incremental generation (i.e. with a cache file on disk saving the full set of types to generate).</summary>
    [CommandLineArgumentName("--enable-incremental-generation")]
    public required bool EnableIncrementalGeneration { get; init; }

    /// <summary>Gets whether to treat warnings coming from 'cswinrtgen' as errors (regardless of the global 'TreatWarningsAsErrors' setting).</summary>
    [CommandLineArgumentName("--treat-warnings-as-errors")]
    public required bool TreatWarningsAsErrors { get; init; }

    /// <summary>Gets the maximum number of parallel tasks to use for execution.</summary>
    [CommandLineArgumentName("--max-degrees-of-parallelism")]
    public required int MaxDegreesOfParallelism { get; init; }

    /// <summary>Gets the token for the operation.</summary>
    public required CancellationToken Token { get; init; }

    /// <summary>Gets the directory to use to place the debug repro, if requested.</summary>
    [CommandLineArgumentName("--debug-repro-directory")]
    public string? DebugReproDirectory { get; init; }

    /// <summary>
    /// Parses an <see cref="InteropGeneratorArgs"/> instance from a target response file.
    /// </summary>
    /// <param name="path">The path to the response file.</param>
    /// <param name="token">The token for the operation.</param>
    /// <returns>The resulting <see cref="InteropGeneratorArgs"/> instance.</returns>
    public static InteropGeneratorArgs ParseFromResponseFile(string path, CancellationToken token)
    {
        Dictionary<string, string> argsMap = GeneratorArgs.ParseFromResponseFile(path);

        // Parse all commands to create the managed arguments to use
        return new()
        {
            ReferenceAssemblyPaths = GeneratorArgs.GetStringArrayArgument<InteropGeneratorArgs>(argsMap, nameof(ReferenceAssemblyPaths)),
            OutputAssemblyPath = GeneratorArgs.GetStringArgument<InteropGeneratorArgs>(argsMap, nameof(OutputAssemblyPath)),
            GeneratedAssemblyDirectory = GeneratorArgs.GetStringArgument<InteropGeneratorArgs>(argsMap, nameof(GeneratedAssemblyDirectory)),
            UseWindowsUIXamlProjections = GeneratorArgs.GetBooleanArgument<InteropGeneratorArgs>(argsMap, nameof(UseWindowsUIXamlProjections)),
            ValidateWinRTRuntimeAssemblyVersion = GeneratorArgs.GetBooleanArgument<InteropGeneratorArgs>(argsMap, nameof(ValidateWinRTRuntimeAssemblyVersion)),
            ValidateWinRTRuntimeDllVersion2References = GeneratorArgs.GetBooleanArgument<InteropGeneratorArgs>(argsMap, nameof(ValidateWinRTRuntimeDllVersion2References)),
            EnableIncrementalGeneration = GeneratorArgs.GetBooleanArgument<InteropGeneratorArgs>(argsMap, nameof(EnableIncrementalGeneration)),
            TreatWarningsAsErrors = GeneratorArgs.GetBooleanArgument<InteropGeneratorArgs>(argsMap, nameof(TreatWarningsAsErrors)),
            MaxDegreesOfParallelism = GeneratorArgs.GetInt32Argument<InteropGeneratorArgs>(argsMap, nameof(MaxDegreesOfParallelism)),
            DebugReproDirectory = GeneratorArgs.GetNullableStringArgument<InteropGeneratorArgs>(argsMap, nameof(DebugReproDirectory)),
            Token = token
        };
    }
}
