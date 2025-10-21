// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ABI.System;
using Windows.Foundation;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;
using static System.Runtime.InteropServices.ComWrappers;

#pragma warning disable IDE0008, IDE1006

namespace ABI.Windows.Foundation;

/// <summary>
/// Marshaller for <see cref="IAsyncInfo"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IAsyncInfoMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeObjectMarshaller.ConvertToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(IAsyncInfo? value)
    {
        return WindowsRuntimeInterfaceMarshaller<IAsyncInfo>.ConvertToUnmanaged(value, in WellKnownInterfaceIIDs.IID_IAsyncInfo);
    }

    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.ConvertToManaged"/>
    public static IAsyncInfo? ConvertToManaged(void* value)
    {
        return (IAsyncInfo?)WindowsRuntimeObjectMarshaller.ConvertToManaged(value);
    }
}

/// <summary>
/// Interop methods for <see cref="IAsyncInfo"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IAsyncInfoMethods
{
    /// <see cref="IAsyncInfo.Id"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static uint Id(WindowsRuntimeObjectReference thisReference)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        uint result;

        HRESULT hresult = ((IAsyncInfoVftbl*)*(void***)thisPtr)->get_Id(thisPtr, &result);

        RestrictedErrorInfo.ThrowExceptionForHR(hresult);

        return result;
    }

    /// <see cref="IAsyncInfo.Status"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static AsyncStatus Status(WindowsRuntimeObjectReference thisReference)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        AsyncStatus result;

        HRESULT hresult = ((IAsyncInfoVftbl*)*(void***)thisPtr)->get_Status(thisPtr, &result);

        RestrictedErrorInfo.ThrowExceptionForHR(hresult);

        return result;
    }

    /// <see cref="IAsyncInfo.ErrorCode"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static global::System.Exception? ErrorCode(WindowsRuntimeObjectReference thisReference)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        System.Exception result;

        HRESULT hresult = ((IAsyncInfoVftbl*)*(void***)thisPtr)->get_ErrorCode(thisPtr, &result);

        RestrictedErrorInfo.ThrowExceptionForHR(hresult);

        return ExceptionMarshaller.ConvertToManaged(result);
    }

    /// <see cref="IAsyncInfo.Cancel"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Cancel(WindowsRuntimeObjectReference thisReference)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();

        HRESULT hresult = ((IAsyncInfoVftbl*)*(void***)thisPtr)->Cancel(thisPtr);

        RestrictedErrorInfo.ThrowExceptionForHR(hresult);
    }

    /// <see cref="IAsyncInfo.Close"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Close(WindowsRuntimeObjectReference thisReference)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();

        HRESULT hresult = ((IAsyncInfoVftbl*)*(void***)thisPtr)->Close(thisPtr);

        RestrictedErrorInfo.ThrowExceptionForHR(hresult);
    }
}

/// <summary>
/// Binding type for <see cref="IAsyncInfo"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct IAsyncInfoVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, System.Exception*, HRESULT> get_ErrorCode;
    public delegate* unmanaged[MemberFunction]<void*, uint*, HRESULT> get_Id;
    public delegate* unmanaged[MemberFunction]<void*, AsyncStatus*, HRESULT> get_Status;
    public delegate* unmanaged[MemberFunction]<void*, HRESULT> Cancel;
    public delegate* unmanaged[MemberFunction]<void*, HRESULT> Close;
}

/// <summary>
/// The <see cref="IAsyncInfo"/> implementation.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IAsyncInfoImpl
{
    /// <summary>
    /// The <see cref="IAsyncInfoVftbl"/> value for the managed <see cref="IAsyncInfo"/> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly IAsyncInfoVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static IAsyncInfoImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.get_ErrorCode = &get_ErrorCode;
        Vftbl.get_Id = &get_Id;
        Vftbl.get_Status = &get_Status;
        Vftbl.Cancel = &Cancel;
        Vftbl.Close = &Close;
    }

    /// <summary>
    /// Gets a pointer to the managed <see cref="IAsyncInfo"/> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Vftbl);
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/asyncinfo/nf-asyncinfo-iasyncinfo-get_errorcode"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT get_ErrorCode(void* thisPtr, System.Exception* errorCode)
    {
        if (errorCode is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            var unboxedValue = ComInterfaceDispatch.GetInstance<IAsyncInfo>((ComInterfaceDispatch*)thisPtr);

            *errorCode = ExceptionMarshaller.ConvertToUnmanaged(unboxedValue.ErrorCode);

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/asyncinfo/nf-asyncinfo-iasyncinfo-get_id"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT get_Id(void* thisPtr, uint* id)
    {
        if (id is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            var unboxedValue = ComInterfaceDispatch.GetInstance<IAsyncInfo>((ComInterfaceDispatch*)thisPtr);

            *id = unboxedValue.Id;

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/asyncinfo/nf-asyncinfo-iasyncinfo-get_status"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT get_Status(void* thisPtr, AsyncStatus* status)
    {
        if (status is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            var unboxedValue = ComInterfaceDispatch.GetInstance<IAsyncInfo>((ComInterfaceDispatch*)thisPtr);

            *status = unboxedValue.Status;

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/asyncinfo/nf-asyncinfo-iasyncinfo-cancel"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT Cancel(void* thisPtr)
    {
        try
        {
            var unboxedValue = ComInterfaceDispatch.GetInstance<IAsyncInfo>((ComInterfaceDispatch*)thisPtr);

            unboxedValue.Cancel();

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/asyncinfo/nf-asyncinfo-iasyncinfo-close"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT Close(void* thisPtr)
    {
        try
        {
            var unboxedValue = ComInterfaceDispatch.GetInstance<IAsyncInfo>((ComInterfaceDispatch*)thisPtr);

            unboxedValue.Close();

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}

/// <summary>
/// The <see cref="IDynamicInterfaceCastable"/> implementation for <see cref="IAsyncInfo"/>.
/// </summary>
[DynamicInterfaceCastableImplementation]
file interface IAsyncInfoInterfaceImpl : IAsyncInfo
{
    /// <inheritdoc/>
    uint IAsyncInfo.Id
    {
        get
        {
            var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(IAsyncInfo).TypeHandle);

            return IAsyncInfoMethods.Id(thisReference);
        }
    }

    /// <inheritdoc/>
    AsyncStatus IAsyncInfo.Status
    {
        get
        {
            var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(IAsyncInfo).TypeHandle);

            return IAsyncInfoMethods.Status(thisReference);
        }
    }

    /// <inheritdoc/>
    global::System.Exception? IAsyncInfo.ErrorCode
    {
        get
        {
            var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(IAsyncInfo).TypeHandle);

            return IAsyncInfoMethods.ErrorCode(thisReference);
        }
    }

    /// <inheritdoc/>
    void IAsyncInfo.Cancel()
    {
        var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(IAsyncInfo).TypeHandle);

        IAsyncInfoMethods.Cancel(thisReference);
    }

    /// <inheritdoc/>
    void IAsyncInfo.Close()
    {
        var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(IAsyncInfo).TypeHandle);

        IAsyncInfoMethods.Close(thisReference);
    }
}
