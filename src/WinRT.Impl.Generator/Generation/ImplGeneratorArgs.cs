// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using WindowsRuntime.Generator.Attributes;
using WindowsRuntime.Generator.Generation;

namespace WindowsRuntime.ImplGenerator.Generation;

/// <summary>
/// Input parameters for <see cref="ImplGenerator"/>.
/// </summary>
internal sealed partial class ImplGeneratorArgs
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

    /// <summary>
    /// Parses an <see cref="ImplGeneratorArgs"/> instance from a target response file.
    /// </summary>
    /// <param name="path">The path to the response file.</param>
    /// <param name="token">The token for the operation.</param>
    /// <returns>The resulting <see cref="ImplGeneratorArgs"/> instance.</returns>
    public static ImplGeneratorArgs ParseFromResponseFile(string path, CancellationToken token)
    {
        Dictionary<string, string> argsMap = GeneratorArgs.ParseFromResponseFile(path);

        // Parse all commands to create the managed arguments to use
        return new()
        {
            ReferenceAssemblyPaths = GeneratorArgs.GetStringArrayArgument<ImplGeneratorArgs>(argsMap, nameof(ReferenceAssemblyPaths)),
            OutputAssemblyPath = GeneratorArgs.GetStringArgument<ImplGeneratorArgs>(argsMap, nameof(OutputAssemblyPath)),
            GeneratedAssemblyDirectory = GeneratorArgs.GetStringArgument<ImplGeneratorArgs>(argsMap, nameof(GeneratedAssemblyDirectory)),
            TreatWarningsAsErrors = GeneratorArgs.GetBooleanArgument<ImplGeneratorArgs>(argsMap, nameof(TreatWarningsAsErrors)),
            AssemblyOriginatorKeyFile = GeneratorArgs.GetNullableStringArgument<ImplGeneratorArgs>(argsMap, nameof(AssemblyOriginatorKeyFile)),
            Token = token
        };
    }
}
