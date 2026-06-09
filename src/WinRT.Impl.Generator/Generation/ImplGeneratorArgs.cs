// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using WindowsRuntime.Generator;
using WindowsRuntime.Generator.Attributes;

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

    /// <inheritdoc/>
    public required CancellationToken Token { get; init; }

    /// <inheritdoc/>
    [CommandLineArgumentName("--debug-repro-directory")]
    public string? DebugReproDirectory { get; init; }
}

