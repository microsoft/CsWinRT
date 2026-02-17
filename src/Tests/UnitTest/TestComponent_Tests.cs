using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using test_component_base;
using test_component_derived.Nested;
using TestComponent;  // Error CS0246? run get_testwinrt.cmd
using Windows.Foundation;
using WindowsRuntime;
using WindowsRuntime.InteropServices;

namespace UnitTest
{
    [TestClass]
    public class TestWinRT
    {
        public ITests Tests { get; private set; } = TestRunner.MakeTests();

        // TestInitialize does not work with AOT
        // See https://devblogs.microsoft.com/dotnet/testing-your-native-aot-dotnet-apps/
        // [TestInitialize]
        // public void Initialize()
        // {
        //     Tests = TestRunner.MakeTests();
        // }

        public static bool AllEqual<T>(T x, params T[] list) =>
            list.All(y => x.Equals(y));

        public static bool AllEqual<T>(T[] x, params T[][] list) =>
            list.All(y => x.SequenceEqual(y));

        public static bool SequencesEqual<T>(IEnumerable<T> x, params IEnumerable<T>[] list) =>
            list.All(y => x.SequenceEqual(y));

        [TestMethod]
        public void Params_Bool()
        {
            bool a = true;
            bool b;
            bool c = Tests.Param1(a, out b);
            Assert.IsTrue(b && c);
        }

        [TestMethod]
        public void Params_Byte()
        {
            byte a = 123;
            byte b;
            byte c = Tests.Param2(a, out b);
            Assert.IsTrue(a == b && a == c);
        }

        [TestMethod]
        public void Params_UInt16()
        {
            UInt16 a = 123;
            UInt16 b;
            UInt16 c = Tests.Param3(a, out b);
            Assert.IsTrue(a == b && a == c);
        }

        [TestMethod]
        public void Params_UInt32()
        {
            UInt32 a = 123;
            UInt32 b;
            UInt32 c = Tests.Param4(a, out b);
            Assert.IsTrue(a == b && a == c);
        }

        [TestMethod]
        public void Params_UInt64()
        {
            UInt64 a = 123;
            UInt64 b;
            UInt64 c = Tests.Param5(a, out b);
            Assert.IsTrue(a == b && a == c);
        }

        [TestMethod]
        public void Params_Int16()
        {
            Int16 a = 123;
            Int16 b;
            Int16 c = Tests.Param6(a, out b);
            Assert.IsTrue(a == b && a == c);
        }

        [TestMethod]
        public void Params_Int32()
        {
            Int32 a = 123;
            Int32 b;
            Int32 c = Tests.Param7(a, out b);
            Assert.IsTrue(a == b && a == c);
        }

        [TestMethod]
        public void Params_Int64()
        {
            Int64 a = 123;
            Int64 b;
            Int64 c = Tests.Param8(a, out b);
            Assert.IsTrue(a == b && a == c);
        }

        [TestMethod]
        public void Params_Float()
        {
            float a = 12.3f;
            float b;
            float c = Tests.Param9(a, out b);
            Assert.IsTrue(a == b && a == c);
        }

        [TestMethod]
        public void Params_Double()
        {
            double a = 12.3;
            double b;
            double c = Tests.Param10(a, out b);
            Assert.IsTrue(a == b && a == c);
        }

        [TestMethod]
        public void Params_Char()
        {
            char a = 'W';
            char b;
            char c = Tests.Param11(a, out b);
            Assert.IsTrue(a == b && a == c);
        }

        [TestMethod]
        public void Params_String()
        {
            string a = "WinRT";
            string b;
            string c = Tests.Param12(a, out b);
            Assert.IsTrue(a == b && a == c);
        }

        [TestMethod]
        public void Params_Blittable()
        {
            Blittable a = new Blittable(1, 2, 3, 4, -5, -6, -7, 8.0f, 9.0, typeof(ITests).GUID);
            Blittable b;
            Blittable c = Tests.Param13(a, in a, out b);
            Assert.IsTrue(AllEqual(a, b, c));
        }

        [TestMethod]
        public void Params_NonBlittable()
        {
            NonBlittable a = new NonBlittable(false, 'X', "WinRT", (long?)PropertyValue.CreateInt64(1234));
            NonBlittable b;
            NonBlittable c = Tests.Param14(a, in a, out b);
            Assert.IsTrue(AllEqual(a, b, c));
        }

        [TestMethod]
        public void Params_Nested()
        {
            Nested a = new Nested(
                new Blittable(1, 2, 3, 4, -5, -6, -7, 8.0f, 9.0, typeof(ITests).GUID),
                new NonBlittable(false, 'X', "WinRT", (long?)PropertyValue.CreateInt64(1234)));
            Nested b;
            Nested c = Tests.Param15(a, in a, out b);
            Assert.IsTrue(AllEqual(a, b, c));
        }

