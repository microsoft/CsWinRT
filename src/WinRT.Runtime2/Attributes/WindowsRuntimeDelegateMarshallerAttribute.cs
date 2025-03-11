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
    /// <summary>
    /// Marshals a boxed object (RCW, boxed projected value type, or boxed managed type), to the right CCW.
    /// </summary>
    /// <param name="value">The input managed object to marshal.</param>
    /// <returns>A pointer to the resulting CCW.</returns>
    /// <remarks>
    /// <para>
    /// It is not required for implementations of this methods to perform a <c>QueryInterface</c> call
    /// for <c>IInspectable</c>, as callers will have to do one regardless. As such, implementations
    /// are allowed to save work and return the most convenient interface pointer they prefer, if needed.
    /// </para>
    /// <para>
    /// For instance, if creating a CCW via <see cref="InteropServices.WindowsRuntimeComWrappers"/>, which
    /// will return an <c>IUnknown</c> interface pointer, that value can then be returned directly from here.
    /// </para>
    /// </remarks>
    public abstract object ConvertToManagedUnsafe(WindowsRuntimeObjectReferenceValue value);
}
