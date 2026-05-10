// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Input parameters for <see cref="ProjectionWriter"/>. CLI options
/// </summary>
public sealed class ProjectionWriterOptions
{
    /// <summary>
    /// One or more <c>.winmd</c> files (or directories that will be recursively scanned for <c>.winmd</c> files)
    /// providing the Windows Runtime metadata to project.
    /// </summary>
    public required IReadOnlyList<string> InputPaths { get; init; }

    /// <summary>
    /// The output folder where generated <c>.cs</c> files will be placed. Will be created if it doesn't exist.
    /// </summary>
    public required string OutputFolder { get; init; }

    /// <summary>
    /// Optional list of namespace prefixes to include in the projection.
    /// </summary>
    public IReadOnlyList<string> Include { get; init; } = [];

    /// <summary>
    /// Optional list of namespace prefixes to exclude from the projection.
    /// </summary>
    public IReadOnlyList<string> Exclude { get; init; } = [];

    /// <summary>
    /// Optional list of namespace prefixes to exclude from the projection additions.
    /// </summary>
    public IReadOnlyList<string> AdditionExclude { get; init; } = [];

    /// <summary>Generate a Windows Runtime component projection.</summary>
    public bool Component { get; init; }

    /// <summary>Generate an internal (non-public) projection.</summary>
    public bool Internal { get; init; }

    /// <summary>Generate an embedded projection.</summary>
    public bool Embedded { get; init; }

    /// <summary>If <c>true</c> with embedded option, generate enums as public.</summary>
    public bool PublicEnums { get; init; }

    /// <summary>Make exclusive-to interfaces public in the projection (default is internal).</summary>
    public bool PublicExclusiveTo { get; init; }

    /// <summary>Make exclusive-to interfaces support <c>IDynamicInterfaceCastable</c>.</summary>
    public bool IdicExclusiveTo { get; init; }

    /// <summary>Generate a projection to be used as a reference assembly.</summary>
    public bool ReferenceProjection { get; init; }

    /// <summary>Show detailed progress information.</summary>
    public bool Verbose { get; init; }

    /// <summary>Cancellation token for the operation.</summary>
    public CancellationToken CancellationToken { get; init; }
}