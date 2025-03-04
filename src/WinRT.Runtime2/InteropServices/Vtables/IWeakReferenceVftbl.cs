// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;

#pragma warning disable CS0649

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Binding type for the <c>IWeakReference</c> interface vtable.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/weakreference/nn-weakreference-iweakreference"/>
internal unsafe struct IWeakReferenceVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> Resolve;

    /// <summary>
    /// Resolves a weak reference by returning a strong reference to the object.
    /// </summary>
    /// <param name="thisPtr">The target COM object.</param>
    /// <param name="riid">A reference to the interface identifier (IID) of the object.</param>
    /// <param name="objectReference">A strong reference to the object.</param>
    /// <returns>If this method succeeds, then it returns <c>S_OK</c>. Otherwise, it returns an <c>HRESULT</c> error code.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HRESULT ResolveUnsafe(void* thisPtr, Guid* riid, void** objectReference)
    {
        return ((IWeakReferenceVftbl*)thisPtr)->Resolve(thisPtr, riid, objectReference);
    }
}
