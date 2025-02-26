// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime;

/// <summary>
/// An attribute containing the untyped marshalling logic for objects being passed to native code.
/// It is used in either of the following scenarios:
/// <list type="bullet">
///   <item>Projected types (either RCWs, or boxed value types).</item>
///   <item>Managed types (in which case, the attribute is on their associated proxy type).</item>
/// </list>
/// </summary>
[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Interface |
    AttributeTargets.Enum |
    AttributeTargets.Struct |
    AttributeTargets.Delegate,
    AllowMultiple = false)]
public abstract unsafe class WindowsRuntimeMarshallerAttribute : Attribute
{
    /// <summary>
    /// Marshals a boxed object (RCW, boxed projected value type, or boxed managed type), to the right <c>IInspectable</c> CCW.
    /// </summary>
    /// <param name="value">The input managed object to marshal.</param>
    /// <returns>A pointer to the resulting <c>IInspectable</c> CCW.</returns>
    public abstract void* ConvertToUnmanaged(object? value);
}
