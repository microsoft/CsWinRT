// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Configuration bag passed to <see cref="ProjectionWriter.Run(ProjectionWriterOptions)"/>.
/// Specifies the input <c>.winmd</c> metadata, the output folder, namespace include / exclude
/// filters, and per-projection-mode toggles (component authoring, reference-only projection,
/// public enums, etc.).
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

    /// <summary>
    /// Generate a Windows Runtime component projection.
    /// </summary>
    public bool Component { get; init; }

    /// <summary>
    /// Generate an internal (non-public) projection.
    /// </summary>
    public bool Internal { get; init; }

    /// <summary>
    /// If <c>true</c> with embedded option, generate enums as public.
    /// </summary>
    public bool PublicEnums { get; init; }

    /// <summary>
    /// Make exclusive-to interfaces public in the projection (default is internal).
    /// </summary>
    public bool PublicExclusiveTo { get; init; }

    /// <summary>
    /// Make exclusive-to interfaces support <c>IDynamicInterfaceCastable</c>.
    /// </summary>
    public bool IdicExclusiveTo { get; init; }

    /// <summary>
    /// Generate a projection to be used as a reference assembly.
    /// </summary>
    public bool ReferenceProjection { get; init; }

    /// <summary>
    /// Show detailed progress information.
    /// </summary>
    public bool Verbose { get; init; }

    /// <summary>
    /// Optional logger callback invoked for each verbose progress message (only used when
    /// <see cref="Verbose"/> is <see langword="true"/>). Defaults to <see langword="null"/>,
    /// in which case verbose messages are forwarded to <see cref="Console.Out"/>.
    /// </summary>
    public Action<string>? Logger { get; init; }

    /// <summary>
    /// Maximum number of parallel work items to dispatch when generating projections.
    /// Defaults to <c>-1</c>, which lets the runtime pick (typically <see cref="System.Environment.ProcessorCount"/>).
    /// Set to <c>1</c> to force fully sequential execution (useful for debugging or when a deterministic
    /// thread schedule is required).
    /// </summary>
    public int MaxDegreesOfParallelism { get; init; } = -1;

    /// <summary>
    /// Gets the cancellation token observed during projection generation. Defaults to
    /// <see cref="CancellationToken.None"/>, which never signals cancellation.
    /// </summary>
    public CancellationToken CancellationToken { get; init; }
}
