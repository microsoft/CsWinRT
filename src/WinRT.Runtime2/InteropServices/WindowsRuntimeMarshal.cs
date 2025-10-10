// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable CS8909

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Provides low-level marshalling functionality for Windows Runtime objects.
/// </summary>
public static unsafe class WindowsRuntimeMarshal
{
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
        if (TryUnwrapObjectReference(left, out WindowsRuntimeObjectReference? leftReference) &&
            TryUnwrapObjectReference(right, out WindowsRuntimeObjectReference? rightReference))
        {
            using WindowsRuntimeObjectReferenceValue leftUnknown = leftReference.AsValue(WellKnownInterfaceIds.IID_IUnknown);
            using WindowsRuntimeObjectReferenceValue rightUnknown = rightReference.AsValue(WellKnownInterfaceIds.IID_IUnknown);

            return leftUnknown.GetThisPtrUnsafe() == rightUnknown.GetThisPtrUnsafe();
        }

        return false;
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
        if (externalComObject != null && IsReferenceToManagedObject(externalComObject))
        {
            result = ComWrappers.ComInterfaceDispatch.GetInstance<object>((ComWrappers.ComInterfaceDispatch*)externalComObject);

            return true;
        }

        result = null;

        return false;
    }

    /// <summary>
    /// Attempts to extract a <see cref="WindowsRuntimeObjectReference"/> from the specified object.
    /// </summary>
    /// <param name="value">The object to attempt to unwrap.</param>
    /// <param name="objectReference">The unwrapped <see cref="WindowsRuntimeObjectReference"/> object, if successfully retrieved.</param>
    /// <returns>Whether <paramref name="objectReference"/> was successfully unwrapped.</returns>
    /// <remarks>
    /// This method supports unwrapping objects that are either:
    /// <list type="bullet">
    ///   <item>A <see cref="WindowsRuntimeObject"/> with a native object reference that can be unwrapped.</item>
    ///   <item>
    ///     A <see cref="Delegate"/> whose target is a <see cref="WindowsRuntimeObjectReference"/>. Such instances
    ///     are created by the generated projections, for all projected Windows runtime delegate types.
    ///   </item>
    /// </list>
    /// If the object does not meet these criteria, this method will just return <see langword="null"/>.
    /// </remarks>
    public static bool TryUnwrapObjectReference(
        [NotNullWhen(true)] object? value,
        [NotNullWhen(true)] out WindowsRuntimeObjectReference? objectReference)
    {
        switch (value)
        {
            // If 'value' is a 'WindowsRuntimeObject' that can be unwrapped, return the wrapped object reference
            case WindowsRuntimeObject { HasUnwrappableNativeObjectReference: true } windowsRuntimeObject:
                objectReference = windowsRuntimeObject.NativeObjectReference;
                return true;

            // If 'value' is a marshalled delegate, return the target object reference directly
            case Delegate { Target: WindowsRuntimeObjectReference targetObjectReference }:
                objectReference = targetObjectReference;
                return true;

            // Otherwise, we can't unwrap the value at all
            default:
                objectReference = null;
                return false;
        }
    }
}
