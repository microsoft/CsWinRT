using BenchmarkComponent;
using BenchmarkDotNet.Attributes;
using System;

using WinRT;
using System.Threading;

namespace Benchmarks
{
    class NonAgileClassCaller
    {
        public void AcquireObject()
        {
            // Assert.Equal(ApartmentState.STA, Thread.CurrentThread.GetApartmentState());
            nonAgileObject = new Windows.UI.Popups.PopupMenu();
            nonAgileObject.Commands.Add(new Windows.UI.Popups.UICommand("test"));
            nonAgileObject.Commands.Add(new Windows.UI.Popups.UICommand("test2"));
            // Assert.ThrowsAny<System.Exception>(() => nonAgileObject.As<IAgileObject>());

            agileReference = nonAgileObject.AsAgile();
            objectAcquired.Set();
            valueAcquired.WaitOne();

            // Object gets proxied to the apartment.
            // Assert.Equal(2, proxyObject.Commands.Count);
            agileReference.Dispose();

            proxyObject2 = agileReference2.Get();
        }

        public void CheckValue()
        {
            objectAcquired.WaitOne();
            // Assert.Equal(ApartmentState.MTA, Thread.CurrentThread.GetApartmentState());
            proxyObject = agileReference.Get();
            // Assert.Equal(2, proxyObject.Commands.Count);

            nonAgileObject2 = new Windows.UI.Popups.PopupMenu();
            agileReference2 = nonAgileObject2.AsAgile();

            valueAcquired.Set();
        }

        public void CallProxyObject()
        {
            // Call to a proxy object which we internally use an agile reference
            // to resolve after the apartment is gone should throw.
            //Assert.ThrowsAny<System.Exception>(() => proxyObject.Commands);
        }

        private Windows.UI.Popups.PopupMenu nonAgileObject, nonAgileObject2;
        private Windows.UI.Popups.PopupMenu proxyObject, proxyObject2;
        private AgileReference<Windows.UI.Popups.PopupMenu> agileReference, agileReference2;
        private readonly AutoResetEvent objectAcquired = new AutoResetEvent(false);
        private readonly AutoResetEvent valueAcquired = new AutoResetEvent(false);
    }

    /*
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
    */

    [MemoryDiagnoser]
    public class EventPerf
    {
        /*
        ClassWithMarshalingRoutines instance;
        int z2;
        ClassWithMarshalingRoutines instance2;
        int z4;
        ManagedEvents events;
        EventOperations operations;
        */

        [GlobalSetup]
        public void Setup()
        {
            /*
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
            */
        }

        [Benchmark]
        public void TestNonAgileObjectCall()
        {
            NonAgileClassCaller caller = new NonAgileClassCaller();
            Thread staThread = new Thread(new ThreadStart(caller.AcquireObject));
            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start();

            Thread mtaThread = new Thread(new ThreadStart(caller.CheckValue));
            mtaThread.SetApartmentState(ApartmentState.MTA);
            mtaThread.Start();
            mtaThread.Join();
            staThread.Join();

            // Spin another STA thread after the other 2 threads are done and try to
            // access one of the proxied objects.  They should fail as there is no context
            // to switch to in order to marshal it to the current apartment.
            Thread anotherStaThread = new Thread(new ThreadStart(caller.CallProxyObject));
            anotherStaThread.SetApartmentState(ApartmentState.STA);
            anotherStaThread.Start();
            anotherStaThread.Join();
        }

        /*
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
        */
    }
}