// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Binding type for the <c>IWeakReferenceSource</c> interface vtable.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/weakreference/nn-weakreference-iweakreferencesource"/>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct IWeakReferenceSourceVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, void**, HRESULT> GetWeakReference;

    /// <summary>
    /// Retrieves a weak reference from an <c>IWeakReferenceSource</c>.
    /// </summary>
    /// <param name="thisPtr">The target COM object.</param>
    /// <param name="weakReference">The weak reference.</param>
    /// <returns>If this method succeeds, then it returns <c>S_OK</c>. Otherwise, it returns an <c>HRESULT</c> error code.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HRESULT GetWeakReferenceUnsafe(void* thisPtr, void** weakReference)
    {
        return ((IWeakReferenceSourceVftbl*)thisPtr)->GetWeakReference(thisPtr, weakReference);
    }
}
