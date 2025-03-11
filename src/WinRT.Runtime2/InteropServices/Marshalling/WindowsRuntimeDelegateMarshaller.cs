// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// A marshaller for Windows Runtime delegates.
/// </summary>
public static unsafe class WindowsRuntimeDelegateMarshaller
{
    /// <summary>
    /// Marshals a Windows Runtime delegate to a native COM object interface pointer.
    /// </summary>
    /// <param name="value">The input delegate to marshal.</param>
    /// <param name="iid">The IID of the delegate type.</param>
    /// <returns>The resulting marshalled object for <paramref name="value"/>.</returns>
    /// <remarks>
    /// The returned <see cref="WindowsRuntimeObjectReferenceValue"/> value will own an additional
    /// reference for the marshalled <paramref name="value"/> instance (either its underlying native object, or
    /// a runtime-provided CCW for the managed object instance). It is responsibility of the caller to always
    /// make sure that the returned <see cref="WindowsRuntimeObjectReferenceValue"/> instance is disposed.
    /// </remarks>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(Delegate? value, in Guid iid)
    {
        if (value is null)
        {
            return default;
        }

        // Delegates coming from native code are projected with a type deriving from 'WindowsRuntimeObject',
        // which has a method implementing the ABI function, which the returned 'Delegate' instance closes
        // over. So to check for that case, we get the target of the delegate and check if we can unwrap it.
        if (value.Target is WindowsRuntimeObject { HasUnwrappableNativeObjectReference: true } windowsRuntimeObject)
        {
            return windowsRuntimeObject.NativeObjectReference.AsValue(in iid);
        }

        // The input delegate is a managed one, so we need to find the proxy type to marshal it.
        // Contrary to when normal objects are marshalled, delegates can't be marshalled if no
        // associated marshalling info is available, as otherwise we wouldn't be able to have
        // the necessary marshalling stub to dispatch delegate invocations from native code.
        return WindowsRuntimeMarshallingInfo.GetInfo(value.GetType()).GetObjectMarshaller().ConvertToUnmanaged(value);
    }

    /// <summary>
    /// Converts an unmanaged pointer to a Windows Runtime delegate to its managed <typeparamref name="T"/> object.
    /// </summary>
    /// <typeparam name="T">The type of delegate to marshal values to (it cannot be <see cref="Delegate"/>).</typeparam>
    /// <param name="value">The input delegate to convert to managed.</param>
    /// <returns>The resulting managed <typeparamref name="T"/> value.</returns>
    public static T? ConvertToManaged<T>(void* value)
        where T : Delegate
    {
        if (value is null)
        {
            return null;
        }

        WindowsRuntimeComWrappers.CreateDelegateTargetType = typeof(T);
        WindowsRuntimeComWrappers.CreateObjectTargetType = null;

        object? managedDelegate = WindowsRuntimeComWrappers.Default.GetOrCreateObjectForComInstance((nint)value, CreateObjectFlags.TrackerObject);

        WindowsRuntimeComWrappers.CreateDelegateTargetType = null;

        return (T?)managedDelegate;
    }

    /// <summary>
    /// Unboxes and converts an unmanaged pointer to a Windows Runtime object to its managed <typeparamref name="T"/> object.
    /// </summary>
    /// <typeparam name="T">The type of delegate to marshal values to (it cannot be <see cref="Delegate"/>).</typeparam>
    /// <param name="value">The input object to unbox and convert to managed.</param>
    /// <param name="iid">The IID of the <c>IReference`1</c> generic instantiation for boxed <typeparamref name="T"/> native delegates.</param>
    /// <returns>The resulting managed <typeparamref name="T"/> value.</returns>
    /// <remarks>
    /// <para>
    /// This method should only be used to unbox <c>IReference`1</c> objects to their underlying Windows Runtime delegate type.
    /// </para>
    /// <para>
    /// Unlike <see cref="ConvertToManaged"/>, the <paramref name="value"/> parameter is expected to be an <c>IInspectable</c> pointer.
    /// </para>
    /// </remarks>
    public static T? UnboxToManaged<T>(void* value, in Guid iid)
        where T : Delegate
    {
        if (value is null)
        {
            return null;
        }

        // First, make sure we have the right 'IReference<T>' interface on 'value'
        HRESULT hresult = IUnknownVftbl.QueryInterfaceUnsafe(value, in iid, out void* referencePtr);

        Marshal.ThrowExceptionForHR(hresult);

        // Now that we have the 'IReference<T>' interface, we can unbox the native delegate
        hresult = IReferenceVftbl.ValueUnsafe(referencePtr, out void* delegatePtr);

        Marshal.ThrowExceptionForHR(hresult);

        // At this point, we just convert the native delegate to a 'T' instance normally
        return ConvertToManaged<T>(delegatePtr);
    }
}
