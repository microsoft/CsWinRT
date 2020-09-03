using System;
using System.Reflection;
using Windows.Foundation.Metadata;

namespace MyTypes
{
    public delegate void ExampleDelegate(UInt32 value);
    public delegate int ExampleDelegateDouble(double newvalue);

    public class ExampleClass
    {
        public ExampleClass()
        {
        }

        public event ExampleDelegate SampleEvent;

        public ExampleClass(ExampleClass other)
        {
            other.MethodA();
        }

        public ExampleClass(int alpha)
        {
            MethodB(alpha, alpha);
        }

        public void MethodA()
        {
            System.Console.WriteLine("Yay!");
        }

        public int MethodB(int param1, double param2)
        {
            return 4;
        }

        public ExampleClass Get()
        {
            return null;
        }

        private static void HelperMethod(ExampleClass instance)
        {
            instance.MethodA();
        }

        public double ExampleDouble { get; set; }
    }

    public interface ITest
    {
        int GetTest();
    }

    [Version(3u)]
    public interface ITest2
    {
        [Version(5u)]
        double GetTest(int test);
        int GetTest2();
        double GetSetDouble { get; set; }
    }

    internal enum PrivateEnum
    {
        privatetest,
        privatetest2
    }

    public enum TestEnum
    {
        test,
        test2
    }

    public enum NumericEnum : int
    {
        five = -5,
        six = 6
    }

    public enum UnsignedEnum : uint
    {
        zero,
        one,
        two,
        three
    }

    public struct MyStruct
    {
        public int test, test3;
        public double test2;
    }

    public class Test : ITest2
    {
        public int GetSetInt { get; set; }
        public double GetSetDouble { get; set; }
        public int GetInt { get; }

        public double GetTest(int test)
        {
            return test;
        }

        public int GetTest2()
        {
            return 4;
        }
    }

    public class ExampleClass2 : ExampleClass
    {
        public void Delta()
        {
        }

        public int Method()
        {
            return 4;
        }

        public int Method2(int param1, double param2)
        {
            return 4;
        }

        public void Pen()
        {
        }

        public int Method3(double p1, double param2, int param3)
        {
            return 4;
        }

        public double Method4(double m4, double m5, int m6, int m7)
        {
            return 4;
        }
    }

    public class Test3 : Test
    {

    }


    public class Test8 : Windows.Foundation.IWwwFormUrlDecoderEntry
    {
        public string Name => throw new NotImplementedException();

        public string Value => throw new NotImplementedException();
    }

    public class NewTest4 : ITest4
    {
        public double GetSetDouble { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public event ExampleDelegate TestEvent;

        public double GetTest(int test)
        {
            throw new NotImplementedException();
        }

        public int GetTest2()
        {
            throw new NotImplementedException();
        }

        public int GetTest4()
        {
            throw new NotImplementedException();
        }
    }

    public interface ITest4 : ITest2
    {
        int GetTest4();

        public event ExampleDelegate TestEvent;
    }

    public class TestCustom
    {
        public TestCustom(int num)
        {
            _num = num;
        }

        public int GetNum()
        {
            return _num;
        }

        private int _num;
    }

    internal class SomeMoreCode
    {
        public static void Stuff()
        {
            const ExampleClass instance1 = null;
        }

        ExampleClass MyProp2
        {
            get { return null; }
        }
    }
}
