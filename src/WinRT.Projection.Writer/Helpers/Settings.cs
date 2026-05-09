// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Configuration bag for a projection-writer invocation: input metadata paths, output
/// folder, namespace include/exclude filters, and per-emission-mode flags (component,
/// reference projection, public enums, etc.).
/// </summary>
internal sealed class Settings
{
    public HashSet<string> Input { get; } = new();
    public string OutputFolder { get; set; } = string.Empty;
    public bool Verbose { get; set; }
    public HashSet<string> Include { get; } = new();
    public HashSet<string> Exclude { get; } = new();
    public HashSet<string> AdditionExclude { get; } = new();
    public TypeFilter Filter { get; set; } = TypeFilter.Empty;
    public TypeFilter AdditionFilter { get; set; } = TypeFilter.Empty;
    public bool Component { get; set; }
    public bool Internal { get; set; }
    public bool Embedded { get; set; }
    public bool PublicEnums { get; set; }
    public bool PublicExclusiveTo { get; set; }
    public bool IdicExclusiveTo { get; set; }
    public bool ReferenceProjection { get; set; }
}
