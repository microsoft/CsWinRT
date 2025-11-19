using BenchmarkComponent;
using BenchmarkDotNet.Attributes;
using System;
using System.Reflection;

#if !NETCOREAPP3_1
using WinRT;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class GuidPerf
    {
        [Benchmark]
        public object GetClassGuid()
        {
            return WinRT.GuidGenerator.GetIID(typeof(ClassWithMarshalingRoutines));
        }

        [Benchmark]
        public object GetDelegateGuid()
        {
            return WinRT.GuidGenerator.GetIID(typeof(Windows.Foundation.AsyncActionCompletedHandler));
        }

        [Benchmark]
        public object CreateListGuid()
        {
            return WinRT.GuidGenerator.CreateIID(typeof(System.Collections.Generic.IList<ClassWithMarshalingRoutines>));
        }

        [Benchmark]
        public object CreateDictionaryWithStringKeyGuid()
        {
            return WinRT.GuidGenerator.CreateIID(typeof(System.Collections.Generic.IDictionary<String, ClassWithMarshalingRoutines>));
        }

        [Benchmark]
        public object CreateDictionaryWithBoolKeyGuid()
        {
            return WinRT.GuidGenerator.CreateIID(typeof(System.Collections.Generic.IDictionary<bool, ClassWithMarshalingRoutines>));
        }

        [Benchmark]
        public object CreateReadOnlyEnumListGuid()
        {
            return WinRT.GuidGenerator.CreateIID(typeof(System.Collections.Generic.IReadOnlyList<Windows.Foundation.AsyncStatus>));
        }

        [Benchmark]
        public object CreateReadOnlyFlagEnumListGuid()
        {
            return WinRT.GuidGenerator.CreateIID(typeof(System.Collections.Generic.IReadOnlyList<Windows.Storage.FileAttributes>));
        }

        [Benchmark]
        public object CreateReadOnlyStructListGuid()
        {
            return WinRT.GuidGenerator.CreateIID(typeof(System.Collections.Generic.IReadOnlyList<NonBlittable>));
        }

        [Benchmark]
        public object CreateReadOnlyInterfaceListGuid()
        {
            return WinRT.GuidGenerator.CreateIID(typeof(System.Collections.Generic.IReadOnlyList<IIntProperties>));
        }

        [Benchmark]
        public object CreateReadOnlyClassListGuid()
        {
            return WinRT.GuidGenerator.CreateIID(typeof(System.Collections.Generic.IReadOnlyList<EventOperations>));
        }

        [Benchmark]
        public object CreateReadOnlyDelegateListGuid()
        {
            return WinRT.GuidGenerator.CreateIID(typeof(System.Collections.Generic.IReadOnlyList<ProvideInt>));
        }
    }
}
#endif