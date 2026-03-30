// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;
using System.ComponentModel;
using System.Threading;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// The <see cref="DispatcherQueueSynchronizationContext"/> type allows developers to await calls and get back onto
/// the UI thread. It needs to be installed on the UI thread through <see cref="SynchronizationContext.SetSynchronizationContext"/>
/// invoked on a wrapping <see cref="SynchronizationContext"/> managed object (which is generated in a projection .dll).
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public readonly struct DispatcherQueueSynchronizationContext
{
    /// <summary>
    /// The <see cref="WindowsRuntimeObjectReference"/> instance for the target dispatcher queue.
    /// </summary>
    private readonly WindowsRuntimeObjectReference _objectReference;

    /// <summary>
    /// Creates a new <see cref="DispatcherQueueSynchronizationContext"/> instance with the specified parameters.
    /// </summary>
    /// <param name="dispatcherQueue">The target dispatcher queue instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="dispatcherQueue"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The <paramref name="dispatcherQueue"/> parameter must be either a <see href="https://learn.microsoft.com/uwp/api/windows.system.dispatcherqueue"><c>Windows.System.DispatcherQueue</c></see>
    /// instance, or a <see href="https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.dispatching.dispatcherqueue"><c>Microsoft.UI.Dispatching.DispatcherQueue</c></see> instance.
    /// </remarks>
    public DispatcherQueueSynchronizationContext(WindowsRuntimeObject dispatcherQueue)
    {
        ArgumentNullException.ThrowIfNull(dispatcherQueue);

        // The two 'DispatcherQueue' types are sealed, so we can rely on them always being unwrappable. They can never be unsealed, as
        // doing so would be a binary breaking change, so this is safe even for the WinUI 3 type, which is undocked from the Windows SDK.
        _objectReference = dispatcherQueue.NativeObjectReference;
    }

    /// <summary>
    /// Creates a new <see cref="DispatcherQueueSynchronizationContext"/> instance with the specified parameters.
    /// </summary>
    /// <param name="objectReference">The <see cref="WindowsRuntimeObjectReference"/> instance for the target dispatcher queue.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="objectReference"/> is <see langword="null"/>.</exception>
    private DispatcherQueueSynchronizationContext(WindowsRuntimeObjectReference objectReference)
    {
        ArgumentNullException.ThrowIfNull(objectReference);

        _objectReference = objectReference;
    }

    /// <inheritdoc cref="SynchronizationContext.Post"/>
    public unsafe void Post(SendOrPostCallback d, object? state)
    {
        ArgumentNullException.ThrowIfNull(d);

        DispatcherQueueProxyHandler* proxyHandler = DispatcherQueueProxyHandler.Create(d, state);

        try
        {
            using WindowsRuntimeObjectReferenceValue objectValue = _objectReference.AsValue();

            void* thisPtr = objectValue.GetThisPtrUnsafe();
            bool success;

            // Try to enqueue the native handler, which wraps the input callback and state
            HRESULT hresult = IDispatcherQueueVftbl.TryEnqueueUnsafe(thisPtr, proxyHandler, &success);

            RestrictedErrorInfo.ThrowExceptionForHR(hresult);

            // This method will never return 'false', unless 'DispatcherQueueController.ShutdownQueueAsync()'
            // has been called on the controller for the dispatcher queue in use. In that case, trying to
            // enqueue a new callback on this dispatcher queue instance is not actually valid. For more info,
            // see https://docs.microsoft.com/en-us/uwp/api/windows.system.dispatcherqueue.tryenqueue.
            if (!success)
            {
                RestrictedErrorInfo.ThrowExceptionForHR(WellKnownErrorCodes.E_NOT_VALID_STATE);
            }
        }
        finally
        {
            // This call does not have a corresponding 'AddRef' invocation that is visible, and
            // that is because the static constructors for all existing custom handlers already
            // set the internal reference count to 1. Releasing the ref count here ensures the
            // object is always freed regardless of whether the dispatching was successful. If it
            // was, the ref count would be 2 at this point (the dispatcher queue increments it),
            // so this 'Release' lowers it to 1 and allows the dispatcher queue to release it once
            // the callback has been invoked. If the dispatching failed instead, this call will
            // free the allocated handler immediately (as the ref count would still be 1 here).
            _ = proxyHandler->Release();
        }
    }

    /// <inheritdoc cref="SynchronizationContext.Send"/>
    public void Send(SendOrPostCallback d, object? state)
    {
        throw new NotSupportedException("'SynchronizationContext.Send' is not supported.");
    }
}
#endif