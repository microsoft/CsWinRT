// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using WindowsRuntime.ProjectionWriter.Helpers;

namespace WindowsRuntime.ProjectionWriter.Generation;

/// <summary>
/// Configuration bag for a projection-writer invocation: input metadata paths, output
/// folder, namespace include/exclude filters, and per-emission-mode flags (component,
/// reference projection, public enums, etc.).
/// </summary>
internal sealed class Settings
{
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
    /// Gets the namespace prefixes to include in projection (when empty, all namespaces are included).
    /// </summary>
    public HashSet<string> Include { get; } = [];

    /// <summary>
    /// Gets the namespace prefixes to exclude from projection.
    /// </summary>
    public HashSet<string> Exclude { get; } = [];

    /// <summary>
    /// Gets the namespace prefixes whose namespace-additions resources should be excluded.
    /// </summary>
    public HashSet<string> AdditionExclude { get; } = [];

    /// <summary>
    /// Gets the compiled type-name filter built from <see cref="Include"/> and <see cref="Exclude"/>.
    /// </summary>
    public TypeFilter Filter
    {
        get
        {
            if (!_filterCached)
            {
                _filter = new TypeFilter(Include, Exclude);
                _filterCached = true;
            }
            return _filter;
        }
    }

    [SuppressMessage("Style", "IDE0032:Use auto property", Justification = "Lazy-initialized backing field for the computed filter.")]
    private TypeFilter _filter = TypeFilter.Empty;
    private bool _filterCached;

    /// <summary>
    /// Gets the compiled type-name filter built from <see cref="Include"/> and <see cref="AdditionExclude"/>, used for namespace-additions resources only.
    /// </summary>
    public TypeFilter AdditionFilter
    {
        get
        {
            if (!_additionFilterCached)
            {
                _additionFilter = new TypeFilter(Include, AdditionExclude);
                _additionFilterCached = true;
            }
            return _additionFilter;
        }
    }

    [SuppressMessage("Style", "IDE0032:Use auto property", Justification = "Lazy-initialized backing field for the computed addition filter.")]
    private TypeFilter _additionFilter = TypeFilter.Empty;
    private bool _additionFilterCached;

    /// <summary>
    /// Gets or sets a value indicating whether component-authoring mode is enabled.
    /// </summary>
    public bool Component { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether projected types are emitted as <c>internal</c> rather than <c>public</c>.
    /// </summary>
    public bool Internal { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the projection is embedded into a consuming assembly (forces <c>internal</c> visibility).
    /// </summary>
    public bool Embedded { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether projected enums are forced to <c>public</c> visibility (overrides <see cref="Internal"/>).
    /// </summary>
    public bool PublicEnums { get; init; }

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
}
