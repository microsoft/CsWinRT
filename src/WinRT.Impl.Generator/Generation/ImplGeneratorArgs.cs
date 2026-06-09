// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Threading;
using WindowsRuntime.Generator;
using WindowsRuntime.Generator.Attributes;
using WindowsRuntime.Generator.Parsing;
using WindowsRuntime.ImplGenerator.Errors;

namespace WindowsRuntime.ImplGenerator.Generation;

/// <summary>
/// Input parameters for <see cref="ImplGenerator"/>.
/// </summary>
internal sealed class ImplGeneratorArgs : IGeneratorArgs
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

    /// <summary>Gets whether to treat warnings coming from 'cswinrtimplgen' as errors (regardless of the global 'TreatWarningsAsErrors' setting).</summary>
    [CommandLineArgumentName("--treat-warnings-as-errors")]
    public required bool TreatWarningsAsErrors { get; init; }

    /// <summary>Gets the path to the file containing the key to sign the output assembly, if any.</summary>
    [CommandLineArgumentName("--assembly-originator-key-file")]
    public string? AssemblyOriginatorKeyFile { get; init; }

    /// <summary>Gets the token for the operation.</summary>
    public required CancellationToken Token { get; init; }

    /// <summary>Gets the directory to use to place the debug repro, if requested.</summary>
    [CommandLineArgumentName("--debug-repro-directory")]
    public string? DebugReproDirectory { get; init; }

    /// <summary>
    /// Parses an <see cref="ImplGeneratorArgs"/> instance from a response file at the given path.
    /// </summary>
    /// <param name="path">The path to the response file (optionally prefixed with <c>@</c>).</param>
    /// <param name="token">The cancellation token for the operation.</param>
    /// <returns>The resulting <see cref="ImplGeneratorArgs"/> instance.</returns>
    public static ImplGeneratorArgs ParseFromResponseFile(string path, CancellationToken token)
    {
        return ResponseFileParser.Parse<ImplGeneratorArgs, WellKnownImplExceptions>(path, token);
    }

    /// <summary>
    /// Parses an <see cref="ImplGeneratorArgs"/> instance from a response file read from a stream.
    /// </summary>
    /// <param name="stream">The stream containing the response file content.</param>
    /// <param name="token">The cancellation token for the operation.</param>
    /// <returns>The resulting <see cref="ImplGeneratorArgs"/> instance.</returns>
    public static ImplGeneratorArgs ParseFromResponseFile(Stream stream, CancellationToken token)
    {
        return ResponseFileParser.Parse<ImplGeneratorArgs, WellKnownImplExceptions>(stream, token);
    }

    /// <summary>
    /// Formats the current <see cref="ImplGeneratorArgs"/> instance into a response file text.
    /// </summary>
    /// <returns>The resulting response file text.</returns>
    public string FormatToResponseFile()
    {
        return ResponseFileBuilder.Format(this);
    }
}
