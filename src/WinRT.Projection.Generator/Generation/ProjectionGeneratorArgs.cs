// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using WindowsRuntime.ProjectionGenerator.Attributes;

namespace WindowsRuntime.ProjectionGenerator.Generation;

/// <summary>
/// Input parameters for <see cref="ProjectionGenerator"/>.
/// </summary>
internal sealed partial class ProjectionGeneratorArgs
{
    /// <summary>Gets the input .dll paths.</summary>
    [CommandLineArgumentName("--reference-assembly-paths")]
    public required string[] ReferenceAssemblyPaths { get; init; }

    /// <summary>Gets the directory to use to place the generated assembly.</summary>
    [CommandLineArgumentName("--generated-assembly-directory")]
    public required string GeneratedAssemblyDirectory { get; init; }

    /// <summary>Gets the input .winmd paths.</summary>
    [CommandLineArgumentName("--winmd-paths")]
    public required string[] WinMDPaths { get; init; }

    /// <summary>Gets the target framework being built for.</summary>
    [CommandLineArgumentName("--target-framework")]
    public required string TargetFramework { get; init; }

    /// <summary>Gets the Windows WinMD or version which the projection targets.</summary>
    [CommandLineArgumentName("--windows-metadata")]
    public required string WindowsMetadata { get; init; }

    /// <summary>Gets the path to CsWinRT.exe.</summary>
    [CommandLineArgumentName("--cswinrt-exe-path")]
    public required string CsWinRTExePath { get; init; }

    /// <summary>Gets the token for the operation.</summary>
    public required CancellationToken Token { get; init; }
}