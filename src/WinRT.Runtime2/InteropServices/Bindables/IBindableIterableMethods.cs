// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Runtime.CompilerServices;
using WindowsRuntime.InteropServices.Marshalling;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Methods for <c>Windows.UI.Xaml.Interop.IBindableIterable</c> types.
/// </summary>
internal static unsafe class IBindableIterableMethods
{
    /// <summary>
    /// Returns an iterator for the items in the collection.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <returns>The iterator.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindableiterable.first"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IEnumerator First(WindowsRuntimeObjectReference thisReference)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        void* result;

        RestrictedErrorInfo.ThrowExceptionForHR(((IBindableIterableVftbl*)*(void***)thisPtr)->First(thisPtr, &result));

        try
        {
            return ABI.System.Collections.IEnumeratorMarshaller.ConvertToManaged(result)!;
        }
        finally
        {
            WindowsRuntimeUnknownMarshaller.Free(result);
        }
    }
}
