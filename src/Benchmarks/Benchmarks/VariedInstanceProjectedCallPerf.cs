using BenchmarkComponent;
using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class VariedInstanceProjectedCallPerf
    {
        private const int InstanceCount = 1024;
        private const int InstanceMask = InstanceCount - 1;

        private ClassWithMultipleInterfaces[] instances;
        private ClassWithFastAbi[] fastAbiInstances;
        private int index;

        [GlobalSetup]
        public void Setup()
        {
            instances = new ClassWithMultipleInterfaces[InstanceCount];
            fastAbiInstances = new ClassWithFastAbi[InstanceCount];

            for (int i = 0; i < InstanceCount; i++)
            {
                instances[i] = new ClassWithMultipleInterfaces();
                fastAbiInstances[i] = new ClassWithFastAbi();
            }
        }

        [Benchmark]
        public int VariedInstanceDefaultProperty()
        {
            index = (index + 31) & InstanceMask;
            return instances[index].DefaultIntProperty;
        }

        [Benchmark]
        public int VariedInstanceNonDefaultProperty()
        {
            index = (index + 31) & InstanceMask;
            return instances[index].IntProperty;
        }

        [Benchmark]
        public int VariedInstanceDefaultMethod()
        {
            index = (index + 31) & InstanceMask;
            return instances[index].DefaultIntMethod(42);
        }

        [Benchmark]
        public int VariedInstanceNonDefaultMethod()
        {
            index = (index + 31) & InstanceMask;
            return instances[index].IntMethod(42);
        }

        [Benchmark]
        public int VariedInstanceFastAbiDefaultProperty()
        {
            index = (index + 31) & InstanceMask;
            return fastAbiInstances[index].DefaultIntProperty;
        }

        [Benchmark]
        public int VariedInstanceFastAbiNonDefaultProperty()
        {
            index = (index + 31) & InstanceMask;
            return fastAbiInstances[index].NonDefaultIntProperty;
        }
    }
}
