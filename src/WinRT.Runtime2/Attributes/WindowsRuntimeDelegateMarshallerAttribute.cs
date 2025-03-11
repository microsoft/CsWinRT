// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.InteropServices;

namespace WindowsRuntime;

/// <summary>
/// An attribute containing the untyped marshalling logic for Windows Runtime delegates being passed to native code.
/// </summary>
/// <remarks>
/// This attribute is only meant to be used on ABI types for Windows Runtime delegate types.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public abstract unsafe class WindowsRuntimeDelegateMarshallerAttribute : Attribute
{
    /// <inheritdoc cref="InteropServices.Marshalling.WindowsRuntimeDelegateMarshaller.ConvertToUnmanagedUnsafe"/>
    public abstract WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(Delegate value);

    /// <inheritdoc cref="InteropServices.Marshalling.WindowsRuntimeDelegateMarshaller.ConvertToManagedUnsafe"/>
    public abstract Delegate ConvertToManagedUnsafe(in WindowsRuntimeObjectReferenceValue value);
}
