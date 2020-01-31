using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using WinRT;

using WF = Windows.Foundation;
using WFC = Windows.Foundation.Collections;
using Windows.Foundation;
using Windows.Foundation.Collections;

using TestComp;

namespace UnitTest
{
    public class TestComp
    {
        public Class TestObject { get; private set; }

        public TestComp()
        {
            TestObject = new Class();
        }

        [Fact]
        public void TestUri()
        {
            var base_uri = "https://github.com";
            var relative_uri = "microsoft/CsWinRT";
            var full_uri = base_uri + "/" + relative_uri;

            var uri1 = new WF.Uri(full_uri);
            var str1 = uri1.ToString();
            Assert.Equal(full_uri, str1);

            var uri2 = new WF.Uri(base_uri, relative_uri);
            var str2 = uri2.ToString();
            Assert.Equal(full_uri, str2);

            Assert.True(uri1.Equals(uri2));
        }

        [Fact]
        public void TestFactories()
        {
            var cls1 = new Class();

            var cls2 = new Class(42);
            Assert.Equal(42, cls2.IntProperty);

            var cls3 = new Class(42, "foo");
            Assert.Equal(42, cls3.IntProperty);
            Assert.Equal("foo", cls3.StringProperty);
        }

        [Fact]
        public void TestStaticMembers()
        {
            Class.StaticIntProperty = 42;
            Assert.Equal(42, Class.StaticIntProperty);

            Class.StaticStringProperty = "foo";
            Assert.Equal("foo", Class.StaticStringProperty);
        }

        [Fact]
        public void TestStaticClass()
        {
            Assert.Equal(0, StaticClass.NumClasses);
            var obj = StaticClass.MakeClass();
            Assert.Equal(1, StaticClass.NumClasses);
        }

        [Fact]
        public void TestInterfaces()
        {
            var expected = "hello";
            TestObject.StringProperty = expected;

            // projected wrapper
            Assert.Equal(expected, TestObject.ToString());

            // implicit cast
            var str = (IStringable)TestObject;
            Assert.Equal(expected, str.ToString());

            var str2 = TestObject as IStringable;
            Assert.Equal(expected, str2.ToString());

            Assert.IsAssignableFrom<IStringable>(TestObject);
        }

        // TODO: project asyncs as awaitable tasks
        [Fact]
        public void TestAsync()
        {
            TestObject.IntProperty = 42;
            var async_get_int = TestObject.GetIntAsync();
            int async_int = 0;
            async_get_int.Completed = (info, status) => async_int = info.GetResults();
            async_get_int.GetResults();
            Assert.Equal(42, async_int);

            TestObject.StringProperty = "foo";
            var async_get_string = TestObject.GetStringAsync();
            string async_string = "";
            async_get_string.Completed = (info, status) => async_string = info.GetResults();
            int async_progress;
            async_get_string.Progress = (info, progress) => async_progress = progress;
            async_get_string.GetResults();
            Assert.Equal("foo", async_string);
        }

        [Fact]
        public void TestCollections()
        {
            // TODO: need more - currently just a smoke test for generics
            var strings = TestObject.StringsProperty;
            Assert.Equal(2u, strings.Size);
        }

        [Fact]
        public void TestArrays()
        {
            // TODO
        }

        [Fact]
        public void TestPrimitives()
        {
            var test_int = 21;
            TestObject.IntPropertyChanged += (IInspectable sender, Int32 value) =>
            {
                var c = Class.FromAbi(sender.ThisPtr);
                Assert.Equal(value, test_int);
            };
            TestObject.IntProperty = test_int;

            var expectedVal = true;
            var hits = 0;
            TestObject.BoolPropertyChanged += (IInspectable sender, bool value) =>
            {
                Assert.Equal(expectedVal, value);
                ++hits;
            };

            TestObject.BoolProperty = true;
            Assert.Equal(1, hits);

            expectedVal = false;
            TestObject.CallForBool(() => false);
            Assert.Equal(2, hits);

            TestObject.RaiseBoolChanged();
            Assert.Equal(3, hits);
        }

