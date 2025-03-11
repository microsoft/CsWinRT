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
}
