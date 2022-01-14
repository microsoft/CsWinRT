using System;
using System.Threading;
using Microsoft.System;

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

        public override void Post(SendOrPostCallback d, object state)
        {
            if (d == null)
                throw new ArgumentNullException(nameof(d));

            m_dispatcherQueue.TryEnqueue(() => d(state));
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            if (m_dispatcherQueue.HasThreadAccess)
            {
                d(state);
            }
            else
            {
                var m = new ManualResetEvent(false);
                m_dispatcherQueue.TryEnqueue(() =>
                {
                    d(state);
                    m.Set();
                });
                m.WaitOne();
            }
        }

        public override SynchronizationContext CreateCopy()
        {
            return new DispatcherQueueSynchronizationContext(m_dispatcherQueue);
        }
    }
}