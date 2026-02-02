// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// The <c>IMarshal</c> implementation for managed types.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/objidl/nn-objidl-imarshal"/>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IMarshalImpl
{
    /// <summary>
    /// The <see cref="IMarshalVftbl"/> value for the managed <c>IMarshal</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly IMarshalVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static IMarshalImpl()
    {
        *(IUnknownVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IUnknownVftbl*)IUnknownImpl.Vtable;

        Vftbl.GetUnmarshalClass = &GetUnmarshalClass;
        Vftbl.GetMarshalSizeMax = &GetMarshalSizeMax;
        Vftbl.MarshalInterface = &MarshalInterface;
        Vftbl.UnmarshalInterface = &UnmarshalInterface;
        Vftbl.ReleaseMarshalData = &ReleaseMarshalData;
        Vftbl.DisconnectObject = &DisconnectObject;
    }

    /// <summary>
    /// Gets a pointer to the managed <c>IMarshal</c> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Vftbl);
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/objidl/nf-objidl-imarshal-getunmarshalclass"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT GetUnmarshalClass(void* thisPtr, Guid* riid, void* pv, uint dwDestContext, void* pvDestContext, uint mshlFlags, Guid* pCid)
    {
        *pCid = default;

        try
        {
            FreeThreadedMarshaler.InstanceForCurrentThread.GetUnmarshalClass(riid, pv, dwDestContext, pvDestContext, mshlFlags, pCid);
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
        *pSize = 0;

        try
        {
            FreeThreadedMarshaler.InstanceForCurrentThread.GetMarshalSizeMax(riid, pv, dwDestContext, pvDestContext, mshlflags, pSize);
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
            FreeThreadedMarshaler.InstanceForCurrentThread.MarshalInterface(pStm, riid, pv, dwDestContext, pvDestContext, mshlflags);
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
        *ppv = null;

        try
        {
            FreeThreadedMarshaler.InstanceForCurrentThread.UnmarshalInterface(pStm, riid, ppv);
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
            FreeThreadedMarshaler.InstanceForCurrentThread.ReleaseMarshalData(pStm);
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
            FreeThreadedMarshaler.InstanceForCurrentThread.DisconnectObject(dwReserved);
            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception ex)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(ex);
        }
    }
}