using BenchmarkComponent;
using BenchmarkDotNet.Attributes;
using System.Threading;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class MultiApartmentActivationFactoryPerf
    {
        private const int OperationsPerBatch = 128;

        private StaActivationWorker firstWorker;
        private StaActivationWorker secondWorker;
        private bool useSecondWorker;

        [GlobalSetup]
        public void Setup()
        {
            firstWorker = new StaActivationWorker();
            secondWorker = new StaActivationWorker();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            firstWorker.Dispose();
            secondWorker.Dispose();
        }

        [Benchmark(OperationsPerInvoke = OperationsPerBatch)]
        public void SingleApartmentConstruction()
        {
            firstWorker.ConstructBatch();
        }

        [Benchmark(OperationsPerInvoke = OperationsPerBatch)]
        public void AlternatingApartmentConstruction()
        {
            useSecondWorker = !useSecondWorker;
            (useSecondWorker ? secondWorker : firstWorker).ConstructBatch();
        }

        private sealed class StaActivationWorker
        {
            private readonly AutoResetEvent request = new(false);
            private readonly AutoResetEvent completed = new(false);
            private readonly Thread thread;
            private volatile bool exit;

            public StaActivationWorker()
            {
                thread = new Thread(Run);
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
            }

            public void ConstructBatch()
            {
                request.Set();
                completed.WaitOne();
            }

            public void Dispose()
            {
                exit = true;
                request.Set();
                thread.Join();
                request.Dispose();
                completed.Dispose();
            }

            private void Run()
            {
                while (request.WaitOne())
                {
                    if (exit)
                    {
                        return;
                    }

                    for (int i = 0; i < OperationsPerBatch; i++)
                    {
                        _ = new NonAgileClassWithMultipleInterfaces();
                    }
                    completed.Set();
                }
            }
        }
    }
}
