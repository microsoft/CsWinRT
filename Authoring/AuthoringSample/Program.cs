using System;
using System.Reflection;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using WinRT;

namespace AuthoringSample
{
    public enum BasicEnum
    {
        First = -1,
        Second = 0,
        Third = 1,
        Fourth
    }

    [Flags]
    public enum FlagsEnum : uint
    {
        First = 0,
        Second = 1,
        Third = 2,
        Fourth = 4
    }

    internal enum PrivateEnum
    {
        PrivateFirst,
        PrivateSecond
    }

    public delegate void BasicDelegate(uint value);
    public delegate bool ComplexDelegate(double value, int value2);

    public sealed class BasicClass
    {
        private BasicEnum basicEnum = BasicEnum.First;
        private FlagsEnum flagsEnum = FlagsEnum.Second | FlagsEnum.Third;

        public event ComplexDelegate ComplexDelegateEvent;
        private event ComplexDelegate ComplexDelegateEvent2;

        public Point GetPoint()
        {
            Point p = new Point
            {
                X = 2,
                Y = 3
            };
            return p;
        }

        public CustomWWW GetCustomWWW()
        {
            return new CustomWWW();
        }

        public BasicStruct GetBasicStruct()
        {
            BasicStruct basicStruct;
            basicStruct.X = 4;
            basicStruct.Y = 8;
            basicStruct.Value = "CsWinRT";
            return basicStruct;
        }

        public int GetSumOfInts(BasicStruct basicStruct)
        {
            return basicStruct.X + basicStruct.Y;
        }

        public ComplexStruct GetComplexStruct()
        {
            ComplexStruct complexStruct;
            complexStruct.X = 12;
            complexStruct.Val = true;
            complexStruct.BasicStruct = GetBasicStruct();
            return complexStruct;
        }

        public int? GetX(ComplexStruct basicStruct)
        {
            return basicStruct.X;
        }

        public void SetBasicEnum(BasicEnum basicEnum)
        {
            this.basicEnum = basicEnum;
        }

        public BasicEnum GetBasicEnum()
        {
            return basicEnum;
        }

        public void SetFlagsEnum(FlagsEnum flagsEnum)
        {
            this.flagsEnum = flagsEnum;
        }

        public FlagsEnum GetFlagsEnum()
        {
            return flagsEnum;
        }

        public BasicClass ReturnParameter(BasicClass basicClass)
        {
            return basicClass;
        }

        private void PrivateFunction()
        {
        }
    }

    public struct BasicStruct
    {
        public int X, Y;
        public string Value;
    }

    public struct ComplexStruct
    {
        public int? X;
        public bool? Val;
        public BasicStruct BasicStruct;
    }

    public class CustomWWW : IWwwFormUrlDecoderEntry
    {
        public string Name => "CustomWWW";

        public string Value => "CsWinRT";
    }

    [Version(3u)]
    public interface IDouble
    {
        double GetDouble();
        double GetDouble(bool ignoreFactor);
    }

    public interface IAnotherInterface
    {
        event ComplexDelegate ComplexDelegateEvent;

        bool FireComplexDelegate(double value, int value2);

        [Version(5u)]
        int GetThree();
    }

    public sealed class TestClass : IDouble, IAnotherInterface
    {
        public event BasicDelegate BasicDelegateEvent, BasicDelegateEvent2;
        public event ComplexDelegate ComplexDelegateEvent;

        public int Factor { get; set; }
        private int Factor2 { get; set; }
        public uint DelegateValue { get; set; }

        public TestClass()
        {
            Factor = 1;
            Factor2 = 1;
            BasicDelegateEvent += TestClass_BasicDelegateEvent;
        }

        private void TestClass_BasicDelegateEvent(uint value)
        {
            DelegateValue = value;
        }

        // Factory

        public TestClass(int factor)
        {
            Factor = factor;
        }

        // Statics
        public static int GetDefaultFactor()
        {
            return 1;
        }

        public static int GetDefaultNumber()
        {
            return 2;
        }

        // Default interface

        public void FireBasicDelegate(uint value)
        {
            BasicDelegateEvent.Invoke(value);
        }

        public void FireBasicDelegate2(uint value)
        {
            BasicDelegateEvent2.Invoke(value);
        }

        public int GetFactor()
        {
            return Factor;
        }

        // Method overloading

        public int GetNumber()
        {
            return 2 * Factor;
        }

        public int GetNumber(bool ignoreFactor)
        {
            return ignoreFactor ? 2 : GetNumber();
        }

        public int GetNumberWithDelta(bool ignoreFactor, int delta)
        {
            return delta + (ignoreFactor ? 2 : GetNumber());
        }

        // Implementing interface

        public double GetDouble()
        {
            return 2.0 * Factor;
        }

        public double GetDouble(bool ignoreFactor)
        {
            return ignoreFactor ? 2.0 : GetNumber();
        }

        // Implementing another interface

        public bool FireComplexDelegate(double value, int value2)
        {
            return ComplexDelegateEvent.Invoke(value, value2);
        }

        public int GetThree()
        {
            return 3;
        }

    }

    internal class InternalClass
    {
        public static void Get()
        {
        }
    }
}