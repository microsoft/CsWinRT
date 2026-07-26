// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Threading;
using WindowsRuntime.Generator;
using WindowsRuntime.Generator.Attributes;

namespace WindowsRuntime.ProjectionGenerator.Generation;

/// <summary>
/// Input parameters for <see cref="ProjectionGenerator"/>.
/// </summary>
internal sealed class ProjectionGeneratorArgs : IGeneratorArgs
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

    /// <summary>Gets the output assembly name. Defaults to 'WinRT.Projection'.</summary>
    [CommandLineArgumentName("--assembly-name")]
    [DefaultValue("WinRT.Projection")]
    public string AssemblyName { get; init; } = "WinRT.Projection";

    /// <summary>
    /// Gets whether to only include the Windows SDK projection (Windows and WindowsRuntime.Internal namespaces).
    /// When 'false' (the default), the Windows SDK types are excluded and only non-Windows
    /// projection types are included.
    /// </summary>
    [CommandLineArgumentName("--windows-sdk-only")]
    public bool WindowsSdkOnly { get; init; }

    /// <summary>
    /// Gets whether to generate the Windows.UI.Xaml projection (WinRT.Sdk.Xaml.Projection).
    /// When 'true', the tool includes the Windows.UI.Xaml namespace filters.
    /// </summary>
    [CommandLineArgumentName("--windows-ui-xaml-projection")]
    public bool WindowsUIXamlProjection { get; init; }

    /// <summary>
    /// Gets whether to emit only the authoring surface for the runtime classes in
    /// <see cref="AuthoringWinMDPaths"/>: their exclusive-to and factory interfaces, plus the abstract
    /// <c>ABI.&lt;Ns&gt;.&lt;Class&gt;</c> and <c>ABI.&lt;Ns&gt;.&lt;Class&gt;Factory</c> base classes a
    /// component extends to implement Windows Runtime types defined in an existing <c>.winmd</c>.
    /// </summary>
    /// <remarks>
    /// The output is deliberately a pure function of the input metadata, so that every project implementing
    /// types from a given <c>.winmd</c> produces the same assembly and they can safely share one identity.
    /// </remarks>
    [CommandLineArgumentName("--implement-winmd-types")]
    public bool ImplementWinMDTypes { get; init; }

    /// <summary>
    /// Gets the <c>.winmd</c> files whose runtime classes are implemented (authored) in C# (see the
    /// <c>CsWinRTImplementWinMD</c> build item). Their types are added to both the metadata inputs and the
    /// include filter, on top of whatever is discovered from the input references.
    /// </summary>
    /// <remarks>
    /// This is passed both when producing an authoring projection (together with
    /// <see cref="ImplementWinMDTypes"/>) and when producing <c>WinRT.Component.dll</c>, so that the
    /// marshalling code backing the generated abstract base classes is generated for the application.
    /// </remarks>
    [CommandLineArgumentName("--authoring-winmd-paths")]
    public string[] AuthoringWinMDPaths { get; init; } = [];

    /// <summary>Gets the maximum number of parallel tasks to use for execution.</summary>
    [CommandLineArgumentName("--max-degrees-of-parallelism")]
    public required int MaxDegreesOfParallelism { get; init; }

    /// <summary>
    /// Gets whether to emit the <c>ProjectionTypesInitializer</c> module initializer
    /// (calls <c>Assembly.SetEntryAssembly</c>) into <c>WinRT.Component.dll</c>. Only
    /// needed under JIT to enable <c>[TypeMapAssemblyTarget]</c> discovery at the merged
    /// component dll; AOT uses a separate exe-project workaround. Will become
    /// unnecessary once the <c>TypeMappingEntryAssembly</c> MSBuild property is available.
    /// </summary>
    [CommandLineArgumentName("--emit-entry-point-initializer")]
    public bool EmitEntryPointInitializer { get; init; }

    /// <inheritdoc/>
    public required CancellationToken Token { get; init; }

    /// <inheritdoc/>
    [CommandLineArgumentName("--debug-repro-directory")]
    public string? DebugReproDirectory { get; init; }
}

