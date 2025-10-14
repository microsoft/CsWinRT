// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// The <c>IMarshal</c> implementation for managed types.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/objidl/nn-objidl-imarshal"/>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage, DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId)]
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
    /// Gets the IID for the <c>IMarshal</c> interface.
    /// </summary>
    public static ref readonly Guid IID
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref WellKnownInterfaceIds.IID_IMarshal;
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
