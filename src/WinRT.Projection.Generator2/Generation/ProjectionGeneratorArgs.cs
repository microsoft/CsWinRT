// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using WindowsRuntime.ProjectionGenerator.Attributes;

namespace WindowsRuntime.ProjectionGenerator.Generation;

/// <summary>
/// Input parameters for the projection generator, mapping to the cswinrt CLI options.
/// </summary>
internal sealed partial class ProjectionGeneratorArgs
{
    /// <summary>Gets the paths to the input .winmd files.</summary>
    [CommandLineArgumentName("--input-file-paths")]
    public required string[] InputFilePaths { get; init; }

    /// <summary>Gets the output directory for generated files.</summary>
    [CommandLineArgumentName("--output-directory")]
    public required string OutputDirectory { get; init; }

    /// <summary>Gets the optional namespace prefixes to include in the projection.</summary>
    [CommandLineArgumentName("--include-namespaces")]
    public string[]? IncludeNamespaces { get; init; }

    /// <summary>Gets the optional namespace prefixes to exclude from the projection.</summary>
    [CommandLineArgumentName("--exclude-namespaces")]
    public string[]? ExcludeNamespaces { get; init; }

    /// <summary>Gets the optional namespace prefixes to exclude from additions.</summary>
    [CommandLineArgumentName("--addition-exclude-namespaces")]
    public string[]? AdditionExcludeNamespaces { get; init; }

    /// <summary>Gets whether to generate a component projection.</summary>
    [CommandLineArgumentName("--component")]
    public bool IsComponent { get; init; }

    /// <summary>Gets whether to show detailed progress.</summary>
    [CommandLineArgumentName("--verbose")]
    public bool IsVerbose { get; init; }

    /// <summary>Gets whether to generate a private (internal) projection.</summary>
    [CommandLineArgumentName("--internal")]
    public bool IsInternal { get; init; }

    /// <summary>Gets whether to generate an embedded projection.</summary>
    [CommandLineArgumentName("--embedded")]
    public bool IsEmbedded { get; init; }

    /// <summary>Gets whether to make enums public when used with embedded projections.</summary>
    [CommandLineArgumentName("--public-enums")]
    public bool PublicEnums { get; init; }

    /// <summary>Gets whether to make exclusiveto interfaces public.</summary>
    [CommandLineArgumentName("--public-exclusive-to")]
    public bool PublicExclusiveTo { get; init; }

    /// <summary>Gets whether to support <c>IDynamicInterfaceCastable</c> for exclusiveto interfaces.</summary>
    [CommandLineArgumentName("--idic-exclusive-to")]
    public bool IdicExclusiveTo { get; init; }

    /// <summary>Gets whether to allow providing an additional component activation factory.</summary>
    [CommandLineArgumentName("--partial-factory")]
    public bool PartialFactory { get; init; }

    /// <summary>Gets whether to generate the projection as a reference assembly.</summary>
    [CommandLineArgumentName("--reference-projection")]
    public bool IsReferenceProjection { get; init; }

    /// <summary>Gets the optional directory to use to place the debug repro.</summary>
    [CommandLineArgumentName("--debug-repro-directory")]
    public string? DebugReproDirectory { get; init; }

    /// <summary>Gets or sets the cancellation token for the current operation.</summary>
    public CancellationToken Token { get; set; }
}
