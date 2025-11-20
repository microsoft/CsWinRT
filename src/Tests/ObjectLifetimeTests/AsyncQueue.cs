using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.Windows.System;

namespace ObjectLifetimeTests
{
    public class AsyncQueue
    {
        private DispatcherQueue _dispatcher;

        public AsyncQueue(DispatcherQueue dispatcher)
        {
            _dispatcher = dispatcher;
        }

        Queue<(bool onUIThread, Action action)> _queue = new Queue<(bool, Action)>();
        public AsyncQueue CallFromUIThread(Action action)
        {
            _queue.Enqueue((onUIThread: true, action));
            return this;
        }

        [MethodImplAttribute(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public AsyncQueue CollectUntilTrue(Func<bool> fn, string timeoutMessage)
        {
            _queue.Enqueue((onUIThread: false, () => DoCollectUntilTrue(fn, timeoutMessage)));
            return this;
        }


        [MethodImplAttribute(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        void DoCollectUntilTrue(Func<bool> fn, string timeoutMessage)
        {
            TimeSpan timeout = TimeSpan.FromMilliseconds(5000);
            var isTimeout = false;

            bool isDone = false;
            DateTime startTime = DateTime.Now;

            while (!isDone && !isTimeout)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();

                isDone = fn();
                isTimeout = DateTime.Now - startTime >= timeout;
            }

            if (isTimeout)
            {
                throw new Exception(String.IsNullOrEmpty(timeoutMessage) ? "Timed out waiting for true" : timeoutMessage);
            }
        }

        private AsyncQueue WaitForHandle(WaitHandle handle, string timeoutErrorMessage, TimeSpan timeout)
        {
            if (handle == null)
                throw new ArgumentNullException("handle");

            if (timeoutErrorMessage == null)
                timeoutErrorMessage = "WaitHandle was not set in expected time";

            var semaphore = new Semaphore(1, 1);
            var timedOut = false;

            _queue.Enqueue((onUIThread: false, () =>
            {
                if (!handle.WaitOne(timeout))
                {
                    timedOut = true;
                }

                if (timedOut)
                {
                    throw new Exception(timeoutErrorMessage);
                }
            }
            ));

            return this;
        }

        private static readonly System.TimeSpan DefaultTimeout = System.TimeSpan.FromSeconds(8);

        public virtual AsyncQueue WaitForHandle(WaitHandle handle, string timeoutErrorMessage)
        {
            return WaitForHandle(handle, timeoutErrorMessage, DefaultTimeout);
        }

        public AsyncQueue WaitForHandle(WaitHandle handle, string timeoutErrorMessage, int timeout)
        {
            return WaitForHandle(handle, timeoutErrorMessage, TimeSpan.FromMilliseconds(timeout));
        }

        Semaphore _sem = new Semaphore(0, 1);

        public void Run() // Off UI thread
        {
            while (_queue.Any())
            {
                var item = _queue.Dequeue();
                {
                    bool succeeded = false;
                    var errorMessage = "";

                    if (item.onUIThread)
                    {
                        _dispatcher.TryEnqueue(() =>
                        {
                            try
                            {
                                item.action();
                                succeeded = true;
                            }
                            catch (Exception e)
                            {
                                succeeded = false;
                                errorMessage = e.Message;
                            }

                            _sem.Release();
                        });
                        _sem.WaitOne();
                    }
                    else
                    {
                        try
                        {
                            item.action();
                            succeeded = true;
                        }
                        catch (Exception e)
                        {
                            succeeded = false;
                            errorMessage = e.Message;
                        }
                    }

                    if (!succeeded)
                    {
                        _queue.Clear();
                        throw new TestException(errorMessage);
                    }

                }
            }
        }

    }
}