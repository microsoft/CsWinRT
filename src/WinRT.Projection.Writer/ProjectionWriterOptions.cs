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
    /// Optional paths to the managed implementation assemblies (<c>.dll</c>) of the authored
    /// Windows Runtime component(s) being projected. Only meaningful in <see cref="Component"/> mode.
    /// </summary>
    /// <remarks>
    /// The input <c>.winmd</c> metadata does not carry implementation details such as the
    /// <c>static</c> fields backing XAML dependency properties, so the writer consults these
    /// managed assemblies to decide whether each generated activation factory needs to force the
    /// authored type's class constructor to run before activation. When the list is empty (e.g. the
    /// managed assembly is not available yet), the writer conservatively keeps that constructor.
    /// </remarks>
    public IReadOnlyList<string> ComponentImplementationAssemblyPaths { get; init; } = [];

    /// <summary>
    /// Make exclusive-to interfaces public in the projection (default is internal).
    /// </summary>
    public bool PublicExclusiveTo { get; init; }

    /// <summary>
    /// Emit only the authoring surface for the runtime classes selected by <see cref="Include"/>: their
    /// exclusive-to and factory interfaces, plus the abstract <c>ABI.&lt;Ns&gt;.&lt;Class&gt;</c> and
    /// <c>ABI.&lt;Ns&gt;.&lt;Class&gt;Factory</c> base classes an author extends to implement Windows Runtime
    /// types that are already defined in an existing <c>.winmd</c>. Every member the type requires is
    /// declared <c>abstract</c>, so the compiler enforces a complete implementation.
    /// <para>
    /// This is the 3.0 replacement for <see cref="PublicExclusiveTo"/>: instead of exposing every
    /// exclusive-to interface as <c>public</c> for the author to implement one by one, the author extends a
    /// single generated abstract base. The result is compiled into a standalone authoring projection
    /// assembly, so all projections are left completely unchanged.
    /// </para>
    /// </summary>
    public bool ImplementWinMDTypes { get; init; }

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
    /// Defaults to <c>-1</c>, which lets the runtime pick (typically <see cref="Environment.ProcessorCount"/>).
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