        [TestMethod]
        public void Params_Bool_Call()
        {
            Tests.Param1Call((bool a, out bool b) => { b = a; return a; });
        }

        [TestMethod]
        public void Params_Byte_Call()
        {
            Tests.Param2Call((byte a, out byte b) => { b = a; return a; });
        }

        [TestMethod]
        public void Params_UInt16_Call()
        {
            Tests.Param3Call((UInt16 a, out UInt16 b) => { b = a; return a; });
        }

        [TestMethod]
        public void Params_UInt32_Call()
        {
            Tests.Param4Call((UInt32 a, out UInt32 b) => { b = a; return a; });
        }

        [TestMethod]
        public void Params_UInt64_Call()
        {
            Tests.Param5Call((UInt64 a, out UInt64 b) => { b = a; return a; });
        }

        [TestMethod]
        public void Params_Int16_Call()
        {
            Tests.Param6Call((Int16 a, out Int16 b) => { b = a; return a; });
        }

        [TestMethod]
        public void Params_Int32_Call()
        {
            Tests.Param7Call((Int32 a, out Int32 b) => { b = a; return a; });
        }

        [TestMethod]
        public void Params_Int64_Call()
        {
            Tests.Param8Call((Int64 a, out Int64 b) => { b = a; return a; });
        }

        [TestMethod]
        public void Params_Float_Call()
        {
            Tests.Param9Call((float a, out float b) => { b = a; return a; });
        }

        [TestMethod]
        public void Params_Double_Call()
        {
            Tests.Param10Call((double a, out double b) => { b = a; return a; });
        }

        [TestMethod]
        public void Params_Char_Call()
        {
            Tests.Param11Call((char a, out char b) => { b = a; return a; });
        }

        [TestMethod]
        public void Params_String_Call()
        {
            Tests.Param12Call((string a, out string b) => { b = a; return a; });
        }

        [TestMethod]
        public void Params_Blittable_Call()
        {
            Tests.Param13Call((Blittable a, in Blittable b, out Blittable c) => { c = a; return a; });
        }

        [TestMethod]
        public void Params_NonBlittable_Call()
        {
            Tests.Param14Call((NonBlittable a, in NonBlittable b, out NonBlittable c) => { c = a; return a; });
        }

        [TestMethod]
        public void Params_Nested_Call()
        {
            Tests.Param15Call((Nested a, in Nested b, out Nested c) => { c = a; return a; });
        }

        [TestMethod]
        public void Array_Bool()
        {
            bool[] a = new[] { true, false, true };
            bool[] b = new bool[a.Length];
            bool[] c;
            bool[] d = Tests.Array1(a, b, out c);
            Assert.IsTrue(AllEqual(a, b, c, d));
        }

        [TestMethod]
        public void Array_Byte()
        {
            byte[] a = new byte[] { 1, 2, 3 };
            byte[] b = new byte[a.Length];
            byte[] c;
            byte[] d = Tests.Array2(a, b, out c);
            Assert.IsTrue(AllEqual(a, b, c, d));
        }

        [TestMethod]
        public void Array_UInt16()
        {
            UInt16[] a = new UInt16[] { 1, 2, 3 };
            UInt16[] b = new UInt16[a.Length];
            UInt16[] c;
            UInt16[] d = Tests.Array3(a, b, out c);
            Assert.IsTrue(AllEqual(a, b, c, d));
        }

        [TestMethod]
        public void Array_UInt32()
        {
            UInt32[] a = new UInt32[] { 1, 2, 3 };
            UInt32[] b = new UInt32[a.Length];
            UInt32[] c;
            UInt32[] d = Tests.Array4(a, b, out c);
            Assert.IsTrue(AllEqual(a, b, c, d));
        }

        [TestMethod]
        public void Array_UInt64()
        {
            UInt64[] a = new UInt64[] { 1, 2, 3 };
            UInt64[] b = new UInt64[a.Length];
            UInt64[] c;
            UInt64[] d = Tests.Array5(a, b, out c);
            Assert.IsTrue(AllEqual(a, b, c, d));
        }

        [TestMethod]
        public void Array_Int16()
        {
            Int16[] a = new Int16[] { 1, 2, 3 };
            Int16[] b = new Int16[a.Length];
            Int16[] c;
            Int16[] d = Tests.Array6(a, b, out c);
            Assert.IsTrue(AllEqual(a, b, c, d));
        }

        [TestMethod]
        public void Array_Int32()
        {
            Int32[] a = new Int32[] { 1, 2, 3 };
            Int32[] b = new Int32[a.Length];
            Int32[] c;
            Int32[] d = Tests.Array7(a, b, out c);
            Assert.IsTrue(AllEqual(a, b, c, d));
        }

