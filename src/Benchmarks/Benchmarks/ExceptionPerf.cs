using BenchmarkComponent;
using BenchmarkDotNet.Attributes;
using System;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class ExceptionPerf
    {
        private ClassWithMultipleInterfaces instance;
        private ClassWithMarshalingRoutines marshalingInstance;
        private ProvideInt throwingDelegate;

        [GlobalSetup]
        public void Setup()
        {
            instance = new ClassWithMultipleInterfaces();
            marshalingInstance = new ClassWithMarshalingRoutines();
            throwingDelegate = static () => throw new InvalidOperationException();
        }

        [Benchmark]
        public int NativeInvalidArgument()
        {
            try
            {
                instance.ThrowInvalidArgument();
                return 0;
            }
            catch (ArgumentException exception)
            {
                return exception.HResult;
            }
        }

        [Benchmark]
        public int RestoredManagedException()
        {
            try
            {
                marshalingInstance.CallForInt(throwingDelegate);
                return 0;
            }
            catch (InvalidOperationException exception)
            {
                return exception.HResult;
            }
        }
    }
}
