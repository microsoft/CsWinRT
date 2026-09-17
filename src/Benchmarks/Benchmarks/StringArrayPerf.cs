using BenchmarkComponent;
using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class StringArrayPerf
    {
        private ClassWithMultipleInterfaces instance;
        private string[] values;

        [Params(4, 16, 64)]
        public int Count { get; set; }

        [Params(4, 64)]
        public int Length { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            instance = new ClassWithMultipleInterfaces();
            values = new string[Count];

            for (int i = 0; i < values.Length; i++)
            {
                values[i] = new string('a', Length);
            }
        }

        [Benchmark]
        public void PassStringArray()
        {
            instance.AcceptStringArray(values);
        }
    }
}
