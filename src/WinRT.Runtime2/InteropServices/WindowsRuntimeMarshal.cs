// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;

#pragma warning disable CS8909

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Provides low-level marshalling functionality for Windows Runtime objects.
/// </summary>
public static unsafe class WindowsRuntimeMarshal
{
    /// <summary>
    /// Checks whether two objects are the same, or represent the same underlying native object.
    /// </summary>
    /// <param name="left">The first object to compare.</param>
    /// <param name="right">The second object to compare.</param>
    /// <returns>Whether <paramref name="left"/> and <paramref name="right"/> are the same object or wrap the same underlying native object.</returns>
    public static bool NativeReferenceEquals(object? left, object? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        // Unwrap both objects and check whether the underlying object is the same. To do this we also need
        // to query for the 'IUnknown' interface pointer, to ensure we get the actual identity of the objects.
        if (WindowsRuntimeComWrappersMarshal.TryUnwrapObjectReference(left, out WindowsRuntimeObjectReference? leftReference) &&
            WindowsRuntimeComWrappersMarshal.TryUnwrapObjectReference(right, out WindowsRuntimeObjectReference? rightReference))
        {
            using WindowsRuntimeObjectReferenceValue leftUnknown = leftReference.AsValue(WellKnownWindowsInterfaceIIDs.IID_IUnknown);
            using WindowsRuntimeObjectReferenceValue rightUnknown = rightReference.AsValue(WellKnownWindowsInterfaceIIDs.IID_IUnknown);

            return leftUnknown.GetThisPtrUnsafe() == rightUnknown.GetThisPtrUnsafe();
        }

        return false;
    }

    /// <summary>
    /// Checks whether a pointer to a COM object is actually a reference to a CCW produced for a managed object that was marshalled to native code.
    /// </summary>
    /// <param name="externalComObject">The external COM object to check.</param>
    /// <returns>Whether <paramref name="externalComObject"/> refers to a CCW for a managed object, rather than a native COM object.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="externalComObject"/> is <see langword="null"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsReferenceToManagedObject(void* externalComObject)
    {
        ArgumentNullException.ThrowIfNull(externalComObject);

        IUnknownVftbl* unknownVftbl = (IUnknownVftbl*)*(void***)externalComObject;
        IUnknownVftbl* runtimeVftbl = (IUnknownVftbl*)IUnknownImpl.Vtable;

        return
            unknownVftbl->QueryInterface == runtimeVftbl->QueryInterface &&
            unknownVftbl->AddRef == runtimeVftbl->AddRef &&
            unknownVftbl->Release == runtimeVftbl->Release;
    }

    /// <summary>
    /// Tries to retrieve a managed object from a pointer to a COM object, if it is actually a reference to a CCW that was marshalled to native code.
    /// </summary>
    /// <param name="externalComObject">The external COM object to try to get a managed object from.</param>
    /// <param name="result">The resulting managed object, if successfully retrieved.</param>
    /// <returns>Whether <paramref name="externalComObject"/> was a reference to a managed object, and <paramref name="result"/> could be retrieved.</returns>
    public static bool TryGetManagedObject(void* externalComObject, [NotNullWhen(true)] out object? result)
    {
        // If the input pointer is a reference to a managed object, we can resolve the original managed object
        if (externalComObject is not null && IsReferenceToManagedObject(externalComObject))
        {
            result = ComWrappers.ComInterfaceDispatch.GetInstance<object>((ComWrappers.ComInterfaceDispatch*)externalComObject);

            return true;
        }

        result = null;

        return false;
    }

    /// <summary>
    /// Tries to retrieve a native object from a managed object, if it is actually a wrapper of some native object.
    /// </summary>
    /// <param name="managedObject">The managed object to try to get a native object from.</param>
    /// <param name="result">The resulting native object, if successfully retrieved.</param>
    /// <returns>Whether <paramref name="managedObject"/> was a reference to a native object, and <paramref name="result"/> could be retrieved.</returns>
    public static bool TryGetNativeObject([NotNullWhen(true)] object? managedObject, out void* result)
    {
        // If the input object is wrapping a native object, we can unwrap it and return it after incrementing its reference count
        if (WindowsRuntimeComWrappersMarshal.TryUnwrapObjectReference(managedObject, out WindowsRuntimeObjectReference? objectReference))
        {
            result = objectReference.GetThisPtr();

            return true;
        }

        result = null;

        return false;
    }

    /// <summary>
    /// Marshals a managed object to a native object, unwrapping it or creating a CCW for it as needed.
    /// </summary>
    /// <param name="managedObject">The managed object to marshal.</param>
    /// <remarks>
    /// <para>
    /// The returned native object will own an additional reference for the marshalled <paramref name="managedObject"/>
    /// instance (either its underlying native object, or a runtime-provided CCW for the managed object instance). It is
    /// responsibility of the caller to always make sure that the returned native object is disposed.
    /// </para>
    /// <para>
    /// Additionally, it is responsibility of the caller to perform proper reference tracking or to handle different
    /// COM contexts, in case the returned pointer is stored on a class field or passed across different threads.
    /// </para>
    /// </remarks>
    /// <seealso cref="System.Runtime.InteropServices.Marshalling.ComInterfaceMarshaller{T}.ConvertToUnmanaged"/>
    public static void* ConvertToUnmanaged(object? managedObject)
    {
        return WindowsRuntimeUnknownMarshaller.ConvertToUnmanaged(managedObject).DetachThisPtrUnsafe();
    }

    /// <summary>
    /// Converts an unmanaged pointer to a Windows Runtime object to a managed object, either by unwrapping the
    /// original managed object that was previously marshalled, or retrieving or creating an RCW for it.
    /// </summary>
    /// <param name="value">The input object to convert to managed.</param>
    /// <returns>The resulting managed managed object.</returns>
    /// <seealso cref="System.Runtime.InteropServices.Marshalling.ComInterfaceMarshaller{T}.ConvertToManaged"/>
    public static object? ConvertToManaged(void* value)
    {
        return WindowsRuntimeObjectMarshaller.ConvertToManaged(value);
    }

    /// <summary>
    /// Release a given native object.
    /// </summary>
    /// <param name="value">The input object to free.</param>
    /// <remarks>
    /// Unlike <see cref="Marshal.Release"/>, this method will not throw <see cref="ArgumentNullException"/>
    /// if <paramref name="value"/> is <see langword="null"/>. This method can be used with any object type.
    /// </remarks>
    /// <seealso cref="System.Runtime.InteropServices.Marshalling.ComInterfaceMarshaller{T}.Free"/>
    public static void Free(void* value)
    {
        WindowsRuntimeUnknownMarshaller.Free(value);
    }
}
