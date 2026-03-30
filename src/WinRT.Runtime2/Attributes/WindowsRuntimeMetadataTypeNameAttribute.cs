// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;

namespace WindowsRuntime;

/// <summary>
/// Indicates the metadata type class name to use for types exposed to the Windows Runtime.
/// </summary>
/// <remarks>
/// <para>
/// This attribute is only needed for the <see cref="Type"/> marshalling infrastructure for custom-mapped types.
/// </para>
/// <para>
/// It differs from <see cref="WindowsRuntimeClassNameAttribute"/> in that it represents the metadata name of
/// the type itself, not the runtime class name for when an instance is marshalled to native as an object.
/// For instance, when applied to value types it would contain their type name, not the <c>IReference&lt;T&gt;</c> name.
/// </para>
/// </remarks>
[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Struct |
    AttributeTargets.Enum |
    AttributeTargets.Interface |
    AttributeTargets.Delegate,
    AllowMultiple = false,
    Inherited = false)]
public sealed class WindowsRuntimeMetadataTypeNameAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeMetadataTypeNameAttribute"/> instance with the specified parameters.
    /// </summary>
    /// <param name="metadataTypeName">The metadata type name to use.</param>
    public WindowsRuntimeMetadataTypeNameAttribute(string metadataTypeName)
    {
        MetadataTypeName = metadataTypeName;
    }

    /// <summary>
    /// Gets the metadata type name for the current instance.
    /// </summary>
    public string MetadataTypeName { get; }
}
#endif