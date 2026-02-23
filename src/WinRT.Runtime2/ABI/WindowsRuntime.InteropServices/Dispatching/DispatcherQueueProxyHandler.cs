// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using WindowsRuntime.InteropServices;

namespace ABI.WindowsRuntime.InteropServices;

/// <summary>
/// The native implementation of <see href="https://learn.microsoft.com/uwp/api/windows.system.dispatcherqueuehandler"><c>IDispatcherQueueHandler</c></see> for <see cref="DispatcherQueueProxyHandler"/>.
/// </summary>
internal static unsafe class DispatcherQueueProxyHandlerImpl
{
    /// <summary>
    /// The <see cref="IDispatcherQueueHandlerVftbl"/> value for the <see cref="DispatcherQueueProxyHandler"/> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly IDispatcherQueueHandlerVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static DispatcherQueueProxyHandlerImpl()
    {
        Vftbl.QueryInterface = &QueryInterface;
        Vftbl.AddRef = &AddRef;
        Vftbl.Release = &Release;
        Vftbl.Invoke = &Invoke;
    }

    /// <summary>
    /// Gets a pointer to the <see cref="DispatcherQueueProxyHandler"/> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Vftbl);
    }

    /// <inheritdoc href="https://learn.microsoft.com/windows/win32/api/unknwn/nf-unknwn-iunknown-queryinterface(refiid_void)"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    public static HRESULT QueryInterface(void* @this, Guid* riid, void** ppvObject)
    {
        return ((DispatcherQueueProxyHandler*)@this)->QueryInterface(riid, ppvObject);
    }

    /// <inheritdoc href="https://learn.microsoft.com/windows/win32/api/unknwn/nf-unknwn-iunknown-addref"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    public static uint AddRef(void* @this)
    {
        return ((DispatcherQueueProxyHandler*)@this)->AddRef();
    }

    /// <inheritdoc href="https://learn.microsoft.com/windows/win32/api/unknwn/nf-unknwn-iunknown-release"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    public static uint Release(void* @this)
    {
        return ((DispatcherQueueProxyHandler*)@this)->Release();
    }

    /// <inheritdoc href="https://learn.microsoft.com/uwp/api/windows.system.dispatcherqueuehandler"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    public static HRESULT Invoke(void* @this)
    {
        try
        {
            ((DispatcherQueueProxyHandler*)@this)->Invoke();
        }
        catch (Exception e)
        {
            // This method triggers the special exception serialization logic in Native AOT that
            // allows stacktraces from async crashes such as this to be correctly captured in
            // crash dumps. Without this call, they would only show up as "stowed exception"-s,
            // and all crashes like this would end up being classified as being in the same bucket.
            ExceptionHandling.RaiseAppDomainUnhandledExceptionEvent(e);

            // Register the exception with the global error handler. The failfast behavior
            // is governed by the state of the 'UnhandledExceptionEventArgs.Handled' property.
            // If 'Handled' is true the app continues running, else it failfasts.
            RestrictedErrorInfo.ReportUnhandledError(e);
        }

        return WellKnownErrorCodes.S_OK;
    }
}