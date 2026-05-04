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

    /// <summary>Gets the output assembly name. Defaults to 'WinRT.Projection'.</summary>
    [CommandLineArgumentName("--assembly-name")]
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
    /// Gets whether the consuming build is publishing with Native AOT. When <c>true</c> in
    /// component mode (i.e. producing <c>WinRT.Component.dll</c>), an
    /// <c>[UnmanagedCallersOnly]</c> <c>DllGetActivationFactory</c> wrapper is emitted
    /// alongside the merged managed activation logic. JIT consumers do not need this
    /// (the host reaches the merged factory through a managed delegation chain).
    /// </summary>
    [CommandLineArgumentName("--publish-aot")]
    public bool PublishAot { get; init; }

    /// <summary>Gets the token for the operation.</summary>
    public required CancellationToken Token { get; init; }
}