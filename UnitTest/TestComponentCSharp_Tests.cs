using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using WinRT;

using Windows.Foundation;
using Windows.UI;
using Windows.Storage;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Interop;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Media3D;

using TestComponentCSharp;
using System.Collections.Generic;
using System.Collections;
using WinRT.Interop;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using System.Reflection;

#if NET5_0
using WeakRefNS = System;
#else
using WeakRefNS = WinRT;
#endif

namespace UnitTest
{
    public class TestCSharp
    {
        public Class TestObject { get; private set; }

        public TestCSharp()
        {
            TestObject = new Class();
        }

        [Fact]
        public void TestGetByte()
        {
            var array = new byte[] { 0x01 };
            var buff = array.AsBuffer();
            Assert.True(buff.Length == 1);
            byte b = buff.GetByte(0);
            Assert.True(b == 0x01);
        }

        [Fact]
        public void TestManyBufferExtensionMethods()
        {
            var arrayLen3 = new byte[] { 0x01, 0x02, 0x03 };
            var buffLen3 = arrayLen3.AsBuffer();

            var arrayLen4 = new byte[] { 0x11, 0x12, 0x13, 0x14 };
            var buffLen4 = arrayLen4.AsBuffer();

            var arrayLen4Again = new byte[4];

            arrayLen3.CopyTo(1, buffLen4, 0, 1); // copy just the second element of the array to the beginning of the buffer 
            Assert.True(buffLen4.Length == 4);
            Assert.Throws<ArgumentException>(() => buffLen4.GetByte(5)); // shouldn't have a 5th element
            Assert.True(buffLen4.GetByte(0) == 0x02); // make sure we got the 2nd element of the array
            
            arrayLen3.CopyTo(buffLen4); // Array to Buffer copying
            Assert.True(buffLen4.Length == 4);
            Assert.True(buffLen4.GetByte(0) == 0x01); // make sure we updated the first few 
            Assert.True(buffLen4.GetByte(1) == 0x02); 
            Assert.True(buffLen4.GetByte(2) == 0x03);
            Assert.True(buffLen4.GetByte(3) == 0x14); // and kept the last one 

            var buffLen3Again = buffLen3.ToArray().AsBuffer();
            Assert.True(buffLen3Again.GetByte(0) == 0x01);
            Assert.True(buffLen3Again.GetByte(1) == 0x02);
            Assert.True(buffLen3Again.GetByte(2) == 0x03);

            Assert.False(buffLen3.IsSameData(buffLen3Again)); // different memory regions

            buffLen4.CopyTo(arrayLen4Again);  // Buffer to Array copying
            var array4 = buffLen4.ToArray();
            Assert.True(arrayLen4Again.Length == array4.Length);
            for (int i = 0; i < arrayLen4Again.Length; ++i)
            {
                Assert.True(arrayLen4Again[i] == array4[i]); // make sure we have equal array
            }
        }

        [Fact]
        public void TestIsSameDataDifferentArrays()
        {
            var arr = new byte[] { 0x01, 0x02 };
            var buf1 = arr.AsBuffer();
            var arr2 = new byte[] { 0x01, 0x02 };
            var buf2 = arr2.AsBuffer();
            Assert.False(buf1.IsSameData(buf2));
        }

        [Fact]
        public void TestIsSameDataUsingCopyTo()
        {
            var arr = new byte[] { 0x01, 0x02 };
            var buf1 = arr.AsBuffer();
            var buf2 = new Windows.Storage.Streams.Buffer(2);
            buf1.CopyTo(buf2);
            Assert.False(buf1.IsSameData(buf2));
        }

        [Fact]
        public void TestIsSameDataUsingAsBufferTwice()
        {
            var arr = new byte[] { 0x01, 0x02 };
            var buf1 = arr.AsBuffer();
            var buf2 = arr.AsBuffer();
            Assert.True(buf1.IsSameData(buf2));
        }

        [Fact]
        public void TestIsSameDataUsingToArray()
        {
            var arr = new byte[] { 0x01, 0x02 };
            var buf1 = arr.AsBuffer();
            var buf2 = buf1.ToArray().AsBuffer();
            Assert.False(buf1.IsSameData(buf2));
        }

        [Fact]
        public void TestBufferAsStreamUsingAsBuffer()
        {
            var arr = new byte[] { 0x01, 0x02 };
            Stream stream = arr.AsBuffer().AsStream();            
            Assert.True(stream != null);
            Assert.True(stream.Length == 2);
        }

        [Fact]
        public void TestBufferAsStreamWithEmptyBuffer1()
        { 
            var buffer = new Windows.Storage.Streams.Buffer(0);
            Stream stream = buffer.AsStream();
            Assert.True(stream != null);
            Assert.True(stream.Length == 0);
        }

        [Fact]
        public void TestBufferImproperReadCopyToOutOfBounds()
        {
            var array = new byte[] { 0x01, 0x02, 0x03 };
            var buffer = array.AsBuffer();
            var biggerBuffer = new Windows.Storage.Streams.Buffer(5);
            buffer.CopyTo(biggerBuffer);
            Assert.Throws<ArgumentException>(() => biggerBuffer.ToArray(4, 2));
        }

        [Fact]
        public void TestBufferImproperReadCopyToStraddleBounds()
        {
            var array = new byte[] { 0x01, 0x02, 0x03 };
            var buffer = array.AsBuffer();
            var biggerBuffer = new Windows.Storage.Streams.Buffer(5);
            buffer.CopyTo(biggerBuffer);
            Assert.Throws<ArgumentException>(() => biggerBuffer.ToArray(2, 2));
        }

        [Fact]
        public void TestBufferImproperReadGetByte()
        {
            var array = new byte[] { 0x01, 0x02, 0x03 };
            var buffer = array.AsBuffer();
            Assert.Throws<ArgumentException>(() => buffer.GetByte(4));
        }

        [Fact]
        public void TestEmptyBufferToArray()
        {
            var buffer = new Windows.Storage.Streams.Buffer(0);
            var array = buffer.ToArray();
            Assert.True(array.Length == 0);
        }

        [Fact]
        public void TestArrayCopyToBufferEndToBeginning()
        {
            IBuffer buf = new Windows.Storage.Streams.Buffer(3);
            byte[] arr = new byte[] { 0x01, 0x02, 0x03 };
            arr.CopyTo(3, buf, 0, 0);
        }

        [Fact]
        public void TestArrayCopyToBufferEndToEnd2()
        {
            IBuffer buf = new Windows.Storage.Streams.Buffer(3);
            byte[] arr = new byte[] { 0x01, 0x02, 0x03 };
            arr.CopyTo(0, buf, 0, 3);
        }

        [Fact]
        public void TestArrayCopyToBufferEndToEnd()
        {
            IBuffer buf = new Windows.Storage.Streams.Buffer(3);
            byte[] arr = new byte[] { 0x01, 0x02, 0x03 };
            arr.CopyTo(3, buf, 3, 0);
        }

        [Fact]
        public void TestArrayCopyToBufferMidToMid()
        {
            IBuffer buf = new Windows.Storage.Streams.Buffer(3);
            byte[] arr = new byte[] { 0x01, 0x02, 0x03 };
            arr.CopyTo(1, buf, 1, 0);
        }

        [Fact]
        public void TestArrayCopyToBufferMidToEnd()
        {
            IBuffer buf = new Windows.Storage.Streams.Buffer(3);
            byte[] arr = new byte[] { 0x01, 0x02, 0x03 };
            arr.CopyTo(1, buf, 3, 0);
        }

        [Fact]
        public void TestBufferCopyToArrayEndToEnd()
        {
            byte[] arr = new byte[] { 0x01, 0x02, 0x03 };
            var buf = arr.AsBuffer();
            var target = new byte[4];
            buf.CopyTo(3, target, 4, 0);
        }

