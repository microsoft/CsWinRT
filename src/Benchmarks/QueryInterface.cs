using BenchmarkComponent;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using System;
using System.Runtime.InteropServices;
using Windows.ApplicationModel.Chat;

namespace Benchmarks
{
    [MemoryDiagnoser]
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
            return instance.DefaultIntProperty;
        }

        [Benchmark]
        public int QueryNonDefaultInterface()
        {
            return instance.IntProperty;
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

        // The following 2 benchmarks try to benchmark the time taken for the first call
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
        public int StaticPropertyCall()
        {
            return Windows.System.Power.PowerManager.RemainingChargePercent;
        }

        [Benchmark]
        public int GetVector()
        {
            var list = new HierarchyA().GetList();
            var count = 0;
            foreach (var element in list)
            {
                count += (int)element;
            }
            return count;
        }

        [Benchmark]
        public int GetVectorInt()
        {
            var list = new HierarchyA().GetIntList();
            var count = 0;
            foreach (var element in list)
            {
                count += element;
            }
            return count;
        }

        [Benchmark]
        public int GetVectorXaml()
        {
            var list = new HierarchyA().GetXamlList();
            var count = 0;
            foreach (var element in list)
            {
                count += (int)element;
            }
            return count;
        }

        [Benchmark]
        public Guid GuidByte()
        {
            return new Guid(0xd57af411, 0x737b, 0xc042, 0xab, 0xae, 0x87, 0x8b, 0x1e, 0x16, 0xad, 0xee);
        }

        [Benchmark]
        public Guid GuidString()
        {
            return new Guid("d57af411-737b-c042-abae-878b1e16adee");
        }

        public struct C
        {
            public bool throwExcep;
            public bool isError;

            public void Work() 
            {
                if(throwExcep)
                {
                    throw new TypeAccessException();
                }
            }

            public void Dispose() 
            {
                if(throwExcep && isError)
                {
                    throw new FieldAccessException();
                }
            }
        }

        [Benchmark]
        public void WhenClause()
        {
            var c = new C();

            try
            {
                c.Work();
            }
            catch (Exception) when (dispose(ref c))
            {
            }

            static bool dispose(ref C c)
            {
                c.Dispose();
                return false;
            }
        }


        [Benchmark]
        public void FinallyClause()
        {
            var c = new C();
            bool success = false;

            try
            {
                c.Work();
                success = true;
            }
            finally
            {
                if(!success)
                {
                    c.Dispose();
                }
            }
        }

        [Benchmark]
        public void WhenClauseException()
        {
            try
            {
                WhenClauseException2();
            }
            catch (Exception) 
            {
            }
        }

        public void WhenClauseException2()
        {
            var c = new C();
            c.throwExcep = true;

            try
            {
                c.Work();
            }
            catch (Exception) when (dispose(ref c))
            {
            }

            static bool dispose(ref C c)
            {
                c.Dispose();
                return false;
            }
        }

        [Benchmark]
        public void FinallyClauseException()
        {
            try
            {
                FinallyClauseException2();
            }
            catch (Exception)
            {
            }
        }


        [Benchmark]
        public void FinallyClauseException2()
        {
            var c = new C();
            c.throwExcep = true;
            bool success = false;

            try
            {
                c.Work();
                success = true;
            }
            finally
            {
                if (!success)
                {
                    c.Dispose();
                }
            }
        }

    }
}