        [Fact]
        public void TestStrings()
        {
            string test_string = "x";
            string test_string2 = "y";

            // In hstring from managed->native implicitly creates hstring reference
            TestObject.StringProperty = test_string;

            // Out hstring from native->managed only creates System.String on demand
            var sp = TestObject.StringProperty;
            Assert.Equal(sp, test_string);

            // Out hstring from managed->native always creates HString from System.String
            TestObject.CallForString(() => test_string2);
            Assert.Equal(TestObject.StringProperty, test_string2);

            // In hstring from native->managed only creates System.String on demand
            TestObject.StringPropertyChanged += (Class sender, string value) => sender.StringProperty2 = value;
            TestObject.RaiseStringChanged();
            Assert.Equal(TestObject.StringProperty2, test_string2);
        }

        [Fact]
        public void TestBlittableStruct()
        {
            // Property setter/getter
            var val = new BlittableStruct(){ i32 = 42 };
            TestObject.BlittableStructProperty = val;
            Assert.Equal(42, TestObject.BlittableStructProperty.i32);

            // Manual getter
            Assert.Equal(42, TestObject.GetBlittableStruct().i32);

            // Manual setter
            val.i32 = 8;
            TestObject.SetBlittableStruct(val);
            Assert.Equal(8, TestObject.BlittableStructProperty.i32);

            // Output argument
            val = default;
            TestObject.OutBlittableStruct(out val);
            Assert.Equal(8, val.i32);
        }

        [Fact]
        public void TestComposedBlittableStruct()
        {
            // Property setter/getter
            var val = new ComposedBlittableStruct(){ blittable = new BlittableStruct(){ i32 = 42 } };
            TestObject.ComposedBlittableStructProperty = val;
            Assert.Equal(42, TestObject.ComposedBlittableStructProperty.blittable.i32);

            // Manual getter
            Assert.Equal(42, TestObject.GetComposedBlittableStruct().blittable.i32);

            // Manual setter
            val.blittable.i32 = 8;
            TestObject.SetComposedBlittableStruct(val);
            Assert.Equal(8, TestObject.ComposedBlittableStructProperty.blittable.i32);

            // Output argument
            val = default;
            TestObject.OutComposedBlittableStruct(out val);
            Assert.Equal(8, val.blittable.i32);
        }

        [Fact]
        public void TestNonBlittableStringStruct()
        {
            // Property getter/setter
            var val = new NonBlittableStringStruct(){ str = "I like tacos" };
            TestObject.NonBlittableStringStructProperty = val;
            Assert.Equal("I like tacos", TestObject.NonBlittableStringStructProperty.str.ToString());

            // Manual getter
            Assert.Equal("I like tacos", TestObject.GetNonBlittableStringStruct().str.ToString());

            // Manual setter
            val.str = "Hello, world";
            TestObject.SetNonBlittableStringStruct(val);
            Assert.Equal("Hello, world", TestObject.NonBlittableStringStructProperty.str.ToString());

            // Output argument
            val = default;
            TestObject.OutNonBlittableStringStruct(out val);
            Assert.Equal("Hello, world", val.str.ToString());
        }

        [Fact]
        public void TestNonBlittableBoolStruct()
        {
            // Property getter/setter
            var val = new NonBlittableBoolStruct() { w = true, x = false, y = true, z = false };
            TestObject.NonBlittableBoolStructProperty = val;
            Assert.True(TestObject.NonBlittableBoolStructProperty.w);
            Assert.False(TestObject.NonBlittableBoolStructProperty.x);
            Assert.True(TestObject.NonBlittableBoolStructProperty.y);
            Assert.False(TestObject.NonBlittableBoolStructProperty.z);

            // Manual getter
            Assert.True(TestObject.GetNonBlittableBoolStruct().w);
            Assert.False(TestObject.GetNonBlittableBoolStruct().x);
            Assert.True(TestObject.GetNonBlittableBoolStruct().y);
            Assert.False(TestObject.GetNonBlittableBoolStruct().z);

            // Manual setter
            val.w = false;
            val.x = true;
            val.y = false;
            val.z = true;
            TestObject.SetNonBlittableBoolStruct(val);
            Assert.False(TestObject.NonBlittableBoolStructProperty.w);
            Assert.True(TestObject.NonBlittableBoolStructProperty.x);
            Assert.False(TestObject.NonBlittableBoolStructProperty.y);
            Assert.True(TestObject.NonBlittableBoolStructProperty.z);

            // Output argument
            val = default;
            TestObject.OutNonBlittableBoolStruct(out val);
            Assert.False(val.w);
            Assert.True(val.x);
            Assert.False(val.y);
            Assert.True(val.z);
        }

