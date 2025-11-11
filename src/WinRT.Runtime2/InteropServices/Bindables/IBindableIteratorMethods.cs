// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using WindowsRuntime.InteropServices.Marshalling;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Methods for <c>Windows.UI.Xaml.Interop.IBindableIterator</c> types.
/// </summary>
internal static unsafe class IBindableIteratorMethods
{
    /// <summary>
    /// Gets the current item in the collection.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <returns>The current item in the collection.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindableiterator.current"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static object? Current(WindowsRuntimeObjectReference thisReference)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        void* result;

        RestrictedErrorInfo.ThrowExceptionForHR(((IBindableIteratorVftbl*)*(void***)thisPtr)->get_Current(thisPtr, &result));

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
    /// Gets a value that indicates whether the iterator refers to a current item or is at the end of the collection.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <returns>
    /// True if the iterator refers to a valid item in the collection, false if the iterator passes the end of the collection.
    /// </returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindableiterator.hascurrent"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool HasCurrent(WindowsRuntimeObjectReference thisReference)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        bool result;

        RestrictedErrorInfo.ThrowExceptionForHR(((IBindableIteratorVftbl*)*(void***)thisPtr)->get_HasCurrent(thisPtr, &result));

        return result;
    }

    /// <summary>
    /// Moves the iterator forward to the next item and returns <see cref="HasCurrent"/>.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <returns>
    /// True if the iterator refers to a valid item in the collection, false if the iterator passes the end of the collection.
    /// </returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindableiterator.movenext"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool MoveNext(WindowsRuntimeObjectReference thisReference)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        bool result;

        RestrictedErrorInfo.ThrowExceptionForHR(((IBindableIteratorVftbl*)*(void***)thisPtr)->MoveNext(thisPtr, &result));

        return result;
    }
}
