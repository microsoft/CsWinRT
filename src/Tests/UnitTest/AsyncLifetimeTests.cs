using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TestComponentCSharp;
using Windows.Foundation;
using Xunit;

namespace UnitTest
{
    public class AsyncLifetimeTests
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
        public void PendingTaskKeepsAsyncInfoAlive(int kind, AsyncStatus status)
        {
            var instance = new Class();
            var (task, operation) = Start(instance, kind);

            Collect();
            Assert.True(operation.IsAlive, "The pending task must keep its WinRT operation alive.");
            Assert.False(task.IsCompleted);

            const int E_FAIL = unchecked((int)0x80004005);
            if (status == AsyncStatus.Canceled)
            {
                Cancel(operation);
            }
            else if (kind == 4)
            {
                instance.CompleteWrlAsync(status == AsyncStatus.Error ? E_FAIL : 0);
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
                }
                else if (task is Task<uint> unsignedResult)
                {
                    Assert.Equal(50u, unsignedResult.Result);
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

            // Task completion can wake this thread before the native completion callback has returned.
            Assert.True(SpinWait.SpinUntil(() =>
            {
                Collect();
                return !operation.IsAlive;
            }, 5000), "The completed task must release its WinRT operation.");
            GC.KeepAlive(task);
            GC.KeepAlive(instance);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Cancel(WeakReference operation)
        {
            ((IAsyncInfo)operation.Target).Cancel();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static (Task Task, WeakReference Operation) Start(Class instance, int kind)
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
