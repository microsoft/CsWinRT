using System;
using System.Threading;
using WinRT;

#nullable enable

// CA1416 is "validate platform compatibility". This suppresses the warnings for IWinRTObject.NativeObject and
// IObjectReference.ThisPtr only being supported on Windows (since WASDK as a whole only works on Windows anyway).
#pragma warning disable CA1416

namespace Microsoft.System
{
    /// <summary>
    /// DispatcherQueueSyncContext allows developers to await calls and get back onto the
    /// UI thread. Needs to be installed on the UI thread through DispatcherQueueSyncContext.SetForCurrentThread
    /// </summary>
    public partial class DispatcherQueueSynchronizationContext : SynchronizationContext
    {
        private readonly DispatcherQueue m_dispatcherQueue;

        public DispatcherQueueSynchronizationContext(DispatcherQueue dispatcherQueue)
        {
            m_dispatcherQueue = dispatcherQueue;
        }

        /// <inheritdoc/>
        public override unsafe void Post(SendOrPostCallback d, object? state)
        {
            if (d is null)
            {
                static void ThrowArgumentNullException()
                {
                    throw new ArgumentNullException(nameof(d));
                }

                ThrowArgumentNullException();
            }

#if NET5_0_OR_GREATER
            DispatcherQueueProxyHandler* dispatcherQueueProxyHandler = DispatcherQueueProxyHandler.Create(d!, state);
            int hResult;

            try
            {
                IDispatcherQueue* dispatcherQueue = (IDispatcherQueue*)((IWinRTObject)m_dispatcherQueue).NativeObject.ThisPtr;
                bool success;

                hResult = dispatcherQueue->TryEnqueue(dispatcherQueueProxyHandler, &success);

                GC.KeepAlive(this);
            }
            finally
            {
                dispatcherQueueProxyHandler->Release();
            }

            if (hResult != 0)
            {
                ExceptionHelpers.ThrowExceptionForHR(hResult);
            }
#else
            _ = m_dispatcherQueue.TryEnqueue(() => d!(state));
#endif
        }

        /// <inheritdoc/>
        public override void Send(SendOrPostCallback d, object? state)
        {
            throw new NotSupportedException("Send not supported");
        }

        /// <inheritdoc/>
        public override SynchronizationContext CreateCopy()
        {
            return new DispatcherQueueSynchronizationContext(m_dispatcherQueue);
        }
    }
}