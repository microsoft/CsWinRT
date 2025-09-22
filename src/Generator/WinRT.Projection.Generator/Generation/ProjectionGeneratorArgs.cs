// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using WindowsRuntime.Generator.Attributes;
using WindowsRuntime.Generator.Generation;

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

    /// <summary>Gets the path to CsWinRT.exe.</summary>
    [CommandLineArgumentName("--cswinrt-path")]
    public required string CsWinRTPath { get; init; }

    /// <summary>Gets the token for the operation.</summary>
    public required CancellationToken Token { get; init; }

    /// <summary>
    /// Parses an <see cref="ProjectionGeneratorArgs"/> instance from a target response file.
    /// </summary>
    /// <param name="path">The path to the response file.</param>
    /// <param name="token">The token for the operation.</param>
    /// <returns>The resulting <see cref="ProjectionGeneratorArgs"/> instance.</returns>
    public static ProjectionGeneratorArgs ParseFromResponseFile(string path, CancellationToken token)
    {
        Dictionary<string, string> argsMap = GeneratorArgs.ParseFromResponseFile(path);

        // Parse all commands to create the managed arguments to use
        return new()
        {
            ReferenceAssemblyPaths = GeneratorArgs.GetStringArrayArgument<ProjectionGeneratorArgs>(argsMap, nameof(ReferenceAssemblyPaths)),
            GeneratedAssemblyDirectory = GeneratorArgs.GetStringArgument<ProjectionGeneratorArgs>(argsMap, nameof(GeneratedAssemblyDirectory)),
            WinMDPaths = GeneratorArgs.GetStringArrayArgument<ProjectionGeneratorArgs>(argsMap, nameof(WinMDPaths)),
            TargetFramework = GeneratorArgs.GetStringArgument<ProjectionGeneratorArgs>(argsMap, nameof(TargetFramework)),
            CsWinRTPath = GeneratorArgs.GetStringArgument<ProjectionGeneratorArgs>(argsMap, nameof(CsWinRTPath)),
            Token = token
        };
    }
}
