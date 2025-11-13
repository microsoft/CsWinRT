// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Indicates the runtime class name to use for types exposed to the Windows Runtime.
/// </summary>
/// <remarks>
/// <para>
/// By default, types marshalled as Windows Runtime objects will automatically have their runtime class name be the one for the
/// first projected Windows Runtime interface they implement. This attribute allows to customize that behavior and explicitly set
/// a runtime class name. This name is the one that will be returned to native callers when
/// <see href="https://learn.microsoft.com/windows/win32/api/inspectable/nf-inspectable-iinspectable-getruntimeclassname"><c>IInspectable::GetRuntimeClassName</c></see>
/// is invoked on a CCW for the marshalled object.
/// </para>
/// <para>
/// For instance, this can be useful in OOP (out-of-process) scenarios, where having the runtime class name of a managed type matching the
/// class name that will be used with MBM (metadata-based marshalling), will ensure that marshalling will work correctly at runtime.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
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
