// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Threading;
using WindowsRuntime.GeneratorCli;
using WindowsRuntime.GeneratorCli.Attributes;
using WindowsRuntime.GeneratorCli.Parsing;
using WindowsRuntime.WinMDGenerator.Errors;

namespace WindowsRuntime.WinMDGenerator.Generation;

/// <summary>
/// Input parameters for <see cref="WinMDGenerator"/>.
/// </summary>
internal sealed class WinMDGeneratorArgs : IGeneratorArgs
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

    /// <summary>
    /// Parses a <see cref="WinMDGeneratorArgs"/> instance from a response file at the given path.
    /// </summary>
    /// <param name="path">The path to the response file (optionally prefixed with <c>@</c>).</param>
    /// <param name="token">The cancellation token for the operation.</param>
    /// <returns>The resulting <see cref="WinMDGeneratorArgs"/> instance.</returns>
    public static WinMDGeneratorArgs ParseFromResponseFile(string path, CancellationToken token)
    {
        return ResponseFileParser.Parse<WinMDGeneratorArgs, WellKnownWinMDExceptions>(path, token);
    }

    /// <summary>
    /// Parses a <see cref="WinMDGeneratorArgs"/> instance from a response file read from a stream.
    /// </summary>
    /// <param name="stream">The stream containing the response file content.</param>
    /// <param name="token">The cancellation token for the operation.</param>
    /// <returns>The resulting <see cref="WinMDGeneratorArgs"/> instance.</returns>
    public static WinMDGeneratorArgs ParseFromResponseFile(Stream stream, CancellationToken token)
    {
        return ResponseFileParser.Parse<WinMDGeneratorArgs, WellKnownWinMDExceptions>(stream, token);
    }

    /// <summary>
    /// Formats the current <see cref="WinMDGeneratorArgs"/> instance into a response file text.
    /// </summary>
    /// <returns>The resulting response file text.</returns>
    public string FormatToResponseFile()
    {
        return ResponseFileBuilder.Format(this);
    }
}
