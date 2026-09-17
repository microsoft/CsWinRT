using BenchmarkDotNet.Attributes;
using BenchmarkComponent;
using System.Threading;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class NonAgileObjectPerf
    {
        private const int OperationsPerBatch = 128;

        AutoResetEvent createObject;
        AutoResetEvent exitThread;
        AutoResetEvent objectCreated;
        Thread staThread;
        private volatile Windows.UI.Popups.PopupMenu nonAgileObject;
        private volatile NonAgileClassWithMultipleInterfaces nonAgileBenchmarkObject;
        private volatile bool createBenchmarkObject;
        private volatile bool queryBenchmarkObjectOnWorker;
        private volatile int workerQueryResult;

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
                    if (queryBenchmarkObjectOnWorker)
                    {
                        int result = 0;

                        for (int i = 0; i < OperationsPerBatch; i++)
                        {
                            nonAgileBenchmarkObject = new NonAgileClassWithMultipleInterfaces();
                            result += QueryMultipleInterfaces();
                        }

                        workerQueryResult = result;
                    }
                    else
                    {
                        nonAgileBenchmarkObject = new NonAgileClassWithMultipleInterfaces();
                    }
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

        private int QueryMultipleInterfaces()
        {
            int result = nonAgileBenchmarkObject.DefaultIntProperty;
            result += nonAgileBenchmarkObject.IntProperty;
            result += nonAgileBenchmarkObject.BoolProperty ? 1 : 0;
            result += (int)nonAgileBenchmarkObject.DoubleProperty;

            return result;
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
            queryBenchmarkObjectOnWorker = false;
            createObject.Set();
            objectCreated.WaitOne();

            int result = QueryMultipleInterfaces();

            objectCreated.Reset();

            return result;
        }

        [Benchmark]
        public void ConstructNonAgileBenchmarkObject()
        {
            createBenchmarkObject = true;
            queryBenchmarkObjectOnWorker = false;
            createObject.Set();
            objectCreated.WaitOne();
            objectCreated.Reset();
        }

        [Benchmark(OperationsPerInvoke = OperationsPerBatch)]
        public int ConstructAndQueryMultipleNonAgileInterfacesInOwningApartment()
        {
            createBenchmarkObject = true;
            queryBenchmarkObjectOnWorker = true;
            createObject.Set();
            objectCreated.WaitOne();
            objectCreated.Reset();

            return workerQueryResult;
        }
    }
}