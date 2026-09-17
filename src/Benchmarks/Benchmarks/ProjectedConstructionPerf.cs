using BenchmarkComponent;
using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class ProjectedConstructionPerf
    {
        private ManagedEvents managedEvents;

        [GlobalSetup]
        public void Setup()
        {
            managedEvents = new ManagedEvents();
        }

        [Benchmark]
        public ClassWithMultipleInterfaces ConstructProjectedClassWithInt()
        {
            return new ClassWithMultipleInterfaces(42);
        }

        [Benchmark]
        public ClassWithMarshalingRoutines ConstructProjectedClassWithString()
        {
            return new ClassWithMarshalingRoutines("Hello");
        }

        [Benchmark]
        public ClassWithFastAbi ConstructFastAbiProjectedClassWithInt()
        {
            return new ClassWithFastAbi(42);
        }

        [Benchmark]
        public ClassWithFastAbiDerived ConstructDerivedFastAbiProjectedClassWithInt()
        {
            return new ClassWithFastAbiDerived(42);
        }

        [Benchmark]
        public EventOperations ConstructProjectedClassWithInterface()
        {
            return new EventOperations(managedEvents);
        }
    }
}
