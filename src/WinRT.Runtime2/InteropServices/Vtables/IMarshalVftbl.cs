// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;

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

    /// <summary>
    /// Retrieves the maximum size of the buffer that will be needed during marshaling.
    /// </summary>
    /// <param name="thisPtr">The target COM object.</param>
    /// <param name="riid">A reference to the identifier of the interface to be marshaled.</param>
    /// <param name="pv">A pointer to the interface to be marshaled (can be <see langword="null"/>).</param>
    /// <param name="dwDestContext">The destination context where the specified interface is to be unmarshaled.</param>
    /// <param name="pvDestContext">This parameter is reserved and must be <see langword="null"/>.</param>
    /// <param name="mshlflags">Indicates whether the data to be marshaled is to be transmitted back to the client process or written to a global table.</param>
    /// <param name="pSize">A pointer to a variable that receives the maximum size of the buffer.</param>
    /// <returns>The <c>HRESULT</c> for the operation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HRESULT GetMarshalSizeMaxUnsafe(
        void* thisPtr,
        Guid* riid,
        void* pv,
        uint dwDestContext,
        void* pvDestContext,
        uint mshlflags,
        uint* pSize)
    {
        return ((IMarshalVftbl*)thisPtr)->GetMarshalSizeMax(thisPtr, riid, pv, dwDestContext, pvDestContext, mshlflags, pSize);
    }

    /// <summary>
    /// Marshals an interface pointer.
    /// </summary>
    /// <param name="thisPtr">The target COM object.</param>
    /// <param name="pStm">A pointer to the stream to be used during marshaling.</param>
    /// <param name="riid">A reference to the identifier of the interface to be marshaled.</param>
    /// <param name="pv">A pointer to the interface to be marshaled (can be <see langword="null"/>).</param>
    /// <param name="dwDestContext">The destination context where the specified interface is to be unmarshaled.</param>
    /// <param name="pvDestContext">This parameter is reserved and must be <see langword="null"/>.</param>
    /// <param name="mshlflags">Indicates whether the data to be marshaled is to be transmitted back to the client process or written to a global table.</param>
    /// <returns>The <c>HRESULT</c> for the operation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HRESULT MarshalInterfaceUnsafe(
        void* thisPtr,
        void* pStm,
        Guid* riid,
        void* pv,
        uint dwDestContext,
        void* pvDestContext,
        uint mshlflags)
    {
        return ((IMarshalVftbl*)thisPtr)->MarshalInterface(thisPtr, pStm, riid, pv, dwDestContext, pvDestContext, mshlflags);
    }

    /// <summary>
    /// Unmarshals an interface pointer.
    /// </summary>
    /// <param name="thisPtr">The target COM object.</param>
    /// <param name="pStm">A pointer to the stream from which the interface pointer is to be unmarshaled.</param>
    /// <param name="riid">A reference to the identifier of the interface to be unmarshaled.</param>
    /// <param name="ppv">The address of pointer variable that receives the interface pointer.</param>
    /// <returns>The <c>HRESULT</c> for the operation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HRESULT UnmarshalInterfaceUnsafe(
        void* thisPtr,
        void* pStm,
        Guid* riid,
        void** ppv)
    {
        return ((IMarshalVftbl*)thisPtr)->UnmarshalInterface(thisPtr, pStm, riid, ppv);
    }

    /// <summary>
    /// Destroys a marshaled data packet.
    /// </summary>
    /// <param name="thisPtr">The target COM object.</param>
    /// <param name="pStm">A pointer to a stream that contains the data packet to be destroyed.</param>
    /// <returns>The <c>HRESULT</c> for the operation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HRESULT ReleaseMarshalDataUnsafe(void* thisPtr, void* pStm)
    {
        return ((IMarshalVftbl*)thisPtr)->ReleaseMarshalData(thisPtr, pStm);
    }

    /// <summary>
    /// Releases all connections to an object.
    /// </summary>
    /// <param name="thisPtr">The target COM object.</param>
    /// <param name="dwReserved">This parameter is reserved and must be <c>0</c>.</param>
    /// <returns>The <c>HRESULT</c> for the operation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HRESULT DisconnectObjectUnsafe(void* thisPtr, uint dwReserved)
    {
        return ((IMarshalVftbl*)thisPtr)->DisconnectObject(thisPtr, dwReserved);
    }
}
