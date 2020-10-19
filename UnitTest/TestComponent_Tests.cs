using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TestComponent;  // Error CS0246? run get_testwinrt.cmd
using Windows.Foundation;
using WinRT;
using Xunit;

namespace UnitTest
{
    public class TestWinRT
    {
        public ITests Tests { get; private set; }

        public TestWinRT()
        {
            Tests = TestRunner.MakeTests();
        }

        public static bool AllEqual<T>(T x, params T[] list) => 
            list.All((y) => x.Equals(y));

        public static bool AllEqual<T>(T[] x, params T[][] list) =>
            list.All((y) => x.SequenceEqual(y));

        public static bool SequencesEqual<T>(IEnumerable<T> x, params IEnumerable<T>[] list) =>
            list.All((y) => x.SequenceEqual(y));

        [Fact]
        public void Params_Bool()
        {
            bool a = true;
            bool b;
            bool c = Tests.Param1(a, out b);
            Assert.True(b && c);
        }

        [Fact]
        public void Params_Byte()
        {
            byte a = 123;
            byte b;
            byte c = Tests.Param2(a, out b);
            Assert.True(a == b && a == c);
        }

        [Fact]
        public void Params_UInt16()
        {
            UInt16 a = 123;
            UInt16 b;
            UInt16 c = Tests.Param3(a, out b);
            Assert.True(a == b && a == c);
        }

        [Fact]
        public void Params_UInt32()
        {
            UInt32 a = 123;
            UInt32 b;
            UInt32 c = Tests.Param4(a, out b);
            Assert.True(a == b && a == c);
        }

        [Fact]
        public void Params_UInt64()
        {
            UInt64 a = 123;
            UInt64 b;
            UInt64 c = Tests.Param5(a, out b);
            Assert.True(a == b && a == c);
        }

        [Fact]
        public void Params_Int16()
        {
            Int16 a = 123;
            Int16 b;
            Int16 c = Tests.Param6(a, out b);
            Assert.True(a == b && a == c);
        }

        [Fact]
        public void Params_Int32()
        {
            Int32 a = 123;
            Int32 b;
            Int32 c = Tests.Param7(a, out b);
            Assert.True(a == b && a == c);
        }

        [Fact]
        public void Params_Int64()
        {
            Int64 a = 123;
            Int64 b;
            Int64 c = Tests.Param8(a, out b);
            Assert.True(a == b && a == c);
        }

        [Fact]
        public void Params_Float()
        {
            float a = 12.3f;
            float b;
            float c = Tests.Param9(a, out b);
            Assert.True(a == b && a == c);
        }

        [Fact]
        public void Params_Double()
        {
            double a = 12.3;
            double b;
            double c = Tests.Param10(a, out b);
            Assert.True(a == b && a == c);
        }

        [Fact]
        public void Params_Char()
        {
            char a = 'W';
            char b;
            char c = Tests.Param11(a, out b);
            Assert.True(a == b && a == c);
        }

        [Fact]
        public void Params_String()
        {
            string a = "WinRT";
            string b;
            string c = Tests.Param12(a, out b);
            Assert.True(a == b && a == c);
        }

        [Fact]
        public void Params_Blittable()
        {
            Blittable a = new Blittable(1, 2, 3, 4, -5, -6, -7, 8.0f, 9.0, typeof(ITests).GUID);
            Blittable b;
            Blittable c = Tests.Param13(a, in a, out b);
            Assert.True(AllEqual(a, b, c));
        }

        [Fact]
        public void Params_NonBlittable()
        {
            NonBlittable a = new NonBlittable(false, 'X', "WinRT", (long?)PropertyValue.CreateInt64(1234));
            NonBlittable b;
            NonBlittable c = Tests.Param14(a, in a, out b);
            Assert.True(AllEqual(a, b, c));
        }

