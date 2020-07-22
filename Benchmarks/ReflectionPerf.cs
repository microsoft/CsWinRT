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
        public object ExecuteComWrappersCreateKeyValuePairsFactoryWithNewObject()
        {
            return instance.NewTypeErasedKeyValuePairObject;
        }

        [Benchmark]
        public object ExecuteComWrappersCreateArrayFactoryWithNewObject()
        {
            return instance.NewTypeErasedArrayObject;
        }

        [Benchmark]
        public object ExecuteComWrappersCreateNullableFactoryWithNewObject()
        {
            return instance.NewTypeErasedNullableObject;
        }

        [Benchmark]
        public object ExecuteComWrappersCreateKeyValuePairsFactoryWithExistingObject()
        {
            return instance.ExistingTypeErasedKeyValuePairObject;
        }

        [Benchmark]
        public object ExecuteComWrappersCreateArrayFactoryWithExistingObject()
        {
            return instance.ExistingTypeErasedArrayObject;
        }

        [Benchmark]
        public object ExecuteComWrappersCreateNullableFactoryWithExistingObject()
        {
            return instance.ExistingTypeErasedNullableObject;
        }

        [Benchmark]
        public object ExecuteCachedReflectionUsingStringMarshaling()
        {
            return instance.DefaultStringProperty;
        }

        [Benchmark]
        public object ExecuteCachedReflectionUsingCustomObjectInterfaceMarshaling()
        {
            return instance.NewWrappedClassObject;
        }
    }
}
