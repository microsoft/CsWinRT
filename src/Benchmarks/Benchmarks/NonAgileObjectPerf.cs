using BenchmarkDotNet.Attributes;
using System.Threading;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class NonAgileObjectPerf
    {
        AutoResetEvent createObject;
        AutoResetEvent exitThread;
        AutoResetEvent objectCreated;
        Thread staThread;
        private volatile Windows.UI.Popups.PopupMenu nonAgileObject;

        [GlobalSetup]
        public void Setup()
        {
            createObject = new AutoResetEvent(false);
            exitThread = new AutoResetEvent(false);
            objectCreated = new AutoResetEvent(false);
            staThread = new Thread(new ThreadStart(ObjectAllocationLoop));
            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            exitThread.Set();
            createObject.Set();
        }

        private void ObjectAllocationLoop()
        {
            while (createObject.WaitOne() && !exitThread.WaitOne(1))
            {
                createObject.Reset();
                nonAgileObject = new Windows.UI.Popups.PopupMenu();
                CallObject();
                objectCreated.Set();
            }
        }

        private int CallObject()
        {
            return nonAgileObject.Commands.Count;
        }

        [Benchmark]
        public void ConstructAndQueryNonAgileObject()
        {
            createObject.Set();
            objectCreated.WaitOne();
            CallObject();
            objectCreated.Reset();
        }

        [Benchmark]
        public void ConstructNonAgileObject()
        {
            createObject.Set();
            objectCreated.WaitOne();
            objectCreated.Reset();
        }
    }
}