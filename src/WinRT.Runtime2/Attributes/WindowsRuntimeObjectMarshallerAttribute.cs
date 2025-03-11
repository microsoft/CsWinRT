// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.InteropServices;

namespace WindowsRuntime;

/// <summary>
/// An attribute containing the untyped marshalling logic for Windows Runtime objects being passed to native code.
/// </summary>
/// <remarks>
/// <para>
/// This attribute is used in either of the following scenarios:
/// <list type="bullet">
///   <item>Projected types (either RCWs, or boxed value types).</item>
///   <item>Managed types (in which case, the attribute is on their associated proxy types).</item>
/// </list>
/// </para>
/// <para>
/// This attribute is only meant to be used to marshal objects which implement the <c>IInspectable</c> interface.
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
public abstract unsafe class WindowsRuntimeObjectMarshallerAttribute : Attribute
{
    /// <inheritdoc cref="InteropServices.Marshalling.WindowsRuntimeObjectMarshaller.ConvertToUnmanaged"/>
    public abstract WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(object value);

    /// <inheritdoc cref="InteropServices.Marshalling.WindowsRuntimeObjectMarshaller.ConvertToManaged"/>
    public abstract object ConvertToManaged(void* value);
}
