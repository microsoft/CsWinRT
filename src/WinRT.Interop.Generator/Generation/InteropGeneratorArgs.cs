// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using WindowsRuntime.InteropGenerator.Attributes;

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

    /// <summary>Gets the maximum number of parallel tasks to use for execution.</summary>
    [CommandLineArgumentName("--max-degrees-of-parallelism")]
    public required int MaxDegreesOfParallelism { get; init; }

    /// <summary>Gets the token for the operation.</summary>
    public required CancellationToken Token { get; init; }

    /// <summary>Gets the directory to use to place the debug repro, if requested.</summary>
    [CommandLineArgumentName("--debug-repro-directory")]
    public string? DebugReproDirectory { get; init; }
}