        [Fact]
        public void Params_Nested()
        {
            Nested a = new Nested(
                new Blittable(1, 2, 3, 4, -5, -6, -7, 8.0f, 9.0, typeof(ITests).GUID),
                new NonBlittable(false, 'X', "WinRT", (long?)PropertyValue.CreateInt64(1234)));
            Nested b;
            Nested c = Tests.Param15(a, in a, out b);
            Assert.True(AllEqual(a, b, c));
        }

        [Fact]
        public void Params_Bool_Call()
        {
            Tests.Param1Call((bool a, out bool b) => { b = a; return a; });
        }

        [Fact]
        public void Params_Byte_Call()
        {
            Tests.Param2Call((byte a, out byte b) => { b = a; return a; });
        }

        [Fact]
        public void Params_UInt16_Call()
        {
            Tests.Param3Call((UInt16 a, out UInt16 b) => { b = a; return a; });
        }

        [Fact]
        public void Params_UInt32_Call()
        {
            Tests.Param4Call((UInt32 a, out UInt32 b) => { b = a; return a; });
        }

        [Fact]
        public void Params_UInt64_Call()
        {
            Tests.Param5Call((UInt64 a, out UInt64 b) => { b = a; return a; });
        }

        [Fact]
        public void Params_Int16_Call()
        {
            Tests.Param6Call((Int16 a, out Int16 b) => { b = a; return a; });
        }

        [Fact]
        public void Params_Int32_Call()
        {
            Tests.Param7Call((Int32 a, out Int32 b) => { b = a; return a; });
        }

        [Fact]
        public void Params_Int64_Call()
        {
            Tests.Param8Call((Int64 a, out Int64 b) => { b = a; return a; });
        }

        [Fact]
        public void Params_Float_Call()
        {
            Tests.Param9Call((float a, out float b) => { b = a; return a; });
        }

        [Fact]
        public void Params_Double_Call()
        {
            Tests.Param10Call((double a, out double b) => { b = a; return a; });
        }

        [Fact]
        public void Params_Char_Call()
        {
            Tests.Param11Call((char a, out char b) => { b = a; return a; });
        }

        [Fact]
        public void Params_String_Call()
        {
            Tests.Param12Call((string a, out string b) => { b = a; return a; });
        }

        [Fact]
        public void Params_Blittable_Call()
        {
            Tests.Param13Call((Blittable a, in Blittable b, out Blittable c) => { c = a; return a; });
        }

        [Fact]
        public void Params_NonBlittable_Call()
        {
            Tests.Param14Call((NonBlittable a, in NonBlittable b, out NonBlittable c) => { c = a; return a; });
        }

        [Fact]
        public void Params_Nested_Call()
        {
            Tests.Param15Call((Nested a, in Nested b, out Nested c) => { c = a; return a; });
        }

        [Fact]
        public void Array_Bool()
        {
            bool[] a = new bool[] { true, false, true };
            bool[] b = new bool[a.Length];
            bool[] c;
            bool[] d = Tests.Array1(a, b, out c);
            Assert.True(AllEqual(a, b, c, d));
        }

        [Fact]
        public void Array_Byte()
        {
            byte[] a = new byte[] { 1, 2, 3 };
            byte[] b = new byte[a.Length];
            byte[] c;
            byte[] d = Tests.Array2(a, b, out c);
            Assert.True(AllEqual(a, b, c, d));
        }

        [Fact]
        public void Array_UInt16()
        {
            UInt16[] a = new UInt16[] { 1, 2, 3 };
            UInt16[] b = new UInt16[a.Length];
            UInt16[] c;
            UInt16[] d = Tests.Array3(a, b, out c);
            Assert.True(AllEqual(a, b, c, d));
        }

        [Fact]
        public void Array_UInt32()
        {
            UInt32[] a = new UInt32[] { 1, 2, 3 };
            UInt32[] b = new UInt32[a.Length];
            UInt32[] c;
            UInt32[] d = Tests.Array4(a, b, out c);
            Assert.True(AllEqual(a, b, c, d));
        }

