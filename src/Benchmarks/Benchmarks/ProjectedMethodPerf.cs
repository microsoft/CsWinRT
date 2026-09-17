using BenchmarkComponent;
using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class ProjectedMethodPerf
    {
        private ClassWithMultipleInterfaces instance;
        private ClassWithFastAbi fastAbiInstance;
        private ClassWithFastAbiDerived fastAbiDerivedInstance;

        [GlobalSetup]
        public void Setup()
        {
            instance = new ClassWithMultipleInterfaces();
            fastAbiInstance = new ClassWithFastAbi();
            fastAbiDerivedInstance = new ClassWithFastAbiDerived();
        }

        [Benchmark]
        public void DefaultVoidMethod()
        {
            instance.DefaultVoidMethod();
        }

        [Benchmark]
        public int DefaultIntMethod()
        {
            return instance.DefaultIntMethod(42);
        }

        [Benchmark]
        public void NonDefaultVoidMethod()
        {
            instance.VoidMethod();
        }

        [Benchmark]
        public int NonDefaultIntMethod()
        {
            return instance.IntMethod(42);
        }

        [Benchmark]
        public void FastAbiDefaultVoidMethod()
        {
            fastAbiInstance.DefaultVoidMethod();
        }

        [Benchmark]
        public int FastAbiDefaultIntMethod()
        {
            return fastAbiInstance.DefaultIntMethod(42);
        }

        [Benchmark]
        public void FastAbiNonDefaultVoidMethod()
        {
            fastAbiInstance.NonDefaultVoidMethod();
        }

        [Benchmark]
        public int FastAbiNonDefaultIntMethod()
        {
            return fastAbiInstance.NonDefaultIntMethod(42);
        }

        [Benchmark]
        public void FastAbiDerivedDefaultVoidMethod()
        {
            fastAbiDerivedInstance.DerivedDefaultVoidMethod();
        }

        [Benchmark]
        public int FastAbiDerivedDefaultIntMethod()
        {
            return fastAbiDerivedInstance.DerivedDefaultIntMethod(42);
        }

        [Benchmark]
        public void FastAbiDerivedNonDefaultVoidMethod()
        {
            fastAbiDerivedInstance.DerivedNonDefaultVoidMethod();
        }

        [Benchmark]
        public int FastAbiDerivedNonDefaultIntMethod()
        {
            return fastAbiDerivedInstance.DerivedNonDefaultIntMethod(42);
        }

        [Benchmark]
        public int FastAbiDerivedBaseDefaultIntMethod()
        {
            return fastAbiDerivedInstance.DefaultIntMethod(42);
        }

        [Benchmark]
        public int FastAbiDerivedBaseNonDefaultIntMethod()
        {
            return fastAbiDerivedInstance.NonDefaultIntMethod(42);
        }
    }
}
