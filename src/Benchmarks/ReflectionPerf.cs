using BenchmarkComponent;
using BenchmarkDotNet.Attributes;
using System;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class ReflectionPerf
    {
        ClassWithMarshalingRoutines instance;

        [GlobalSetup]
        public void Setup()
        {
            //TestObject = new Class();
            instance = new ClassWithMarshalingRoutines();
        }

        //[Benchmark]
        //public object ExecuteMarshalingForNewKeyValuePair()
        //{
        //    return instance.NewTypeErasedKeyValuePairObject;
        //}

        //[Benchmark]
        //public object ExecuteMarshalingForNewArray()
        //{
        //    return instance.NewTypeErasedArrayObject;
        //}

        //[Benchmark]
        //public object ExecuteMarshalingForNewNullable()
        //{
        //    return instance.NewTypeErasedNullableObject;
        //}

        //[Benchmark]
        //public object ExecuteMarshalingForExistingKeyvaluePair()
        //{
        //    return instance.ExistingTypeErasedKeyValuePairObject;
        //}

        //[Benchmark]
        //public object ExecuteMarshalingForExistingArray()
        //{
        //    return instance.ExistingTypeErasedArrayObject;
        //}

        //[Benchmark]
        //public object ExecuteMarshalingForExistingNullable()
        //{
        //    return instance.ExistingTypeErasedNullableObject;
        //}

        //[Benchmark]
        //public object ExecuteMarshalingForString()
        //{
        //    return instance.DefaultStringProperty;
        //}

        //[Benchmark]
        //public object ExecuteMarshalingForCustomObject()
        //{
        //    return instance.NewWrappedClassObject;
        //}

        //[Benchmark]
        //public void StringEventSource()
        //{
        //    string test_string = "x";
        //    string test_string2 = "y";

        //    // In hstring from managed->native implicitly creates hstring reference
        //    TestObject.StringProperty = test_string;

        //    // Out hstring from native->managed only creates System.String on demand
        //    var sp = TestObject.StringProperty;

        //    // Out hstring from managed->native always creates HString from System.String
        //    TestObject.CallForString(() => test_string2);

        //    // In hstring from native->managed only creates System.String on demand
        //    TestObject.StringPropertyChanged += (Class sender, string value) => sender.StringProperty2 = value;
        //    TestObject.RaiseStringChanged();
        //}

        [Benchmark]
        public void IntEventSource()
        {
            ClassWithMarshalingRoutines instance = new ClassWithMarshalingRoutines();

            int x = 0;
            int y = 1;
            int z;

            instance.IntProperty = x;
            instance.CallForInt(() => y);

            instance.IntPropertyChanged += (object sender, int value) => z = value;
            instance.RaiseIntChanged();
        }
    }
}
