// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime;

/// <summary>
/// Indicates a projected type that was originally defined in a .winmd file (Windows Runtime metadata).
/// </summary>
[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Struct |
    AttributeTargets.Enum |
    AttributeTargets.Interface |
    AttributeTargets.Delegate,
    AllowMultiple = false,
    Inherited = false)]
public sealed class WindowsRuntimeTypeAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeTypeAttribute"/> instance with the specified parameters.
    /// </summary>
    /// <param name="sourceMetadata">The original source metadata where the projected type was defined.</param>
    public WindowsRuntimeTypeAttribute(string sourceMetadata)
    {
        SourceMetadata = sourceMetadata;
    }

    /// <summary>
    /// Gets the original source metadata where the projected type was defined.
    /// </summary>
    public string SourceMetadata { get; }
}
