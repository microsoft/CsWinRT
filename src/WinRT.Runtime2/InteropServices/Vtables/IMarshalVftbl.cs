// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Security.Claims;

#pragma warning disable CS0649

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Binding type for the <c>IMarshal</c> interface vtable.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/objidl/nn-objidl-imarshal"/>
internal unsafe struct IMarshalVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void*, uint, void*, uint, Guid*, HRESULT> GetUnmarshalClass;
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void*, uint, void*, uint, uint*, HRESULT> GetMarshalSizeMax;
    public delegate* unmanaged[MemberFunction]<void*, void*, Guid*, void*, uint, void*, uint, HRESULT> MarshalInterface;
    public delegate* unmanaged[MemberFunction]<void*, void*, Guid*, void**, HRESULT> UnmarshalInterface;
    public delegate* unmanaged[MemberFunction]<void*, void*, HRESULT> ReleaseMarshalData;
    public delegate* unmanaged[MemberFunction]<void*, uint, HRESULT> DisconnectObject;

    /// <summary>
    /// Retrieves the CLSID of the unmarshaling code.
    /// </summary>
    /// <param name="thisPtr">The target COM object.</param>
    /// <param name="riid">A reference to the identifier of the interface to be marshaled.</param>
    /// <param name="pv">A pointer to the interface to be marshaled (can be <see langword="null"/>).</param>
    /// <param name="dwDestContext">The destination context where the specified interface is to be unmarshaled.</param>
    /// <param name="pvDestContext">This parameter is reserved and must be <see langword="null"/>.</param>
    /// <param name="mshlflags">Indicates whether the data to be marshaled is to be transmitted back to the client process or written to a global table.</param>
    /// <param name="pCid">A pointer that receives the CLSID to be used to create a proxy in the client process.</param>
    /// <returns>The <c>HRESULT</c> for the operation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HRESULT GetUnmarshalClassUnsafe(
        void* thisPtr,
        Guid* riid,
        void* pv,
        uint dwDestContext,
        void* pvDestContext,
        uint mshlflags,
        Guid* pCid)
    {
        return ((IMarshalVftbl*)thisPtr)->GetUnmarshalClass(thisPtr, riid, pv, dwDestContext, pvDestContext, mshlflags, pCid);
    }
}
