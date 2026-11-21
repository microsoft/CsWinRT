using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using TestComponentCSharp;
using Windows.Foundation;
using Xunit;

namespace UnitTest
{
    public partial class AsyncLifetimeTests
    {
        [Theory]
        [InlineData(0, AsyncStatus.Completed)]
        [InlineData(1, AsyncStatus.Completed)]
        [InlineData(2, AsyncStatus.Completed)]
        [InlineData(3, AsyncStatus.Completed)]
        [InlineData(4, AsyncStatus.Completed)]
        [InlineData(0, AsyncStatus.Error)]
        [InlineData(1, AsyncStatus.Error)]
        [InlineData(2, AsyncStatus.Error)]
        [InlineData(3, AsyncStatus.Error)]
        [InlineData(4, AsyncStatus.Error)]
        [InlineData(0, AsyncStatus.Canceled)]
        [InlineData(1, AsyncStatus.Canceled)]
        [InlineData(2, AsyncStatus.Canceled)]
        [InlineData(3, AsyncStatus.Canceled)]
        [InlineData(4, AsyncStatus.Canceled)]
        public void AsyncInfoSurvivesUntilCompletionAndIsCollected(int kind, AsyncStatus status)
        {
            var instance = new Class();
            var (weakTask, weakOperation) = CompleteOperation(instance, kind, status);
            Assert.True(SpinWait.SpinUntil(() =>
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
            Assert.True(weakOperation.IsAlive, "The WinRT operation must survive collection while awaiting completion.");
            Assert.False(task.IsCompleted);

            const int E_FAIL = unchecked((int)0x80004005);
            if (status == AsyncStatus.Canceled)
            {
                Cancel(weakOperation);
            }
            else if (kind == 4)
            {
                Assert.True(instance.TryCompleteWrlAsync(status == AsyncStatus.Error ? E_FAIL : 0));
            }
            else
            {
                instance.CompleteAsync(status == AsyncStatus.Error ? E_FAIL : 0);
            }

            if (status == AsyncStatus.Completed)
            {
                Assert.True(task.Wait(5000));
                Assert.Equal(TaskStatus.RanToCompletion, task.Status);
                if (task is Task<int> result)
                {
                    Assert.Equal(50, result.Result);
                    Assert.Equal(50, result.GetAwaiter().GetResult());
                }
                else if (task is Task<uint> unsignedResult)
                {
                    Assert.Equal(50u, unsignedResult.Result);
                    Assert.Equal(50u, unsignedResult.GetAwaiter().GetResult());
                }
            }
            else
            {
                var error = Assert.Throws<AggregateException>(() => task.Wait(5000));
                if (status == AsyncStatus.Error)
                {
                    Assert.Equal(TaskStatus.Faulted, task.Status);
                    Assert.Equal(E_FAIL, error.InnerException.HResult);
                }
                else
                {
                    Assert.Equal(TaskStatus.Canceled, task.Status);
                    Assert.IsType<TaskCanceledException>(error.InnerException);
                }
            }

            return (new WeakReference(task), weakOperation);
        }

        [Theory]
        [InlineData(AsyncStatus.Completed, false)]
        [InlineData(AsyncStatus.Error, false)]
        [InlineData(AsyncStatus.Completed, true)]
        [InlineData(AsyncStatus.Error, true)]
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
            Assert.False(task.IsCompleted);
            Assert.Equal(0, callbackCount);

            const int E_FAIL = unchecked((int)0x80004005);
            _ = instance.TryCompleteWrlAsync(status == AsyncStatus.Error ? E_FAIL : 0);
            Assert.True(called.Task.Wait(5000), "The WRL completion handler was not called after collection.");
            Assert.Equal(status, called.Task.Result);
            Assert.Equal(1, callbackCount);
            if (status == AsyncStatus.Completed)
            {
                Assert.True(task.Wait(5000));
                Assert.Equal(50u, task.Result);
            }
            else
            {
                Assert.True(task.IsCompleted);
                var error = Assert.Throws<AggregateException>(task.Wait);
                Assert.Equal(E_FAIL, error.InnerException.HResult);
            }
            GC.KeepAlive(instance);
        }

        [Theory]
        [InlineData(AsyncStatus.Completed)]
        [InlineData(AsyncStatus.Error)]
        public void WrlCompletionSurvivesWithoutCallerTaskReference(AsyncStatus status)
        {
            var instance = new Class();
            var called = new TaskCompletionSource<AsyncStatus>(TaskCreationOptions.RunContinuationsAsynchronously);
            var (weakTask, weakOperation) = StartAbandonedWrlAddition(instance,
                (_, completedStatus) => called.TrySetResult(completedStatus));

            Collect();
            Assert.False(called.Task.IsCompleted);
            const int E_FAIL = unchecked((int)0x80004005);
            Assert.True(instance.TryCompleteWrlAsync(status == AsyncStatus.Error ? E_FAIL : 0));
            Assert.True(called.Task.Wait(5000));
            Assert.Equal(status, called.Task.Result);
            Assert.True(SpinWait.SpinUntil(() =>
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

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WrlOperationCanceledThroughToken(bool alreadyCanceled)
        {
            var instance = new Class();
            var (weakTask, weakOperation) = CancelWrlOperation(instance, alreadyCanceled);
            Assert.True(SpinWait.SpinUntil(() =>
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
                Assert.False(task.IsCompleted);
                Assert.Equal(0, callbackCount);
                cancellation.Cancel();
            }

            Assert.True(called.Task.Wait(5000), "The WRL cancellation completion handler was not called.");
            Assert.Equal(AsyncStatus.Canceled, called.Task.Result);
            Assert.Equal(1, callbackCount);
            // A lambda capturing task would share the closure retained by the native observer above.
            var error = Assert.Throws<TaskCanceledException>(((Task)task).GetAwaiter().GetResult);
            Assert.Equal(cancellation.Token, error.CancellationToken);
            return (new WeakReference(task), weakOperation);
        }

        [Theory]
        [InlineData(false, 0)]
        [InlineData(true, 0)]
        [InlineData(false, unchecked((int)0x80010108))]
        [InlineData(true, unchecked((int)0x80010108))]
        [InlineData(false, unchecked((int)0x800706BA))]
        [InlineData(true, unchecked((int)0x800706BA))]
        [InlineData(false, unchecked((int)0x89020001))]
        [InlineData(true, unchecked((int)0x89020001))]
        [InlineData(false, unchecked((int)0x80004005))]
        [InlineData(true, unchecked((int)0x80004005))]
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
                Assert.False(task.IsCompleted);
                if (cancelError == unchecked((int)0x80004005))
                {
                    var error = Assert.Throws<AggregateException>(() => cancellation.Cancel());
                    Assert.Same(nativeError, Assert.Single(error.InnerExceptions));
                }
                else
                {
                    cancellation.Cancel();
                }
            }
            Assert.Equal(1, action.CancelCalls);
            Assert.True(task.IsCanceled);
            var canceled = Assert.Throws<TaskCanceledException>(() => task.GetAwaiter().GetResult());
            Assert.Equal(cancellation.Token, canceled.CancellationToken);
        }

        [Fact]
        public void CancellationStopsOnFirstExceptionStillCancelsTask()
        {
            using var cancellation = new CancellationTokenSource();
            var nativeError = new COMException("Test cancellation failure.", unchecked((int)0x80004005));
            var action = new CancellationOnlyAsyncAction(nativeError);
            var task = action.AsTask(cancellation.Token);

            var error = Assert.Throws<COMException>(() => cancellation.Cancel(throwOnFirstException: true));
            Assert.Same(nativeError, error);
            Assert.Equal(1, action.CancelCalls);
            Assert.True(task.IsCanceled);
            var canceled = Assert.Throws<TaskCanceledException>(() => task.GetAwaiter().GetResult());
            Assert.Equal(cancellation.Token, canceled.CancellationToken);
        }

        [Fact]
        public void CompletionUnregistersCancellation()
        {
            using var cancellation = new CancellationTokenSource();
            var action = new CancellationOnlyAsyncAction(null);
            var task = action.AsTask(cancellation.Token);
            action.Complete();
            Assert.True(task.Wait(5000));
            cancellation.Cancel();
            Assert.Equal(0, action.CancelCalls);
            Assert.Equal(TaskStatus.RanToCompletion, task.Status);
        }

        [Fact]
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
                Assert.True(Task.WhenAll(cancel, complete).Wait(5000));
                Assert.True(task.IsCompleted);
                if (task.IsCanceled)
                {
                    var error = Assert.Throws<TaskCanceledException>(() => task.GetAwaiter().GetResult());
                    Assert.Equal(cancellation.Token, error.CancellationToken);
                }
                else
                {
                    Assert.Equal(TaskStatus.RanToCompletion, task.Status);
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

            // Cancellation intentionally does not invoke Completed, even when it succeeds.
            public void Cancel()
            {
                Interlocked.Increment(ref _cancelCalls);
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