        [Fact]
        public void TestNonBlittableRefStruct()
        {
            // Property getter/setter
            // TODO: Need to either support interface inheritance or project IReference/INullable for setter
            Assert.Equal(42, TestObject.NonBlittableRefStructProperty.ref32.Value);

            // Manual getter
            Assert.Equal(42, TestObject.GetNonBlittableRefStruct().ref32.Value);

            // TODO: Manual setter

            // Output argument
            NonBlittableRefStruct val;
            TestObject.OutNonBlittableRefStruct(out val);
            Assert.Equal(42, val.ref32.Value);
        }

        [Fact]
        public void TestComposedNonBlittableStruct()
        {
            // Property getter/setter
            var val = new ComposedNonBlittableStruct()
            {
                blittable = new BlittableStruct(){ i32 = 42 },
                strings = new NonBlittableStringStruct(){ str = "I like tacos" },
                bools = new NonBlittableBoolStruct(){ w = true, x = false, y = true, z = false },
                refs = TestObject.NonBlittableRefStructProperty // TODO: Need to either support interface inheritance or project IReference/INullable for setter
            };
            TestObject.ComposedNonBlittableStructProperty = val;
            Assert.Equal(42, TestObject.ComposedNonBlittableStructProperty.blittable.i32);
            Assert.Equal("I like tacos", TestObject.ComposedNonBlittableStructProperty.strings.str);
            Assert.True(TestObject.ComposedNonBlittableStructProperty.bools.w);
            Assert.False(TestObject.ComposedNonBlittableStructProperty.bools.x);
            Assert.True(TestObject.ComposedNonBlittableStructProperty.bools.y);
            Assert.False(TestObject.ComposedNonBlittableStructProperty.bools.z);

            // Manual getter
            Assert.Equal(42, TestObject.GetComposedNonBlittableStruct().blittable.i32);
            Assert.Equal("I like tacos", TestObject.GetComposedNonBlittableStruct().strings.str);
            Assert.True(TestObject.GetComposedNonBlittableStruct().bools.w);
            Assert.False(TestObject.GetComposedNonBlittableStruct().bools.x);
            Assert.True(TestObject.GetComposedNonBlittableStruct().bools.y);
            Assert.False(TestObject.GetComposedNonBlittableStruct().bools.z);

            // Manual setter
            val.blittable.i32 = 8;
            val.strings.str = "Hello, world";
            val.bools.w = false;
            val.bools.x = true;
            val.bools.y = false;
            val.bools.z = true;
            TestObject.SetComposedNonBlittableStruct(val);
            Assert.Equal(8, TestObject.ComposedNonBlittableStructProperty.blittable.i32);
            Assert.Equal("Hello, world", TestObject.ComposedNonBlittableStructProperty.strings.str);
            Assert.False(TestObject.ComposedNonBlittableStructProperty.bools.w);
            Assert.True(TestObject.ComposedNonBlittableStructProperty.bools.x);
            Assert.False(TestObject.ComposedNonBlittableStructProperty.bools.y);
            Assert.True(TestObject.ComposedNonBlittableStructProperty.bools.z);

            // Output argument
            val = default;
            TestObject.OutComposedNonBlittableStruct(out val);
            Assert.Equal(8, val.blittable.i32);
            Assert.Equal("Hello, world", val.strings.str);
            Assert.False(val.bools.w);
            Assert.True(val.bools.x);
            Assert.False(val.bools.y);
            Assert.True(val.bools.z);
        }

        [Fact]
        public void TestGenericCast()
        {
            var ints = TestObject.GetIntVector();
            var abiView = (ABI.Windows.Foundation.Collections.IVectorView<int>)ints;
            Assert.Equal(abiView.ThisPtr, abiView.As<WinRT.IInspectable>().As<ABI.Windows.Foundation.Collections.IVectorView<int>.Vftbl>().ThisPtr);
        }

        [Fact]
        public void TestFundamentalGeneric()
        {
            var ints = TestObject.GetIntVector();
            Assert.Equal(10u, ints.Size);
            for (int i = 0; i < 10; ++i)
            {
                Assert.Equal(i, ints.GetAt((uint)i));
            }

            var bools = TestObject.GetBoolVector();
            Assert.Equal(4u, bools.Size);
            for (uint i = 0; i < 4u; ++i)
            {
                Assert.Equal(i % 2 == 0, bools.GetAt(i));
            }
        }

