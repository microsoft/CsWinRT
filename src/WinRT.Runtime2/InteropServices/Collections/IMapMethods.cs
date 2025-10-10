// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

#pragma warning disable CS1573

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Methods for <c>Windows.Foundation.Collections.IMap&lt;K, V&gt;</c> types.
/// </summary>
/// <remarks>
/// This type should only be used by generated code.
/// </remarks>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IMapMethods
{
    /// <inheritdoc cref="System.Collections.Generic.ICollection{T}.Count"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.imap-2.count"/>
    public static int Count(WindowsRuntimeObjectReference thisReference)
    {
        // The vtable slot for 'get_Size' and the desired semantics are identical between 'IMap<T>' and 'IVectorView<T>'
        return IReadOnlyListMethods.Count(thisReference);
    }

    /// <inheritdoc cref="System.Collections.Generic.ICollection{T}.Clear"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.imap-2.clear"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Clear(WindowsRuntimeObjectReference thisReference)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();

        RestrictedErrorInfo.ThrowExceptionForHR(((IMapVftbl*)*(void***)thisPtr)->Clear(thisPtr));
    }
}
