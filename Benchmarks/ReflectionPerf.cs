using BenchmarkComponent;
using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class ReflectionPerf
    {
        ClassWithMarshalingRoutines instance;

        [GlobalSetup]
        public void Setup()
        {
            instance = new ClassWithMarshalingRoutines();
        }

        [Benchmark]
        public void ExecuteComWrappersCreateKeyValuePairsFactory()
        {
            var value = instance.DefaultKeyValuePairProperty;
        }

        [Benchmark]
        public void ExecuteComWrappersCreateArrayFactory()
        {
            var value = instance.DefaultArrayProperty;
        }

        [Benchmark]
        public void ExecuteComWrappersCreateNullableFactory()
        {
            var value = instance.DefaultNullableProperty;
        }
    }
}