        [TestMethod]
        public void Array_Int64()
        {
            Int64[] a = new Int64[] { 1, 2, 3 };
            Int64[] b = new Int64[a.Length];
            Int64[] c;
            Int64[] d = Tests.Array8(a, b, out c);
            Assert.IsTrue(AllEqual(a, b, c, d));
        }

        [TestMethod]
        public void Array_Float()
        {
            float[] a = new float[] { 1.0f, 2.0f, 3.0f };
            float[] b = new float[a.Length];
            float[] c;
            float[] d = Tests.Array9(a, b, out c);
            Assert.IsTrue(AllEqual(a, b, c, d));
        }

        [TestMethod]
        public void Array_Double()
        {
            double[] a = new double[] { 1.0, 2.0, 3.0 };
            double[] b = new double[a.Length];
            double[] c;
            double[] d = Tests.Array10(a, b, out c);
            Assert.IsTrue(AllEqual(a, b, c, d));
        }

        [TestMethod]
        public void Array_Char()
        {
            char[] a = new char[] { 'a', 'b', 'c' };
            char[] b = new char[a.Length];
            char[] c;
            char[] d = Tests.Array11(a, b, out c);
            Assert.IsTrue(AllEqual(a, b, c, d));
        }

        [TestMethod]
        public void Array_String()
        {
            string[] a = new string[] { "apples", "oranges", "pears" };
            string[] b = new string[a.Length];
            string[] c;
            string[] d = Tests.Array12(a, b, out c);
            Assert.IsTrue(AllEqual(a, b, c, d));
        }

        [TestMethod]
        public void Array_NullStringArray()
        {
            string[] a = null;
            string[] b = null;
            string[] c;
            string[] d = Tests.Array12(a, b, out c);
            Assert.AreEqual(0, c.Length);
            Assert.AreEqual(0, d.Length);
        }

        [TestMethod]
        public void Array_Blittable()
        {
            Blittable[] a = new Blittable[] {
                new Blittable(1, 2, 3, 4, -5, -6, -7, 8.0f, 9.0, typeof(ITests).GUID),
                new Blittable(10, 20, 30, 40, -50, -60, -70, 80.0f, 90.0, typeof(IStringable).GUID)
            };
            Blittable[] b = new Blittable[a.Length];
            Blittable[] c;
            Blittable[] d = Tests.Array13(a, b, out c);
            Assert.IsTrue(AllEqual(a, b, c, d));
        }

        [TestMethod]
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
            Assert.IsTrue(AllEqual(a, b, c, d));
        }