        [Fact]
        public void TestStringGeneric()
        {
            var strings = TestObject.GetStringVector();
            Assert.Equal(5u, strings.Size);
            for (uint i = 0; i < 5u; ++i)
            {
                Assert.Equal("String" + i, strings.GetAt(i));
            }
        }

        [Fact]
        public void TestStructGeneric()
        {
            var blittable = TestObject.GetBlittableStructVector();
            Assert.Equal(5u, blittable.Size);
            for (int i = 0; i < 5; ++i)
            {
                Assert.Equal(i, blittable.GetAt((uint)i).blittable.i32);
            }

            var nonblittable = TestObject.GetNonBlittableStructVector();
            Assert.Equal(3u, nonblittable.Size);
            for (int i = 0; i < 3; ++i)
            {
                var val = nonblittable.GetAt((uint)i);
                Assert.Equal(i, val.blittable.i32);
                Assert.Equal("String" + i, val.strings.str);
                Assert.Equal(i % 2 == 0, val.bools.w);
                Assert.Equal(i % 2 == 1, val.bools.x);
                Assert.Equal(i % 2 == 0, val.bools.y);
                Assert.Equal(i % 2 == 1, val.bools.z);
                Assert.Equal(i, val.refs.ref32.Value);
            }
        }

        [Fact]
        public void TestObjectGeneric()
        {
            var objs = TestObject.GetObjectVector();
            Assert.Equal(3u, objs.Size);
            for (int i = 0; i < 3; ++i)
            {
                // TOOD: casting projection needs some work
                IPropertyValue propVal = new ABI.Windows.Foundation.IPropertyValue(objs.GetAt((uint)i).As<ABI.Windows.Foundation.IPropertyValue.Vftbl>());
                Assert.Equal(i, propVal.GetInt32());
            }
        }

        [Fact]
        void TestInterfaceGeneric()
        {
            var objs = TestObject.GetInterfaceVector();
            Assert.Equal(3u, objs.Size);
            TestObject.ReadWriteProperty = 42;
            for (uint i = 0; i < 3; ++i)
            {
                var obj = objs.GetAt(i);
                // TODO: Validate that each item 'is' TestObject (RCW caching)
                //Assert.Same(obj, TestObject);
                Assert.Equal(42, obj.ReadWriteProperty);
            }
        }

        [Fact]
        void TestClassGeneric()
        {
            var objs = TestObject.GetClassVector();
            Assert.Equal(3u, objs.Size);
            for (uint i = 0; i < 3; ++i)
            {
                var obj = objs.GetAt(i);
                // TODO: Validate that each item 'is' TestObject (RCW caching)
                //Assert.Same(obj, TestObject);
                Assert.Equal(TestObject.ThisPtr, objs.GetAt(i).ThisPtr);
            }
        }

        readonly int E_FAIL = -2147467259;

        async Task InvokeDoitAsync()
        {
            await TestObject.DoitAsync();
        }

        [Fact]
        public void TestAsyncAction()
        {
            var task = InvokeDoitAsync();
            Assert.False(task.Wait(25));
            TestObject.CompleteAsync();
            Assert.True(task.Wait(1000));
            Assert.Equal(TaskStatus.RanToCompletion, task.Status);

            task = InvokeDoitAsync();
            Assert.False(task.Wait(25));
            TestObject.CompleteAsync(E_FAIL);
            var e = Assert.Throws<AggregateException>(() => task.Wait(1000));
            Assert.Equal(E_FAIL, e.InnerException.HResult);
            Assert.Equal(TaskStatus.Faulted, task.Status);

            var src = new CancellationTokenSource();
            task = TestObject.DoitAsync().AsTask(src.Token);
            Assert.False(task.Wait(25));
            src.Cancel();
            e = Assert.Throws<AggregateException>(() => task.Wait(1000));
            Assert.True(e.InnerException is TaskCanceledException);
            Assert.Equal(TaskStatus.Canceled, task.Status);
        }

        async Task InvokeDoitAsyncWithProgress()
        {
            await TestObject.DoitAsyncWithProgress();
        }

