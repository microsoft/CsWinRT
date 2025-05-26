// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Methods for implementations of <c>Windows.Foundation.Collections.IVectorView&lt;T&gt;</c>. types.
/// </summary>
/// <remarks>
/// This type should only be used by generated code.
/// </remarks>
[Obsolete("This type is an implementation detail, and it's only meant to be consumed by 'cswinrtgen'")]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IVectorViewMethods
{
    /// <summary>
    /// Gets a value that indicates whether the iterator refers to a current item or is at the end of the collection.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.iiterator-1.hascurrent"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static uint Size(WindowsRuntimeObjectReference thisReference)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        uint result;

        RestrictedErrorInfo.ThrowExceptionForHR(((IVectorViewVftbl*)*(void***)thisPtr)->get_Size(thisPtr, &result));

        return result;
    }
}
