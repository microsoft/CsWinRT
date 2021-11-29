using System;
using System.Threading;

namespace Microsoft.System
{
    /// <summary>
    /// DispatcherQueueSyncContext allows developers to await calls and get back onto the
    /// UI thread. Needs to be installed on the UI thread through DispatcherQueueSyncContext.SetForCurrentThread
    /// </summary>
    public class DispatcherQueueSynchronizationContext : SynchronizationContext
    {
        private readonly DispatcherQueue m_dispatcherQueue;

        public DispatcherQueueSynchronizationContext(DispatcherQueue dispatcherQueue)
        {
            m_dispatcherQueue = dispatcherQueue;
        }

        /// <inheritdoc/>
        public override void Post(SendOrPostCallback d, object state)
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

            m_dispatcherQueue.TryEnqueue(() => d(state));
        }

        /// <inheritdoc/>
        public override void Send(SendOrPostCallback d, object state)
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