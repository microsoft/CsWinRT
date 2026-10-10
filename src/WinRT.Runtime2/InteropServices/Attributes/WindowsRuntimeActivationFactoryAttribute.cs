// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Indicates the authored Windows Runtime class type that a given activation factory is for.
/// </summary>
/// <remarks>
/// Apply this to a class implementing the activation factory for a Windows Runtime type being authored in C#. CsWinRT
/// registers the annotated type as the activation factory for the runtime class name of <see cref="RuntimeClassType"/>,
/// so that activation requests for that class are served by it.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class WindowsRuntimeActivationFactoryAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeActivationFactoryAttribute"/> instance with the specified parameters.
    /// </summary>
    /// <param name="runtimeClassType">The authored Windows Runtime class type that the annotated activation factory is for.</param>
    public WindowsRuntimeActivationFactoryAttribute(Type runtimeClassType)
    {
        RuntimeClassType = runtimeClassType;
    }

    /// <summary>
    /// Gets the authored Windows Runtime class type that the annotated activation factory is for.
    /// </summary>
    public Type RuntimeClassType { get; }
}
