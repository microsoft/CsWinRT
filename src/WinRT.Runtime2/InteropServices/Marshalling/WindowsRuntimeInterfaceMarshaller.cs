﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// A marshaller for Windows Runtime interfaces.
/// </summary>
public static unsafe class WindowsRuntimeInterfaceMarshaller
{
    /// <summary>
    /// Marshals a Windows Runtime interface to a <see cref="WindowsRuntimeObjectReferenceValue"/> instance.
    /// </summary>
    /// <typeparam name="T">The type of the interface being marshalled.</typeparam>
    /// <param name="value">The input <typeparamref name="T"/> object to marshal.</param>
    /// <param name="iid">The IID for the interface being marshalled.</param>
    /// <returns>A <see cref="WindowsRuntimeObjectReferenceValue"/> instance for the requested interface.</returns>
    /// <remarks>
    /// <para>
    /// This method does not validate the <paramref name="iid"/> value. It is responsibility of the caller to
    /// ensure the parameter matches the actual IID of the <typeparamref name="T"/> interface being marshalled.
    /// </para>
    /// <para>
    /// Furthermore, the returned <see cref="WindowsRuntimeObjectReferenceValue"/> value will own an additional
    /// reference for the marshalled <paramref name="value"/> instance (either its underlying native object, or
    /// a runtime-provided CCW for the managed object instance). It is responsibility of the caller to always
    /// make sure that the returned <see cref="WindowsRuntimeObjectReferenceValue"/> instance is disposed.
    /// </para>
    /// <para>
    /// This method is only meant to be used for interface types. For other types, use the appropriate marshaller.
    /// Calling this method with <typeparamref name="T"/> being a non-interface type results in undefined behavior.
    /// </para>
    /// </remarks>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged<T>(T? value, in Guid iid)
        where T : class
    {
        if (value is null)
        {
            return default;
        }

        // If 'value' is a projected runtime class, get the cached object reference for the interface.
        // This is a critical optimization that avoids 'QueryInterface' for interface method parameters.
        if (value is IWindowsRuntimeInterface<T> windowsRuntimeInterface)
        {
            return windowsRuntimeInterface.GetInterface();
        }

        // If we got here, it means that 'value' is a managed, user-defined type implementing the Windows Runtime interface.
        // We can then get or create the CCW for it. The interface should be present in the generated vtable for the type.
        void* thisPtr = WindowsRuntimeMarshallingInfo.TryGetInfo(value.GetType(), out WindowsRuntimeMarshallingInfo? info)
            ? info.GetComWrappersMarshaller().GetOrCreateComInterfaceForObject(value)
            : (void*)WindowsRuntimeComWrappers.Default.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.TrackerSupport);

        // We need an interface pointer, so in this scenario we can't really avoid a 'QueryInterface' call.
        // The local cache for object references only applies to projected runtime classes, not managed types.
        HRESULT hresult = IUnknownVftbl.QueryInterfaceUnsafe(thisPtr, in iid, out void* interfacePtr);

        // We can release the 'IUnknown' reference now, it's no longer needed
        _ = IUnknownVftbl.ReleaseUnsafe(thisPtr);

        // It is very unlikely for this 'QueryInterface' to fail (it means either a managed object has an invalid vtable,
        // or something else happened that is not really supported). Still, we can produce a nice error message for it.
        if (!WellKnownErrorCodes.Succeeded(hresult))
        {
            // Helper to throw the exception without increasing the codegen in the fast path.
            // The JIT can already optimize throw helpers, but that's only when they contain
            // a 'throw new Exception(...)' body. Because this method is more complex, chances
            // are that optimization would not kick in. So we're just manually not inlining it.
            [StackTraceHidden]
            [MethodImpl(MethodImplOptions.NoInlining)]
            static void ThrowException(object value, in Guid iid, HRESULT hresult)
            {
                // Special case 'E_NOINTERFACE' to provide a better exception message. It is intentional that this exception is
                // thrown inline like this and without setting up the 'IErrorInfo' infrastructure. That is because APIs do not
                // typically originate the 'IErrorInfo' data for 'E_NOINTERFACE', so that is not necessary here for consistency.
                if (hresult == WellKnownErrorCodes.E_NOINTERFACE)
                {
                    throw new InvalidCastException(
                        $"Failed to create a CCW for object of type '{value.GetType()}' for interface with " +
                        $"IID '{iid.ToString().ToUpperInvariant()}': the specified cast is not valid.");
                }

                RestrictedErrorInfo.ThrowExceptionForHR(hresult);
            }

            ThrowException(value, in iid, hresult);
        }

        return new(interfacePtr);
    }
}
