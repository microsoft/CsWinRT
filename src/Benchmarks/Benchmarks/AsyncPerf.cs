using System;
using System.Threading.Tasks;
using BenchmarkComponent;
using BenchmarkDotNet.Attributes;
using Windows.Foundation;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class AsyncPerf
    {
        ClassWithAsync instance;
        IAsyncAction completedAction;

        [GlobalSetup]
        public void Setup()
        {
            instance = new ClassWithAsync();
            completedAction = instance.Complete();
        }

        [Benchmark]
        public async Task Complete()
        {
            await instance.Complete();
        }

        [Benchmark]
        public async Task YieldComplete()
        {
            await instance.YieldComplete();
        }

        [Benchmark]
        public async Task<int> Return()
        {
            return await instance.Return(5);
        }

        [Benchmark]
        public async Task<int> YieldReturn()
        {
            return await instance.YieldReturn(5);
        }

        [Benchmark]
        public AsyncStatus GetCompletedStatus()
        {
            return completedAction.Status;
        }

        [Benchmark]
        public IAsyncAction CompletedTaskAsAsyncAction()
        {
            return Task.CompletedTask.AsAsyncAction();
        }

        [Benchmark]
        public IAsyncOperation<int> CompletedTaskAsAsyncOperation()
        {
            return Task.FromResult(42).AsAsyncOperation();
        }
    }
}