// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime;

/// <summary>
/// Indicates the projected Windows Runtime class type that a given interface is exclusive to.
/// </summary>
[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class WindowsRuntimeExclusiveToInterfaceAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeExclusiveToInterfaceAttribute"/> instance with the specified parameters.
    /// </summary>
    /// <param name="runtimeClassType">The projected Windows Runtime class type that the annotated interface is exclusive to.</param>
    public WindowsRuntimeExclusiveToInterfaceAttribute(Type runtimeClassType)
    {
        RuntimeClassType = runtimeClassType;
    }

    /// <summary>
    /// Gets the projected Windows Runtime class type that the annotated interface is exclusive to.
    /// </summary>
    public Type RuntimeClassType { get; }
}