        [Fact]
        public void Array_UInt64()
        {
            UInt64[] a = new UInt64[] { 1, 2, 3 };
            UInt64[] b = new UInt64[a.Length];
            UInt64[] c;
            UInt64[] d = Tests.Array5(a, b, out c);
            Assert.True(AllEqual(a, b, c, d));
        }

        [Fact]
        public void Array_Int16()
        {
            Int16[] a = new Int16[] { 1, 2, 3 };
            Int16[] b = new Int16[a.Length];
            Int16[] c;
            Int16[] d = Tests.Array6(a, b, out c);
            Assert.True(AllEqual(a, b, c, d));
        }

        [Fact]
        public void Array_Int32()
        {
            Int32[] a = new Int32[] { 1, 2, 3 };
            Int32[] b = new Int32[a.Length];
            Int32[] c;
            Int32[] d = Tests.Array7(a, b, out c);
            Assert.True(AllEqual(a, b, c, d));
        }

        [Fact]
        public void Array_Int64()
        {
            Int64[] a = new Int64[] { 1, 2, 3 };
            Int64[] b = new Int64[a.Length];
            Int64[] c;
            Int64[] d = Tests.Array8(a, b, out c);
            Assert.True(AllEqual(a, b, c, d));
        }

        [Fact]
        public void Array_Float()
        {
            float[] a = new float[] { 1.0f, 2.0f, 3.0f };
            float[] b = new float[a.Length];
            float[] c;
            float[] d = Tests.Array9(a, b, out c);
            Assert.True(AllEqual(a, b, c, d));
        }

        [Fact]
        public void Array_Double()
        {
            double[] a = new double[] { 1.0, 2.0, 3.0 };
            double[] b = new double[a.Length];
            double[] c;
            double[] d = Tests.Array10(a, b, out c);
            Assert.True(AllEqual(a, b, c, d));
        }

        [Fact]
        public void Array_Char()
        {
            char[] a = new char[] { 'a', 'b', 'c' };
            char[] b = new char[a.Length];
            char[] c;
            char[] d = Tests.Array11(a, b, out c);
            Assert.True(AllEqual(a, b, c, d));
        }

        [Fact]
        public void Array_String()
        {
            string[] a = new string[] { "apples", "oranges", "pears" };
            string[] b = new string[a.Length];
            string[] c;
            string[] d = Tests.Array12(a, b, out c);
            Assert.True(AllEqual(a, b, c, d));
        }

        [Fact]
        public void Array_Blittable()
        {
            Blittable[] a = new Blittable[] {
                new Blittable(1, 2, 3, 4, -5, -6, -7, 8.0f, 9.0, typeof(ITests).GUID),
                new Blittable(10, 20, 30, 40, -50, -60, -70, 80.0f, 90.0, typeof(IStringable).GUID)
            };
            Blittable[] b = new Blittable[a.Length];
            Blittable[] c;
            Blittable[] d = Tests.Array13(a, b, out c);
            Assert.True(AllEqual(a, b, c, d));
        }

        [Fact]
        public void Array_NonBlittable()
        {
            NonBlittable[] a = new NonBlittable[] {
                new NonBlittable(false, 'X', "First", (long?)PropertyValue.CreateInt64(123)),
                new NonBlittable(true, 'Y', "Second", (long?)PropertyValue.CreateInt64(456)),
                new NonBlittable(false, 'Z', "Third", (long?)PropertyValue.CreateInt64(789))
            };
            NonBlittable[] b = new NonBlittable[a.Length];
            NonBlittable[] c;
            NonBlittable[] d = Tests.Array14(a, b, out c);
            Assert.True(AllEqual(a, b, c, d));
        }

