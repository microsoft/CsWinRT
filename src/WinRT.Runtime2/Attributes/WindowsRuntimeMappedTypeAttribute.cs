// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime;

/// <summary>
/// Indicates a mapped type for a Windows Runtime type projection (ie. a metadata provider type).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class WindowsRuntimeMappedTypeAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeMappedTypeAttribute"/> instance with the specified parameters.
    /// </summary>
    /// <param name="publicType">The public type associated with the current instance (ie. the type that would be used directly by developers).</param>
    public WindowsRuntimeMappedTypeAttribute(Type publicType)
    {
        PublicType = publicType;
    }

    /// <summary>
    /// Gets the public type associated with the current instance (ie. the type that would be used directly by developers).
    /// </summary>
    public Type PublicType { get; }
}
