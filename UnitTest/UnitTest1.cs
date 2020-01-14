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
    using A = IIterable<IStringable>;
    using B = IKeyValuePair<string, IAsyncOperationWithProgress</*A*/IIterable<IStringable>, float>>;

    public class TestGuids
    {
        private static void AssertGuid<T>(string expected)
        {
            var actual = GuidGenerator.CreateIID(typeof(T));
            Assert.Equal(actual, new Guid(expected));
        }

        [Fact]
        public void TestGenerics()
        {
            // Ensure every generic instance has a unique PIID
            Assert.NotEqual(ABI.Windows.Foundation.Collections.IMap<bool, string>.Vftbl.PIID, ABI.Windows.Foundation.Collections.IMap<string, bool>.Vftbl.PIID);

            AssertGuid<IStringable>("96369f54-8eb6-48f0-abce-c1b211e627c3");

            // Generated Windows.Foundation GUIDs
            AssertGuid<IAsyncActionWithProgress<A>>("dd725452-2da3-5103-9c7d-22ee9bb14ad3");
            AssertGuid<IAsyncOperationWithProgress<A, B>>("94645425-b9e5-5b91-b509-8da4df6a8916");
            AssertGuid<IAsyncOperation<A>>("2bd35ee6-72d9-5c5d-9827-05ebb81487ab");
            AssertGuid<IReferenceArray<A>>("4a33fe03-e8b9-5346-a124-5449913eca57");
            AssertGuid<IReference<A>>("f9e4006c-6e8c-56df-811c-61f9990ebfb0");
            AssertGuid<AsyncActionProgressHandler<A>>("c261d8d0-71ba-5f38-a239-872342253a18");
            AssertGuid<AsyncActionWithProgressCompletedHandler<A>>("9a0d211c-0374-5d23-9e15-eaa3570fae63");
            AssertGuid<AsyncOperationCompletedHandler<A>>("9d534225-231f-55e7-a6d0-6c938e2d9160");
            AssertGuid<AsyncOperationProgressHandler<A, B>>("264f1e0c-abe4-590b-9d37-e1cc118ecc75");
            AssertGuid<AsyncOperationWithProgressCompletedHandler<A, B>>("c2d078d8-ac47-55ab-83e8-123b2be5bc5a");
            AssertGuid<WF.EventHandler<A>>("fa0b7d80-7efa-52df-9b69-0574ce57ada4");
            AssertGuid<TypedEventHandler<A, B>>("edb31843-b4cf-56eb-925a-d4d0ce97a08d");

            // Generated Windows.Foundation.Collections GUIDs
            AssertGuid<IIterable<A>>("96565eb9-a692-59c8-bcb5-647cde4e6c4d");
            AssertGuid<IIterator<A>>("3c9b1e27-8357-590b-8828-6e917f172390");
            AssertGuid<IKeyValuePair<A, B>>("89336cd9-8b66-50a7-9759-eb88ccb2e1fe");
            AssertGuid<IMapChangedEventArgs<A>>("e1aa5138-12bd-51a1-8558-698dfd070abe");
            AssertGuid<IMapView<A, B>>("b78f0653-fa89-59cf-ba95-726938aae666");
            AssertGuid<IMap<A, B>>("9962cd50-09d5-5c46-b1e1-3c679c1c8fae");
            AssertGuid<IObservableMap<A, B>>("75f99e2a-137e-537e-a5b1-0b5a6245fc02");
            AssertGuid<IObservableVector<A>>("d24c289f-2341-5128-aaa1-292dd0dc1950");
            AssertGuid<IVectorView<A>>("5f07498b-8e14-556e-9d2e-2e98d5615da9");
            AssertGuid<IVector<A>>("0e3f106f-a266-50a1-8043-c90fcf3844f6");
            AssertGuid<MapChangedEventHandler<A, B>>("19046f0b-cf81-5dec-bbb2-7cc250da8b8b");
            AssertGuid<VectorChangedEventHandler<A>>("a1e9acd7-e4df-5a79-aefa-de07934ab0fb");

            // Generated primitive GUIDs
            AssertGuid<IReference<bool>>("3c00fd60-2950-5939-a21a-2d12c5a01b8a");
            AssertGuid<IReference<sbyte>>("95500129-fbf6-5afc-89df-70642d741990");
            AssertGuid<IReference<Int16>>("6ec9e41b-6709-5647-9918-a1270110fc4e");
            AssertGuid<IReference<Int32>>("548cefbd-bc8a-5fa0-8df2-957440fc8bf4");
            AssertGuid<IReference<Int64>>("4dda9e24-e69f-5c6a-a0a6-93427365af2a");
            AssertGuid<IReference<byte>>("e5198cc8-2873-55f5-b0a1-84ff9e4aad62");
            AssertGuid<IReference<UInt16>>("5ab7d2c3-6b62-5e71-a4b6-2d49c4f238fd");
            AssertGuid<IReference<UInt32>>("513ef3af-e784-5325-a91e-97c2b8111cf3");
            AssertGuid<IReference<UInt64>>("6755e376-53bb-568b-a11d-17239868309e");
            AssertGuid<IReference<float>>("719cc2ba-3e76-5def-9f1a-38d85a145ea8");
            AssertGuid<IReference<double>>("2f2d6c29-5473-5f3e-92e7-96572bb990e2");
            AssertGuid<IReference<char>>("fb393ef3-bbac-5bd5-9144-84f23576f415");
            AssertGuid<IReference<Guid>>("7d50f649-632c-51f9-849a-ee49428933ea");
            AssertGuid<IReference<HResult>>("6ff27a1e-4b6a-59b7-b2c3-d1f2ee474593");
            AssertGuid<IReference<string>>("fd416dfb-2a07-52eb-aae3-dfce14116c05");
            //AssertGuid<IReference<event_token>>("a9b18291-ce2a-5dae-8a23-b7f7388416db");
            AssertGuid<IReference<WF.TimeSpan>>("604d0c4c-91de-5c2a-935f-362f13eaf800");
            AssertGuid<IReference<WF.DateTime>>("5541d8a7-497c-5aa4-86fc-7713adbf2a2c");
            AssertGuid<IReference<Point>>("84f14c22-a00a-5272-8d3d-82112e66df00");
            AssertGuid<IReference<Rect>>("80423f11-054f-5eac-afd3-63b6ce15e77b");
            AssertGuid<IReference<Size>>("61723086-8e53-5276-9f36-2a4bb93e2b75");

            // Enums, structs, IInspectable, classes, and delegates
            AssertGuid<IReference<PropertyType>>("ecebde54-fac0-5aeb-9ba9-9e1fe17e31d5");
            AssertGuid<IReference<Point>>("84f14c22-a00a-5272-8d3d-82112e66df00");
            AssertGuid<IVector<IInspectable>>("b32bdca4-5e52-5b27-bc5d-d66a1a268c2a");
            AssertGuid<IVector<WF.Uri>>("0d82bd8d-fe62-5d67-a7b9-7886dd75bc4e");
            AssertGuid<IVector<AsyncActionCompletedHandler>>("5dafe591-86dc-59aa-bfda-07f5d59fc708");
        }
    }


    public class TestComponent
    {
        public Class TestObject { get; private set; }

        public TestComponent()
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
            // BUG : Managed to ABI delegate marshaling is broken (ref count not stable causing premature GC).
            // Should return Delegate.InitialReference from ToAbi, within a using statement to enforce Dispose.
            return;

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

        /* TODO: Events are currently broken for value types
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
        */

        [Fact]
        public void TestStrings()
        {
            string test_string = "x";
            string test_string2 = "y";

            var href = new WinRT.HStringReference(test_string);

            // In hstring from managed->native implicitly creates hstring reference
            TestObject.StringProperty = test_string;

            // Out hstring from native->managed only creates System.String on demand
            var sp = TestObject.StringProperty;
            Assert.Equal(sp, test_string);

            // Out hstring from managed->native always creates HString from System.String
            TestObject.CallForString(() => test_string2);
            Assert.Equal(TestObject.StringProperty, test_string2);

            // In hstring from native->managed only creates System.String on demand
            TestObject.StringPropertyChanged += (Class sender, WinRT.HString value) => sender.StringProperty2 = value;
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

        /* TODO: Object types are currently not handled by generics
        [Fact]
        public void TestObjectGeneric()
        {
            var objs = TestObject.GetObjectVector();
            Assert.Equal(3u, objs.Size);
            for (int i = 0; i < 3; ++i)
            {
                IPropertyValue propVal = new ABI.Windows.Foundation.IPropertyValue(objs.GetAt((uint)i).As<ABI.Windows.Foundation.IPropertyValue>());
                Assert.Equal(i, propVal.GetInt32());
            }
        }
        */

        /* TODO: Interface types are currently not handled by generics
        [Fact]
        void TestInterfaceGeneric()
        {
            var objs = TestObject.GetInterfaceVector();
            Assert.Equal(3u, objs.Size);
            TestObject.ReadWriteProperty = 42;
            for (uint i = 0; i < 3; ++i)
            {
                // TODO: Validate that each item 'is' TestObject
                Assert.Equal(42, objs.GetAt(i).ReadWriteProperty);
            }
        }
        */

        /* TODO: Class types are currently not handled by generics
        [Fact]
        void TestClassGeneric()
        {
            var objs = TestObject.GetClassVector();
            Assert.Equal(3u, objs.Size);
            for (uint i = 0; i < 3; ++i)
            {
                // TODO: Assert.Equal(TestObject, objs.GetAt(i));
                Assert.Equal(TestObject.ThisPtr, objs.GetAt(i).ThisPtr);
            }
        }
        */

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
        }
    }
}
