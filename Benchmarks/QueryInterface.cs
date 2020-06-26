using BenchmarkComponent;
using BenchmarkDotNet.Attributes;
using Windows.ApplicationModel.Chat;

namespace Benchmarks
{
    public class QueryInterfacePerf
    {
        ClassWithMultipleInterfaces instance;
        ChatMessage message;

        [GlobalSetup]
        public void Setup()
        {
            instance = new ClassWithMultipleInterfaces();
            message = new ChatMessage();
        }

        [Benchmark]
        public int QueryDefaultInterface()
        {
            return instance.Property1;
        }

        [Benchmark]
        public int QueryNonDefaultInterface()
        {
            return instance.IntProperty1;
        }

        [Benchmark]
        public bool QueryNonDefaultInterface2()
        {
            return instance.BoolProperty1;
        }

        [Benchmark]
        public void QueryDefaultInterfaceSetProperty()
        {
            instance.Property1 = 4;
        }

        [Benchmark]
        public void QueryNonDefaultInterfaceSetProperty()
        {
            instance.IntProperty1 = 4;
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

        // The following 2 benchmarks try to benchmark the time taken for the first call
        // rather than the mean time over several calls.  It has the overhead of the object
        // construction, but it can be used to track regressions to performance.
        [Benchmark]
        public int ConstructAndQueryDefaultInterfaceFirstCall()
        {
            ClassWithMultipleInterfaces instance2 = new ClassWithMultipleInterfaces();
            return instance2.Property1;
        }

        [Benchmark]
        public int ConstructAndQueryNonDefaultInterfaceFirstCall()
        {
            ClassWithMultipleInterfaces instance2 = new ClassWithMultipleInterfaces();
            return instance2.IntProperty1;
        }
    }
}