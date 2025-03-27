﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(object? value)
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

        // If 'value' is a managed wrapper for a native delegate, it probably can't be marshalled
        if (value is Delegate { Target: WindowsRuntimeObjectReference windowsRuntimeDelegate })
        {
            // Try to do a 'QueryInterface' just in case, and throw if it fails (which is very likely)
            if (!windowsRuntimeDelegate.TryAsUnsafe(in WellKnownInterfaceIds.IID_IInspectable, out void* inspectablePtr))
            {
                [DoesNotReturn]
                [StackTraceHidden]
                static void ThrowArgumentException(object value)
                {
                    throw new NotSupportedException(
                        $"This delegate instance of type '{value.GetType()}' cannot be marshalled as a Windows Runtime 'IInspectable' object, because it is wrapping a native " +
                        $"Windows Runtime delegate object, which does not implement the 'IInspectable' interface. Only managed delegate instances can be marshalled this way.");
                }

                ThrowArgumentException(value);
            }

            return new(inspectablePtr);
        }

        // Marshal 'value' as an 'IInspectable' (this method will take care of correctly marshalling objects with the right vtables)
        void* thisPtr = (void*)WindowsRuntimeComWrappers.Default.GetOrCreateInspectableInterfaceForObject(value);

        // 'ComWrappers' returns an 'IUnknown' pointer, so we need to do an actual 'QueryInterface' for the interface IID
        HRESULT hresult = IUnknownVftbl.QueryInterfaceUnsafe(thisPtr, in WellKnownInterfaceIds.IID_IInspectable, out void* interfacePtr);

        // We can release the 'IUnknown' reference now, it's no longer needed
        _ = IUnknownVftbl.ReleaseUnsafe(thisPtr);

        // Ensure the 'QueryInterface' succeeded (if it doesn't, it's some kind of authoring error)
        Marshal.ThrowExceptionForHR(hresult);

        return new(interfacePtr);
    }

    /// <summary>
    /// Converts an unmanaged pointer to a Windows Runtime object to a managed object.
    /// </summary>
    /// <param name="value">The input object to convert to managed.</param>
    /// <returns>The resulting managed managed object.</returns>
    public static object? ConvertToManaged(void* value)
    {
        if (value is null)
        {
            return null;
        }

        WindowsRuntimeComWrappers.ComWrappersCallback = null;
        WindowsRuntimeComWrappers.CreateObjectTargetType = null;
        WindowsRuntimeComWrappers.CreateObjectTargetInterfacePointer = value;

        return WindowsRuntimeComWrappers.Default.GetOrCreateObjectForComInstance((nint)value, CreateObjectFlags.TrackerObject);
    }
}
