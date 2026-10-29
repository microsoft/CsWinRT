using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using TestComponentCSharp;
using Windows.Foundation;

namespace UnitTest
{
    [TestClass]
    public partial class AsyncLifetimeTests
    {
        [TestMethod]
        [DataRow(0, AsyncStatus.Completed)]
        [DataRow(1, AsyncStatus.Completed)]
        [DataRow(2, AsyncStatus.Completed)]
        [DataRow(3, AsyncStatus.Completed)]
        [DataRow(4, AsyncStatus.Completed)]
        [DataRow(0, AsyncStatus.Error)]
        [DataRow(1, AsyncStatus.Error)]
        [DataRow(2, AsyncStatus.Error)]
        [DataRow(3, AsyncStatus.Error)]
        [DataRow(4, AsyncStatus.Error)]
        [DataRow(0, AsyncStatus.Canceled)]
        [DataRow(1, AsyncStatus.Canceled)]
        [DataRow(2, AsyncStatus.Canceled)]
        [DataRow(3, AsyncStatus.Canceled)]
        [DataRow(4, AsyncStatus.Canceled)]
        public void AsyncInfoSurvivesUntilCompletionAndIsCollected(int kind, AsyncStatus status)
        {
            var instance = new Class();
            var (weakTask, weakOperation) = CompleteOperation(instance, kind, status);
            Assert.IsTrue(SpinWait.SpinUntil(() =>
            {
                Collect();
                return !weakTask.IsAlive && !weakOperation.IsAlive;
            }, 5000), "The completed task and its operation must be collectible once the caller releases the task.");
            GC.KeepAlive(instance);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static (WeakReference WeakTask, WeakReference WeakOperation) CompleteOperation(
            Class instance,
            int kind,
            AsyncStatus status)
        {
            var (task, weakOperation) = Start(instance, kind);

            Collect();
            Assert.IsTrue(weakOperation.IsAlive, "The WinRT operation must survive collection while awaiting completion.");
            Assert.IsFalse(task.IsCompleted);

            const int E_FAIL = unchecked((int)0x80004005);
            if (status == AsyncStatus.Canceled)
            {
                Cancel(weakOperation);
            }
            else if (kind == 4)
            {
                Assert.IsTrue(instance.TryCompleteWrlAsync(status == AsyncStatus.Error ? E_FAIL : 0));
            }
            else
            {
                instance.CompleteAsync(status == AsyncStatus.Error ? E_FAIL : 0);
            }

            if (status == AsyncStatus.Completed)
            {
                Assert.IsTrue(task.Wait(5000));
                Assert.AreEqual(TaskStatus.RanToCompletion, task.Status);
                if (task is Task<int> result)
                {
                    Assert.AreEqual(50, result.Result);
                    Assert.AreEqual(50, result.GetAwaiter().GetResult());
                }
                else if (task is Task<uint> unsignedResult)
                {
                    Assert.AreEqual(50u, unsignedResult.Result);
                    Assert.AreEqual(50u, unsignedResult.GetAwaiter().GetResult());
                }
            }
            else
            {
                var error = Assert.ThrowsExactly<AggregateException>(() => task.Wait(5000));
                if (status == AsyncStatus.Error)
                {
                    Assert.AreEqual(TaskStatus.Faulted, task.Status);
                    Assert.AreEqual(E_FAIL, error.InnerException.HResult);
                }
                else
                {
                    Assert.AreEqual(TaskStatus.Canceled, task.Status);
                    Assert.IsInstanceOfType<TaskCanceledException>(error.InnerException);
                }
            }

            return (new WeakReference(task), weakOperation);
        }

        [TestMethod]
        [DataRow(AsyncStatus.Completed, false)]
        [DataRow(AsyncStatus.Error, false)]
        [DataRow(AsyncStatus.Completed, true)]
        [DataRow(AsyncStatus.Error, true)]
        public void WrlCompletionHandlerCalledAfterCollection(AsyncStatus status, bool cancellable)
        {
            var instance = new Class();
            using var cancellation = new CancellationTokenSource();
            var called = new TaskCompletionSource<AsyncStatus>(TaskCreationOptions.RunContinuationsAsynchronously);
            int callbackCount = 0;
            var task = StartObservedWrlAddition(instance, (_, completedStatus) =>
            {
                Interlocked.Increment(ref callbackCount);
                called.TrySetResult(completedStatus);
            }, cancellable ? cancellation.Token : CancellationToken.None);

            Collect();
            Assert.IsFalse(task.IsCompleted);
            Assert.AreEqual(0, callbackCount);

            const int E_FAIL = unchecked((int)0x80004005);
            _ = instance.TryCompleteWrlAsync(status == AsyncStatus.Error ? E_FAIL : 0);
            Assert.IsTrue(called.Task.Wait(5000), "The WRL completion handler was not called after collection.");
            Assert.AreEqual(status, called.Task.Result);
            Assert.AreEqual(1, callbackCount);
            if (status == AsyncStatus.Completed)
            {
                Assert.IsTrue(task.Wait(5000));
                Assert.AreEqual(50u, task.Result);
            }
            else
            {
                Assert.IsTrue(task.IsCompleted);
                var error = Assert.ThrowsExactly<AggregateException>(task.Wait);
                Assert.AreEqual(E_FAIL, error.InnerException.HResult);
            }
            GC.KeepAlive(instance);
        }

        [TestMethod]
        [DataRow(AsyncStatus.Completed)]
        [DataRow(AsyncStatus.Error)]
        public void WrlCompletionSurvivesWithoutCallerTaskReference(AsyncStatus status)
        {
            var instance = new Class();
            var called = new TaskCompletionSource<AsyncStatus>(TaskCreationOptions.RunContinuationsAsynchronously);
            var (weakTask, weakOperation) = StartAbandonedWrlAddition(instance,
                (_, completedStatus) => called.TrySetResult(completedStatus));

            Collect();
            Assert.IsFalse(called.Task.IsCompleted);
            const int E_FAIL = unchecked((int)0x80004005);
            Assert.IsTrue(instance.TryCompleteWrlAsync(status == AsyncStatus.Error ? E_FAIL : 0));
            Assert.IsTrue(called.Task.Wait(5000));
            Assert.AreEqual(status, called.Task.Result);
            Assert.IsTrue(SpinWait.SpinUntil(() =>
            {
                Collect();
                return !weakTask.IsAlive && !weakOperation.IsAlive;
            }, 5000), "The abandoned task and its WinRT operation must be collectible after completion.");
            GC.KeepAlive(instance);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static (WeakReference WeakTask, WeakReference WeakOperation) StartAbandonedWrlAddition(Class instance,
            AsyncOperationWithProgressCompletedHandler<uint, uint> observer)
        {
            var operation = instance.WrlAddAsyncWithProgress(42, 8, observer);
            var task = operation.AsTask();
            return (new WeakReference(task), new WeakReference(operation));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void WrlOperationCanceledThroughToken(bool alreadyCanceled)
        {
            var instance = new Class();
            var (weakTask, weakOperation) = CancelWrlOperation(instance, alreadyCanceled);
            Assert.IsTrue(SpinWait.SpinUntil(() =>
            {
                Collect();
                return !weakTask.IsAlive && !weakOperation.IsAlive;
            }, 5000), "The canceled task and its operation must be collectible once the caller releases the task.");
            GC.KeepAlive(instance);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static (WeakReference WeakTask, WeakReference WeakOperation) CancelWrlOperation(
            Class instance, bool alreadyCanceled)
        {
            using var cancellation = new CancellationTokenSource();
            if (alreadyCanceled)
            {
                cancellation.Cancel();
            }
            var called = new TaskCompletionSource<AsyncStatus>(TaskCreationOptions.RunContinuationsAsynchronously);
            int callbackCount = 0;
            var (task, weakOperation) = StartCancellableWrlAddition(instance, (_, status) =>
            {
                Interlocked.Increment(ref callbackCount);
                called.TrySetResult(status);
            }, cancellation.Token);

            if (!alreadyCanceled)
            {
                Collect();
                Assert.IsFalse(task.IsCompleted);
                Assert.AreEqual(0, callbackCount);
                cancellation.Cancel();
            }

            Assert.IsTrue(called.Task.Wait(5000), "The WRL cancellation completion handler was not called.");
            Assert.AreEqual(AsyncStatus.Canceled, called.Task.Result);
            Assert.AreEqual(1, callbackCount);
            // A lambda capturing task would share the closure retained by the native observer above.
            var error = Assert.ThrowsExactly<TaskCanceledException>(((Task)task).GetAwaiter().GetResult);
            Assert.AreEqual(cancellation.Token, error.CancellationToken);
            return (new WeakReference(task), weakOperation);
        }

        [TestMethod]
        [DataRow(false, 0)]
        [DataRow(true, 0)]
        [DataRow(false, unchecked((int)0x80010108))]
        [DataRow(true, unchecked((int)0x80010108))]
        [DataRow(false, unchecked((int)0x800706BA))]
        [DataRow(true, unchecked((int)0x800706BA))]
        [DataRow(false, unchecked((int)0x89020001))]
        [DataRow(true, unchecked((int)0x89020001))]
        [DataRow(false, unchecked((int)0x80004005))]
        [DataRow(true, unchecked((int)0x80004005))]
        public void CancellationCompletesTaskWithoutNativeCompletion(bool alreadyCanceled, int cancelError)
        {
            using var cancellation = new CancellationTokenSource();
            if (alreadyCanceled)
            {
                cancellation.Cancel();
            }
            var nativeError = cancelError == 0 ? null : new COMException("Test cancellation failure.", cancelError);
            var action = new CancellationOnlyAsyncAction(nativeError);
            var task = action.AsTask(cancellation.Token);
            if (!alreadyCanceled)
            {
                Assert.IsFalse(task.IsCompleted);
                if (cancelError == unchecked((int)0x80004005))
                {
                    var error = Assert.ThrowsExactly<AggregateException>(() => cancellation.Cancel());
                    Assert.HasCount(1, error.InnerExceptions);
                    Assert.AreSame(nativeError, error.InnerExceptions[0]);
                }
                else
                {
                    cancellation.Cancel();
                }
            }
            Assert.AreEqual(1, action.CancelCalls);
            Assert.IsTrue(task.IsCanceled);
            var canceled = Assert.ThrowsExactly<TaskCanceledException>(() => task.GetAwaiter().GetResult());
            Assert.AreEqual(cancellation.Token, canceled.CancellationToken);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void CancellationUsesCancelingExecutionContext(bool hasResult)
        {
            using var cancellation = new CancellationTokenSource();
            var ambient = new AsyncLocal<string>();
            string observed = null;
            Action onCancel = () => observed = ambient.Value;
            ambient.Value = "registration";

            try
            {
                Task task = hasResult
                    ? new CancellationOnlyAsyncOperation(onCancel).AsTask(cancellation.Token)
                    : new CancellationOnlyAsyncAction(null) { OnCancel = onCancel }.AsTask(cancellation.Token);

                ambient.Value = "cancellation";
                cancellation.Cancel();

                Assert.AreEqual("cancellation", observed);
                Assert.IsTrue(task.IsCanceled);
                var canceled = Assert.ThrowsExactly<TaskCanceledException>(() => task.GetAwaiter().GetResult());
                Assert.AreEqual(cancellation.Token, canceled.CancellationToken);
            }
            finally
            {
                ambient.Value = null;
            }
        }

        [TestMethod]
        public void CancellationStopsOnFirstExceptionStillCancelsTask()
        {
            using var cancellation = new CancellationTokenSource();
            var nativeError = new COMException("Test cancellation failure.", unchecked((int)0x80004005));
            var action = new CancellationOnlyAsyncAction(nativeError);
            var task = action.AsTask(cancellation.Token);

            var error = Assert.ThrowsExactly<COMException>(() => cancellation.Cancel(throwOnFirstException: true));
            Assert.AreSame(nativeError, error);
            Assert.AreEqual(1, action.CancelCalls);
            Assert.IsTrue(task.IsCanceled);
            var canceled = Assert.ThrowsExactly<TaskCanceledException>(() => task.GetAwaiter().GetResult());
            Assert.AreEqual(cancellation.Token, canceled.CancellationToken);
        }

        [TestMethod]
        public void CompletionUnregistersCancellation()
        {
            using var cancellation = new CancellationTokenSource();
            var action = new CancellationOnlyAsyncAction(null);
            var task = action.AsTask(cancellation.Token);
            action.Complete();
            Assert.IsTrue(task.Wait(5000));
            cancellation.Cancel();
            Assert.AreEqual(0, action.CancelCalls);
            Assert.AreEqual(TaskStatus.RanToCompletion, task.Status);
        }

        [TestMethod]
        public void CancellationRacingWithCompletion()
        {
            for (int i = 0; i < 100; i++)
            {
                using var cancellation = new CancellationTokenSource();
                using var ready = new ManualResetEventSlim();
                var action = new CancellationOnlyAsyncAction(null);
                var task = action.AsTask(cancellation.Token);
                var cancel = Task.Run(() =>
                {
                    ready.Wait();
                    cancellation.Cancel();
                });
                var complete = Task.Run(() =>
                {
                    ready.Wait();
                    action.Complete();
                });
                ready.Set();
                Assert.IsTrue(Task.WhenAll(cancel, complete).Wait(5000));
                Assert.IsTrue(task.IsCompleted);
                if (task.IsCanceled)
                {
                    var error = Assert.ThrowsExactly<TaskCanceledException>(() => task.GetAwaiter().GetResult());
                    Assert.AreEqual(cancellation.Token, error.CancellationToken);
                }
                else
                {
                    Assert.AreEqual(TaskStatus.RanToCompletion, task.Status);
                }
            }
        }

        private sealed partial class CancellationOnlyAsyncAction : IAsyncAction
        {
            private readonly Exception _cancelError;
            private int _cancelCalls;
            private int _status = (int)AsyncStatus.Started;

            public CancellationOnlyAsyncAction(Exception cancelError) => _cancelError = cancelError;
            public int CancelCalls => Volatile.Read(ref _cancelCalls);
            public uint Id => 1;
            public AsyncStatus Status => (AsyncStatus)Volatile.Read(ref _status);
            public Exception ErrorCode => null;
            public AsyncActionCompletedHandler Completed { get; set; }
            public Action OnCancel { get; init; }

            // Cancellation intentionally does not invoke Completed, even when it succeeds.
            public void Cancel()
            {
                Interlocked.Increment(ref _cancelCalls);
                OnCancel?.Invoke();
                if (_cancelError != null)
                {
                    throw _cancelError;
                }
            }

            public void Complete()
            {
                Volatile.Write(ref _status, (int)AsyncStatus.Completed);
                Completed(this, AsyncStatus.Completed);
            }

            public void Close() => throw new InvalidOperationException("The test action must not be closed by AsTask.");
            public void GetResults() => throw new InvalidOperationException("The test action has no results.");
        }

        private sealed partial class CancellationOnlyAsyncOperation(Action onCancel) : IAsyncOperation<int>
        {
            public uint Id => 1;
            public AsyncStatus Status => AsyncStatus.Started;
            public Exception ErrorCode => null;
            public AsyncOperationCompletedHandler<int> Completed { get; set; }
            public void Cancel() => onCancel();
            public void Close() => throw new InvalidOperationException("The test operation must not be closed by AsTask.");
            public int GetResults() => throw new InvalidOperationException("The test operation never completes.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static (Task<uint> Task, WeakReference WeakOperation) StartCancellableWrlAddition(Class instance,
            AsyncOperationWithProgressCompletedHandler<uint, uint> observer, CancellationToken token)
        {
            var operation = instance.WrlAddAsyncWithProgress(42, 8, observer);
            return (operation.AsTask(token), new WeakReference(operation));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Task<uint> StartObservedWrlAddition(Class instance,
            AsyncOperationWithProgressCompletedHandler<uint, uint> observer, CancellationToken token)
        {
            return instance.WrlAddAsyncWithProgress(42, 8, observer).AsTask(token);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Cancel(WeakReference weakOperation)
        {
            ((IAsyncInfo)weakOperation.Target).Cancel();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static (Task Task, WeakReference WeakOperation) Start(Class instance, int kind)
        {
            switch (kind)
            {
                case 0:
                    var action = instance.DoitAsync();
                    return (action.AsTask(), new WeakReference(action));
                case 1:
                    var operation = instance.AddAsync(42, 8);
                    return (operation.AsTask(), new WeakReference(operation));
                case 2:
                    var actionWithProgress = instance.DoitAsyncWithProgress();
                    return (actionWithProgress.AsTask(), new WeakReference(actionWithProgress));
                case 3:
                    var operationWithProgress = instance.AddAsyncWithProgress(42, 8);
                    return (operationWithProgress.AsTask(), new WeakReference(operationWithProgress));
                case 4:
                    var wrlOperation = instance.WrlAddAsyncWithProgress(42, 8);
                    return (wrlOperation.AsTask(), new WeakReference(wrlOperation));
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        private static void Collect()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}
