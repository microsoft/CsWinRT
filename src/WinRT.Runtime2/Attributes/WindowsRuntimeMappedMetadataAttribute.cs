// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime;

/// <summary>
/// Indicates the mapped source Windows Runtime metadata file (.winmd) that a given custom-mapped type is from.
/// </summary>
[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Struct |
    AttributeTargets.Enum |
    AttributeTargets.Interface |
    AttributeTargets.Delegate,
    AllowMultiple = false,
    Inherited = false)]
[WindowsRuntimeImplementationOnlyMember]
public sealed class WindowsRuntimeMappedMetadataAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeMappedMetadataAttribute"/> instance with the specified parameters.
    /// </summary>
    /// <param name="name">The name of the mapped source Windows Runtime metadata file (.winmd) that the current custom-mapped type is from.</param>
    public WindowsRuntimeMappedMetadataAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Gets the name of the mapped source Windows Runtime metadata file (.winmd) that the current custom-mapped type is from.
    /// </summary>
    public string Name { get; }
}
