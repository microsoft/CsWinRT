// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime;

/// <summary>
/// Indicates the source Windows Runtime metadata file (.winmd) that a given projected type is from.
/// </summary>
/// <remarks>
/// This attribute is emitted by the CsWinRT generator, and it is not meant to be used directly.
/// </remarks>
[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Struct |
    AttributeTargets.Enum |
    AttributeTargets.Interface |
    AttributeTargets.Delegate,
    AllowMultiple = false,
    Inherited = false)]
public sealed class WindowsRuntimeMetadataAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeMetadataAttribute"/> instance with the specified parameters.
    /// </summary>
    /// <param name="name">The name of the source Windows Runtime metadata file (.winmd) that the current projected type is from.</param>
    public WindowsRuntimeMetadataAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Gets the name of the source Windows Runtime metadata file (.winmd) that the current projected type is from.
    /// </summary>
    public string Name { get; }
}
