using System;
using System.Threading;
using WinRT;

#nullable enable
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
#if NET6_0
            ArgumentNullException.ThrowIfNull(d, nameof(d));
#else
            static void ThrowArgumentNullException()
            {
                throw new ArgumentNullException(nameof(d));
            }

            if (d is null)
            {
                ThrowArgumentNullException();
            }
#endif

#if NET5_0_OR_GREATER
            DispatcherQueueProxyHandler* dispatcherQueueProxyHandler = DispatcherQueueProxyHandler.Create(d!, state);
            bool success;
            int hResult;

            try
            {
                IDispatcherQueue* dispatcherQueue = (IDispatcherQueue*)((IWinRTObject)m_dispatcherQueue).NativeObject.ThisPtr;

                hResult = dispatcherQueue->TryEnqueue(dispatcherQueueProxyHandler, (byte*)&success);

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
            m_dispatcherQueue.TryEnqueue(() => d!(state));
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