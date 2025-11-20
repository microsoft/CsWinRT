using BenchmarkComponent;
using BenchmarkDotNet.Attributes;
using System;

namespace Benchmarks
{
    public class ManagedEvents : IEvents
    {
        public event EventHandler<double> DoublePropertyChanged;
        public event EventHandler<int> IntPropertyChanged;

        public void RaiseDoubleChanged()
        {
            DoublePropertyChanged.Invoke(this, 2.2);
        }

        public void RaiseIntChanged()
        {
            IntPropertyChanged.Invoke(this, 4);
        }
    }

    [MemoryDiagnoser]
    public class EventPerf
    {
        ClassWithMarshalingRoutines instance;
        int z2;
        ClassWithMarshalingRoutines instance2;
        int z4;
        ManagedEvents events;
        EventOperations operations;


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

            events = new ManagedEvents();
            operations = new EventOperations(events);
            operations.AddIntEvent();
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
        }

        [Benchmark]
        public object AddAndInvokeMultipleIntEventsToSameEventSource()
        {
            ClassWithMarshalingRoutines instance = new ClassWithMarshalingRoutines();
            int z;
            System.EventHandler<int> s = (object sender, int value) => z = value;
            System.EventHandler<int> t = (object sender, int value) => z = value * 2;
            System.EventHandler<int> u = (object sender, int value) => z = value * 3;
            instance.IntPropertyChanged += s;
            instance.IntPropertyChanged += t;
            instance.IntPropertyChanged += u;
            instance.RaiseIntChanged();
            return instance;
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

        [Benchmark]
        public object NativeIntEventOverhead()
        {
            ManagedEvents events = new ManagedEvents();
            EventOperations operations = new EventOperations(events);
            return operations;
        }

        [Benchmark]
        public object AddNativeIntEventToNewEventSource()
        {
            ManagedEvents events = new ManagedEvents();
            EventOperations operations = new EventOperations(events);
            operations.AddIntEvent();
            return operations;
        }

        [Benchmark]
        public object AddMultipleNativeEventsToNewEventSource()
        {
            ManagedEvents events = new ManagedEvents();
            EventOperations operations = new EventOperations(events);
            operations.AddIntEvent();
            operations.AddDoubleEvent();
            return operations;
        }

        [Benchmark]
        public object AddAndInvokeNativeIntEventOnNewEventSource()
        {
            ManagedEvents events = new ManagedEvents();
            EventOperations operations = new EventOperations(events);
            operations.AddIntEvent();
            operations.FireIntEvent();
            return operations;
        }

        [Benchmark]
        public void InvokeNativeIntEvent()
        {
            operations.FireIntEvent();
        }

        [Benchmark]
        public object AddAndRemoveNativeIntEventOnNewEventSource()
        {
            ManagedEvents events = new ManagedEvents();
            EventOperations operations = new EventOperations(events);
            operations.AddIntEvent();
            operations.RemoveIntEvent();
            return operations;
        }
    }
}