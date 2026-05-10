// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace WindowsRuntime.ProjectionWriter.Helpers;

/// <summary>
/// Configuration bag for a projection-writer invocation: input metadata paths, output
/// folder, namespace include/exclude filters, and per-emission-mode flags (component,
/// reference projection, public enums, etc.).
/// </summary>
internal sealed class Settings
{
    /// <summary>Gets the set of input <c>.winmd</c> file paths to project.</summary>
    public HashSet<string> Input { get; } = [];

    /// <summary>Gets or sets the output folder where generated <c>.cs</c> files are written.</summary>
    public string OutputFolder { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether verbose progress is logged to the console.</summary>
    public bool Verbose { get; set; }

    /// <summary>Gets the namespace prefixes to include in projection (when empty, all namespaces are included).</summary>
    public HashSet<string> Include { get; } = [];

    /// <summary>Gets the namespace prefixes to exclude from projection.</summary>
    public HashSet<string> Exclude { get; } = [];

    /// <summary>Gets the namespace prefixes whose namespace-additions resources should be excluded.</summary>
    public HashSet<string> AdditionExclude { get; } = [];

    /// <summary>Gets or sets the compiled type-name filter built from <see cref="Include"/> and <see cref="Exclude"/>.</summary>
    public TypeFilter Filter { get; set; } = TypeFilter.Empty;

    /// <summary>Gets or sets the compiled type-name filter built from <see cref="Include"/> and <see cref="AdditionExclude"/>, used for namespace-additions resources only.</summary>
    public TypeFilter AdditionFilter { get; set; } = TypeFilter.Empty;

    /// <summary>Gets or sets a value indicating whether component-authoring mode is enabled.</summary>
    public bool Component { get; set; }

    /// <summary>Gets or sets a value indicating whether projected types are emitted as <c>internal</c> rather than <c>public</c>.</summary>
    public bool Internal { get; set; }

    /// <summary>Gets or sets a value indicating whether the projection is embedded into a consuming assembly (forces <c>internal</c> visibility).</summary>
    public bool Embedded { get; set; }

    /// <summary>Gets or sets a value indicating whether projected enums are forced to <c>public</c> visibility (overrides <see cref="Internal"/>).</summary>
    public bool PublicEnums { get; set; }

    /// <summary>Gets or sets a value indicating whether <c>[ExclusiveTo]</c> interfaces are emitted as <c>public</c> rather than <c>internal</c>.</summary>
    public bool PublicExclusiveTo { get; set; }

    /// <summary>Gets or sets a value indicating whether the IDIC pattern is applied to <c>[ExclusiveTo]</c> interfaces.</summary>
    public bool IdicExclusiveTo { get; set; }

    /// <summary>Gets or sets a value indicating whether reference-only projection mode is enabled (no implementation, no IID file).</summary>
    public bool ReferenceProjection { get; set; }
}