        [Fact]
        public void BufferToArrayWithZeroCountAtEnd2()
        {
            byte[] array = { 0xA1, 0xA2, 0xA3 };
            var result = array.AsBuffer().ToArray(3, 0);
            Assert.True(result != null);
            Assert.True(0 == result.Length);
        }

        [Fact]
        public void BufferToArrayWithZeroCountAtEnd_WorksWithSpans()
        {
            byte[] array = { 0xA1, 0xA2, 0xA3 };
            var result = array.AsSpan().Slice(3, 0).ToArray();
            Assert.True(result != null);
            Assert.True(0 == result.Length);
        }

        [Fact]
        public void TestWinRTBufferWithZeroLength()
        {
            byte[] arr = new byte[] { 0x01, 0x02, 0x03 };
            MemoryStream stream = new MemoryStream(arr, 0, 3, false, true);
            IBuffer buff = stream.GetWindowsRuntimeBuffer(3, 0);
            Assert.True(buff != null);
            Assert.True(buff.Length == 0);
        }

        [Fact]
        public void TestEmptyBufferCopyTo()
        { 
            var buffer = new Windows.Storage.Streams.Buffer(0);
            byte[] array = { };
            buffer.CopyTo(array);
            Assert.True(array.Length == 0);
        }

        [Fact]
        public void TestTypePropertyWithSystemType()
        {
            TestObject.TypeProperty = typeof(System.Type);
            Assert.Equal("Windows.UI.Xaml.Interop.TypeName", TestObject.GetTypePropertyAbiName());
            Assert.Equal("Metadata", TestObject.GetTypePropertyKind());
        }

        class CustomDictionary : Dictionary<string, string> { }

        [Fact]
        public void TestTypePropertyWithCustomType()
        {
            TestObject.TypeProperty = typeof(CustomDictionary);
            var name = TestObject.GetTypePropertyAbiName();
            Assert.Equal("UnitTest.TestCSharp+CustomDictionary, UnitTest, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", name);
        }

        [Fact]
        public void TestVectorCastConversion()
        {
            var vector = TestObject.GetUriVectorAsIInspectableVector();
            var uriVector = vector.Cast<Uri>();
            var first = uriVector.First();
            Assert.Equal(vector, uriVector);
        }

        async Task LookupPorts()
        {
            var ports = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(
                Windows.Devices.SerialCommunication.SerialDevice.GetDeviceSelector(),
                new string[] { "System.ItemNameDisplay" });
            foreach (var port in ports)
            {
                object o = port.Properties["System.ItemNameDisplay"];
                Assert.NotNull(o);
            }
        }

        [Fact]
        public void TestReadOnlyDictionaryLookup()
        {
            Assert.True(LookupPorts().Wait(1000));
        }

#if NET5_0
        [Fact]
        public void TestAsStream()
        {
            using InMemoryRandomAccessStream winrtStream = new InMemoryRandomAccessStream();
            using Stream normalStream = winrtStream.AsStream();
            using var memoryStream = new MemoryStream();
            normalStream.CopyTo(memoryStream);
        }

        async Task InvokeStreamWriteAndReadAsync()
        {
            var random = new Random(42);
            byte[] data = new byte[256];
            random.NextBytes(data);

            using var stream = new InMemoryRandomAccessStream().AsStream();
            await stream.WriteAsync(data, 0, data.Length);
            stream.Seek(0, SeekOrigin.Begin);

            byte[] read = new byte[256];
            await stream.ReadAsync(read, 0, read.Length);
            Assert.Equal(read, data);
        }

        [Fact]
        public void TestStreamWriteAndRead()
        {
            Assert.True(InvokeStreamWriteAndReadAsync().Wait(1000));
        }

        [Fact]
        public void TestDynamicInterfaceCastingOnValidInterface()
        {
            var agileObject = (IAgileObject)(IWinRTObject)TestObject;
            Assert.NotNull(agileObject);
        }

        [Fact]
        public void TestDynamicInterfaceCastingOnInvalidInterface()
        {
            Assert.ThrowsAny<System.Exception>(() => (IStringableInterop)(IWinRTObject)TestObject);
        }

        [Fact]
        public void TestBuffer()
        {
            var arr1 = new byte[] { 0x01, 0x02 };
            var buff = arr1.AsBuffer();
            var arr2 = buff.ToArray(0,2);
            Assert.True(arr1[0] == arr2[0]);
            Assert.True(arr1[1] == arr2[1]);
        }

#endif

        async Task TestStorageFileAsync()
        {
            var folderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            StorageFile file = await StorageFile.GetFileFromPathAsync(folderPath + "\\UnitTest.dll");
            var handle = WindowsRuntimeStorageExtensions.CreateSafeFileHandle(file, FileAccess.Read);
            Assert.NotNull(handle);
        }

        [Fact]
        public void TestStorageFile()
        {
            Assert.True(TestStorageFileAsync().Wait(1000));
        }

        async Task TestStorageFolderAsync()
        {
            var folderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(folderPath);
            var handle = WindowsRuntimeStorageExtensions.CreateSafeFileHandle(folder, "UnitTest.dll", FileMode.Open, FileAccess.Read);
            Assert.NotNull(handle);
        }

        [Fact]
        public void TestStorageFolder()
        {
            Assert.True(TestStorageFolderAsync().Wait(1000));
        }

        async Task InvokeWriteBufferAsync()
        {
            var random = new Random(42);
            byte[] data = new byte[256];
            random.NextBytes(data);

            using var stream = new InMemoryRandomAccessStream();
            IBuffer buffer = data.AsBuffer();
            await stream.WriteAsync(buffer);
        }

        [Fact]
        public void TestWriteBuffer()
        {
            Assert.True(InvokeWriteBufferAsync().Wait(1000)); 
        }

        [Fact]
        public void TestUri()
        {
            var base_uri = "https://github.com";
            var relative_uri = "microsoft/CsWinRT";
            var full_uri = base_uri + "/" + relative_uri;
            var managedUri = new Uri(full_uri);

            var uri1 = ABI.System.Uri.FromAbi(ABI.System.Uri.FromManaged(managedUri));
            var str1 = uri1.ToString();
            Assert.Equal(full_uri, str1);

            var expected = new Uri("http://expected");
            TestObject.UriProperty = expected;
            Assert.Equal(expected, TestObject.UriProperty);

            TestObject.CallForUri(() => managedUri);
            TestObject.UriPropertyChanged +=
                (object sender, Uri value) => Assert.Equal(managedUri, value);
            TestObject.RaiseUriChanged();

            var uri2 = MarshalInspectable<System.Uri>.FromAbi(ABI.System.Uri.FromManaged(managedUri));
            var str2 = uri2.ToString();
            Assert.Equal(full_uri, str2);

            var uri3 = MarshalInspectable<object>.FromAbi(ABI.System.Uri.FromManaged(managedUri));
            var str3 = uri3.ToString();
            Assert.Equal(full_uri, str3);
        }

        [Fact]
        public void TestNulls()
        {
            TestObject.StringProperty = null;
            Assert.Equal("", TestObject.StringProperty);
            TestObject.CallForString(() => null);
            TestObject.StringPropertyChanged +=
                (Class sender, string value) => Assert.Equal("", value);
            TestObject.RaiseStringChanged();

            TestObject.UriProperty = null;
            Assert.Null(TestObject.UriProperty);
            TestObject.CallForUri(() => null);
            TestObject.UriPropertyChanged +=
                (object sender, Uri value) => Assert.Null(value);
            TestObject.RaiseUriChanged();

            TestObject.ObjectProperty = null;
            Assert.Null(TestObject.ObjectProperty);
            TestObject.CallForObject(() => null);
            TestObject.ObjectPropertyChanged +=
                (object sender, Object value) => Assert.Null(value);
            TestObject.RaiseObjectChanged();

            // todo: arrays, delegates, event args, mapped types...
        }

