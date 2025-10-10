// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Methods for <c>Windows.Foundation.Collections.IIterator&lt;T&gt;</c> types.
/// </summary>
/// <remarks>
/// This type should only be used by generated code.
/// </remarks>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IIteratorMethods
{
    /// <summary>
    /// Gets a value that indicates whether the iterator refers to a current item or is at the end of the collection.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.iiterator-1.hascurrent"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool HasCurrent(WindowsRuntimeObjectReference thisReference)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        bool result;

        RestrictedErrorInfo.ThrowExceptionForHR(((IIteratorVftbl*)*(void***)thisPtr)->get_HasCurrent(thisPtr, &result));

        return result;
    }

    /// <summary>
    /// Advances the iterator to the next item in the collection.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <returns>
    /// True if the iterator refers to a valid item in the collection, false if the iterator passes the end of the collection.
    /// </returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.iiterator-1.movenext"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool MoveNext(WindowsRuntimeObjectReference thisReference)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        bool result;

        RestrictedErrorInfo.ThrowExceptionForHR(((IIteratorVftbl*)*(void***)thisPtr)->MoveNext(thisPtr, &result));

        return result;
    }
}
