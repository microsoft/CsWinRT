// TODO: implement these changes in IXP projection 

using System;
using System.Threading;
using Microsoft.System;
using System.Runtime.ExceptionServices;

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

            // We explicitly choose to ignore the return value here. This enqueue operation might fail if the
            // dispatcher queue was shut down before we got here. In that case, we choose to just move on and
            // pretend nothing happened.
            _ = m_dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, new Invoker(d, state).Invoke);
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            throw new NotSupportedException("Send not supported");
        }

        public override SynchronizationContext CreateCopy()
        {
            return new DispatcherQueueSynchronizationContext(m_dispatcherQueue);
        }

        public class Invoker
        {
            private readonly ExecutionContext _executionContext;

            private readonly SendOrPostCallback _callback;

            private readonly object _state;

            private static readonly ContextCallback s_contextCallback = InvokeInContext;

            public Invoker(SendOrPostCallback callback, object state)
            {
                _executionContext = ExecutionContext.Capture();
                _callback = callback;
                _state = state;
            }

            public void Invoke()
            {
                if (_executionContext == null)
                {
                    InvokeCore();
                }
                else
                {
                    ExecutionContext.Run(_executionContext, s_contextCallback, this);
                }
            }

            private static void InvokeInContext(object thisObj)
            {
                ((Invoker)thisObj).InvokeCore();
            }

            private void InvokeCore()
            {
                try
                {
                    _callback(_state);
                }
                catch (Exception ex)
                {
                    if (!(ex is ThreadAbortException) && !(ex is AppDomainUnloadedException))
                    {
                        WinRT.ExceptionHelpers.ReportUnhandledError(ex);
                    }
                }
            }
        }
    }
}