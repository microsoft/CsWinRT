// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.WinMDGenerator.Models;

/// <summary>
/// Contains type info originated from a given mapped type.
/// </summary>
/// <see cref="MappedType"/>
internal readonly struct MappedTypeInfo
{
    /// <summary>
    /// Creates a new <see cref="MappedTypeInfo"/> with a fixed mapping.
    /// </summary>
    /// <param name="namespace">The Windows Runtime namespace.</param>
    /// <param name="name">The Windows Runtime type name.</param>
    /// <param name="assembly">The Windows Runtime contract assembly name.</param>
    /// <param name="isValueType">Whether this mapped type is a value type.</param>
    /// <param name="isBlittable">Whether this mapped type is blittable.</param>
    public MappedTypeInfo(string @namespace, string name, string assembly, bool isValueType = false, bool isBlittable = false)
    {
        Namespace = @namespace;
        Name = name;
        Assembly = assembly;
        IsValueType = isValueType;
        IsBlittable = isBlittable;
    }

    /// <summary>
    /// Gets the Windows Runtime namespace for the type.
    /// </summary>
    public string Namespace { get; }

    /// <summary>
    /// Gets the Windows Runtime type name for the type.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the Windows Runtime contract assembly name for the type.
    /// </summary>
    public string Assembly { get; }

    /// <summary>
    /// Gets whether this mapped type is a value type.
    /// </summary>
    public bool IsValueType { get; }

    /// <summary>
    /// Gets whether the mapped type is blittable.
    /// </summary>
    public bool IsBlittable => IsValueType && field;

    /// <summary>
    /// Gets the full name for the type.
    /// </summary>
    public string FullName => string.IsNullOrEmpty(Namespace) ? Name : $"{Namespace}.{Name}";
}
