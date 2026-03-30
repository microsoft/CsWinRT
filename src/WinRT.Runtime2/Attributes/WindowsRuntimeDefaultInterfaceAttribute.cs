// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime;

/// <summary>
/// Indicates the default interface for a projected Windows Runtime class type. This attribute is applied to a
/// centralized lookup type to associate a runtime class type with its default interface, rather than being placed
/// on the runtime class type itself. This allows the interface type reference to be trimmed away when not needed.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class WindowsRuntimeDefaultInterfaceAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeDefaultInterfaceAttribute"/> instance with the specified parameters.
    /// </summary>
    /// <param name="runtimeClassType">The projected Windows Runtime class type that the default interface is for.</param>
    /// <param name="interfaceType">The type of the default interface for the specified Windows Runtime class type.</param>
    public WindowsRuntimeDefaultInterfaceAttribute(Type runtimeClassType, Type interfaceType)
    {
        RuntimeClassType = runtimeClassType;
        InterfaceType = interfaceType;
    }

    /// <summary>
    /// Gets the projected Windows Runtime class type that the default interface is for.
    /// </summary>
    public Type RuntimeClassType { get; }

    /// <summary>
    /// Gets the type of the default interface for the specified Windows Runtime class type.
    /// </summary>
    public Type InterfaceType { get; }
}