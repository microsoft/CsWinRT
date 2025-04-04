// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// The <c>IMarshal</c> implementation for managed types.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/objidl/nn-objidl-imarshal"/>
internal static unsafe class IMarshalImpl
{
    /// <summary>
    /// The vtable for the <c>IMarshal</c> implementation.
    /// </summary>
    public static nint AbiToProjectionVftablePtr { get; } = GetAbiToProjectionVftablePtr();

    /// <summary>
    /// Computes the <c>IMarshal</c> implementation vtable.
    /// </summary>
    private static nint GetAbiToProjectionVftablePtr()
    {
        IMarshalVftbl* vftbl = (IMarshalVftbl*)WindowsRuntimeHelpers.AllocateTypeAssociatedUnknownVtable(typeof(IMarshalImpl), 9);

        vftbl->GetUnmarshalClass = &GetUnmarshalClass;
        vftbl->GetMarshalSizeMax = &GetMarshalSizeMax;
        vftbl->MarshalInterface = &MarshalInterface;
        vftbl->UnmarshalInterface = &UnmarshalInterface;
        vftbl->ReleaseMarshalData = &ReleaseMarshalData;
        vftbl->DisconnectObject = &DisconnectObject;

        return (nint)vftbl;
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/objidl/nf-objidl-imarshal-getunmarshalclass"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT GetUnmarshalClass(void* thisPtr, Guid* riid, void* pv, uint dwDestContext, void* pvDestContext, uint mshlFlags, Guid* pCid)
    {
        *pCid = default;

        try
        {
            // EnsureHasFreeThreadedMarshaler();
            // t_freeThreadedMarshaler.GetUnmarshalClass(riid, pv, dwDestContext, pvDestContext, mshlFlags, pCid);
        }
        catch (Exception ex)
        {
            return Marshal.GetHRForException(ex);
        }

        return WellKnownErrorCodes.S_OK;
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/objidl/nf-objidl-imarshal-getmarshalsizemax"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT GetMarshalSizeMax(void* thisPtr, Guid* riid, void* pv, uint dwDestContext, void* pvDestContext, uint mshlflags, uint* pSize)
    {
        *pSize = 0;

        try
        {
            // EnsureHasFreeThreadedMarshaler();
            // t_freeThreadedMarshaler.GetMarshalSizeMax(riid, pv, dwDestContext, pvDestContext, mshlflags, pSize);
        }
        catch (Exception ex)
        {
            return Marshal.GetHRForException(ex);
        }

        return WellKnownErrorCodes.S_OK;
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/objidl/nf-objidl-imarshal-marshalinterface"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT MarshalInterface(void* thisPtr, void* pStm, Guid* riid, void* pv, uint dwDestContext, void* pvDestContext, uint mshlflags)
    {
        try
        {
            // EnsureHasFreeThreadedMarshaler();
            // t_freeThreadedMarshaler.MarshalInterface(pStm, riid, pv, dwDestContext, pvDestContext, mshlflags);
        }
        catch (Exception ex)
        {
            return Marshal.GetHRForException(ex);
        }

        return WellKnownErrorCodes.S_OK;
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/objidl/nf-objidl-imarshal-unmarshalinterface"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT UnmarshalInterface(void* thisPtr, void* pStm, Guid* riid, void** ppv)
    {
        *ppv = null;

        try
        {
            // EnsureHasFreeThreadedMarshaler();
            // t_freeThreadedMarshaler.UnmarshalInterface(pStm, riid, ppv);
        }
        catch (Exception ex)
        {
            return Marshal.GetHRForException(ex);
        }

        return WellKnownErrorCodes.S_OK;
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/objidl/nf-objidl-imarshal-releasemarshaldata"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT ReleaseMarshalData(void* thisPtr, void* pStm)
    {
        try
        {
            // EnsureHasFreeThreadedMarshaler();
            // t_freeThreadedMarshaler.ReleaseMarshalData(pStm);
        }
        catch (Exception ex)
        {
            return Marshal.GetHRForException(ex);
        }

        return WellKnownErrorCodes.S_OK;
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/objidl/nf-objidl-imarshal-disconnectobject"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT DisconnectObject(void* thisPtr, uint dwReserved)
    {
        try
        {
            // EnsureHasFreeThreadedMarshaler();
            // t_freeThreadedMarshaler.DisconnectObject(dwReserved);
        }
        catch (Exception ex)
        {
            return Marshal.GetHRForException(ex);
        }

        return WellKnownErrorCodes.S_OK;
    }
}
