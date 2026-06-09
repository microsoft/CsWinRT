// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Threading;
using WindowsRuntime.GeneratorCli.Attributes;
using WindowsRuntime.GeneratorCli.Parsing;
using WindowsRuntime.InteropGenerator.Errors;

namespace WindowsRuntime.InteropGenerator.Generation;

/// <summary>
/// Input parameters for <see cref="InteropGenerator"/>.
/// </summary>
internal sealed class InteropGeneratorArgs
{
    /// <summary>Gets the input reference .dll paths.</summary>
    [CommandLineArgumentName("--reference-assembly-paths")]
    public required string[] ReferenceAssemblyPaths { get; init; }

    /// <summary>Gets the input implementation .dll paths.</summary>
    [CommandLineArgumentName("--implementation-assembly-paths")]
    public required string[] ImplementationAssemblyPaths { get; init; }

    /// <summary>Gets the path of the assembly that was built.</summary>
    [CommandLineArgumentName("--output-assembly-path")]
    public required string OutputAssemblyPath { get; init; }

    /// <summary>Gets the path of the <c>WinRT.Sdk.Projection.dll</c> assembly.</summary>
    [CommandLineArgumentName("--winrt-sdk-projection-assembly-path")]
    public required string WinRTSdkProjectionAssemblyPath { get; init; }

    /// <summary>Gets the path of the <c>WinRT.Sdk.Xaml.Projection.dll</c> assembly, if available.</summary>
    [CommandLineArgumentName("--winrt-sdk-xaml-projection-assembly-path")]
    public string? WinRTSdkXamlProjectionAssemblyPath { get; init; }

    /// <summary>Gets the path of the <c>WinRT.Projection.dll</c> assembly.</summary>
    [CommandLineArgumentName("--winrt-projection-assembly-path")]
    public string? WinRTProjectionAssemblyPath { get; init; }

    /// <summary>Gets the path of the <c>WinRT.Component.dll</c> assembly, if available.</summary>
    [CommandLineArgumentName("--winrt-component-assembly-path")]
    public string? WinRTComponentAssemblyPath { get; init; }

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

    /// <summary>Gets whether to treat warnings coming from 'cswinrtinteropgen' as errors (regardless of the global 'TreatWarningsAsErrors' setting).</summary>
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
    /// Parses an <see cref="InteropGeneratorArgs"/> instance from a response file at the given path.
    /// </summary>
    /// <param name="path">The path to the response file (optionally prefixed with <c>@</c>).</param>
    /// <param name="token">The cancellation token for the operation.</param>
    /// <returns>The resulting <see cref="InteropGeneratorArgs"/> instance.</returns>
    public static InteropGeneratorArgs ParseFromResponseFile(string path, CancellationToken token)
    {
        return ResponseFileParser.Parse<InteropGeneratorArgs, WellKnownInteropExceptions>(path, token);
    }

    /// <summary>
    /// Parses an <see cref="InteropGeneratorArgs"/> instance from a response file read from a stream.
    /// </summary>
    /// <param name="stream">The stream containing the response file content.</param>
    /// <param name="token">The cancellation token for the operation.</param>
    /// <returns>The resulting <see cref="InteropGeneratorArgs"/> instance.</returns>
    public static InteropGeneratorArgs ParseFromResponseFile(Stream stream, CancellationToken token)
    {
        return ResponseFileParser.Parse<InteropGeneratorArgs, WellKnownInteropExceptions>(stream, token);
    }

    /// <summary>
    /// Formats the current <see cref="InteropGeneratorArgs"/> instance into a response file text.
    /// </summary>
    /// <returns>The resulting response file text.</returns>
    public string FormatToResponseFile()
    {
        return ResponseFileBuilder.Format(this);
    }
}
