// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime;

/// <summary>
/// Indicates the runtime class name to use for types exposed to the Windows Runtime.
/// </summary>
/// <remarks>
/// <para>
/// This attribute is emitted by the CsWinRT generator for non-authored types implementing Windows Runtime interfaces.
/// </para>
/// <para>
/// It can also be used manually to explicitly customize the runtime class name of exposed types.
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
public sealed class WindowsRuntimeClassNameAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeClassNameAttribute"/> instance with the specified parameters.
    /// </summary>
    /// <param name="runtimeClassName">The runtime class name to use.</param>
    public WindowsRuntimeClassNameAttribute(string runtimeClassName)
    {
        RuntimeClassName = runtimeClassName;
    }

    /// <summary>
    /// Gets the runtime class name for the current instance.
    /// </summary>
    public string RuntimeClassName { get; }
}
