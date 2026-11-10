// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;

namespace WindowsRuntime.InteropServices;

/// <inheritdoc cref="IMarshalImpl"/>
public unsafe partial class IMarshalImpl
{
    /// <summary>
    /// Gets a pointer to the managed <c>IMarshal</c> implementation using <see cref="RoBufferMarshaler"/>.
    /// </summary>
    internal static nint RoBufferVtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in RoBufferMarshalImpl.Vftbl);
    }
}

/// <summary>
/// The <c>IMarshal</c> implementation using <see cref="RoBufferMarshaler"/>.
/// </summary>
file static unsafe class RoBufferMarshalImpl
{
    /// <summary>
    /// The <see cref="IMarshalVftbl"/> value for the managed <c>IMarshal</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    public static readonly IMarshalVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static RoBufferMarshalImpl()
    {
        *(IUnknownVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IUnknownVftbl*)IUnknownImpl.Vtable;

        Vftbl.GetUnmarshalClass = &GetUnmarshalClass;
        Vftbl.GetMarshalSizeMax = &GetMarshalSizeMax;
        Vftbl.MarshalInterface = &MarshalInterface;
        Vftbl.UnmarshalInterface = &UnmarshalInterface;
        Vftbl.ReleaseMarshalData = &ReleaseMarshalData;
        Vftbl.DisconnectObject = &DisconnectObject;
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/objidl/nf-objidl-imarshal-getunmarshalclass"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT GetUnmarshalClass(void* thisPtr, Guid* riid, void* pv, uint dwDestContext, void* pvDestContext, uint mshlFlags, Guid* pCid)
    {
        if (pCid is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        *pCid = default;

        try
        {
            RoBufferMarshaler.InstanceForCurrentThread.GetUnmarshalClass(riid, pv, dwDestContext, pvDestContext, mshlFlags, pCid);

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception ex)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(ex);
        }
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/objidl/nf-objidl-imarshal-getmarshalsizemax"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT GetMarshalSizeMax(void* thisPtr, Guid* riid, void* pv, uint dwDestContext, void* pvDestContext, uint mshlflags, uint* pSize)
    {
        if (pSize is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        *pSize = 0;

        try
        {
            RoBufferMarshaler.InstanceForCurrentThread.GetMarshalSizeMax(riid, pv, dwDestContext, pvDestContext, mshlflags, pSize);

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception ex)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(ex);
        }
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/objidl/nf-objidl-imarshal-marshalinterface"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT MarshalInterface(void* thisPtr, void* pStm, Guid* riid, void* pv, uint dwDestContext, void* pvDestContext, uint mshlflags)
    {
        try
        {
            RoBufferMarshaler.InstanceForCurrentThread.MarshalInterface(pStm, riid, pv, dwDestContext, pvDestContext, mshlflags);

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception ex)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(ex);
        }
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/objidl/nf-objidl-imarshal-unmarshalinterface"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT UnmarshalInterface(void* thisPtr, void* pStm, Guid* riid, void** ppv)
    {
        if (ppv is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        *ppv = null;

        try
        {
            RoBufferMarshaler.InstanceForCurrentThread.UnmarshalInterface(pStm, riid, ppv);

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception ex)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(ex);
        }
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/objidl/nf-objidl-imarshal-releasemarshaldata"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT ReleaseMarshalData(void* thisPtr, void* pStm)
    {
        try
        {
            RoBufferMarshaler.InstanceForCurrentThread.ReleaseMarshalData(pStm);

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception ex)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(ex);
        }
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/objidl/nf-objidl-imarshal-disconnectobject"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT DisconnectObject(void* thisPtr, uint dwReserved)
    {
        try
        {
            RoBufferMarshaler.InstanceForCurrentThread.DisconnectObject(dwReserved);

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception ex)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(ex);
        }
    }
}