        [Fact]
        public void Array_Nested()
        {
            Nested[] a = new Nested[]{
                new Nested(
                    new Blittable(1, 2, 3, 4, -5, -6, -7, 8.0f, 9.0, typeof(ITests).GUID),
                    new NonBlittable(false, 'X', "First", (long?)PropertyValue.CreateInt64(123))),
                new Nested(
                    new Blittable(10, 20, 30, 40, -50, -60, -70, 80.0f, 90.0, typeof(IStringable).GUID),
                    new NonBlittable(true, 'Y', "Second", (long?)PropertyValue.CreateInt64(456))),
                new Nested(
                    new Blittable(1, 2, 3, 4, -5, -6, -7, 8.0f, 9.0, typeof(IInspectable).GUID),
                    new NonBlittable(false, 'Z', "Third", (long?)PropertyValue.CreateInt64(789)))
            };
            Nested[] b = new Nested[a.Length];
            Nested[] c;
            Nested[] d = Tests.Array15(a, b, out c);
            Assert.True(AllEqual(a, b, c, d));
        }

        [Fact]
        public void Array_Stringable()
        {
            IStringable[] a = new IStringable[] {
                Windows.Data.Json.JsonValue.CreateNumberValue(3),
                Windows.Data.Json.JsonValue.CreateNumberValue(4),
                Windows.Data.Json.JsonValue.CreateNumberValue(5.0)
            };
            IStringable[] b = new IStringable[a.Length];
            IStringable[] c;
            IStringable[] d = Tests.Array16(a, b, out c);
            Assert.True(AllEqual(a, b, c, d));
        }

        private T[] Array_Call<T>(T[] a, T[] b, out T[] c)
        {
            Assert.True(a.Length == b.Length);
            a.CopyTo(b, 0);
            c = (T[])a.Clone();
            return a;
        }

        [Fact]
        public void Array_Bool_Call()
        {
            Tests.Array1Call(Array_Call);
        }

        [Fact]
        public void Array_Byte_Call()
        {
            Tests.Array2Call(Array_Call);
        }

        [Fact]
        public void Array_UInt16_Call()
        {
            Tests.Array3Call(Array_Call);
        }

        [Fact]
        public void Array_UInt32_Call()
        {
            Tests.Array4Call(Array_Call);
        }

        [Fact]
        public void Array_UInt64_Call()
        {
            Tests.Array5Call(Array_Call);
        }

        [Fact]
        public void Array_Int16_Call()
        {
            Tests.Array6Call(Array_Call);
        }

        [Fact]
        public void Array_Int32_Call()
        {
            Tests.Array7Call(Array_Call);
        }

        [Fact]
        public void Array_Int64_Call()
        {
            Tests.Array8Call(Array_Call);
        }

        [Fact]
        public void Array_Float_Call()
        {
            Tests.Array9Call(Array_Call);
        }

        [Fact]
        public void Array_Double_Call()
        {
            Tests.Array10Call(Array_Call);
        }

        [Fact]
        public void Array_Char_Call()
        {
            Tests.Array11Call(Array_Call);
        }

        [Fact]
        public void Array_String_Call()
        {
            Tests.Array12Call(Array_Call);
        }

        [Fact]
        public void Array_Blittable_Call()
        {
            Tests.Array13Call(Array_Call);
        }

        [Fact]
        public void Array_NonBlittable_Call()
        {
            Tests.Array14Call(Array_Call);
        }

        [Fact]
        public void Array_Nested_Call()
        {
            Tests.Array15Call(Array_Call);
        }

        [Fact]
        public void Array_Stringable_Call()
        {
            Tests.Array16Call(Array_Call);
        }

        [Fact]
        public void Collections_IEnumerable()
        {
            string[] a = new string[] { "apples", "oranges", "pears" };
            IEnumerable<string> b = null;
            var c = Tests.Collection1(a, out b);
            Assert.True(SequencesEqual(a, b, c));
        }

