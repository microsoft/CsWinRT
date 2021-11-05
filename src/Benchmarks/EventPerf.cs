using BenchmarkComponent;
using BenchmarkDotNet.Attributes;
using System;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class EventPerf
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
        public object IntEventOverhead()
        {
            ClassWithMarshalingRoutines instance = new ClassWithMarshalingRoutines();
            int z;
            System.EventHandler<int> s = (object sender, int value) => z = value;
            return instance;
            GC.KeepAlive(s);
        }

        [Benchmark]
        public object AddIntEventToNewEventSource()
        {
            ClassWithMarshalingRoutines instance = new ClassWithMarshalingRoutines();
            int z;
            System.EventHandler<int> s = (object sender, int value) => z = value;
            instance.IntPropertyChanged += s;
            return instance;
            GC.KeepAlive(s);
        }

        [Benchmark]
        public object AddMultipleEventsToNewEventSource()
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
        public object AddAndInvokeIntEventOnNewEventSource()
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
        public int InvokeIntEvent()
        {
            instance.RaiseIntChanged();
            return z2;
        }

        [Benchmark]
        public int InvokeIntEventWithSenderCheck()
        {
            instance2.RaiseIntChanged();
            return z4;
        }

        [Benchmark]
        public object AddAndRemoveIntEventOnNewEventSource()
        {
            ClassWithMarshalingRoutines instance = new ClassWithMarshalingRoutines();
            int z;
            System.EventHandler<int> s = (object sender, int value) => z = value;
            instance.IntPropertyChanged += s;
            instance.IntPropertyChanged -= s;
            return instance;
            GC.KeepAlive(s);
        }

//        [Benchmark]
        public void AddIntEvent()
        {
            int z;
            instance.IntPropertyChanged += (object sender, int value) => z = value;
        }

//        [Benchmark]
        public void AddAndInvokeIntEvent()
        {
            int z;
            instance.IntPropertyChanged += (object sender, int value) => z = value;
            instance.RaiseIntChanged();
        }
    }
}