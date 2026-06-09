// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using WindowsRuntime.GeneratorCli.Attributes;

namespace WindowsRuntime.WinMDGenerator.Generation;

/// <summary>
/// Input parameters for <see cref="WinMDGenerator"/>.
/// </summary>
internal sealed partial class WinMDGeneratorArgs
{
    /// <summary>Gets the path to the compiled input assembly (.dll) to analyze.</summary>
    [CommandLineArgumentName("--input-assembly-path")]
    public required string InputAssemblyPath { get; init; }

    /// <summary>Gets the input reference .dll paths for type resolution.</summary>
    [CommandLineArgumentName("--reference-assembly-paths")]
    public required string[] ReferenceAssemblyPaths { get; init; }

    /// <summary>Gets the output .winmd file path.</summary>
    [CommandLineArgumentName("--output-winmd-path")]
    public required string OutputWinmdPath { get; init; }

    /// <summary>Gets the assembly version to use for the generated WinMD.</summary>
    [CommandLineArgumentName("--assembly-version")]
    public required string AssemblyVersion { get; init; }

    /// <summary>Gets whether to use <c>Windows.UI.Xaml</c> projections.</summary>
    [CommandLineArgumentName("--use-windows-ui-xaml-projections")]
    public required bool UseWindowsUIXamlProjections { get; init; }

    /// <summary>Gets the token for the operation.</summary>
    public required CancellationToken Token { get; init; }

    /// <summary>Gets the directory to use to place the debug repro, if requested.</summary>
    [CommandLineArgumentName("--debug-repro-directory")]
    public string? DebugReproDirectory { get; init; }
}