// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using WindowsRuntime.InteropServices.Marshalling;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Methods for <c>Windows.UI.Xaml.Interop.IBindableVector</c> types.
/// </summary>
internal static unsafe class IBindableVectorMethods
{
    /// <summary>
    /// Returns the item at the specified index in the vector.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The item at the specified index.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindablevector.getat"/>
    public static object? GetAt(WindowsRuntimeObjectReference thisReference, uint index)
    {
        // The vtable slot for 'GetAt' is identical between 'IBindableVector' and 'IBindableIVectorView'
        return IBindableVectorViewMethods.GetAt(thisReference, index);
    }

    /// <summary>
    /// Gets the number of items in the vector.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <returns>The number of items in the vector view.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindablevector.size"/>
    public static uint Size(WindowsRuntimeObjectReference thisReference)
    {
        // The vtable slot for 'get_Size' is identical between 'IBindableVector' and 'IBindableIVectorView'
        return IBindableVectorViewMethods.Size(thisReference);
    }

    /// <summary>
    /// Retrieves the index of a specified item in the vector.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <param name="value">The item to find in the vector.</param>
    /// <param name="index">If the item is found, this is the zero-based index of the item; otherwise, this parameter is <c>0</c>.</param>
    /// <returns><see langword="true"/> if the item is found; otherwise, <see langword="false"/>.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindablevector.indexof"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool IndexOf(WindowsRuntimeObjectReference thisReference, object? value, out uint index)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();
        using WindowsRuntimeObjectReferenceValue valueValue = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(value);

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        bool result;

        fixed (uint* indexPtr = &index)
        {
            RestrictedErrorInfo.ThrowExceptionForHR(((IBindableVectorVftbl*)*(void***)thisPtr)->IndexOf(
                thisPtr,
                valueValue.GetThisPtrUnsafe(),
                indexPtr,
                &result));
        }

        return result;
    }

    /// <summary>
    /// Sets the value at the specified index in the vector.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <param name="index">The zero-based index at which to set the value.</param>
    /// <param name="value">The item to set.</param>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindablevector.setat"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SetAt(WindowsRuntimeObjectReference thisReference, uint index, object? value)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();
        using WindowsRuntimeObjectReferenceValue valueValue = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(value);

        void* thisPtr = thisValue.GetThisPtrUnsafe();

        RestrictedErrorInfo.ThrowExceptionForHR(((IBindableVectorVftbl*)*(void***)thisPtr)->SetAt(
            thisPtr,
            index,
            valueValue.GetThisPtrUnsafe()));
    }

    /// <summary>
    /// Inserts an item at a specified index in the vector.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <param name="index">The zero-based index.</param>
    /// <param name="value">The item to insert.</param>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindablevector.insertat"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void InsertAt(WindowsRuntimeObjectReference thisReference, uint index, object? value)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();
        using WindowsRuntimeObjectReferenceValue valueValue = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(value);

        void* thisPtr = thisValue.GetThisPtrUnsafe();

        RestrictedErrorInfo.ThrowExceptionForHR(((IBindableVectorVftbl*)*(void***)thisPtr)->InsertAt(
            thisPtr,
            index,
            valueValue.GetThisPtrUnsafe()));
    }

    /// <summary>
    /// Removes the item at the specified index in the vector.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <param name="index">The zero-based index of the vector item to remove.</param>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindablevector.removeat"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void RemoveAt(WindowsRuntimeObjectReference thisReference, uint index)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();

        RestrictedErrorInfo.ThrowExceptionForHR(((IBindableVectorVftbl*)*(void***)thisPtr)->RemoveAt(thisPtr, index));
    }

    /// <summary>
    /// Appends an item to the end of the vector.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <param name="value">The item to append to the vector.</param>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindablevector.append"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Append(WindowsRuntimeObjectReference thisReference, object? value)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();
        using WindowsRuntimeObjectReferenceValue valueValue = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(value);

        void* thisPtr = thisValue.GetThisPtrUnsafe();

        RestrictedErrorInfo.ThrowExceptionForHR(((IBindableVectorVftbl*)*(void***)thisPtr)->Append(
            thisPtr,
            valueValue.GetThisPtrUnsafe()));
    }

    /// <summary>
    /// Removes all items from the vector.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindablevector.clear"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Clear(WindowsRuntimeObjectReference thisReference)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();

        RestrictedErrorInfo.ThrowExceptionForHR(((IBindableVectorVftbl*)*(void***)thisPtr)->Clear(thisPtr));
    }
}