        [Fact]
        public void TestAsyncActionWithProgress()
        {
            int progress = 0;
            var evt = new AutoResetEvent(false);
            var task = TestObject.DoitAsyncWithProgress().AsTask(new Progress<int>((v) =>
            {
                progress = v;
                evt.Set();
            }));

            for (int i = 1; i <= 10; ++i)
            {
                TestObject.AdvanceAsync(10);
                Assert.True(evt.WaitOne(1000));
                Assert.Equal(10 * i, progress);
            }

            TestObject.CompleteAsync();
            Assert.True(task.Wait(1000));
            Assert.Equal(TaskStatus.RanToCompletion, task.Status);

            task = InvokeDoitAsyncWithProgress();
            TestObject.CompleteAsync(E_FAIL);
            var e = Assert.Throws<AggregateException>(() => task.Wait(1000));
            Assert.Equal(E_FAIL, e.InnerException.HResult);
            Assert.Equal(TaskStatus.Faulted, task.Status);

            var src = new CancellationTokenSource();
            task = TestObject.DoitAsyncWithProgress().AsTask(src.Token);
            Assert.False(task.Wait(25));
            src.Cancel();
            e = Assert.Throws<AggregateException>(() => task.Wait(1000));
            Assert.True(e.InnerException is TaskCanceledException);
            Assert.Equal(TaskStatus.Canceled, task.Status);
        }

        async Task<int> InvokeAddAsync(int lhs, int rhs)
        {
            return await TestObject.AddAsync(lhs, rhs);
        }

        [Fact]
        public void TestAsyncOperation()
        {
            var task = InvokeAddAsync(42, 8);
            Assert.False(task.Wait(25));
            TestObject.CompleteAsync();
            Assert.True(task.Wait(1000));
            Assert.Equal(TaskStatus.RanToCompletion, task.Status);
            Assert.Equal(50, task.Result);

            task = InvokeAddAsync(0, 0);
            Assert.False(task.Wait(25));
            TestObject.CompleteAsync(E_FAIL);
            var e = Assert.Throws<AggregateException>(() => task.Wait(1000));
            Assert.Equal(E_FAIL, e.InnerException.HResult);
            Assert.Equal(TaskStatus.Faulted, task.Status);

            var src = new CancellationTokenSource();
            task = TestObject.AddAsync(0, 0).AsTask(src.Token);
            Assert.False(task.Wait(25));
            src.Cancel();
            e = Assert.Throws<AggregateException>(() => task.Wait(1000));
            Assert.True(e.InnerException is TaskCanceledException);
            Assert.Equal(TaskStatus.Canceled, task.Status);
        }

        async Task<int> InvokeAddAsyncWithProgress(int lhs, int rhs)
        {
            return await TestObject.AddAsyncWithProgress(lhs, rhs);
        }

        [Fact]
        public void TestAsyncOperationWithProgress()
        {
            int progress = 0;
            var evt = new AutoResetEvent(false);
            var task = TestObject.AddAsyncWithProgress(42, 8).AsTask(new Progress<int>((v) =>
            {
                progress = v;
                evt.Set();
            }));

            for (int i = 1; i <= 10; ++i)
            {
                TestObject.AdvanceAsync(10);
                Assert.True(evt.WaitOne(1000));
                Assert.Equal(10 * i, progress);
            }

            TestObject.CompleteAsync();
            Assert.True(task.Wait(1000));
            Assert.Equal(TaskStatus.RanToCompletion, task.Status);
            Assert.Equal(50, task.Result);

            task = InvokeAddAsyncWithProgress(0, 0);
            TestObject.CompleteAsync(E_FAIL);
            var e = Assert.Throws<AggregateException>(() => task.Wait(1000));
            Assert.Equal(E_FAIL, e.InnerException.HResult);
            Assert.Equal(TaskStatus.Faulted, task.Status);

            var src = new CancellationTokenSource();
            task = TestObject.AddAsyncWithProgress(0, 0).AsTask(src.Token);
            Assert.False(task.Wait(25));
            src.Cancel();
            e = Assert.Throws<AggregateException>(() => task.Wait(1000));
            Assert.True(e.InnerException is TaskCanceledException);
            Assert.Equal(TaskStatus.Canceled, task.Status);
        }
    }
}
