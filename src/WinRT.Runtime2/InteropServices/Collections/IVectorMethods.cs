// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Methods for <c>Windows.Foundation.Collections.IVector&lt;T&gt;</c> types.
/// </summary>
/// <remarks>
/// This type should only be used by generated code.
/// </remarks>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage, DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IVectorMethods
{
    /// <summary>
    /// Gets the number of items in the vector.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <returns>The number of items in the vector.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivector-1.size"/>
    public static uint Size(WindowsRuntimeObjectReference thisReference)
    {
        // The vtable slot for 'get_Size' is identical between 'IVector<T>' and 'IVectorView<T>'
        return IVectorViewMethods.Size(thisReference);
    }

    /// <summary>
    /// Removes the item at the specified index in the vector.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <param name="index">The zero-based index of the vector item to remove.</param>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivector-1.removeat"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void RemoveAt(WindowsRuntimeObjectReference thisReference, uint index)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();

        RestrictedErrorInfo.ThrowExceptionForHR(((IVectorVftbl*)*(void***)thisPtr)->RemoveAt(thisPtr, index));
    }

    /// <summary>
    /// Removes all items from the vector.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivector-1.clear"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Clear(WindowsRuntimeObjectReference thisReference)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();

        RestrictedErrorInfo.ThrowExceptionForHR(((IVectorVftbl*)*(void***)thisPtr)->Clear(thisPtr));
    }
}
