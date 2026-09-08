// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using WindowsRuntime.ProjectionWriter.Errors;
using WindowsRuntime.ProjectionWriter.Helpers;

namespace WindowsRuntime.ProjectionWriter.Generation;

/// <summary>
/// Configuration bag for a projection-writer invocation: input metadata paths, output
/// folder, namespace include/exclude filters, and per-emission-mode flags (component,
/// reference projection, public enums, etc.).
/// </summary>
/// <remarks>
/// Callers populate the mutable input/include/exclude sets and <c>init</c> properties up
/// front, then call <see cref="MakeReadOnly"/> exactly once before passing this instance
/// to <see cref="ProjectionGenerator"/>. <see cref="MakeReadOnly"/> eagerly computes the
/// derived <see cref="Filter"/> and <see cref="AdditionFilter"/> so subsequent parallel
/// reads from work items have a stable, non-racy view.
/// </remarks>
internal sealed class Settings
{
    /// <summary>
    /// Indicates whether <see cref="MakeReadOnly"/> has been called.
    /// </summary>
    private volatile bool _isReadOnly;

    /// <summary>
    /// Gets the set of input <c>.winmd</c> file paths to project.
    /// </summary>
    public HashSet<string> Input { get; } = [];

    /// <summary>
    /// Gets or sets the output folder where generated <c>.cs</c> files are written.
    /// </summary>
    public string OutputFolder { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether verbose progress is logged to the console.
    /// </summary>
    public bool Verbose { get; init; }

    /// <summary>
    /// Optional callback invoked for each verbose progress message. When <see langword="null"/>,
    /// verbose messages are forwarded to <see cref="Console.Out"/>. Has no effect unless
    /// <see cref="Verbose"/> is also set.
    /// </summary>
    public Action<string>? Logger { get; init; }

    /// <summary>
    /// Maximum number of parallel work items dispatched when generating projections.
    /// Defaults to <c>-1</c> (let the runtime decide; typically <see cref="Environment.ProcessorCount"/>).
    /// Set to <c>1</c> to force fully sequential execution.
    /// </summary>
    public int MaxDegreesOfParallelism { get; init; } = -1;

    /// <summary>
    /// Gets the namespace or type-name prefixes to include in the projection.
    /// </summary>
    public HashSet<string> Include { get; } = [];

    /// <summary>
    /// Gets the fully qualified type names to include in the projection, matched exactly.
    /// </summary>
    public HashSet<string> IncludeTypes { get; } = [];

    /// <summary>
    /// Gets the namespace prefixes to exclude from projection.
    /// </summary>
    public HashSet<string> Exclude { get; } = [];

    /// <summary>
    /// Gets the namespace prefixes whose namespace-additions resources should be excluded.
    /// </summary>
    public HashSet<string> AdditionExclude { get; } = [];

    /// <summary>
    /// Gets the compiled type-name filter built from <see cref="Include"/>, <see cref="IncludeTypes"/>, and <see cref="Exclude"/>.
    /// Only valid after <see cref="MakeReadOnly"/> has been called.
    /// </summary>
    /// <exception cref="WellKnownProjectionWriterException">
    /// Thrown if accessed before <see cref="MakeReadOnly"/> has been called.
    /// </exception>
    public TypeFilter Filter
    {
        get => field ?? throw WellKnownProjectionWriterExceptions.SettingsNotReadOnly();
        private set;
    }

    /// <summary>
    /// Gets the compiled filter used for namespace-additions resources only.
    /// Exact type includes keep an otherwise empty filter from including all namespaces, but do not match namespaces themselves.
    /// Only valid after <see cref="MakeReadOnly"/> has been called.
    /// </summary>
    /// <exception cref="WellKnownProjectionWriterException">
    /// Thrown if accessed before <see cref="MakeReadOnly"/> has been called.
    /// </exception>
    public TypeFilter AdditionFilter
    {
        get => field ?? throw WellKnownProjectionWriterExceptions.SettingsNotReadOnly();
        private set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether component-authoring mode is enabled.
    /// </summary>
    public bool Component { get; init; }

    /// <summary>
    /// Gets the paths to the managed implementation assemblies of the authored Windows Runtime
    /// component(s) being projected. Used in component mode to inspect implementation details that
    /// are absent from the input <c>.winmd</c> metadata (e.g. the <c>static</c> fields backing XAML
    /// dependency properties). May be empty when those assemblies are not available.
    /// </summary>
    public HashSet<string> ComponentImplementationAssemblies { get; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether <c>[ExclusiveTo]</c> interfaces are emitted as <c>public</c> rather than <c>internal</c>.
    /// </summary>
    public bool PublicExclusiveTo { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the IDIC pattern is applied to <c>[ExclusiveTo]</c> interfaces.
    /// </summary>
    public bool IdicExclusiveTo { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether reference-only projection mode is enabled (no implementation, no IID file).
    /// </summary>
    public bool ReferenceProjection { get; init; }

    /// <summary>
    /// Finalizes the settings: eagerly builds the derived <see cref="Filter"/> and
    /// <see cref="AdditionFilter"/> from the configured include/exclude sets, then marks
    /// the instance as read-only. Must be called exactly once before passing the instance
    /// to <see cref="ProjectionGenerator"/>.
    /// </summary>
    /// <exception cref="WellKnownProjectionWriterException">
    /// Thrown if <see cref="MakeReadOnly"/> was already called on this instance.
    /// </exception>
    public void MakeReadOnly()
    {
        if (_isReadOnly)
        {
            throw WellKnownProjectionWriterExceptions.SettingsAlreadyReadOnly();
        }

        Filter = new TypeFilter(Include, Exclude, IncludeTypes);
        AdditionFilter = new TypeFilter(Include, AdditionExclude, IncludeTypes);
        _isReadOnly = true;
    }
}
