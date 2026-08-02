using BenchmarkComponent;
using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class StringMarshallingPerf
    {
        private ClassWithMultipleInterfaces instance;
        private string value;

        [Params(5, 256)]
        public int Length { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            instance = new ClassWithMultipleInterfaces();
            value = new string('a', Length);
            instance.DefaultStringProperty = value;
        }

        [Benchmark]
        public string GetString()
        {
            return instance.DefaultStringProperty;
        }

        [Benchmark]
        public void SetString()
        {
            instance.DefaultStringProperty = value;
        }

        [Benchmark]
        public string StringMethod()
        {
            return instance.DefaultStringMethod(value);
        }
    }
}
