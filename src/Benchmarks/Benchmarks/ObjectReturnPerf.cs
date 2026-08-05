using BenchmarkComponent;
using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class ObjectReturnPerf
    {
        private ClassWithMarshalingRoutines instance;

        [GlobalSetup]
        public void Setup()
        {
            instance = new ClassWithMarshalingRoutines();
        }

        [Benchmark]
        public WrappedClass NewSealedObject()
        {
            return instance.GetNewSealedObject();
        }

        [Benchmark]
        public WrappedClass ExistingSealedObject()
        {
            return instance.GetExistingSealedObject();
        }

        [Benchmark]
        public ClassWithFastAbi NewUnsealedObject()
        {
            return instance.GetNewUnsealedObject();
        }

        [Benchmark]
        public ClassWithFastAbi ExistingUnsealedObject()
        {
            return instance.GetExistingUnsealedObject();
        }

        [Benchmark]
        public ClassWithFastAbi NewDerivedAsBase()
        {
            return instance.GetNewDerivedAsBase();
        }
    }
}
