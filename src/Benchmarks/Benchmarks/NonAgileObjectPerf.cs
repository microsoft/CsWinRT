using BenchmarkDotNet.Attributes;
using BenchmarkComponent;
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
        private volatile NonAgileClassWithMultipleInterfaces nonAgileBenchmarkObject;
        private volatile bool createBenchmarkObject;

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
                if (createBenchmarkObject)
                {
                    nonAgileBenchmarkObject = new NonAgileClassWithMultipleInterfaces();
                }
                else
                {
                    nonAgileObject = new Windows.UI.Popups.PopupMenu();
                    CallObject();
                }
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
            createBenchmarkObject = false;
            createObject.Set();
            objectCreated.WaitOne();
            CallObject();
            objectCreated.Reset();
        }

        [Benchmark]
        public void ConstructNonAgileObject()
        {
            createBenchmarkObject = false;
            createObject.Set();
            objectCreated.WaitOne();
            objectCreated.Reset();
        }

        [Benchmark]
        public int ConstructAndQueryMultipleNonAgileInterfaces()
        {
            createBenchmarkObject = true;
            createObject.Set();
            objectCreated.WaitOne();

            int result = nonAgileBenchmarkObject.DefaultIntProperty;
            result += nonAgileBenchmarkObject.IntProperty;
            result += nonAgileBenchmarkObject.BoolProperty ? 1 : 0;
            result += (int)nonAgileBenchmarkObject.DoubleProperty;

            objectCreated.Reset();

            return result;
        }

        [Benchmark]
        public void ConstructNonAgileBenchmarkObject()
        {
            createBenchmarkObject = true;
            createObject.Set();
            objectCreated.WaitOne();
            objectCreated.Reset();
        }
    }
}