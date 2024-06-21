using System;
using System.Threading;
using WinRT;

#nullable enable

namespace Microsoft.Windows.System;

/// <summary>
/// The <see cref="DispatcherQueueSynchronizationContext"/> type allows developers to await calls and get back onto
/// the UI thread. Needs to be installed on the UI thread through <see cref="SynchronizationContext.SetSynchronizationContext"/>.
/// </summary>
public sealed partial class DispatcherQueueSynchronizationContext : SynchronizationContext
{
    /// <summary>
    /// The <see cref="IObjectReference"/> instance for the target dispatcher queue.
    /// </summary>
    private readonly IObjectReference _objectReference;

    /// <summary>
    /// Creates a new <see cref="DispatcherQueueSynchronizationContext"/> instance with the specified parameters.
    /// </summary>
    /// <param name="dispatcherQueue">The target <see cref="global::Windows.System.DispatcherQueue"/> instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="dispatcherQueue"/> is <see langword="null"/>.</exception>
    public DispatcherQueueSynchronizationContext(global::Windows.System.DispatcherQueue dispatcherQueue)
    {
        ArgumentNullException.ThrowIfNull(dispatcherQueue);

        _objectReference = ((IWinRTObject)dispatcherQueue).NativeObject;
    }

    /// <summary>
    /// Creates a new <see cref="DispatcherQueueSynchronizationContext"/> instance with the specified parameters.
    /// </summary>
    /// <param name="objectReference">The <see cref="IObjectReference"/> instance for the target dispatcher queue.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="objectReference"/> is <see langword="null"/>.</exception>
    private DispatcherQueueSynchronizationContext(IObjectReference objectReference)
    {
        ArgumentNullException.ThrowIfNull(objectReference);

        _objectReference = objectReference;
    }

    /// <inheritdoc/>
    public override unsafe void Post(SendOrPostCallback d, object? state)
    {
        ArgumentNullException.ThrowIfNull(d);

        global::ABI.Windows.System.DispatcherQueueProxyHandler* dispatcherQueueProxyHandler = global::ABI.Windows.System.DispatcherQueueProxyHandler.Create(d, state);
        int hresult;

        try
        {
            void* thisPtr = (void*)_objectReference.ThisPtr;
            bool success;

            // Note: we're intentionally ignoring the retval for 'DispatcherQueue::TryEnqueue'.
            // This matches the behavior for the equivalent type on WinUI 3 as well.
            hresult = ((delegate* unmanaged<void*, void*, byte*, int>)(*(void***)thisPtr)[7])(thisPtr, dispatcherQueueProxyHandler, (byte*)&success);

            GC.KeepAlive(_objectReference);
        }
        finally
        {
            dispatcherQueueProxyHandler->Release();
        }

        ExceptionHelpers.ThrowExceptionForHR(hresult);
    }

    /// <inheritdoc/>
    public override void Send(SendOrPostCallback d, object? state)
    {
        throw new NotSupportedException("'SynchronizationContext.Send' is not supported.");
    }

    /// <inheritdoc/>
    public override SynchronizationContext CreateCopy()
    {
        return new DispatcherQueueSynchronizationContext(_objectReference);
    }
}