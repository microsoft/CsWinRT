using System;
using System.Threading;
using Microsoft.System;

namespace Microsoft.UI.Threading
{
    /// <summary>
    /// DispatcherQueueSyncContext allows developers to await calls and get back onto the
    /// UI thread. Needs to be installed on the UI thread through DispatcherQueueSyncContext.SetForCurrentThread
    /// </summary>
    public class DispatcherQueueSyncContext : SynchronizationContext
    {
        private readonly DispatcherQueue m_dispatcherQueue;

        /// <summary>
        /// Installs a DispatcherQueueSyncContext on the current thread
        /// </summary> 
        public static void SetForCurrentThread()
        {
            var context = new DispatcherQueueSyncContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
        }

        internal DispatcherQueueSyncContext(DispatcherQueue dispatcherQueue)
        {
            m_dispatcherQueue = dispatcherQueue;
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            if (d == null)
                throw new ArgumentNullException(nameof(d));

            m_dispatcherQueue.TryEnqueue(() => d(state));
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            throw new NotSupportedException("Send not supported");
        }

        public override SynchronizationContext CreateCopy()
        {
            return new DispatcherQueueSyncContext(m_dispatcherQueue);
        }
    }
}