using System;
using System.Threading;
using Microsoft.UI.Dispatching;

namespace Microsoft.UI.Dispatching
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

            m_dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    d(state);
                }
                catch (Exception ex)
                {
                    ExceptionHelpers.ReportUnhandledError(ex);
                    throw;
                }
            });
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            throw new NotSupportedException("Send not supported");
        }

        public override SynchronizationContext CreateCopy()
        {
            return new DispatcherQueueSynchronizationContext(m_dispatcherQueue);
        }
    }
}