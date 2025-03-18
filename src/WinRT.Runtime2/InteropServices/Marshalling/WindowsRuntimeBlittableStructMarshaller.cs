﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// A marshaller for Windows Runtime blittable struct types.
/// </summary>
public static unsafe class WindowsRuntimeBlittableStructMarshaller
{
    /// <summary>
    /// Marshals a Windows Runtime blittable struct value to a native COM object interface pointer for a boxed value.
    /// </summary>
    /// <typeparam name="T">The blittable struct type to marshal.</typeparam>
    /// <param name="value">The input blittable struct value to marshal.</param>
    /// <param name="iid">The IID of the <c>IReference`1</c> interface for the Windows Runtime blittable struct type.</param>
    /// <returns>The resulting marshalled object for <paramref name="value"/>, as a boxed <c>IReference`1</c> interface pointer.</returns>
    /// <exception cref="Exception">Thrown if <paramref name="value"/> cannot be marshalled.</exception>
    public static WindowsRuntimeObjectReferenceValue BoxToUnmanaged<T>(T? value, in Guid iid)
        where T : unmanaged
    {
        if (value is null)
        {
            return default;
        }

        // Box the value type (we don't need reference tracking support here, it's just a 
        void* thisPtr = (void*)WindowsRuntimeComWrappers.Default.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.None);

        // Do the 'QueryInterface' for the 'IReference<T>' interface (this should always succeed)
        HRESULT hresult = IUnknownVftbl.QueryInterfaceUnsafe(thisPtr, in iid, out void* boxPtr);

        // We can release the 'IUnknown' reference now
        _ = IUnknownVftbl.ReleaseUnsafe(thisPtr);

        // Ensure the 'QueryInterface' succeeded (same as above)
        Marshal.ThrowExceptionForHR(hresult);

        return new(boxPtr);
    }

    /// <summary>
    /// Unboxes and converts an unmanaged pointer to a Windows Runtime object to its managed representation.
    /// </summary>
    /// <typeparam name="T">The blittable struct type to marshal.</typeparam>
    /// <param name="value">The input object to unbox and convert to managed.</param>
    /// <returns>The resulting managed Windows Runtime blittable struct value.</returns>
    /// <remarks>
    /// <para>
    /// This method should only be used to unbox <c>IReference`1</c> objects to their underlying Windows Runtime blittable struct type.
    /// </para>
    /// <para>
    /// The <paramref name="value"/> parameter is expected to be an <c>IReference`1</c> pointer.
    /// </para>
    /// </remarks>
    /// <exception cref="Exception">Thrown if <paramref name="value"/> cannot be marshalled.</exception>
    public static T? UnboxToManaged<T>(void* value)
        where T : unmanaged
    {
        if (value is null)
        {
            return null;
        }

        T result;

        // Unbox the blittable value (we always just discard the outer reference)
        HRESULT hresult = IReferenceVftbl.ValueUnsafe(value, &result);

        Marshal.ThrowExceptionForHR(hresult);

        return result;
    }
}
