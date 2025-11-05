// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using WindowsRuntime.InteropServices.Marshalling;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Methods for <c>Windows.UI.Xaml.Interop.IBindableVectorView</c> types.
/// </summary>
internal static unsafe class IBindableVectorViewMethods
{
    /// <summary>
    /// Returns the item at the specified index in the vector view.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The item at the specified index.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindablevectorview.getat"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static object? GetAt(WindowsRuntimeObjectReference thisReference, uint index)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        void* result;

        RestrictedErrorInfo.ThrowExceptionForHR(((IBindableVectorViewVftbl*)*(void***)thisPtr)->GetAt(thisPtr, index, &result));

        try
        {
            return WindowsRuntimeObjectMarshaller.ConvertToManaged(result);

        }
        finally
        {
            WindowsRuntimeUnknownMarshaller.Free(result);
        }
    }

    /// <summary>
    /// Gets the number of items in the vector view.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <returns>The number of items in the vector view.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindablevectorview.size"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static uint Size(WindowsRuntimeObjectReference thisReference)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        uint result;

        RestrictedErrorInfo.ThrowExceptionForHR(((IBindableVectorViewVftbl*)*(void***)thisPtr)->get_Size(thisPtr, &result));

        return result;
    }

    /// <summary>
    /// Retrieves the index of a specified item in the vector.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <param name="value">The item to find in the vector.</param>
    /// <param name="index">If the item is found, this is the zero-based index of the item; otherwise, this parameter is <c>0</c>.</param>
    /// <returns><see langword="true"/> if the item is found; otherwise, <see langword="false"/>.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindablevectorview.indexof"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool IndexOf(WindowsRuntimeObjectReference thisReference, object value, out uint index)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();
        using WindowsRuntimeObjectReferenceValue valueValue = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(value);

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        bool result;

        fixed (uint* indexPtr = &index)
        {
            RestrictedErrorInfo.ThrowExceptionForHR(((IBindableVectorViewVftbl*)*(void***)thisPtr)->IndexOf(
                thisPtr,
                valueValue.GetThisPtrUnsafe(),
                indexPtr,
                &result));
        }

        return result;
    }
}