        [Fact]
        public void TestEvents()
        {
            int events_expected = 0;
            int events_received = 0;

            TestObject.Event0 += () => events_received++;
            TestObject.InvokeEvent0();
            events_expected++;

            TestObject.Event1 += (Class sender) =>
            {
                events_received++;
                Assert.IsAssignableFrom<Class>(sender);
            };
            TestObject.InvokeEvent1(TestObject);
            events_expected++;

            int int0 = 42;
            TestObject.Event2 += (Class sender, int arg0) =>
            {
                events_received++;
                Assert.Equal(arg0, int0);
            };
            TestObject.InvokeEvent2(TestObject, int0);
            events_expected++;

            string string1 = "foo";
            TestObject.Event3 += (Class sender, int arg0, string arg1) =>
            {
                events_received++;
                Assert.Equal(arg1, string1);
            };
            TestObject.InvokeEvent3(TestObject, int0, string1);
            events_expected++;

            int[] ints = { 1, 2, 3 };
            TestObject.NestedEvent += (object sender, IList<int> arg0) =>
            {
                events_received++;
                Assert.True(arg0.SequenceEqual(ints));
            };
            TestObject.InvokeNestedEvent(TestObject, ints);
            events_expected++;

            TestObject.ReturnEvent += (int arg0) =>
            {
                events_received++;
                return arg0;
            };
            Assert.Equal(42, TestObject.InvokeReturnEvent(42));
            events_expected++;

            var collection0 = new int[] { 42, 1729 };
            var collection1 = new Dictionary<int, string> { [1] = "foo", [2] = "bar" };
            TestObject.CollectionEvent += (Class sender, IList<int> arg0, IDictionary<int, string> arg1) =>
            {
                events_received++;
                Assert.True(arg0.SequenceEqual(collection0));
                Assert.True(arg1.SequenceEqual(collection1));
            };
            TestObject.InvokeCollectionEvent(TestObject, collection0, collection1);
            events_expected++;

            Assert.Equal(events_received, events_expected);
        }

        class ManagedUriHandler : IUriHandler
        {
            public Class TestObject { get; private set; }

            public ManagedUriHandler(Class testObject)
            {
                TestObject = testObject;
            }

            public void AddUriHandler(ProvideUri provideUri)
            {
                TestObject.CallForUri(provideUri);
                Assert.Equal(new Uri("http://github.com"), TestObject.UriProperty);
            }
        }

        [Fact]
        public void TestDelegateUnwrapping()
        {
            var obj = TestObject.GetUriDelegate();
            TestObject.CallForUri(obj);
            Assert.Equal(new Uri("http://microsoft.com"), TestObject.UriProperty);

            TestObject.AddUriHandler(new ManagedUriHandler(TestObject));
        }

        // TODO: when the public WinUI nuget supports IXamlServiceProvider, just use the projection
        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("68B3A2DF-8173-539F-B524-C8A2348F5AFB")]
        internal unsafe interface IServiceProviderInterop
        {
            // Note: Invoking methods on ComInterfaceType.InterfaceIsIInspectable interfaces
            // no longer appears supported in the runtime (probably with removal of WinRT support),
            // so simulate with IUnknown.
            void GetIids(out int iidCount, out IntPtr iids);
            void GetRuntimeClassName(out IntPtr className);
            void GetTrustLevel(out TrustLevel trustLevel);

            void GetService(IntPtr type, out IntPtr service);
        }

        [Fact]
        public void TestCustomProjections()
        {
            // INotifyDataErrorsInfo
            string propertyName = "";
            TestObject.ErrorsChanged += (object sender, System.ComponentModel.DataErrorsChangedEventArgs e) =>
            {
                propertyName = e.PropertyName;
            };
            TestObject.RaiseDataErrorChanged();
            Assert.Equal("name", propertyName);

            // IXamlServiceProvider <-> IServiceProvider
            var serviceProvider = Class.ServiceProvider.As<IServiceProviderInterop>();
            IntPtr service;
            serviceProvider.GetService(IntPtr.Zero, out service);
            Assert.Equal(new IntPtr(42), service);

            // Ensure robustness with bad runtime class names (parsing errors, type not found, etc)
            var badRuntimeClassName = Class.BadRuntimeClassName;
            Assert.NotNull(badRuntimeClassName);
        }

        [Fact]
        public void TestKeyValuePair()
        {
            var expected = new KeyValuePair<string, string>("key", "value");
            TestObject.StringPairProperty = expected;
            Assert.Equal(expected, TestObject.StringPairProperty);

            expected = new KeyValuePair<string, string>("foo", "bar");
            TestObject.CallForStringPair(() => expected);
            TestObject.StringPairPropertyChanged +=
                (object sender, KeyValuePair<string, string> value) => Assert.Equal(expected, value);
            TestObject.RaiseStringPairChanged();
        }

        [Fact]
        public void TestObjectCasting()
        {
            var expected = new KeyValuePair<string, string>("key", "value");
            TestObject.ObjectProperty = expected;
            var out_pair = (KeyValuePair<string, string>)TestObject.ObjectProperty;
            Assert.Equal(expected, out_pair);

            var nested = new KeyValuePair<KeyValuePair<int, int>, KeyValuePair<string, string>>(
                new KeyValuePair<int, int>(42, 1729),
                new KeyValuePair<string, string>("key", "value")
            );
            TestObject.ObjectProperty = nested;
            var out_nested = (KeyValuePair<KeyValuePair<int, int>, KeyValuePair<string, string>>)TestObject.ObjectProperty;
            Assert.Equal(nested, out_nested);

            var strings_in = new[] { "hello", "world" };
            TestObject.StringsProperty = strings_in;
            var strings_out = TestObject.StringsProperty;
            Assert.True(strings_out.SequenceEqual(strings_in));

            TestObject.ObjectProperty = strings_in;
            strings_out = (string[])TestObject.ObjectProperty;
            Assert.True(strings_out.SequenceEqual(strings_in));

            var objects = new List<ManagedType>() { new ManagedType(), new ManagedType() };
            var query = from item in objects select item;
            TestObject.ObjectIterableProperty = query;
        }

        [Fact]
        public void TestStringMap()
        {
            var map = new Dictionary<string, string> { ["foo"] = "bar", ["hello"] = "world" };
            var stringMap = new Windows.Foundation.Collections.StringMap();
            foreach (var item in map)
            {
                stringMap[item.Key] = item.Value;
            }
            Assert.Equal(map.Count, stringMap.Count);
            foreach (var item in map)
            {
                Assert.Equal(stringMap[item.Key], item.Value);
            }
        }

        [Fact]
        public void TestPropertySet()
        {
            var map = new Dictionary<string, string> { ["foo"] = "bar", ["hello"] = "world" };
            var propertySet = new Windows.Foundation.Collections.PropertySet();
            foreach (var item in map)
            {
                propertySet[item.Key] = item.Value;
            }
            Assert.Equal(map.Count, propertySet.Count);
            foreach (var item in map)
            {
                Assert.Equal(propertySet[item.Key], item.Value);
            }
        }