        [Fact]
        public void Collections_IEnumerable_Pair()
        {
            var a = new KeyValuePair<string, string>[] {
                new KeyValuePair<string,string>("apples", "1"),
                new KeyValuePair<string,string>("oranges", "2"),
                new KeyValuePair<string,string>("pears", "3")
            };
            IEnumerable<KeyValuePair<string, string>> b = null;
            var c = Tests.Collection2(a, out b);
            Assert.True(SequencesEqual(a, b, c));
        }

        [Fact]
        public void Collections_Dictionary()
        {
            var a = new Dictionary<string, string>()
            {
                ["apples"] = "1",
                ["oranges"] = "2",
                ["pears"] = "3"
            };
            IDictionary<string, string> b = null;
            var c = Tests.Collection3(a, out b);
            Assert.True(SequencesEqual(a, b, c));
        }

        [Fact]
        public void Collections_ReadOnly_Dictionary()
        {
            var a = new Dictionary<string, string>()
            {
                ["apples"] = "1",
                ["oranges"] = "2",
                ["pears"] = "3"
            };
            IReadOnlyDictionary<string, string> b = null;
            var c = Tests.Collection4(a, out b);
            Assert.True(SequencesEqual(a, b, c));
        }

        [Fact]
        public void Collections_List()
        {
            string[] a = new string[] { "apples", "oranges", "pears" };
            IList<string> b = null;
            var c = Tests.Collection5(a, out b);
            Assert.True(SequencesEqual(a, b, c));
        }

        [Fact]
        public void CastListToEnum_String()
        {
            string[] a = new string[] { "apples", "oranges", "pears" };
            IList<string> b = null;
            var c = Tests.Collection5(a, out b);
            var j = (IEnumerable<string>)(object)b;
            Assert.True(SequencesEqual(a, b, j));
        }

        [Fact]
        public void Collections_ReadOnly_List()
        {
            string[] a = new string[] { "apples", "oranges", "pears" };
            IReadOnlyList<string> b = null;
            var c = Tests.Collection6(a, out b);
            Assert.True(SequencesEqual(a, b, c));
        }

        [Fact]
        public void Collections_IEnumerable_Call()
        {
            Tests.Collection1Call((IEnumerable<string> a, out IEnumerable<string> b) =>
            {
                b = a.Select(s => s);
                return b.Select(s => s);
            });
        }

        [Fact]
        public void Collections_IEnumerable_Pair_Call()
        {
            Tests.Collection2Call((IEnumerable<KeyValuePair<string, string>> a, out IEnumerable<KeyValuePair<string, string>> b) =>
            {
                b = a.Select(s => s);
                return b.Select(s => s);
            });
        }

        [Fact]
        public void Collections_Dictionary_Call()
        {
            Tests.Collection3Call((IDictionary<string, string> a, out IDictionary<string, string> b) =>
            {
                b = new Dictionary<string, string>(a);
                return new Dictionary<string, string>(b);
            });
        }

        [Fact]
        public void Collections_ReadOnly_Dictionary_Call()
        {
            Tests.Collection4Call((IReadOnlyDictionary<string, string> a, out IReadOnlyDictionary<string, string> b) =>
            {
                b = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(a));
                return new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(b));
            });
        }

        [Fact]
        public void Collections_List_Call()
        {
            Tests.Collection5Call((IList<string> a, out IList<string> b) =>
            {
                b = a.Select(s => s).ToList();
                return b.Select(s => s).ToList();
            });
        }

        [Fact]
        public void Collections_ReadOnly_List_Call()
        {
            Tests.Collection6Call((IReadOnlyList<string> a, out IReadOnlyList<string> b) =>
            {
                b = a.Select(s => s).ToList();  
                return b.Select(s => s).ToList();
            });
        }

        // Nota Bene: this test case must always remain the final one
        [Fact]
        public void Z_Check_Coverage()
        {
            Tests.Simple();
            //Assert.Equal((double)Tests.Percentage, (double)100);
        }
    }
}
