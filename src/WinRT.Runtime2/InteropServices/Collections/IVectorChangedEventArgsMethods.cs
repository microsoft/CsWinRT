// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.Foundation.Collections;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Methods for <c>Windows.Foundation.Collections.IVectorChangedEventArgs</c> types.
/// </summary>
/// <remarks>
/// This type should only be used by generated code.
/// </remarks>
[Obsolete("This type is an implementation detail, and it's only meant to be consumed by 'cswinrtgen'")]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IVectorChangedEventArgsMethods
{
    /// <summary>
    /// Gets the type of change that occurred in the vector.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <returns>The type of change in the vector.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivectorchangedeventargs.collectionchange"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static CollectionChange CollectionChange(WindowsRuntimeObjectReference thisReference)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        CollectionChange result;

        RestrictedErrorInfo.ThrowExceptionForHR(((IVectorChangedEventArgsVftbl*)*(void***)thisPtr)->get_CollectionChange(thisPtr, &result));

        return result;
    }

    /// <summary>
    /// Gets the index where the change occurred in the vector.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <returns>The index where the change occurred in the vector.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivectorchangedeventargs.index"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static uint Index(WindowsRuntimeObjectReference thisReference)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        uint result;

        RestrictedErrorInfo.ThrowExceptionForHR(((IVectorChangedEventArgsVftbl*)*(void***)thisPtr)->get_Index(thisPtr, &result));

        return result;
    }
}