        [Fact]
        public void TestValueSet()
        {
            var map = new Dictionary<string, string> { ["foo"] = "bar", ["hello"] = "world" };
            var valueSet = new Windows.Foundation.Collections.ValueSet();
            foreach (var item in map)
            {
                valueSet[item.Key] = item.Value;
            }
            Assert.Equal(map.Count, valueSet.Count);
            foreach (var item in map)
            {
                Assert.Equal(valueSet[item.Key], item.Value);
            }
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

        // TODO: enable TestWinRT coverage
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
        public void TestPrimitives()
        {
            var test_int = 21;
            TestObject.IntPropertyChanged += (object sender, Int32 value) =>
            {
                Assert.IsAssignableFrom<Class>(sender);
                var c = (Class)sender;
                Assert.Equal(value, test_int);
            };
            TestObject.IntProperty = test_int;

            var expectedVal = true;
            var hits = 0;
            TestObject.BoolPropertyChanged += (object sender, bool value) =>
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
            var val = new BlittableStruct() { i32 = 42 };
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
            var val = new ComposedBlittableStruct() { blittable = new BlittableStruct() { i32 = 42 } };
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
            var val = new NonBlittableStringStruct() { str = "I like tacos" };
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
                blittable = new BlittableStruct() { i32 = 42 },
                strings = new NonBlittableStringStruct() { str = "I like tacos" },
                bools = new NonBlittableBoolStruct() { w = true, x = false, y = true, z = false },
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

#if NETCOREAPP2_0
        [Fact]
        public void TestGenericCast()
        {
            var ints = TestObject.GetIntVector();
            var abiView = (ABI.System.Collections.Generic.IReadOnlyList<int>)ints;
            Assert.Equal(abiView.ThisPtr, abiView.As<WinRT.IInspectable>().As<ABI.System.Collections.Generic.IReadOnlyList<int>.Vftbl>().ThisPtr);
        }
#endif

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("96369F54-8EB6-48F0-ABCE-C1B211E627C3")]
        internal unsafe interface IStringableInterop
        {
            // Note: Invoking methods on ComInterfaceType.InterfaceIsIInspectable interfaces
            // no longer appears supported in the runtime (probably with removal of WinRT support),
            // so simulate with IUnknown.
            void GetIids(out int iidCount, out IntPtr iids);
            void GetRuntimeClassName(out IntPtr className);
            void GetTrustLevel(out TrustLevel trustLevel);

            void ToString(out IntPtr hstr);
        }

        [Fact]
        public unsafe void TestFactoryCast()
        {
            IntPtr hstr;

            // Access nonstatic class factory 
            var instanceFactory = Class.As<IStringableInterop>();
            instanceFactory.ToString(out hstr);
            Assert.Equal("Class", MarshalString.FromAbi(hstr));

            // Access static class factory
            var staticFactory = ComImports.As<IStringableInterop>();
            staticFactory.ToString(out hstr);
            Assert.Equal("ComImports", MarshalString.FromAbi(hstr));

            // IInspectable-based (projected) interop interface
            var interop = Windows.Security.Credentials.UI.UserConsentVerifier.As<IUserConsentVerifierInterop>();
            var guid = GuidGenerator.CreateIID(typeof(Windows.Foundation.IAsyncOperation<Windows.Security.Credentials.UI.UserConsentVerificationResult>));
            var operation = interop.RequestVerificationForWindowAsync(0, "message", guid);
            Assert.NotNull(operation);
        }

        [Fact]
        public void TestFundamentalGeneric()
        {
            var ints = TestObject.GetIntVector();
            Assert.Equal(10, ints.Count);
            for (int i = 0; i < 10; ++i)
            {
                Assert.Equal(i, ints[i]);
            }

            var bools = TestObject.GetBoolVector();
            Assert.Equal(4, bools.Count);
            for (int i = 0; i < 4; ++i)
            {
                Assert.Equal(i % 2 == 0, bools[i]);
            }
        }

        [Fact]
        public void TestStringGeneric()
        {
            var strings = TestObject.GetStringVector();
            Assert.Equal(5, strings.Count);
            for (int i = 0; i < 5; ++i)
            {
                Assert.Equal("String" + i, strings[i]);
            }
        }

        [Fact]
        public void TestStructGeneric()
        {
            var blittable = TestObject.GetBlittableStructVector();
            Assert.Equal(5, blittable.Count);
            for (int i = 0; i < 5; ++i)
            {
                Assert.Equal(i, blittable[i].blittable.i32);
            }

            var nonblittable = TestObject.GetNonBlittableStructVector();
            Assert.Equal(3, nonblittable.Count);
            for (int i = 0; i < 3; ++i)
            {
                var val = nonblittable[i];
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
        public void TestValueUnboxing()
        {
            var objs = TestObject.GetObjectVector();
            Assert.Equal(3, objs.Count);
            for (int i = 0; i < 3; ++i)
            {
                Assert.Equal(i, (int)objs[i]);
            }
        }

        [Fact]
        void TestInterfaceGeneric()
        {
            var objs = TestObject.GetInterfaceVector();
            Assert.Equal(3, objs.Count);
            TestObject.ReadWriteProperty = 42;
            for (int i = 0; i < 3; ++i)
            {
                var obj = objs[i];
                Assert.Same(obj, TestObject);
                Assert.Equal(42, obj.ReadWriteProperty);
            }
        }

        [Fact]
        public void TestIterable()
        {
            var ints_in = new int[] { 0, 1, 2 };
            TestObject.SetIntIterable(ints_in);
            var ints_out = TestObject.GetIntIterable();
            Assert.True(ints_in.SequenceEqual(ints_out));
        }

        class ManagedBindableObservable : IBindableObservableVector
        {
            private IList _list;

            public class TObservation : IProperties2
            {
                private int _value = 0;

                public int ReadWriteProperty { get => _value; set => _value = value; }

                int IProperties1.ReadWriteProperty => ReadWriteProperty;
            }
            TObservation _observation;

            public int Observation { get => _observation.ReadWriteProperty; }

            public ManagedBindableObservable(IList list) => _list = new ArrayList(list);

            private void OnChanged()
            {
                VectorChanged.Invoke(this, _observation = new TObservation());
            }

            public event BindableVectorChangedEventHandler VectorChanged;

            public object this[int index]
            {
                get => _list[index];
                set { _list[index] = value; OnChanged(); }
            }

            public bool IsFixedSize => false;

            public bool IsReadOnly => false;

            public int Count => _list.Count;

            public bool IsSynchronized => _list.IsSynchronized;

            public object SyncRoot => _list;

            public int Add(object value)
            {
                var result = _list.Add(value);
                OnChanged();
                return result;
            }

            public void Clear()
            {
                _list.Clear();
                OnChanged();
            }

            public bool Contains(object value) => _list.Contains(value);

            public void CopyTo(Array array, int index) => _list.CopyTo(array, index);

            public IEnumerator GetEnumerator() => _list.GetEnumerator();

            public int IndexOf(object value) => _list.IndexOf(value);

            public void Insert(int index, object value)
            {
                _list.Insert(index, value);
                OnChanged();
            }

            public void Remove(object value)
            {
                _list.Remove(value);
                OnChanged();
            }

            public void RemoveAt(int index)
            {
                _list.RemoveAt(index);
                OnChanged();
            }
        }

        [Fact]
        public void TestBindable()
        {
            var expected = new int[] { 0, 1, 2 };

            TestObject.BindableIterableProperty = expected;
            Assert.Equal(expected, TestObject.BindableIterableProperty);
            TestObject.CallForBindableIterable(() => expected);
            TestObject.BindableIterablePropertyChanged +=
                (object sender, IEnumerable value) => Assert.Equal(expected, value);
            TestObject.RaiseBindableIterableChanged();

            TestObject.BindableVectorProperty = expected;
            Assert.Equal(expected, TestObject.BindableVectorProperty);
            TestObject.CallForBindableVector(() => expected);
            TestObject.BindableVectorPropertyChanged +=
                (object sender, IList value) => Assert.Equal(expected, value);
            TestObject.RaiseBindableVectorChanged();

            var observable = new ManagedBindableObservable(expected);
            TestObject.BindableObservableVectorProperty = observable;
            observable.Add(3);
            Assert.Equal(6, observable.Observation);
        }

        [Fact]
        public void TestClassGeneric()
        {
            var objs = TestObject.GetClassVector();
            Assert.Equal(3, objs.Count);
            for (int i = 0; i < 3; ++i)
            {
                var obj = objs[i];
                Assert.Same(obj, TestObject);
                Assert.Equal(TestObject, objs[i]);
            }
        }
        [Fact]
        public void TestSimpleCCWs()
        {
            var managedProperties = new ManagedProperties(42);
            TestObject.CopyProperties(managedProperties);
            Assert.Equal(managedProperties.ReadWriteProperty, TestObject.ReadWriteProperty);
        }

        [Fact]
        public void TestWeakReference()
        {
            var managedProperties = new ManagedProperties(42);
            TestObject.CopyPropertiesViaWeakReference(managedProperties);
            Assert.Equal(managedProperties.ReadWriteProperty, TestObject.ReadWriteProperty);
        }

        [Fact]
        public void TestCCWIdentity()
        {
            var managedProperties = new ManagedProperties(42);
            IObjectReference ccw1 = MarshalInterface<IProperties1>.CreateMarshaler(managedProperties);
            IObjectReference ccw2 = MarshalInterface<IProperties1>.CreateMarshaler(managedProperties);
            Assert.Equal(ccw1.ThisPtr, ccw2.ThisPtr);
        }

        [Fact]
        public void TestInterfaceCCWLifetime()
        {
            static (WeakReference, IObjectReference) CreateCCW()
            {
                var managedProperties = new ManagedProperties(42);
                IObjectReference ccw1 = MarshalInterface<IProperties1>.CreateMarshaler(managedProperties);
                return (new WeakReference(managedProperties), ccw1);
            }

            static (WeakReference obj, WeakReference ccw) GetWeakReferenceToObjectAndCCW()
            {
                var (reference, ccw) = CreateCCW();

                GC.Collect();
                GC.WaitForPendingFinalizers();

                Assert.True(reference.IsAlive);
                return (reference, new WeakReference(ccw));
            }

            var (obj, ccw) = GetWeakReferenceToObjectAndCCW();

            while (ccw.IsAlive)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            // Now that the CCW is dead, we should have no references to the managed object.
            // Run GC one more time to collect the managed object.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Assert.False(obj.IsAlive);
        }

        [Fact]
        public void TestDelegateCCWLifetime()
        {
            static (WeakReference, IObjectReference) CreateCCW(Action<object, int> action)
            {
                TypedEventHandler<object, int> eventHandler = (o, i) => action(o, i);
                IObjectReference ccw1 = ABI.Windows.Foundation.TypedEventHandler<object, int>.CreateMarshaler(eventHandler);
                return (new WeakReference(eventHandler), ccw1);
            }

            static (WeakReference obj, WeakReference ccw) GetWeakReferenceToObjectAndCCW(Action<object, int> action)
            {
                var (reference, ccw) = CreateCCW(action);

                GC.Collect();
                GC.WaitForPendingFinalizers();

                Assert.True(reference.IsAlive);
                return (reference, new WeakReference(ccw));
            }

            var (obj, ccw) = GetWeakReferenceToObjectAndCCW((o, i) => { });

            while (ccw.IsAlive)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            // Now that the CCW is dead, we should have no references to the managed object.
            // Run GC one more time to collect the managed object.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Assert.False(obj.IsAlive);
        }

        [Fact]
        public void TestCCWIdentityThroughRefCountZero()
        {
            static (WeakReference, IntPtr) CreateCCWReference(IProperties1 properties)
            {
                IObjectReference ccw = MarshalInterface<IProperties1>.CreateMarshaler(properties);
                return (new WeakReference(ccw), ccw.ThisPtr);
            }

            var obj = new ManagedProperties(42);

            var (ccwWeakReference, ptr) = CreateCCWReference(obj);

            GC.Collect();
            GC.WaitForPendingFinalizers();

            Assert.False(ccwWeakReference.IsAlive);

            var (_, ptr2) = CreateCCWReference(obj);

            Assert.Equal(ptr, ptr2);
        }

        [Fact()]
        public void TestExceptionPropagation_Managed()
        {
            var exceptionToThrow = new ArgumentNullException("foo");
            var properties = new ThrowingManagedProperties(exceptionToThrow);
            Assert.Throws<ArgumentNullException>("foo", () => TestObject.CopyProperties(properties));
        }

        class ManagedProperties : IProperties1
        {
            private readonly int _value;

            public ManagedProperties(int value)
            {
                _value = value;
            }
            public int ReadWriteProperty => _value;
        }

        class ThrowingManagedProperties : IProperties1
        {
            public ThrowingManagedProperties(Exception exceptionToThrow)
            {
                ExceptionToThrow = exceptionToThrow;
            }

            public Exception ExceptionToThrow { get; }

            public int ReadWriteProperty => throw ExceptionToThrow;
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

        [Fact]
        public void TestPointTypeMapping()
        {
            var pt = new Point { X = 3.14, Y = 42 };
            TestObject.PointProperty = pt;
            Assert.Equal(pt.X, TestObject.PointProperty.X);
            Assert.Equal(pt.Y, TestObject.PointProperty.Y);
            Assert.True(TestObject.PointProperty == pt);
            Assert.Equal(pt, TestObject.GetPointReference().Value);
        }

        [Fact]
        public void TestRectTypeMapping()
        {
            var rect = new Rect { X = 3.14, Y = 42, Height = 3.14, Width = 42 };
            TestObject.RectProperty = rect;
            Assert.Equal(rect.X, TestObject.RectProperty.X);
            Assert.Equal(rect.Y, TestObject.RectProperty.Y);
            Assert.Equal(rect.Height, TestObject.RectProperty.Height);
            Assert.Equal(rect.Width, TestObject.RectProperty.Width);
            Assert.True(TestObject.RectProperty == rect);
        }

        [Fact]
        public void TestSizeTypeMapping()
        {
            var size = new Size { Height = 3.14, Width = 42 };
            TestObject.SizeProperty = size;
            Assert.Equal(size.Height, TestObject.SizeProperty.Height);
            Assert.Equal(size.Width, TestObject.SizeProperty.Width);
            Assert.True(TestObject.SizeProperty == size);
        }

        [Fact]
        public void TestColorTypeMapping()
        {
            var color = new Color { A = 0x20, R = 0x40, G = 0x60, B = 0x80 };
            TestObject.ColorProperty = color;
            Assert.Equal(color.A, TestObject.ColorProperty.A);
            Assert.Equal(color.R, TestObject.ColorProperty.R);
            Assert.Equal(color.G, TestObject.ColorProperty.G);
            Assert.Equal(color.B, TestObject.ColorProperty.B);
            Assert.True(TestObject.ColorProperty == color);
        }

        [Fact]
        public void TestCornerRadiusTypeMapping()
        {
            var cornerRadius = new CornerRadius { TopLeft = 1, TopRight = 2, BottomRight = 3, BottomLeft = 4 };
            TestObject.CornerRadiusProperty = cornerRadius;
            Assert.Equal(cornerRadius.TopLeft, TestObject.CornerRadiusProperty.TopLeft);
            Assert.Equal(cornerRadius.TopRight, TestObject.CornerRadiusProperty.TopRight);
            Assert.Equal(cornerRadius.BottomRight, TestObject.CornerRadiusProperty.BottomRight);
            Assert.Equal(cornerRadius.BottomLeft, TestObject.CornerRadiusProperty.BottomLeft);
            Assert.True(TestObject.CornerRadiusProperty == cornerRadius);
        }

        [Fact]
        public void TestDurationTypeMapping()
        {
            var duration = new Duration(TimeSpan.FromTicks(42));
            TestObject.DurationProperty = duration;
            Assert.Equal(duration.TimeSpan, TestObject.DurationProperty.TimeSpan);
            Assert.True(TestObject.DurationProperty == duration);
        }

        [Fact]
        public void TestGridLengthTypeMapping()
        {
            var gridLength = new GridLength(42, GridUnitType.Pixel);
            TestObject.GridLengthProperty = gridLength;
            Assert.Equal(gridLength.GridUnitType, TestObject.GridLengthProperty.GridUnitType);
            Assert.Equal(gridLength.Value, TestObject.GridLengthProperty.Value);
            Assert.True(TestObject.GridLengthProperty == gridLength);
        }

        [Fact]
        public void TestThicknessTypeMapping()
        {
            var thickness = new Thickness { Left = 1, Top = 2, Right = 3, Bottom = 4 };
            TestObject.ThicknessProperty = thickness;
            Assert.Equal(thickness.Left, TestObject.ThicknessProperty.Left);
            Assert.Equal(thickness.Top, TestObject.ThicknessProperty.Top);
            Assert.Equal(thickness.Right, TestObject.ThicknessProperty.Right);
            Assert.Equal(thickness.Bottom, TestObject.ThicknessProperty.Bottom);
            Assert.True(TestObject.ThicknessProperty == thickness);
        }

        [Fact]
        public void TestGeneratorPositionTypeMapping()
        {
            var generatorPosition = new GeneratorPosition { Index = 1, Offset = 2 };
            TestObject.GeneratorPositionProperty = generatorPosition;
            Assert.Equal(generatorPosition.Index, TestObject.GeneratorPositionProperty.Index);
            Assert.Equal(generatorPosition.Offset, TestObject.GeneratorPositionProperty.Offset);
            Assert.True(TestObject.GeneratorPositionProperty == generatorPosition);
        }

        [Fact]
        public void TestMatrixTypeMapping()
        {
            var matrix = new Matrix { M11 = 11, M12 = 12, M21 = 21, M22 = 22, OffsetX = 3, OffsetY = 4 };
            TestObject.MatrixProperty = matrix;
            Assert.Equal(matrix.M11, TestObject.MatrixProperty.M11);
            Assert.Equal(matrix.M12, TestObject.MatrixProperty.M12);
            Assert.Equal(matrix.M21, TestObject.MatrixProperty.M21);
            Assert.Equal(matrix.M22, TestObject.MatrixProperty.M22);
            Assert.Equal(matrix.OffsetX, TestObject.MatrixProperty.OffsetX);
            Assert.Equal(matrix.OffsetY, TestObject.MatrixProperty.OffsetY);
            Assert.True(TestObject.MatrixProperty == matrix);
        }

        [Fact]
        public void TestKeyTimeTypeMapping()
        {
            var keyTime = KeyTime.FromTimeSpan(TimeSpan.FromTicks(42));
            TestObject.KeyTimeProperty = keyTime;
            Assert.Equal(keyTime.TimeSpan, TestObject.KeyTimeProperty.TimeSpan);
            Assert.True(TestObject.KeyTimeProperty == keyTime);
        }

        [Fact]
        public void TestRepeatBehaviorTypeMapping()
        {
            var repeatBehavior = new RepeatBehavior
            {
                Count = 1,
                Duration = TimeSpan.FromTicks(42),
                Type = RepeatBehaviorType.Forever
            };
            TestObject.RepeatBehaviorProperty = repeatBehavior;
            Assert.Equal(repeatBehavior.Count, TestObject.RepeatBehaviorProperty.Count);
            Assert.Equal(repeatBehavior.Duration, TestObject.RepeatBehaviorProperty.Duration);
            Assert.Equal(repeatBehavior.Type, TestObject.RepeatBehaviorProperty.Type);
            Assert.True(TestObject.RepeatBehaviorProperty == repeatBehavior);
        }

        [Fact]
        public void TestMatrix3DTypeMapping()
        {
            var matrix3D = new Matrix3D {
                M11 = 11, M12 = 12, M13 = 13, M14 = 14,
                M21 = 21, M22 = 22, M23 = 23, M24 = 24,
                M31 = 31, M32 = 32, M33 = 33, M34 = 34,
                OffsetX = 41, OffsetY = 42, OffsetZ = 43,M44 = 44 };

            TestObject.Matrix3DProperty = matrix3D;
            Assert.Equal(matrix3D.M11, TestObject.Matrix3DProperty.M11);
            Assert.Equal(matrix3D.M12, TestObject.Matrix3DProperty.M12);
            Assert.Equal(matrix3D.M13, TestObject.Matrix3DProperty.M13);
            Assert.Equal(matrix3D.M14, TestObject.Matrix3DProperty.M14);
            Assert.Equal(matrix3D.M21, TestObject.Matrix3DProperty.M21);
            Assert.Equal(matrix3D.M22, TestObject.Matrix3DProperty.M22);
            Assert.Equal(matrix3D.M23, TestObject.Matrix3DProperty.M23);
            Assert.Equal(matrix3D.M24, TestObject.Matrix3DProperty.M24);
            Assert.Equal(matrix3D.M31, TestObject.Matrix3DProperty.M31);
            Assert.Equal(matrix3D.M32, TestObject.Matrix3DProperty.M32);
            Assert.Equal(matrix3D.M33, TestObject.Matrix3DProperty.M33);
            Assert.Equal(matrix3D.M34, TestObject.Matrix3DProperty.M34);
            Assert.Equal(matrix3D.OffsetX, TestObject.Matrix3DProperty.OffsetX);
            Assert.Equal(matrix3D.OffsetY, TestObject.Matrix3DProperty.OffsetY);
            Assert.Equal(matrix3D.OffsetZ, TestObject.Matrix3DProperty.OffsetZ);
            Assert.Equal(matrix3D.M44, TestObject.Matrix3DProperty.M44);
            Assert.True(TestObject.Matrix3DProperty == matrix3D);
        }

        [Fact]
        public void TestMatrix3x2TypeMapping()
        {
            var matrix3x2 = new Matrix3x2
            {
                M11 = 11,
                M12 = 12,
                M21 = 21,
                M22 = 22,
                M31 = 31,
                M32 = 32,
            };
            TestObject.Matrix3x2Property = matrix3x2;
            Assert.Equal(matrix3x2.M11, TestObject.Matrix3x2Property.M11);
            Assert.Equal(matrix3x2.M12, TestObject.Matrix3x2Property.M12);
            Assert.Equal(matrix3x2.M21, TestObject.Matrix3x2Property.M21);
            Assert.Equal(matrix3x2.M22, TestObject.Matrix3x2Property.M22);
            Assert.Equal(matrix3x2.M31, TestObject.Matrix3x2Property.M31);
            Assert.Equal(matrix3x2.M32, TestObject.Matrix3x2Property.M32);
            Assert.True(TestObject.Matrix3x2Property == matrix3x2);
        }

        [Fact]
        public void TestMatrix4x4TypeMapping()
        {
            var matrix4x4 = new Matrix4x4
            {
                M11 = 11, M12 = 12, M13 = 13, M14 = 14,
                M21 = 21, M22 = 22, M23 = 23, M24 = 24,
                M31 = 31, M32 = 32, M33 = 33, M34 = 34,
                M41 = 41, M42 = 42, M43 = 43, M44 = 44
            };
            TestObject.Matrix4x4Property = matrix4x4;
            Assert.Equal(matrix4x4.M11, TestObject.Matrix4x4Property.M11);
            Assert.Equal(matrix4x4.M12, TestObject.Matrix4x4Property.M12);
            Assert.Equal(matrix4x4.M13, TestObject.Matrix4x4Property.M13);
            Assert.Equal(matrix4x4.M14, TestObject.Matrix4x4Property.M14);
            Assert.Equal(matrix4x4.M21, TestObject.Matrix4x4Property.M21);
            Assert.Equal(matrix4x4.M22, TestObject.Matrix4x4Property.M22);
            Assert.Equal(matrix4x4.M23, TestObject.Matrix4x4Property.M23);
            Assert.Equal(matrix4x4.M24, TestObject.Matrix4x4Property.M24);
            Assert.Equal(matrix4x4.M31, TestObject.Matrix4x4Property.M31);
            Assert.Equal(matrix4x4.M32, TestObject.Matrix4x4Property.M32);
            Assert.Equal(matrix4x4.M33, TestObject.Matrix4x4Property.M33);
            Assert.Equal(matrix4x4.M34, TestObject.Matrix4x4Property.M34);
            Assert.Equal(matrix4x4.M41, TestObject.Matrix4x4Property.M41);
            Assert.Equal(matrix4x4.M42, TestObject.Matrix4x4Property.M42);
            Assert.Equal(matrix4x4.M43, TestObject.Matrix4x4Property.M43);
            Assert.Equal(matrix4x4.M44, TestObject.Matrix4x4Property.M44);
            Assert.True(TestObject.Matrix4x4Property == matrix4x4);
        }

        [Fact]
        public void TestPlaneTypeMapping()
        {
            var plane = new Plane { D = 3.14F, Normal = new Vector3(1, 2, 3) };
            TestObject.PlaneProperty = plane;
            Assert.Equal(plane.D, TestObject.PlaneProperty.D);
            Assert.Equal(plane.Normal, TestObject.PlaneProperty.Normal);
            Assert.True(TestObject.PlaneProperty == plane);
        }

        [Fact]
        public void TestQuaternionTypeMapping()
        {
            var quaternion = new Quaternion { W = 3.14F, X = 1, Y = 42, Z = 1729 };
            TestObject.QuaternionProperty = quaternion;
            Assert.Equal(quaternion.W, TestObject.QuaternionProperty.W);
            Assert.Equal(quaternion.X, TestObject.QuaternionProperty.X);
            Assert.Equal(quaternion.Y, TestObject.QuaternionProperty.Y);
            Assert.Equal(quaternion.Z, TestObject.QuaternionProperty.Z);
            Assert.True(TestObject.QuaternionProperty == quaternion);
        }

        [Fact]
        public void TestVector2TypeMapping()
        {
            var vector2 = new Vector2 { X = 1, Y = 42 };
            TestObject.Vector2Property = vector2;
            Assert.Equal(vector2.X, TestObject.Vector2Property.X);
            Assert.Equal(vector2.Y, TestObject.Vector2Property.Y);
            Assert.True(TestObject.Vector2Property == vector2);
        }

        [Fact]
        public void TestVector3TypeMapping()
        {
            var vector3 = new Vector3 { X = 1, Y = 42, Z = 1729 };
            TestObject.Vector3Property = vector3;
            Assert.Equal(vector3.X, TestObject.Vector3Property.X);
            Assert.Equal(vector3.Y, TestObject.Vector3Property.Y);
            Assert.Equal(vector3.Z, TestObject.Vector3Property.Z);
            Assert.True(TestObject.Vector3Property == vector3);
        }

        [Fact]
        public void TestVector4TypeMapping()
        {
            var vector4 = new Vector4 { W = 3.14F, X = 1, Y = 42, Z = 1729 };
            TestObject.Vector4Property = vector4;
            Assert.Equal(vector4.W, TestObject.Vector4Property.W);
            Assert.Equal(vector4.X, TestObject.Vector4Property.X);
            Assert.Equal(vector4.Y, TestObject.Vector4Property.Y);
            Assert.Equal(vector4.Z, TestObject.Vector4Property.Z);
            Assert.True(TestObject.Vector4Property == vector4);
        }

        [Fact]
        public void TestTimeSpanMapping()
        {
            var ts = TimeSpan.FromSeconds(42);
            TestObject.TimeSpanProperty = ts;
            Assert.Equal(ts, TestObject.TimeSpanProperty);
            Assert.Equal(ts, TestObject.GetTimeSpanReference().Value);
            Assert.Equal(ts, Class.FromSeconds(42));
        }

        [Fact]
        public void TestDateTimeMapping()
        {
            var now = DateTimeOffset.Now;
            Assert.InRange((Class.Now() - now).Ticks, -TimeSpan.TicksPerSecond, TimeSpan.TicksPerSecond); // Unlikely to be the same, but should be within a second
            TestObject.DateTimeProperty = now;
            Assert.Equal(now, TestObject.DateTimeProperty);
            Assert.Equal(now, TestObject.GetDateTimeProperty().Value);
        }

        [Fact]
        public void TestExceptionMapping()
        {
            var ex = new ArgumentOutOfRangeException();

            TestObject.HResultProperty = ex;

            Assert.IsType<ArgumentOutOfRangeException>(TestObject.HResultProperty);

            TestObject.HResultProperty = null;

            Assert.Null(TestObject.HResultProperty);
        }

        [Fact]
        public void TestGeneratedRuntimeClassName()
        {
            IInspectable inspectable = new IInspectable(ComWrappersSupport.CreateCCWForObject(new ManagedProperties(2)));
            Assert.Equal(typeof(IProperties1).FullName, inspectable.GetRuntimeClassName());
        }

        [Fact]
        public void TestGeneratedRuntimeClassName_Primitive()
        {
            IInspectable inspectable = new IInspectable(ComWrappersSupport.CreateCCWForObject(2));
            Assert.Equal("Windows.Foundation.IReference`1<Int32>", inspectable.GetRuntimeClassName());
        }

        [Fact]
        public void TestGeneratedRuntimeClassName_Array()
        {
            IInspectable inspectable = new IInspectable(ComWrappersSupport.CreateCCWForObject(new int[0]));
            Assert.Equal("Windows.Foundation.IReferenceArray`1<Int32>", inspectable.GetRuntimeClassName());
        }

        [Fact]
        public void TestValueBoxing()
        {
            int i = 42;
            Assert.Equal(i, Class.UnboxInt32(i));

            bool b = true;
            Assert.Equal(b, Class.UnboxBoolean(b));

            string s = "Hello World!";
            Assert.Equal(s, Class.UnboxString(s));
        }

        [Fact]
        public void TestArrayBoxing()
        {
            int[] i = new[] { 42, 1, 4, 50, 0, -23 };
            Assert.Equal((IEnumerable<int>)i, Class.UnboxInt32Array(i));

            bool[] b = new[] { true, false, true, true, false };
            Assert.Equal((IEnumerable<bool>)b, Class.UnboxBooleanArray(b));

            string[] s = new[] { "Hello World!", "WinRT", "C#", "Boxing" };
            Assert.Equal((IEnumerable<string>)s, Class.UnboxStringArray(s));
        }

        [Fact]
        public void TestArrayUnboxing()
        {
            int[] i = new[] { 42, 1, 4, 50, 0, -23 };

            var obj = PropertyValue.CreateInt32Array(i);
            Assert.IsType<int[]>(obj);
            Assert.Equal(i, (IEnumerable<int>)obj);
        }

        [Fact]
        public void PrimitiveTypeInfo()
        {
            Assert.Equal(typeof(int), Class.Int32Type);
            Assert.True(Class.VerifyTypeIsInt32Type(typeof(int)));
        }

        [Fact]
        public void WinRTTypeInfo()
        {
            Assert.Equal(typeof(Class), Class.ThisClassType);
            Assert.True(Class.VerifyTypeIsThisClassType(typeof(Class)));
        }

        [Fact]
        public void ProjectedTypeInfo()
        {
            Assert.Equal(typeof(int?), Class.ReferenceInt32Type);
            Assert.True(Class.VerifyTypeIsReferenceInt32Type(typeof(int?)));
        }

        [Fact]
        public void TypeInfoGenerics()
        {
            var typeName = Class.GetTypeNameForType(typeof(IList<int>));

            Assert.Equal("Windows.Foundation.Collections.IVector`1<Int32>", typeName);
        }

        [Fact]
        public void TestGenericTypeMarshalling()
        {
            Assert.Equal(typeof(ABI.System.Type), Marshaler<Type>.AbiType);
        }

        [Fact]
        public void TestStringUnboxing()
        {
            var str1 = Class.EmptyString;
            var str2 = Class.EmptyString;
            Assert.IsType<string>(str1);
            Assert.IsType<string>(str2);
            Assert.Equal(string.Empty, (string)str1);
            Assert.Equal(string.Empty, (string)str2);
        }

        internal class ManagedType { }

        [Fact]
        public void CCWOfListOfManagedType()
        {
            using var ccw = ComWrappersSupport.CreateCCWForObject(new List<ManagedType>());
            using var qiResult = ccw.As(GuidGenerator.GetIID(typeof(global::System.Collections.Generic.IEnumerable<object>).GetHelperType()));
        }

        [Fact]
        public void WeakReferenceOfManagedObject()
        {
            var properties = new ManagedProperties(42);
            WeakRefNS.WeakReference<IProperties1> weakReference = new WeakRefNS.WeakReference<IProperties1>(properties);
            Assert.True(weakReference.TryGetTarget(out var propertiesStrong));
            Assert.Same(properties, propertiesStrong);
        }

        [Fact]
        public void WeakReferenceOfNativeObject()
        {
            var weakReference = new WeakRefNS.WeakReference<Class>(TestObject);
            Assert.True(weakReference.TryGetTarget(out var classStrong));
            Assert.Same(TestObject, classStrong);
        }

        [Fact]
        public void WeakReferenceOfNativeObjectRehydratedAfterWrapperIsCollected()
        {
            static (WeakRefNS.WeakReference<Class> winrt, WeakReference net, IObjectReference objRef) GetWeakReferences()
            {
                var obj = new Class();
                ComWrappersSupport.TryUnwrapObject(obj, out var objRef);
                return (new WeakRefNS.WeakReference<Class>(obj), new WeakReference(obj), objRef);
            }

            var (winrt, net, objRef) = GetWeakReferences();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            Assert.False(net.IsAlive);
            Assert.True(winrt.TryGetTarget(out _));
            GC.KeepAlive(objRef);
        }

        [Fact]
        public void TestUnwrapInspectable()
        {
            using var objRef = MarshalInspectable<object>.CreateMarshaler(TestObject);
            var inspectable = IInspectable.FromAbi(objRef.ThisPtr);
            Assert.True(ComWrappersSupport.TryUnwrapObject(inspectable, out _));

            using var objRef2 = MarshalInspectable<Class>.CreateMarshaler(TestObject);
            var inspectable2 = IInspectable.FromAbi(objRef2.ThisPtr);
            Assert.True(ComWrappersSupport.TryUnwrapObject(inspectable2, out _));
        }

        [Fact]
        public void TestManagedAgileObject()
        {
            using var testObjectAgileRef = TestObject.AsAgile();
            var agileTestObject = testObjectAgileRef.Get();
            Assert.Equal(TestObject, agileTestObject);

            IProperties1 properties = new ManagedProperties(42);
            using var propertiesAgileRef = properties.AsAgile();
            var agileProperties = propertiesAgileRef.Get();
            Assert.Equal(properties.ReadWriteProperty, agileProperties.ReadWriteProperty);

            var agileObject = TestObject.As<IAgileObject>();
            Assert.NotNull(agileObject);

            IProperties1 properties2 = null;
            using var properties2AgileRef = properties2.AsAgile();
            var agileProperties2 = properties2AgileRef.Get();
            Assert.Null(agileProperties2);
        }

        class NonAgileClassCaller
        {
            public void AcquireObject()
            {
                Assert.Equal(ApartmentState.STA, Thread.CurrentThread.GetApartmentState());
                nonAgileObject = new Windows.UI.Popups.PopupMenu();
                nonAgileObject.Commands.Add(new Windows.UI.Popups.UICommand("test"));
                nonAgileObject.Commands.Add(new Windows.UI.Popups.UICommand("test2"));
                Assert.ThrowsAny<System.Exception>(() => nonAgileObject.As<IAgileObject>());

                agileReference = nonAgileObject.AsAgile();
                objectAcquired.Set();
                valueAcquired.WaitOne();

                // Call to proxy object acquired from MTA which should throw
                Assert.ThrowsAny<System.Exception>(() => proxyObject.Commands.Count);
                agileReference.Dispose();
            }

            public void CheckValue()
            {
                objectAcquired.WaitOne();

                Assert.Equal(ApartmentState.MTA, Thread.CurrentThread.GetApartmentState());
                proxyObject = agileReference.Get();
                Assert.Equal(2, proxyObject.Commands.Count);
                valueAcquired.Set();
            }

            private Windows.UI.Popups.PopupMenu nonAgileObject;
            private Windows.UI.Popups.PopupMenu proxyObject;
            private AgileReference<Windows.UI.Popups.PopupMenu> agileReference;
            private readonly AutoResetEvent objectAcquired = new AutoResetEvent(false);
            private readonly AutoResetEvent valueAcquired = new AutoResetEvent(false);
        }


        [Fact]
        public void TestNonAgileObjectCall()
        {
            NonAgileClassCaller caller = new NonAgileClassCaller();
            Thread staThread = new Thread(new ThreadStart(caller.AcquireObject));
            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start();

            Thread mtaThread = new Thread(new ThreadStart(caller.CheckValue));
            mtaThread.SetApartmentState(ApartmentState.MTA);
            mtaThread.Start();
            mtaThread.Join();
            staThread.Join();
        }

        [Fact]
        public void TestNonAgileDelegateCall()
        {
            var expected = new int[] { 0, 1, 2 };
            var observable = new ManagedBindableObservable(expected);
            var nonAgileClass = new NonAgileClass();
            nonAgileClass.Observe(observable);
            observable.Add(3);
            Assert.Equal(6, observable.Observation);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("EECDBF0E-BAE9-4CB6-A68E-9598E1CB57BB")]
        internal interface IWindowNative
        {
            IntPtr WindowHandle { get; }
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("3E68D4BD-7135-4D10-8018-9FB6D9F33FA1")]
        internal interface IInitializeWithWindow
        {
            void Initialize(IntPtr hwnd);
        }

        [Fact]
        unsafe public void TestComImports()
        {
            static Object MakeObject()
            {
                Assert.Equal(0, ComImports.NumObjects);
                var obj = ComImports.MakeObject();
                Assert.Equal(1, ComImports.NumObjects);
                return obj;
            }

            static void TestObject() => MakeObject();

            static (IInitializeWithWindow, IWindowNative) MakeImports() 
            { 
                var obj = MakeObject();
                var initializeWithWindow = obj.As<IInitializeWithWindow>();
                var windowNative = obj.As<IWindowNative>();
                return (initializeWithWindow, windowNative);
            }

            static void TestImports()
            {
                var (initializeWithWindow, windowNative) = MakeImports();
                
                GC.Collect();
                GC.WaitForPendingFinalizers();

                var hwnd = new IntPtr(0x12345678);
                initializeWithWindow.Initialize(hwnd);
                Assert.Equal(windowNative.WindowHandle, hwnd);
            }

            TestObject();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Assert.Equal(0, ComImports.NumObjects);

            TestImports();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Assert.Equal(0, ComImports.NumObjects);
        }

        [Fact]
        public void TestInterfaceObjectMarshalling()
        {
            var nativeProperties = Class.NativeProperties1;

            TestObject.CopyProperties(nativeProperties);

            Assert.Equal(TestObject.ReadWriteProperty, nativeProperties.ReadWriteProperty);
        }

        // Test scenario where type reported by runtimeclass name is not a valid type (i.e. internal type).
        [Fact]
        public void TestNonProjectedRuntimeClass()
        {
            string key = ".....";
            IBuffer keyMaterial = CryptographicBuffer.ConvertStringToBinary(key, BinaryStringEncoding.Utf8);
            MacAlgorithmProvider mac = MacAlgorithmProvider.OpenAlgorithm(MacAlgorithmNames.HmacSha1);
            CryptographicKey cryptoKey = mac.CreateKey(keyMaterial);
            Assert.NotNull(cryptoKey);
        }

        [Fact(Skip="Operation not supported")]
        public void TestIBindableIterator()
        {
            CustomBindableIteratorTest bindableIterator = new CustomBindableIteratorTest();
            Assert.True(bindableIterator.MoveNext());
            Assert.True(bindableIterator.HasCurrent);
            Assert.Equal(27861, bindableIterator.Current);
        }

        [Fact]
        public void TestIDisposable()
        {
            CustomDisposableTest disposable = new CustomDisposableTest();
            disposable.Dispose();
        }
    }
}