        [TestMethod]
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
                    new Blittable(1, 2, 3, 4, -5, -6, -7, 8.0f, 9.0, WellKnownInterfaceIIDs.IID_IInspectable),
                    new NonBlittable(false, 'Z', "Third", (long?)PropertyValue.CreateInt64(789)))
            };
            Nested[] b = new Nested[a.Length];
            Nested[] c;
            Nested[] d = Tests.Array15(a, b, out c);
            Assert.IsTrue(AllEqual(a, b, c, d));
        }

        [TestMethod]
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
            Assert.IsTrue(AllEqual(a, b, c, d));
        }

        [TestMethod]
        public void Array_NullInterfaces()
        {
            IStringable[] a = null;
            IStringable[] b = null;
            IStringable[] c;
            IStringable[] d = Tests.Array16(a, b, out c);
            Assert.AreEqual(0, c.Length);
            Assert.AreEqual(0, d.Length);
        }

        private T[] Array_Call<T>(ReadOnlySpan<T> a, Span<T> b, out T[] c)
        {
            Assert.IsTrue(a.Length == b.Length);
            a.CopyTo(b);
            c = a.ToArray();
            return a.ToArray();
        }

        [TestMethod]
        public void Array_Bool_Call()
        {
            Tests.Array1Call(Array_Call);
        }

        [TestMethod]
        public void Array_Byte_Call()
        {
            Tests.Array2Call(Array_Call);
        }

        [TestMethod]
        public void Array_UInt16_Call()
        {
            Tests.Array3Call(Array_Call);
        }

        [TestMethod]
        public void Array_UInt32_Call()
        {
            Tests.Array4Call(Array_Call);
        }

        [TestMethod]
        public void Array_UInt64_Call()
        {
            Tests.Array5Call(Array_Call);
        }

        [TestMethod]
        public void Array_Int16_Call()
        {
            Tests.Array6Call(Array_Call);
        }

        [TestMethod]
        public void Array_Int32_Call()
        {
            Tests.Array7Call(Array_Call);
        }

        [TestMethod]
        public void Array_Int64_Call()
        {
            Tests.Array8Call(Array_Call);
        }

        [TestMethod]
        public void Array_Float_Call()
        {
            Tests.Array9Call(Array_Call);
        }

        [TestMethod]
        public void Array_Double_Call()
        {
            Tests.Array10Call(Array_Call);
        }

        [TestMethod]
        public void Array_Char_Call()
        {
            Tests.Array11Call(Array_Call);
        }

        [TestMethod]
        public void Array_String_Call()
        {
            Tests.Array12Call(Array_Call);
        }

        [TestMethod]
        public void Array_Blittable_Call()
        {
            Tests.Array13Call(Array_Call);
        }

        [TestMethod]
        public void Array_NonBlittable_Call()
        {
            Tests.Array14Call(Array_Call);
        }

        [TestMethod]
        public void Array_Nested_Call()
        {
            Tests.Array15Call(Array_Call);
        }

        [TestMethod]
        public void Array_Stringable_Call()
        {
            Tests.Array16Call(Array_Call);
        }

        [TestMethod]
        public void Collections_IEnumerable()
        {
            string[] a = new string[] { "apples", "oranges", "pears" };
            IEnumerable<string> b = null;
            var c = Tests.Collection1(a, out b);
            Assert.IsTrue(SequencesEqual(a, b, c));
        }

        [TestMethod]
        public void Collections_IEnumerable_Pair()
        {
            var a = new KeyValuePair<string, string>[] {
                new KeyValuePair<string,string>("apples", "1"),
                new KeyValuePair<string,string>("oranges", "2"),
                new KeyValuePair<string,string>("pears", "3")
            };
            IEnumerable<KeyValuePair<string, string>> b = null;
            var c = Tests.Collection2(a, out b);
            Assert.IsTrue(SequencesEqual(a, b, c));
        }

        [TestMethod]
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
            Assert.IsTrue(SequencesEqual(a, b, c));
            RunDictionaryTests(c);
        }

        sealed class TestIDICInspectable : WindowsRuntimeObject
        {
#pragma warning disable CSWINRT3001 // Type or member is obsolete
            public unsafe TestIDICInspectable(void* ptr)
                : base(WindowsRuntimeComWrappersMarshal.CreateObjectReferenceUnsafe(ptr, WellKnownInterfaceIIDs.IID_IInspectable, out _))
#pragma warning restore CSWINRT3001 // Type or member is obsolete
            {
            }

            protected override bool HasUnwrappableNativeObjectReference => true;

            protected override bool IsOverridableInterface(in Guid iid)
            {
                return false;
            }
        }

        [TestMethod]
        public unsafe void Collections_Dictionary_IDIC()
        {
            var a = new Dictionary<string, string>()
            {
                ["apples"] = "1",
                ["oranges"] = "2",
                ["pears"] = "3"
            };
            var c = Tests.Collection3(a, out _);

            var inspectable = new TestIDICInspectable(WindowsRuntimeMarshal.ConvertToUnmanaged(c));
            var dictCreatedWithIDIC = (IDictionary<string, string>)(object)inspectable;
            RunDictionaryTests(dictCreatedWithIDIC);
        }

        private void RunDictionaryTests(IDictionary<string, string> c)
        {
            Assert.IsTrue(SequencesEqual(c.Keys, new List<string> { "apples", "oranges", "pears" }));
            Assert.IsTrue(SequencesEqual(c.Values, new List<string> { "1", "2", "3" }));
            Assert.IsTrue(SequencesEqual(c, new List<KeyValuePair<string, string>> {
                new KeyValuePair<string, string>("apples", "1"),
                new KeyValuePair<string, string>("oranges", "2"),
                new KeyValuePair<string, string>("pears", "3")
            }));

            c["bananas"] = "4";
            Assert.AreEqual("4", c["bananas"]);

            c.Add("kiwi", "5");
            Assert.AreEqual(5, c.Count);

            Assert.IsTrue(c.ContainsKey("oranges"));

            KeyValuePair<string, string> k = new KeyValuePair<string, string>("pears", "3");
            Assert.IsTrue(c.Contains(k));

            KeyValuePair<string, string>[] pairs = new KeyValuePair<string, string>[5];
            c.CopyTo(pairs, 0);
            Assert.AreEqual(5, pairs.Length);

            c.Remove("kiwi");
            Assert.ThrowsException<KeyNotFoundException>(() => c["kiwi"]);
            Assert.IsFalse(c.TryGetValue("kiwi", out var _));

            Assert.IsTrue(c.TryGetValue("apples", out var keyVal));
            Assert.AreEqual("1", keyVal);

            Assert.AreEqual(4, c.Keys.Count());
            Assert.AreEqual(4, c.Values.Count());

            c.Remove(new KeyValuePair<string, string>("apples", "1"));
            Assert.ThrowsException<KeyNotFoundException>(() => c["apples"]);

            c.Clear();
            Assert.AreEqual(0, c.Count);
        }

        [TestMethod]
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
            Assert.IsTrue(SequencesEqual(a, b, c));
            RunReadOnlyDictionaryTests(c);
        }

        [TestMethod]
        public unsafe void Collections_ReadOnly_Dictionary_IDIC()
        {
            var a = new Dictionary<string, string>()
            {
                ["apples"] = "1",
                ["oranges"] = "2",
                ["pears"] = "3"
            };
            IReadOnlyDictionary<string, string> b = null;
            var c = Tests.Collection4(a, out b);
            var inspectable = new TestIDICInspectable(WindowsRuntimeMarshal.ConvertToUnmanaged(c));
            var dictCreatedWithIDIC = (IReadOnlyDictionary<string, string>)(object)inspectable;
            RunReadOnlyDictionaryTests(dictCreatedWithIDIC);
        }

        private void RunReadOnlyDictionaryTests(IReadOnlyDictionary<string, string> c)
        {
            Assert.IsTrue(SequencesEqual(c.Keys, new List<string> { "apples", "oranges", "pears" }));
            Assert.IsTrue(SequencesEqual(c.Values, new List<string> { "1", "2", "3" }));
            Assert.IsTrue(SequencesEqual(c, new List<KeyValuePair<string, string>> {
                new KeyValuePair<string, string>("apples", "1"),
                new KeyValuePair<string, string>("oranges", "2"),
                new KeyValuePair<string, string>("pears", "3")
            }));

            Assert.AreEqual("2", c["oranges"]);
            Assert.AreEqual(3, c.Count);
            Assert.IsTrue(c.ContainsKey("pears"));
            Assert.AreEqual(3, c.Values.Count());
            Assert.AreEqual(3, c.Keys.Count());
        }

        [TestMethod]
        public void Collections_List()
        {
            string[] a = new string[] { "apples", "oranges", "pears" };
            IList<string> b = null;
            var c = Tests.Collection5(a, out b);
            Assert.IsTrue(SequencesEqual(a, b, c));
            RunListTests(c);
        }

        [TestMethod]
        public unsafe void Collections_List_IDIC()
        {
            string[] a = new string[] { "apples", "oranges", "pears" };
            IList<string> b = null;
            var c = Tests.Collection5(a, out b);
            Assert.IsTrue(SequencesEqual(a, b, c));
            var inspectable = new TestIDICInspectable(WindowsRuntimeMarshal.ConvertToUnmanaged(c));
            var listCreatedWithIDIC = (IList<string>)(object)inspectable;
            RunListTests(listCreatedWithIDIC);
        }

        private void RunListTests(IList<string> c)
        {
            Assert.AreEqual(3, c.Count);
            Assert.AreEqual(1, c.IndexOf("oranges"));

            Assert.IsFalse(c.IsReadOnly);

            c.Add("bananas");

            c[3] = "strawberries";
            Assert.AreEqual("strawberries", c[3]);
            Assert.IsFalse(c.Contains("bananas"));

            c.Insert(3, "kiwis");
            Assert.IsTrue(c.Remove("kiwis"));
            c.RemoveAt(3);

            string[] copied = new string[c.Count];
            c.CopyTo(copied, 0);
            Assert.IsTrue(SequencesEqual<string>(new string[] { "apples", "oranges", "pears" }, copied));

            var enumerator = c.GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNotNull(enumerator.Current);

            c.Clear();
            Assert.AreEqual(0, c.Count);
        }

        [TestMethod]
        public void CastListToEnum_String()
        {
            string[] a = new string[] { "apples", "oranges", "pears" };
            IList<string> b = null;
            var c = Tests.Collection5(a, out b);
            var j = (IEnumerable<string>)(object)b;
            Assert.IsTrue(SequencesEqual(a, b, j));
        }

        [TestMethod]
        public void Collections_ReadOnly_List()
        {
            string[] a = new string[] { "apples", "oranges", "pears" };
            IReadOnlyList<string> b = null;
            var c = Tests.Collection6(a, out b);
            Assert.IsTrue(SequencesEqual(a, b, c));
            RunReadonlyListTests(c);
        }

        [TestMethod]
        public unsafe void Collections_ReadOnly_List_IDIC()
        {
            string[] a = new string[] { "apples", "oranges", "pears" };
            IReadOnlyList<string> b = null;
            var c = Tests.Collection6(a, out b);
            Assert.IsTrue(SequencesEqual(a, b, c));
            var inspectable = new TestIDICInspectable(WindowsRuntimeMarshal.ConvertToUnmanaged(c));
            var listCreatedWithIDIC = (IReadOnlyList<string>)(object)inspectable;
            RunReadonlyListTests(listCreatedWithIDIC);
        }

        private void RunReadonlyListTests(IReadOnlyList<string> c)
        {
            Assert.AreEqual("oranges", c[1]);
            Assert.AreEqual(3, c.Count());
            Assert.AreEqual(3, c.Count);
            Assert.IsNotNull(c.GetEnumerator());
        }

        [TestMethod]
        public void Collections_IEnumerable_Call()
        {
            Tests.Collection1Call((IEnumerable<string> a, out IEnumerable<string> b) =>
            {
                b = a.Select(s => s);
                return b.Select(s => s);
            });
        }

        [TestMethod]
        public void Collections_IEnumerable_Pair_Call()
        {
            Tests.Collection2Call((IEnumerable<KeyValuePair<string, string>> a, out IEnumerable<KeyValuePair<string, string>> b) =>
            {
                b = a.Select(s => s);
                return b.Select(s => s);
            });
        }

        [TestMethod]
        public void Collections_Dictionary_Call()
        {
            Tests.Collection3Call((IDictionary<string, string> a, out IDictionary<string, string> b) =>
            {
                b = new Dictionary<string, string>(a);
                return new Dictionary<string, string>(b);
            });
        }

