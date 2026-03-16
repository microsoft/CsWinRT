using System;
using System.Threading.Tasks;
using BenchmarkComponent;
using BenchmarkDotNet.Attributes;
#if CSWINRT3_0
using Windows.Foundation.Tasks;
#endif

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class AsyncPerf
    {
        ClassWithAsync instance;

        [GlobalSetup]
        public void Setup()
        {
            instance = new ClassWithAsync();
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
    }
}