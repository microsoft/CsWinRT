// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#define WINDOWS_RUNTIME_IMPLEMENTATION_ONLY_FILE

using System;

namespace WindowsRuntime;

/// <summary>
/// Associates a projected Windows Runtime type with the source Windows Runtime metadata file (.winmd) that it is from.
/// This attribute is applied to a centralized lookup type (<c>ABI.WindowsRuntimeMetadataTypes</c>) rather than being placed
/// on the projected type itself, so that the metadata mapping (only consumed by build-time tooling) can be trimmed away when
/// not needed. The projected type itself is instead decorated with the parameterless <see cref="WindowsRuntimeTypeAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
[WindowsRuntimeImplementationOnlyMember]
public sealed class WindowsRuntimeMetadataAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeMetadataAttribute"/> instance with the specified parameters.
    /// </summary>
    /// <param name="type">The projected Windows Runtime type that the metadata is for.</param>
    /// <param name="name">The name of the source Windows Runtime metadata file (.winmd) that <paramref name="type"/> is from.</param>
    public WindowsRuntimeMetadataAttribute(Type type, string name)
    {
        Type = type;
        Name = name;
    }

    /// <summary>
    /// Gets the projected Windows Runtime type that the metadata is for.
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// Gets the name of the source Windows Runtime metadata file (.winmd) that <see cref="Type"/> is from.
    /// </summary>
    public string Name { get; }
}