#if NET
        [TestMethod]
        public void Collections_ReadOnly_Dictionary_Call()
        {
            Tests.Collection4Call((IReadOnlyDictionary<string, string> a, out IReadOnlyDictionary<string, string> b) =>
            {
                b = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(a));
                return new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(b));
            });
        }
#endif

        [TestMethod]
        public void Collections_List_Call()
        {
            Tests.Collection5Call((IList<string> a, out IList<string> b) =>
            {
                b = a.Select(s => s).ToList();
                return b.Select(s => s).ToList();
            });
        }

        [TestMethod]
        public void Collections_ReadOnly_List_Call()
        {
            Tests.Collection6Call((IReadOnlyList<string> a, out IReadOnlyList<string> b) =>
            {
                b = a.Select(s => s).ToList();
                return b.Select(s => s).ToList();
            });
        }

        [TestMethod]
        public void TestComposable()
        {
            HierarchyA hierarchyA = new HierarchyA();
            Assert.AreEqual("HierarchyA.HierarchyA_Method", hierarchyA.HierarchyA_Method());

            HierarchyA hierarchyBAsHierarchyA = new HierarchyB();
            Assert.AreEqual("HierarchyB.HierarchyA_Method", hierarchyBAsHierarchyA.HierarchyA_Method());

            HierarchyB hierarchyB = new HierarchyB();
            Assert.AreEqual("HierarchyB.HierarchyB_Method", hierarchyB.HierarchyB_Method());

            HierarchyC hierarchyC = new HierarchyC();
            Assert.AreEqual("HierarchyC.HierarchyB_Method", hierarchyC.HierarchyB_Method());

            HierarchyB hierarchyCAsHierarchyB = new HierarchyC();
            Assert.AreEqual("HierarchyC.HierarchyB_Method", hierarchyCAsHierarchyB.HierarchyB_Method());
            Assert.AreEqual("HierarchyB.HierarchyA_Method", hierarchyCAsHierarchyB.HierarchyA_Method());

            HierarchyD hierarchyD = new HierarchyD();
            hierarchyD.HierarchyD_Method();

            var hierarchyDAsHierarchyA = (HierarchyA)hierarchyD;
            Assert.AreEqual("HierarchyB.HierarchyA_Method", hierarchyDAsHierarchyA.HierarchyA_Method());

            Assert.IsTrue(hierarchyDAsHierarchyA == hierarchyD);
        }

        [TestMethod]
        public void TestVectorGetMany()
        {
            var bools = new List<bool>()
            {
                true,
                false,
                true,
                true
            };
            var boolSubset = Tests.GetBooleanVectorSubset(bools, 1);
            Assert.IsTrue(bools.GetRange(1, 3).SequenceEqual(boolSubset));

            var blittableObjects = new List<Blittable>()
            {
                new Blittable(1, 2, 3, 4, 5, 6, 7, 8, 9, Guid.Empty),
                new Blittable(3, 4, 5, 6, 7, 8, 9, 10, 11, typeof(ITests).GUID),
                new Blittable(5, 6, 7, 8, 9, 10, 11, 12, 13, Guid.Empty),
                new Blittable(7, 8, 9, 10, 11, 12, 13, 14, 15, Guid.Empty),
                new Blittable(9, 10, 11, 12, 13, 14, 15, 16, 17, typeof(ITests).GUID)
            };
            var blittableObjectsSubset = Tests.GetBlittableVectorSubset(blittableObjects, 1);
            Assert.IsTrue(blittableObjects.GetRange(1, 3).SequenceEqual(blittableObjectsSubset));

            var nonBlittableObjects = new List<NonBlittable>()
            {
                new NonBlittable(true, 'a', "one", 1),
                new NonBlittable(false, 'b', "two", 2),
                new NonBlittable(true, 'c', "three", 3),
                new NonBlittable(true, 'd', "four", 4),
                new NonBlittable(true, 'e', "five", 5),
                new NonBlittable(false, 'f',"six", 6)
            };
            var nonBlittableObjectSubset = Tests.GetNonBlittableVectorSubset(nonBlittableObjects, 1);
            Assert.IsTrue(nonBlittableObjects.GetRange(1, 3).SequenceEqual(nonBlittableObjectSubset));

            var strings = new List<string>()
            {
                "one",
                "two",
                "three",
                "four",
                "five",
                "six"
            };
            var stringSubset = Tests.GetStringVectorSubset(strings, 1);
            Assert.IsTrue(strings.GetRange(1, 3).SequenceEqual(stringSubset));

            var classObjects = new List<Class>()
            {
                new Class(),
                new Class(),
                new Class(),
                new Class()
            };
            var classSubset = Tests.GetClassVectorSubset(classObjects, 1);
            Assert.IsTrue(classObjects.GetRange(1, 3).SequenceEqual(classSubset));

            var objSubset = Tests.GetObjectVectorSubset(classObjects, 1);
            Assert.IsTrue(classObjects.GetRange(1, 3).SequenceEqual(objSubset));

            var interfaceSubset = Tests.GetInterfaceVectorSubset(classObjects, 1);
            Assert.IsTrue(classObjects.GetRange(1, 3).SequenceEqual(interfaceSubset));

            var composableObjects = new List<Composable>()
            {
                new Composable(),
                new Composable(2),
                new Composable(3),
                new Composable(4)
            };
            var composableClassSubset = Tests.GetComposableClassVectorSubset(composableObjects, 1);
            Assert.IsTrue(composableObjects.GetRange(1, 3).SequenceEqual(composableClassSubset));

            objSubset = Tests.GetObjectVectorSubset(composableObjects, 1);
            Assert.IsTrue(composableObjects.GetRange(1, 3).SequenceEqual(objSubset));

            interfaceSubset = Tests.GetInterfaceVectorSubset(composableObjects, 1);
            Assert.IsTrue(composableObjects.GetRange(1, 3).SequenceEqual(interfaceSubset));
        }

        private void Box_type<T>(T val, Func<T, object, object> boxFunc)
        {
            var boxedVal = boxFunc(val, val);
            Assert.IsInstanceOfType<T>(boxedVal);
            Assert.AreEqual((T)boxedVal, val);
        }

        private void Box_array<T>(ReadOnlySpan<T> val, Func<ReadOnlySpan<T>, object, object> boxFunc)
        {
            var boxedVal = boxFunc(val, val.ToArray());
            Assert.IsInstanceOfType<T[]>(boxedVal);
            Assert.IsTrue(val.ToArray().SequenceEqual((T[])boxedVal));
        }

        [TestMethod]
        public void Box_Byte()
        {
            Box_type<byte>(4, Tests.Box1);
        }

        [TestMethod]
        public void Box_UShort()
        {
            Box_type<ushort>(4, Tests.Box2);
        }

        [TestMethod]
        public void Box_UInt()
        {
            Box_type<uint>(4, Tests.Box3);
        }

        [TestMethod]
        public void Box_ULong()
        {
            Box_type<ulong>(4, Tests.Box4);
        }

        [TestMethod]
        public void Box_Short()
        {
            Box_type<short>(4, Tests.Box5);
        }

        [TestMethod]
        public void Box_Int()
        {
            Box_type(4, Tests.Box6);
        }

        [TestMethod]
        public void Box_Long()
        {
            Box_type<long>(4, Tests.Box7);
        }

        [TestMethod]
        public void Box_Bool()
        {
            Box_type(true, Tests.Box8);
        }

        [TestMethod]
        public void Box_Float()
        {
            Box_type<float>(4, Tests.Box9);
        }

        [TestMethod]
        public void Box_Double()
        {
            Box_type(4.0, Tests.Box10);
        }

        [TestMethod]
        public void Box_Guid()
        {
            Box_type(Guid.NewGuid(), Tests.Box11);
        }

        [TestMethod]
        public void Box_Char()
        {
            Box_type('c', Tests.Box12);
        }

        [TestMethod]
        public void Box_String()
        {
            Box_type("test", Tests.Box13);
        }

        [TestMethod]
        public void Box_Timespan()
        {
            Box_type(TimeSpan.FromMilliseconds(4), Tests.Box14);
        }

        [TestMethod]
        public void Box_Blittable()
        {
            Blittable blittable = new Blittable(3, 4, 5, 6, 7, 8, 9, 10, 11, typeof(ITests).GUID);
            Box_type(blittable, Tests.Box15);
        }

        [TestMethod]
        public void Box_NonBittable()
        {
            NonBlittable nonBlittable = new NonBlittable(true, 'a', "one", 1);
            Box_type(nonBlittable, Tests.Box16);
        }

        [TestMethod]
        public void Box_DateTime()
        {
            Box_type(DateTimeOffset.Now, Tests.Box17);
        }

        [TestMethod]
        public void Box_LongArray()
        {
            ReadOnlySpan<long> arr = new long[] { 2, 4, 6 };
            Box_array(arr, Tests.Box18);

            long[] arr2 = new long[] { 2, 4, 6 };
            Box_array(arr2, Tests.Box18);
            Box_array(arr2, Tests.Box18);

            long[] arr3 = new long[0];
            Box_array(arr3, Tests.Box18);

            long[] arr4 = new long[0];
            Box_array(arr4, Tests.Box18);
        }

        [TestMethod]
        public void Box_BoolArray()
        {
            bool[] arr = new bool[] { true, false, true };
            Box_array(arr, Tests.Box19);

            bool[] arr2 = new bool[] { true, false, true };
            Box_array(arr2, Tests.Box19);
            Box_array(arr2, Tests.Box19);

            bool[] arr3 = new bool[0];
            Box_array(arr3, Tests.Box19);

            bool[] arr4 = new bool[0];
            Box_array(arr4, Tests.Box19);
        }

        [TestMethod]
        public void Box_StringArray()
        {
            string[] arr = new string[] { "one", "two", "three" };
            Box_array(arr, Tests.Box20);

            string[] arr2 = new string[] { "four", "five", "six" };
            Box_array(arr2, Tests.Box20);
            Box_array(arr2, Tests.Box20);

            string[] arr3 = new string[0];
            Box_array(arr3, Tests.Box20);

            string[] arr4 = new string[0];
            Box_array(arr4, Tests.Box20);
        }

        [TestMethod]
        public void Box_TimeSpanArray()
        {
            TimeSpan[] arr = new TimeSpan[] { TimeSpan.FromMilliseconds(4), TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(6) };
            Box_array(arr, Tests.Box21);

            TimeSpan[] arr2 = new TimeSpan[] { TimeSpan.FromMilliseconds(4), TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(6) };
            Box_array(arr2, Tests.Box21);
            Box_array(arr2, Tests.Box21);

            TimeSpan[] arr3 = new TimeSpan[0];
            Box_array(arr3, Tests.Box21);

            TimeSpan[] arr4 = new TimeSpan[0];
            Box_array(arr4, Tests.Box21);
        }

        [TestMethod]
        public void Fast_Abi_Simple()
        {
            var simple = new test_component_fast.Simple();
            Assert.IsNotNull(simple);
            simple = new test_component_fast.Simple("Hello");
            Assert.AreEqual("Hello", simple.Property1);
            Assert.AreEqual("StaticMethod1", test_component_fast.Simple.StaticMethod1());
            Assert.AreEqual("StaticMethod2", test_component_fast.Simple.StaticMethod2());
            Assert.AreEqual("Method1", simple.Method1());
            Assert.AreEqual("Method2", simple.Method2());
            Assert.AreEqual("Method3", simple.Method3());
            Assert.AreEqual("Method4", simple.Method4());
            Assert.AreEqual("Method5", simple.Method5());
            Assert.AreEqual("Method6", simple.Method6());
            Assert.AreEqual("Method7", simple.Method7());
            Assert.AreEqual("Method8", simple.Method8());
            Assert.AreEqual("Method9", simple.Method9());
            simple.Property1 = "Property1";
            simple.Property3 = "Property3";
            Assert.AreEqual("Property1", simple.Property1);
            Assert.AreEqual("Property2", simple.Property2);
            Assert.AreEqual("Property3", simple.Property3);
            var ev = "";
            simple.Event0 += () => { ev = "Hello"; };
            simple.InvokeEvent0();
            Assert.AreEqual("Hello", ev);
        }

        [TestMethod]
        public void Fast_Abi_Composition()
        {
            var compositor = new test_component_fast.Composition.Compositor();
            var sv = compositor.CreateSpriteVisual();
            sv.Offset = 10;
            sv.StartAnimationGroup();
            Assert.AreEqual("", sv.Serialize(100));
            Assert.AreEqual(10, sv.Offset);
            Assert.AreEqual(10, sv.Pad);
            sv.ObjectProperty = new List<int> { 1, 2, 3 };
            Assert.AreEqual(3, ((List<int>)sv.ObjectProperty).Count);
        }

        // Nota Bene: this test case must always remain the final one
        [TestMethod]
        public void Z_Check_Coverage()
        {
            Tests.Simple();
            //Assert.AreEqual((double)Tests.Percentage, (double)100);
        }

    }
}