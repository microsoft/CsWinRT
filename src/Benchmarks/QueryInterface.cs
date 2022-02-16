using BenchmarkComponent;
using BenchmarkDotNet.Attributes;
using Windows.ApplicationModel.Chat;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class QueryInterfacePerf
    {
        ClassWithMultipleInterfaces instance;
        ChatMessage message;
        ClassWithFastAbi fastAbiInstance;

        [GlobalSetup]
        public void Setup()
        {
            instance = new ClassWithMultipleInterfaces();
            message = new ChatMessage();
            fastAbiInstance = new ClassWithFastAbi();
        }

        [Benchmark]
        public int QueryDefaultInterface()
        {
            return instance.DefaultIntProperty;
        }

        [Benchmark]
        public int QueryNonDefaultInterface()
        {
            return instance.IntProperty;
        }

        [Benchmark]
        public int QueryFastAbiDefaultInterface()
        {
            return fastAbiInstance.DefaultIntProperty;
        }

        [Benchmark]
        public int QueryFastAbiNonDefaultInterface()
        {
            return fastAbiInstance.NonDefaultIntProperty;
        }

        [Benchmark]
        public bool QueryNonDefaultInterface2()
        {
            return instance.BoolProperty;
        }

        [Benchmark]
        public void QueryDefaultInterfaceSetProperty()
        {
            instance.DefaultIntProperty = 4;
        }

        [Benchmark]
        public void QueryNonDefaultInterfaceSetProperty()
        {
            instance.IntProperty = 4;
        }

        [Benchmark]
        public bool QuerySDKDefaultInterface()
        {
            return message.IsForwardingDisabled;
        }

        [Benchmark]
        public bool QuerySDKNonDefaultInterface()
        {
            return message.IsSeen;
        }

        [Benchmark]
        public object DefaultObjectParameters()
        {
            instance.DefaultObjectProperty = new ClassWithMultipleInterfaces();
            return instance.DefaultObjectProperty;
        }

        [Benchmark]
        public object DefaultStringParameters()
        {
            instance.DefaultStringProperty = "Hello";
            return instance.DefaultStringProperty;
        }

        [Benchmark]
        public object DynamicCast()
        {
            return (ClassWithMarshalingRoutines)instance.NewObject();
        }

        // The following 4 benchmarks try to benchmark the time taken for the first call
        // rather than the mean time over several calls.  It has the overhead of the object
        // construction, but it can be used to track regressions to performance.
        [Benchmark]
        public int ConstructAndQueryDefaultInterfaceFirstCall()
        {
            ClassWithMultipleInterfaces instance2 = new ClassWithMultipleInterfaces();
            return instance2.DefaultIntProperty;
        }

        [Benchmark]
        public int ConstructAndQueryNonDefaultInterfaceFirstCall()
        {
            ClassWithMultipleInterfaces instance2 = new ClassWithMultipleInterfaces();
            return instance2.IntProperty;
        }

        [Benchmark]
        public int ConstructAndQueryFastAbiDefaultInterfaceFirstCall()
        {
            var instance2 = new ClassWithFastAbi();
            return instance2.DefaultIntProperty;
        }

        [Benchmark]
        public int ConstructAndQueryFastAbiNonDefaultInterfaceFirstCall()
        {
            var instance2 = new ClassWithFastAbi();
            return instance2.NonDefaultIntProperty;
        }

        [Benchmark]
        public int StaticPropertyCall()
        {
            return Windows.System.Power.PowerManager.RemainingChargePercent;
        }
    }
}