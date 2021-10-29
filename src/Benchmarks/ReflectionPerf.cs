using BenchmarkComponent;
using BenchmarkDotNet.Attributes;
using System;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class ReflectionPerf
    {
        ClassWithMarshalingRoutines instance;
        int z2;
        ClassWithMarshalingRoutines instance2;
        int z4;

        [GlobalSetup]
        public void Setup()
        {
            instance = new ClassWithMarshalingRoutines();
            System.EventHandler<int> s = (object sender, int value) => z2 = value;
            instance.IntPropertyChanged += s;

            instance2 = new ClassWithMarshalingRoutines();
            System.EventHandler<int> s2 = (object sender, int value) =>
            {
                if (sender == this)
                    z4 = value;
                else
                    z4 = value * 3;
            };
            instance2.IntPropertyChanged += s2;
        }

        [Benchmark]
        public object ExecuteMarshalingForNewKeyValuePair()
        {
            return instance.NewTypeErasedKeyValuePairObject;
        }

        [Benchmark]
        public object ExecuteMarshalingForNewArray()
        {
            return instance.NewTypeErasedArrayObject;
        }

        [Benchmark]
        public object ExecuteMarshalingForNewNullable()
        {
            return instance.NewTypeErasedNullableObject;
        }

        [Benchmark]
        public object ExecuteMarshalingForExistingKeyvaluePair()
        {
            return instance.ExistingTypeErasedKeyValuePairObject;
        }

        [Benchmark]
        public object ExecuteMarshalingForExistingArray()
        {
            return instance.ExistingTypeErasedArrayObject;
        }

        [Benchmark]
        public object ExecuteMarshalingForExistingNullable()
        {
            return instance.ExistingTypeErasedNullableObject;
        }

        [Benchmark]
        public object ExecuteMarshalingForString()
        {
            return instance.DefaultStringProperty;
        }

        [Benchmark]
        public object ExecuteMarshalingForCustomObject()
        {
            return instance.NewWrappedClassObject;
        }

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

        [Benchmark]
        public object IntEventSource2Overhead()
        {
            ClassWithMarshalingRoutines instance = new ClassWithMarshalingRoutines();
            int z;
            System.EventHandler<int> s = (object sender, int value) => z = value;
            return instance;
            GC.KeepAlive(s);
        }

        [Benchmark]
        public object IntEventSource2()
        {
            ClassWithMarshalingRoutines instance = new ClassWithMarshalingRoutines();
            int z;
            System.EventHandler<int> s = (object sender, int value) => z = value;
            instance.IntPropertyChanged += s;
            return instance;
            GC.KeepAlive(s);
        }

        [Benchmark]
        public object IntEventSource2Multiple()
        {
            ClassWithMarshalingRoutines instance = new ClassWithMarshalingRoutines();
            int z;
            double y;
            System.EventHandler<int> s = (object sender, int value) => z = value;
            instance.IntPropertyChanged += s;
            System.EventHandler<double> t = (object sender, double value) => y = value;
            instance.DoublePropertyChanged += t;
            return instance;
            GC.KeepAlive(s);
            GC.KeepAlive(t);
        }

        [Benchmark]
        public object IntEventSource2Invoke()
        {
            ClassWithMarshalingRoutines instance = new ClassWithMarshalingRoutines();
            int z;
            System.EventHandler<int> s = (object sender, int value) => z = value;
            instance.IntPropertyChanged += s;
            instance.RaiseIntChanged();
            return instance;
            GC.KeepAlive(s);
        }

        [Benchmark]
        public int IntEventSource2InvokeOnly()
        {
            instance.RaiseIntChanged();
            return z2;
        }

        [Benchmark]
        public int IntEventSource2InvokeOnlyWithSenderCheck()
        {
            instance2.RaiseIntChanged();
            return z4;
        }

        [Benchmark]
        public object IntEventSource2Remove()
        {
            ClassWithMarshalingRoutines instance = new ClassWithMarshalingRoutines();
            int z;
            System.EventHandler<int> s = (object sender, int value) => z = value;
            instance.IntPropertyChanged += s;
            instance.IntPropertyChanged -= s;
            return instance;
            GC.KeepAlive(s);
        }

        [Benchmark]
        public void IntEventSource4()
        {
            int z;
            instance.IntPropertyChanged += (object sender, int value) => z = value;
        }

        [Benchmark]
        public void IntEventSource5()
        {
            int z;
            instance.IntPropertyChanged += (object sender, int value) => z = value;
            instance.RaiseIntChanged();
        }
    }
}
