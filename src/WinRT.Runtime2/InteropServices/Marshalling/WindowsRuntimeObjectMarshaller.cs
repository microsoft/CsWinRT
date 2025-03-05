﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// A marshaller for Windows Runtime objects.
/// </summary>
public static unsafe class WindowsRuntimeObjectMarshaller
{
    /// <summary>
    /// Marshals a Windows Runtime object to a <see cref="WindowsRuntimeObjectReferenceValue"/> instance.
    /// </summary>
    /// <param name="value">The input object to marshal.</param>
    /// <returns>A <see cref="WindowsRuntimeObjectReferenceValue"/> instance for <paramref name="value"/>.</returns>
    /// <remarks>
    /// The returned <see cref="WindowsRuntimeObjectReferenceValue"/> value will own an additional
    /// reference for the marshalled <paramref name="value"/> instance (either its underlying native object, or
    /// a runtime-provided CCW for the managed object instance). It is responsibility of the caller to always
    /// make sure that the returned <see cref="WindowsRuntimeObjectReferenceValue"/> instance is disposed.
    /// </remarks>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanagedUnsafe(object? value)
    {
        if (value is null)
        {
            return default;
        }

        // If 'value' is a 'WindowsRuntimeObject', return the cached object reference for 'IInspectable'
        if (value is WindowsRuntimeObject { HasUnwrappableNativeObjectReference: true } windowsRuntimeObject)
        {
            return new(windowsRuntimeObject.InspectableObjectReference);
        }

        // If we got here, we need to marshal the object ourselves, like we do for interfaces. This applies
        // to both normal user-defined types, and managed types derived from Windows Runtime classes.
        void* thisPtr = (void*)WindowsRuntimeComWrappers.Default.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.TrackerSupport);

        // 'ComWrappers' returns an 'IUnknown' pointer, so we can't avoid an additional 'QueryInterface' for 'IInspectable'
        HRESULT hresult = IUnknownVftbl.QueryInterfaceUnsafe(thisPtr, in WellKnownInterfaceIds.IID_IInspectable, out void* interfacePtr);

        // Regardless of the 'QueryInterface' result, we should always release the original CCW pointer
        _ = IUnknownVftbl.ReleaseUnsafe(thisPtr);

        // All CCWs produced by our 'ComWrappers' implementation will always implement 'IInspectable'
        Debug.Assert(WellKnownErrorCodes.Succeeded(hresult));

        return new(interfacePtr);
    }
}
