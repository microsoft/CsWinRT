using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ABI.System.Collections.Specialized;
using ABI.System.ComponentModel;
using ABI.Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Interop;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Media3D;
using TestComponentCSharp;
using Windows.Devices.Enumeration;
using Windows.Devices.Enumeration.Pnp;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Notifications;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;

// Test SupportedOSPlatform warnings for APIs targeting 10.0.19041.0:
[assembly: global::System.Runtime.Versioning.SupportedOSPlatform("Windows10.0.18362.0")]

namespace UnitTest
{
    public delegate void DelegateTestCSharp<T>();

    public interface ITestCSharp<T>
    {
        void TestMethod<T>();
    }

    [TestClass]
    public class UnitTestCSharp
    {
        public Class TestObject = new();

        public enum E { A, B, C }

        public struct Estruct
        {
            E value;
        }

        [TestMethod]
        public void TestDelegateCallBackOnSealedType()
        {
            // In CSWinRT 2.0, this scenario would throw an exception but this is fixed in 3.0
            SealedDelegateClassTest testType = new SealedDelegateClassTest();
            // Passing a projected function of SealedDelegateClassTest as a delegate
            StaticDelegateClassTest.Run(testType.Run);
        }

        // Test a fix for a bug in Mono.Cecil that was affecting the IIDOptimizer when it encountered long class names 
        [TestMethod]
        public void TestLongClassNameEventSource()
        {
            bool flag = false;
            var long_class_name = new ABCDEFGHIJKLMNOPQRSTUVQXYZabcdefghijklmnopqrstuvqxyzABCDEFGHIJKLMNOPQRSTUVQXYZabcdefghijklmnopqrstuvqxyzABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz();
            long_class_name.EventForAVeryLongClassName +=
                (ABCDEFGHIJKLMNOPQRSTUVQXYZabcdefghijklmnopqrstuvqxyzABCDEFGHIJKLMNOPQRSTUVQXYZabcdefghijklmnopqrstuvqxyzABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz sender, ABCDEFGHIJKLMNOPQRSTUVQXYZabcdefghijklmnopqrstuvqxyzABCDEFGHIJKLMNOPQRSTUVQXYZabcdefghijklmnopqrstuvqxyzABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz args)
                => flag = true;
            long_class_name.InvokeEvent();
            Assert.IsTrue(flag);
        }

        [TestMethod]
        public void TestEventArgsVector()
        {
            var eventArgsVector = TestObject.GetEventArgsVector();
            Assert.AreEqual(1, eventArgsVector.Count);
            foreach (var dataErrorChangedEventArgs in eventArgsVector)
            {
                var propName = dataErrorChangedEventArgs.PropertyName;
                Assert.AreEqual("name", propName);
            }
        }

        [TestMethod]
        public void TestNonGenericDelegateVector()
        {
            var provideUriVector = TestObject.GetNonGenericDelegateVector();

            Assert.AreEqual(1, provideUriVector.Count);

            foreach (var provideUri in provideUriVector)
            {
                Uri delegateTarget = provideUri.Invoke();
                Assert.AreEqual("http://microsoft.com", delegateTarget.OriginalString);
            }
        }

        [TestMethod]
        public void TestEnums()
        {
            // Enums
            var expectedEnum = EnumValue.Two;
            TestObject.EnumProperty = expectedEnum;
            Assert.AreEqual(expectedEnum, TestObject.EnumProperty);
            expectedEnum = EnumValue.One;
            TestObject.CallForEnum(() => expectedEnum);
            TestObject.EnumPropertyChanged +=
                (object sender, EnumValue value) => Assert.AreEqual(expectedEnum, value);
            TestObject.RaiseEnumChanged();

            var expectedEnumStruct = new EnumStruct() { value = EnumValue.Two };
            TestObject.EnumStructProperty = expectedEnumStruct;
            Assert.AreEqual(expectedEnumStruct, TestObject.EnumStructProperty);
            expectedEnumStruct = new EnumStruct() { value = EnumValue.One };
            TestObject.CallForEnumStruct(() => expectedEnumStruct);
            TestObject.EnumStructPropertyChanged +=
                (object sender, EnumStruct value) => Assert.AreEqual(expectedEnumStruct, value);
            TestObject.RaiseEnumStructChanged();

            var expectedEnums = new EnumValue[] { EnumValue.One, EnumValue.Two };
            TestObject.EnumsProperty = expectedEnums;
            Assert.IsTrue(expectedEnums.SequenceEqual(TestObject.EnumsProperty));
            TestObject.CallForEnums(() => expectedEnums);
            Assert.IsTrue(expectedEnums.SequenceEqual(TestObject.EnumsProperty));

            TestObject.EnumsProperty = null;
            Assert.IsTrue(TestObject.EnumsProperty.SequenceEqual(null));

            var expectedEnumStructs = new EnumStruct[] { new EnumStruct(EnumValue.One), new EnumStruct(EnumValue.Two) };
            TestObject.EnumStructsProperty = expectedEnumStructs;
            Assert.IsTrue(expectedEnumStructs.SequenceEqual(TestObject.EnumStructsProperty));
            TestObject.CallForEnumStructs(() => expectedEnumStructs);
            Assert.IsTrue(expectedEnumStructs.SequenceEqual(TestObject.EnumStructsProperty));

            TestObject.EnumStructsProperty = null;
            Assert.IsTrue(TestObject.EnumStructsProperty.SequenceEqual(null));

            // Flags
            var expectedFlag = FlagValue.All;
            TestObject.FlagProperty = expectedFlag;
            Assert.AreEqual(expectedFlag, TestObject.FlagProperty);
            expectedFlag = FlagValue.One;
            TestObject.CallForFlag(() => expectedFlag);
            TestObject.FlagPropertyChanged +=
                (object sender, FlagValue value) => Assert.AreEqual(expectedFlag, value);
            TestObject.RaiseFlagChanged();

            var expectedFlagStruct = new FlagStruct() { value = FlagValue.All };
            TestObject.FlagStructProperty = expectedFlagStruct;
            Assert.AreEqual(expectedFlagStruct, TestObject.FlagStructProperty);
            expectedFlagStruct = new FlagStruct() { value = FlagValue.One };
            TestObject.CallForFlagStruct(() => expectedFlagStruct);
            TestObject.FlagStructPropertyChanged +=
                (object sender, FlagStruct value) => Assert.AreEqual(expectedFlagStruct, value);
            TestObject.RaiseFlagStructChanged();

            var expectedFlags = new FlagValue[] { FlagValue.One, FlagValue.All };
            TestObject.FlagsProperty = expectedFlags;
            Assert.IsTrue(expectedFlags.SequenceEqual(TestObject.FlagsProperty));
            TestObject.CallForFlags(() => expectedFlags);
            Assert.IsTrue(expectedFlags.SequenceEqual(TestObject.FlagsProperty));

            var expectedFlagStructs = new FlagStruct[] { new FlagStruct(FlagValue.One), new FlagStruct(FlagValue.All) };
            TestObject.FlagStructsProperty = expectedFlagStructs;
            Assert.IsTrue(expectedFlagStructs.SequenceEqual(TestObject.FlagStructsProperty));
            TestObject.CallForFlagStructs(() => expectedFlagStructs);
            Assert.IsTrue(expectedFlagStructs.SequenceEqual(TestObject.FlagStructsProperty));
        }

        [TestMethod]
        public void TestGetByte()
        {
            var array = new byte[] { 0x01 };
            var buff = array.AsBuffer();
            Assert.IsTrue(buff.Length == 1);
            byte b = buff.GetByte(0);
            Assert.IsTrue(b == 0x01);
        }

        [TestMethod]
        public void TestManyBufferExtensionMethods()
        {
            var arrayLen3 = new byte[] { 0x01, 0x02, 0x03 };
            var buffLen3 = arrayLen3.AsBuffer();

            var arrayLen4 = new byte[] { 0x11, 0x12, 0x13, 0x14 };
            var buffLen4 = arrayLen4.AsBuffer();

            var arrayLen4Again = new byte[4];

            arrayLen3.CopyTo(1, buffLen4, 0, 1); // copy just the second element of the array to the beginning of the buffer 
            Assert.IsTrue(buffLen4.Length == 4);
            Assert.ThrowsException<ArgumentException>(() => buffLen4.GetByte(5)); // shouldn't have a 5th element
            Assert.IsTrue(buffLen4.GetByte(0) == 0x02); // make sure we got the 2nd element of the array

            arrayLen3.CopyTo(buffLen4); // Array to Buffer copying
            Assert.IsTrue(buffLen4.Length == 4);
            Assert.IsTrue(buffLen4.GetByte(0) == 0x01); // make sure we updated the first few 
            Assert.IsTrue(buffLen4.GetByte(1) == 0x02);
            Assert.IsTrue(buffLen4.GetByte(2) == 0x03);
            Assert.IsTrue(buffLen4.GetByte(3) == 0x14); // and kept the last one 

            var buffLen3Again = buffLen3.ToArray().AsBuffer();
            Assert.IsTrue(buffLen3Again.GetByte(0) == 0x01);
            Assert.IsTrue(buffLen3Again.GetByte(1) == 0x02);
            Assert.IsTrue(buffLen3Again.GetByte(2) == 0x03);

            Assert.IsFalse(buffLen3.IsSameData(buffLen3Again)); // different memory regions

            buffLen4.CopyTo(arrayLen4Again);  // Buffer to Array copying
            var array4 = buffLen4.ToArray();
            Assert.IsTrue(arrayLen4Again.Length == array4.Length);
            for (int i = 0; i < arrayLen4Again.Length; ++i)
            {
                Assert.IsTrue(arrayLen4Again[i] == array4[i]); // make sure we have equal array
            }
        }

        [TestMethod]
        public void TestIsSameDataDifferentArrays()
        {
            var arr = new byte[] { 0x01, 0x02 };
            var buf1 = arr.AsBuffer();
            var arr2 = new byte[] { 0x01, 0x02 };
            var buf2 = arr2.AsBuffer();
            Assert.IsFalse(buf1.IsSameData(buf2));
        }

        [TestMethod]
        public void TestIsSameDataUsingCopyTo()
        {
            var arr = new byte[] { 0x01, 0x02 };
            var buf1 = arr.AsBuffer();
            var buf2 = new Windows.Storage.Streams.Buffer(2);
            buf1.CopyTo(buf2);
            Assert.IsFalse(buf1.IsSameData(buf2));
        }

        [TestMethod]
        public void TestIsSameDataUsingAsBufferTwice()
        {
            var arr = new byte[] { 0x01, 0x02 };
            var buf1 = arr.AsBuffer();
            var buf2 = arr.AsBuffer();
            Assert.IsTrue(buf1.IsSameData(buf2));
        }

        [TestMethod]
        public void TestIsSameDataUsingToArray()
        {
            var arr = new byte[] { 0x01, 0x02 };
            var buf1 = arr.AsBuffer();
            var buf2 = buf1.ToArray().AsBuffer();
            Assert.IsFalse(buf1.IsSameData(buf2));
        }

        [TestMethod]
        public void TestBufferAsStreamUsingAsBuffer()
        {
            var arr = new byte[] { 0x01, 0x02 };
            Stream stream = arr.AsBuffer().AsStream();
            Assert.IsTrue(stream != null);
            Assert.IsTrue(stream.Length == 2);
        }

        [TestMethod]
        public void TestBufferAsStreamUsingAsBufferWithOffset()
        {
            var arr = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            var buffer = arr.AsBuffer(1, 2);
            Stream stream = buffer.AsStream();
            Assert.IsTrue(stream != null);
            Assert.IsTrue(stream.Length == 2);

            stream.Write(new byte[] { 0x05, 0x06 });
            Assert.IsTrue(stream.Length == 2);
            Assert.IsTrue(buffer.Length == 2);

            Assert.AreEqual((byte)0x05, arr[1]);
            Assert.AreEqual((byte)0x06, arr[2]);
        }

        [TestMethod]
        public void TestBufferAsStreamUsingAsBufferWithOffsetAndCapacity()
        {
            var arr = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            var buffer = arr.AsBuffer(1, 2, 3);
            Stream stream = buffer.AsStream();
            Assert.IsTrue(stream != null);
            Assert.IsTrue(stream.Length == 2);

            stream.Write(new byte[] { 0x05, 0x06, 0x07 });
            Assert.IsTrue(stream.Length == 3);
            Assert.IsTrue(buffer.Length == 3);

            Assert.AreEqual((byte)0x05, arr[1]);
            Assert.AreEqual((byte)0x06, arr[2]);
            Assert.AreEqual((byte)0x07, arr[3]);
        }

        [TestMethod]
        public void TestBufferAsStreamWithEmptyBuffer()
        {
            var buffer = new Windows.Storage.Streams.Buffer(0);
            Stream stream = buffer.AsStream();
            Assert.IsTrue(stream != null);
            Assert.IsTrue(stream.Length == 0);
        }

        [TestMethod]
        public void TestBufferAsStreamRead()
        {
            var arr = new byte[] { 0x01, 0x02 };
            Stream stream = arr.AsBuffer().AsStream();
            Assert.IsTrue(stream != null);
            Assert.IsTrue(stream.Length == 2);
            int byte1 = stream.ReadByte();
            Assert.AreEqual(0x01, byte1);
        }

        [TestMethod]
        public void TestBufferAsStreamWrite()
        {
            var buffer = new Windows.Storage.Streams.Buffer(2);
            Stream stream = buffer.AsStream();
            Assert.IsTrue(stream != null);
            Assert.IsTrue(stream.Length == 0);
            stream.WriteByte(0x01);
            Assert.IsTrue(stream.Length == 1);
            Assert.IsTrue(buffer.Length == 1);
        }

        [TestMethod]
        public void TestBufferImproperReadCopyToOutOfBounds()
        {
            var array = new byte[] { 0x01, 0x02, 0x03 };
            var buffer = array.AsBuffer();
            var biggerBuffer = new Windows.Storage.Streams.Buffer(5);
            buffer.CopyTo(biggerBuffer);
            Assert.ThrowsException<ArgumentException>(() => biggerBuffer.ToArray(4, 2));
        }

        [TestMethod]
        public void TestBufferImproperReadCopyToStraddleBounds()
        {
            var array = new byte[] { 0x01, 0x02, 0x03 };
            var buffer = array.AsBuffer();
            var biggerBuffer = new Windows.Storage.Streams.Buffer(5);
            buffer.CopyTo(biggerBuffer);
            Assert.ThrowsException<ArgumentException>(() => biggerBuffer.ToArray(2, 2));
        }

        [TestMethod]
        public void TestBufferImproperReadGetByte()
        {
            var array = new byte[] { 0x01, 0x02, 0x03 };
            var buffer = array.AsBuffer();
            Assert.ThrowsException<ArgumentException>(() => buffer.GetByte(4));
        }

        [TestMethod]
        public void TestEmptyBufferToArray()
        {
            var buffer = new Windows.Storage.Streams.Buffer(0);
            var array = buffer.ToArray();
            Assert.IsTrue(array.Length == 0);
        }

        [TestMethod]
        public void TestArrayCopyToBufferEndToBeginning()
        {
            IBuffer buf = new Windows.Storage.Streams.Buffer(3);
            byte[] arr = new byte[] { 0x01, 0x02, 0x03 };
            arr.CopyTo(3, buf, 0, 0);
        }

        [TestMethod]
        public void TestArrayCopyToBufferEndToEnd2()
        {
            IBuffer buf = new Windows.Storage.Streams.Buffer(3);
            byte[] arr = new byte[] { 0x01, 0x02, 0x03 };
            arr.CopyTo(0, buf, 0, 3);
        }

        [TestMethod]
        public void TestArrayCopyToBufferEndToEnd()
        {
            IBuffer buf = new Windows.Storage.Streams.Buffer(3);
            byte[] arr = new byte[] { 0x01, 0x02, 0x03 };
            arr.CopyTo(3, buf, 3, 0);
        }

        [TestMethod]
        public void TestArrayCopyToBufferMidToMid()
        {
            IBuffer buf = new Windows.Storage.Streams.Buffer(3);
            byte[] arr = new byte[] { 0x01, 0x02, 0x03 };
            arr.CopyTo(1, buf, 1, 0);
        }

        [TestMethod]
        public void TestArrayCopyToBufferMidToEnd()
        {
            IBuffer buf = new Windows.Storage.Streams.Buffer(3);
            byte[] arr = new byte[] { 0x01, 0x02, 0x03 };
            arr.CopyTo(1, buf, 3, 0);
        }

        [TestMethod]
        public void TestBufferCopyToArrayEndToEnd()
        {
            byte[] arr = new byte[] { 0x01, 0x02, 0x03 };
            var buf = arr.AsBuffer();
            var target = new byte[4];
            buf.CopyTo(3, target, 4, 0);
        }

        [TestMethod]
        public void BufferToArrayWithZeroCountAtEnd2()
        {
            byte[] array = { 0xA1, 0xA2, 0xA3 };
            var result = array.AsBuffer().ToArray(3, 0);
            Assert.IsTrue(result != null);
            Assert.IsTrue(0 == result.Length);
        }

        [TestMethod]
        public void BufferToArrayWithZeroCountAtEnd_WorksWithSpans()
        {
            byte[] array = { 0xA1, 0xA2, 0xA3 };
            var result = array.AsSpan().Slice(3, 0).ToArray();
            Assert.IsTrue(result != null);
            Assert.IsTrue(0 == result.Length);
        }

        [TestMethod]
        public void TestWinRTBufferWithZeroLength()
        {
            byte[] arr = new byte[] { 0x01, 0x02, 0x03 };
            MemoryStream stream = new MemoryStream(arr, 0, 3, false, true);
            IBuffer buff = stream.GetWindowsRuntimeBuffer(3, 0);
            Assert.IsTrue(buff != null);
            Assert.IsTrue(buff.Length == 0);
        }

        [TestMethod]
        public void TestEmptyBufferCopyTo()
        {
            var buffer = new Windows.Storage.Streams.Buffer(0);
            byte[] array = { };
            buffer.CopyTo(array);
            Assert.IsTrue(array.Length == 0);
        }

        [TestMethod]
        public void TestBufferToArrayCapacityLargerThanLength()
        {
            var buffer = new Windows.Storage.Streams.Buffer(100);
            byte[] arr = new byte[] { 0x01, 0x02, 0x03 };
            arr.CopyTo(0, buffer, 0, 3);

            byte[] newArr = buffer.ToArray();
            Assert.IsTrue(newArr.Length == 3);
        }

        [TestMethod]
        public void TestBufferCopyToBufferCapacityLargerThanLength()
        {
            var buffer = new Windows.Storage.Streams.Buffer(100);
            byte[] arr = new byte[] { 0x01, 0x02, 0x03 };
            arr.CopyTo(0, buffer, 0, 3);

            var buffer2 = new Windows.Storage.Streams.Buffer(50);
            byte[] arr2 = new byte[] { 0x01, 0x02 };
            arr2.CopyTo(0, buffer2, 0, 2);

            Assert.IsTrue(buffer2.Length == 2);

            buffer.CopyTo(buffer2);
            Assert.IsTrue(buffer2.Length == 3);
        }

        [TestMethod]
        public void TestTryGetDataUnsafe()
        {
            IBuffer buf = new Windows.Storage.Streams.Buffer(3);
            byte[] arr = new byte[] { 0x01, 0x02, 0x03 };
            arr.CopyTo(0, buf, 0, 3);

            unsafe
            {
                Assert.IsTrue(WindowsRuntimeBufferMarshal.TryGetDataUnsafe(buf, out byte* dataPtr));
                Assert.IsTrue(dataPtr != null);

                Span<byte> buffSpan = new Span<byte>((byte*)dataPtr, (int)buf.Length);

                byte[] arr2 = buffSpan.ToArray();
                Assert.IsTrue(arr.SequenceEqual(arr2));
            }

            // Ensure buf doesn't get collected while we use the data pointer
            GC.KeepAlive(buf);
        }

        [TestMethod]
        public unsafe void TestTryGetDataUnsafe_MemoryBufferReference()
        {
            var buffer = new Windows.Foundation.MemoryBuffer(256);
            var reference = buffer.CreateReference();

            Assert.IsTrue(WindowsRuntimeBufferMarshal.TryGetDataUnsafe(reference, out byte* dataPtr1, out uint capacity1));
            Assert.IsTrue(dataPtr1 != null);
            Assert.IsTrue(capacity1 == 256);

            Assert.IsTrue(WindowsRuntimeBufferMarshal.TryGetDataUnsafe(reference, out byte* dataPtr2, out uint capacity2));
            Assert.IsTrue(dataPtr2 != null);
            Assert.IsTrue(capacity2 == 256);

            Assert.IsTrue(dataPtr1 == dataPtr2);

            // Ensure the reference doesn't get collected while we use the data pointer
            GC.KeepAlive(reference);
        }

        [TestMethod]
        public void TestBufferTryGetArray()
        {
            byte[] arr = new byte[] { 0x01, 0x02, 0x03 };
            var buffer = arr.AsBuffer();

            Assert.IsTrue(WindowsRuntimeBufferMarshal.TryGetArray(buffer, out ArraySegment<byte> array));
            Assert.AreEqual(arr, array.Array);
        }

        [TestMethod]
        public void TestBufferTryGetArraySubset()
        {
            var arr = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            var buffer = arr.AsBuffer(1, 2);

            Assert.IsTrue(WindowsRuntimeBufferMarshal.TryGetArray(buffer, out ArraySegment<byte> array));
            Assert.AreEqual(arr, array.Array);
            Assert.AreEqual(1, array.Offset);
            Assert.AreEqual(2, array.Count);
        }

        [TestMethod]
        // Mapped System.* Types Primitive Types
        [DataRow(typeof(bool), "Boolean", "Primitive")]
        [DataRow(typeof(byte), "UInt8", "Primitive")]
        [DataRow(typeof(char), "Char16", "Primitive")]
        [DataRow(typeof(double), "Double", "Primitive")]
        [DataRow(typeof(short), "Int16", "Primitive")]
        [DataRow(typeof(int), "Int32", "Primitive")]
        [DataRow(typeof(long), "Int64", "Primitive")]
        [DataRow(typeof(float), "Single", "Primitive")]
        [DataRow(typeof(ushort), "UInt16", "Primitive")]
        [DataRow(typeof(uint), "UInt32", "Primitive")]
        [DataRow(typeof(ulong), "UInt64", "Primitive")]
        // Mapped System.* Types Metadata Types
        [DataRow(typeof(DateTimeOffset), "Windows.Foundation.DateTime", "Metadata")]
        [DataRow(typeof(EventHandler<Guid>), "Windows.Foundation.EventHandler`1<Guid>", "Metadata")]
        [DataRow(typeof(Exception), "Windows.Foundation.HResult", "Metadata")]
        [DataRow(typeof(Guid), "Guid", "Metadata")]
        [DataRow(typeof(IDisposable), "Windows.Foundation.IClosable", "Metadata")]
        [DataRow(typeof(IServiceProvider), "Microsoft.UI.Xaml.IXamlServiceProvider", "Metadata")]
        [DataRow(typeof(object), "Object", "Metadata")]
        [DataRow(typeof(string), "String", "Metadata")]
        [DataRow(typeof(TimeSpan), "Windows.Foundation.TimeSpan", "Metadata")]
        [DataRow(typeof(Type), "Windows.UI.Xaml.Interop.TypeName", "Metadata")]
        [DataRow(typeof(Uri), "Windows.Foundation.Uri", "Metadata")]
        [DataRow(typeof(ICommand), "Microsoft.UI.Xaml.Input.ICommand", "Metadata")]
        // Mapped System.Collections.* Types
        [DataRow(typeof(IEnumerable), "Microsoft.UI.Xaml.Interop.IBindableIterable", "Metadata")]
        [DataRow(typeof(IEnumerator), "Microsoft.UI.Xaml.Interop.IBindableIterator", "Metadata")]
        [DataRow(typeof(IList), "Microsoft.UI.Xaml.Interop.IBindableVector", "Metadata")]
        [DataRow(typeof(IReadOnlyList<long>), "Windows.Foundation.Collections.IVectorView`1<Int64>", "Metadata")]
        // Mapped System.Collections.Specialized* Types
        [DataRow(typeof(INotifyCollectionChanged), "Microsoft.UI.Xaml.Interop.INotifyCollectionChanged", "Metadata")]
        [DataRow(typeof(NotifyCollectionChangedEventArgs), "Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventArgs", "Metadata")]
        [DataRow(typeof(NotifyCollectionChangedEventHandler), "Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventHandler", "Metadata")]
        // Mapped System.ComponentModel.* Types
        [DataRow(typeof(DataErrorsChangedEventArgs), "Microsoft.UI.Xaml.Data.DataErrorsChangedEventArgs", "Metadata")]
        [DataRow(typeof(INotifyDataErrorInfo), "Microsoft.UI.Xaml.Data.INotifyDataErrorInfo", "Metadata")]
        [DataRow(typeof(INotifyPropertyChanged), "Microsoft.UI.Xaml.Data.INotifyPropertyChanged", "Metadata")]
        [DataRow(typeof(PropertyChangedEventArgs), "Microsoft.UI.Xaml.Data.PropertyChangedEventArgs", "Metadata")]
        [DataRow(typeof(PropertyChangedEventHandler), "Microsoft.UI.Xaml.Data.PropertyChangedEventHandler", "Metadata")]
        // Mapped System.Numerics.* Types
        [DataRow(typeof(Matrix3x2), "Windows.Foundation.Numerics.Matrix3x2", "Metadata")]
        [DataRow(typeof(Matrix4x4), "Windows.Foundation.Numerics.Matrix4x4", "Metadata")]
        [DataRow(typeof(Plane), "Windows.Foundation.Numerics.Plane", "Metadata")]
        [DataRow(typeof(Quaternion), "Windows.Foundation.Numerics.Quaternion", "Metadata")]
        [DataRow(typeof(Vector2), "Windows.Foundation.Numerics.Vector2", "Metadata")]
        [DataRow(typeof(Vector3), "Windows.Foundation.Numerics.Vector3", "Metadata")]
        [DataRow(typeof(Vector4), "Windows.Foundation.Numerics.Vector4", "Metadata")]
        // Mapped Windows.Foundation.* Types
        [DataRow(typeof(AsyncActionCompletedHandler), "Windows.Foundation.AsyncActionCompletedHandler", "Metadata")]
        [DataRow(typeof(IAsyncAction), "Windows.Foundation.IAsyncAction", "Metadata")]
        [DataRow(typeof(IAsyncInfo), "Windows.Foundation.IAsyncInfo", "Metadata")]
        [DataRow(typeof(Point), "Windows.Foundation.Point", "Metadata")]
        [DataRow(typeof(Rect), "Windows.Foundation.Rect", "Metadata")]
        [DataRow(typeof(Size), "Windows.Foundation.Size", "Metadata")]
        [DataRow(typeof(IStringable), "Windows.Foundation.IStringable", "Metadata")]
        // Mapped Windows.Foundation.Collections.* Types
        [DataRow(typeof(CollectionChange), "Windows.Foundation.Collections.CollectionChange", "Metadata")]
        [DataRow(typeof(IVectorChangedEventArgs), "Windows.Foundation.Collections.IVectorChangedEventArgs", "Metadata")]
        // Mapped WindowsRuntime.InteropServices.* Types
        [DataRow(typeof(EventRegistrationToken), "WindowsRuntime.InteropServices.EventRegistrationToken", "Metadata")]
        // Others
        [DataRow(typeof(NotifyCollectionChangedAction), "Microsoft.UI.Xaml.Interop.NotifyCollectionChangedAction", "Metadata")]
        [DataRow(typeof(NotifyCollectionChangedAction?), "Windows.Foundation.IReference`1<Microsoft.UI.Xaml.Interop.NotifyCollectionChangedAction>", "Metadata")]
        // Projected WinRT Types
        [DataRow(typeof(TestComponent.Class), "TestComponent.Class", "Metadata")]
        [DataRow(typeof(TestComponent.Nested), "TestComponent.Nested", "Metadata")]
        [DataRow(typeof(TestComponent.IRequiredOne), "TestComponent.IRequiredOne", "Metadata")]
        [DataRow(typeof(TestComponent.Param6Handler), "TestComponent.Param6Handler", "Metadata")]
        [DataRow(typeof(TestComponentCSharp.Class), "TestComponentCSharp.Class", "Metadata")]
        [DataRow(typeof(TestComponentCSharp.ComposedBlittableStruct), "TestComponentCSharp.ComposedBlittableStruct", "Metadata")]
        [DataRow(typeof(TestComponentCSharp.IArtist), "TestComponentCSharp.IArtist", "Metadata")]
        [DataRow(typeof(TestComponentCSharp.EnumValue), "TestComponentCSharp.EnumValue", "Metadata")]
        [DataRow(typeof(TestComponentCSharp.EventHandler0), "TestComponentCSharp.EventHandler0", "Metadata")]
        // Nullable Types
        [DataRow(typeof(long?), "Windows.Foundation.IReference`1<Int64>", "Metadata")]
        [DataRow(typeof(Point?), "Windows.Foundation.IReference`1<Windows.Foundation.Point>", "Metadata")]
        [DataRow(typeof(Vector3?), "Windows.Foundation.IReference`1<Windows.Foundation.Numerics.Vector3>", "Metadata")]
        [DataRow(typeof(Guid?), "Windows.Foundation.IReference`1<Guid>", "Metadata")]
        [DataRow(typeof(TestComponentCSharp.EnumValue?), "Windows.Foundation.IReference`1<TestComponentCSharp.EnumValue>", "Metadata")]
        [DataRow(typeof(TestComponentCSharp.BlittableStruct?), "Windows.Foundation.IReference`1<TestComponentCSharp.BlittableStruct>", "Metadata")]
        [DataRow(typeof(IList<Int32?>), "Windows.Foundation.Collections.IVector`1<Windows.Foundation.IReference`1<Int32>>", "Metadata")]
        [DataRow(typeof(Int32?[]), "Windows.Foundation.IReferenceArray`1<Windows.Foundation.IReference`1<Int32>>", "Metadata")]
        // Generic Types
        [DataRow(typeof(IList<long>), "Windows.Foundation.Collections.IVector`1<Int64>", "Metadata")]
        [DataRow(typeof(IList<TestComponentCSharp.EventWithGuid>), "Windows.Foundation.Collections.IVector`1<TestComponentCSharp.EventWithGuid>", "Metadata")] // Using the fully qualified asssembly name
        [DataRow(typeof(IList<TestComponentCSharp.Class>), "Windows.Foundation.Collections.IVector`1<TestComponentCSharp.Class>", "Metadata")] // Using the fully qualified asssembly name
        [DataRow(typeof(IEnumerator<TestComponentCSharp.Class>), "Windows.Foundation.Collections.IIterator`1<TestComponentCSharp.Class>", "Metadata")]
        [DataRow(typeof(IEnumerable<TestComponentCSharp.Class>), "Windows.Foundation.Collections.IIterable`1<TestComponentCSharp.Class>", "Metadata")]
        [DataRow(typeof(EventHandler<TestComponentCSharp.Class>), "Windows.Foundation.EventHandler`1<TestComponentCSharp.Class>", "Metadata")]
        [DataRow(typeof(KeyValuePair<Object, TestComponentCSharp.Class>), "Windows.Foundation.Collections.IKeyValuePair`2<Object, TestComponentCSharp.Class>", "Metadata")]
        [DataRow(typeof(KeyValuePair<Object, Object>), "Windows.Foundation.Collections.IKeyValuePair`2<Object, Object>", "Metadata")]
        [DataRow(typeof(KeyValuePair<String, TestComponentCSharp.Class>), "Windows.Foundation.Collections.IKeyValuePair`2<String, TestComponentCSharp.Class>", "Metadata")]
        // Nested Generics
        [DataRow(typeof(EventHandler<IList<long>>), "Windows.Foundation.EventHandler`1<Windows.Foundation.Collections.IVector`1<Int64>>", "Metadata")]
        [DataRow(typeof(EventHandler<KeyValuePair<Object, Object>>), "Windows.Foundation.EventHandler`1<Windows.Foundation.Collections.IKeyValuePair`2<Object, Object>>", "Metadata")]
        [DataRow(typeof(EventHandler<KeyValuePair<String, TestComponentCSharp.Class>>), "Windows.Foundation.EventHandler`1<Windows.Foundation.Collections.IKeyValuePair`2<String, TestComponentCSharp.Class>>", "Metadata")]
        [DataRow(typeof(IList<KeyValuePair<Object, Object>>), "Windows.Foundation.Collections.IVector`1<Windows.Foundation.Collections.IKeyValuePair`2<Object, Object>>", "Metadata")]
        [DataRow(typeof(IEnumerator<IEnumerable<Object>>), "Windows.Foundation.Collections.IIterator`1<Windows.Foundation.Collections.IIterable`1<Object>>", "Metadata")]
        [DataRow(typeof(IEnumerable<KeyValuePair<string, TestComponentCSharp.Class>>), "Windows.Foundation.Collections.IIterable`1<Windows.Foundation.Collections.IKeyValuePair`2<String, TestComponentCSharp.Class>>", "Metadata")]
        // Custom Types
        [DataRow(typeof(UnitTestCSharp), "UnitTest.UnitTestCSharp, UnitTest, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "Custom")]
        [DataRow(typeof(IList<UnitTestCSharp>), "System.Collections.Generic.IList`1[[UnitTest.UnitTestCSharp, UnitTest, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e", "Custom")]
        [DataRow(typeof(ITestCSharp<double>), "UnitTest.ITestCSharp`1[[System.Double, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], UnitTest, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "Custom")]
        [DataRow(typeof(KeyValuePair<String, UnitTestCSharp>), "System.Collections.Generic.KeyValuePair`2[[System.String, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[UnitTest.UnitTestCSharp, UnitTest, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e", "Custom")]
        [DataRow(typeof(EventHandler<IList<UnitTestCSharp>>), "System.EventHandler`1[[System.Collections.Generic.IList`1[[UnitTest.UnitTestCSharp, UnitTest, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e", "Custom")]
        [DataRow(typeof(EventHandler<UnitTestCSharp>), "System.EventHandler`1[[UnitTest.UnitTestCSharp, UnitTest, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e", "Custom")]
        [DataRow(typeof(DelegateTestCSharp<Guid>), "UnitTest.DelegateTestCSharp`1[[System.Guid, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], UnitTest, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "Custom")]
        [DataRow(typeof(WeakReference<TestComponent.Class>), "System.WeakReference`1[[TestComponent.Class, WinRT.Projection, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e", "Custom")]
        [DataRow(typeof(KeyValuePair<Int32, WeakReference<Object>>), "System.Collections.Generic.KeyValuePair`2[[System.Int32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.WeakReference`1[[System.Object, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e", "Custom")]
        [DataRow(typeof(ICollection<object>), "System.Collections.Generic.ICollection`1[[System.Object, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e", "Custom")]
        [DataRow(typeof(KeyValuePair<Object, Object>?), "System.Nullable`1[[System.Collections.Generic.KeyValuePair`2[[System.Object, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Object, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e", "Custom")]
        [DataRow(typeof(KeyValuePair<Object, Object[]>), "System.Collections.Generic.KeyValuePair`2[[System.Object, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Object[], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e", "Custom")]
        [DataRow(typeof(long[][]), "System.Int64[][], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e", "Custom")]
        // Arrays
        [DataRow(typeof(Object[]), "Windows.Foundation.IReferenceArray`1<Object>", "Metadata")]
        [DataRow(typeof(long[]), "Windows.Foundation.IReferenceArray`1<Int64>", "Metadata")]
        [DataRow(typeof(TestComponentCSharp.Class[]), "Windows.Foundation.IReferenceArray`1<TestComponentCSharp.Class>", "Metadata")]
        [DataRow(typeof(TestComponentCSharp.ComposedBlittableStruct[]), "Windows.Foundation.IReferenceArray`1<TestComponentCSharp.ComposedBlittableStruct>", "Metadata")]
        [DataRow(typeof(TestComponentCSharp.IArtist[]), "Windows.Foundation.IReferenceArray`1<TestComponentCSharp.IArtist>", "Metadata")]
        [DataRow(typeof(TestComponentCSharp.EnumValue[]), "Windows.Foundation.IReferenceArray`1<TestComponentCSharp.EnumValue>", "Metadata")]
        [DataRow(typeof(TestComponentCSharp.EventHandler0[]), "Windows.Foundation.IReferenceArray`1<TestComponentCSharp.EventHandler0>", "Metadata")]
        [DataRow(typeof(Point[]), "Windows.Foundation.IReferenceArray`1<Windows.Foundation.Point>", "Metadata")]
        [DataRow(typeof(IList<long>[]), "Windows.Foundation.IReferenceArray`1<Windows.Foundation.Collections.IVector`1<Int64>>", "Metadata")]
        [DataRow(typeof(IList<long?>[]), "Windows.Foundation.IReferenceArray`1<Windows.Foundation.Collections.IVector`1<Windows.Foundation.IReference`1<Int64>>>", "Metadata")]
        [DataRow(typeof(KeyValuePair<Object, Object>[]), "Windows.Foundation.IReferenceArray`1<Windows.Foundation.Collections.IKeyValuePair`2<Object, Object>>", "Metadata")]
        public void TestTypePropertyConvertToUnmanaged(Type type, string name, string kind)
        {
            // test method here
            TestObject.TypeProperty = type;
            Assert.AreEqual(name, TestObject.GetTypePropertyAbiName());
            Assert.AreEqual(kind, TestObject.GetTypePropertyKind());
        }

        class CustomDictionary : Dictionary<string, string> { }

        [TestMethod]
        public void TestTypePropertyWithCustomType()
        {
            TestObject.TypeProperty = typeof(CustomDictionary);
            var name = TestObject.GetTypePropertyAbiName();
            Assert.AreEqual("UnitTest.UnitTestCSharp+CustomDictionary, UnitTest, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", name);
        }

        [TestMethod]
        public void TestVectorCastConversion()
        {

            var vector = TestObject.GetUriVectorAsIInspectableVector();
            var uriVector = vector.Cast<Uri>();
            var first = uriVector.First();
            // Assert the sequences are equivalent
            Assert.IsTrue(vector.Cast<Uri>().SequenceEqual(uriVector)); // [2]
        }

        async Task LookupPorts()
        {
            var ports = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(
                Windows.Devices.SerialCommunication.SerialDevice.GetDeviceSelector(),
                new string[] { "System.ItemNameDisplay" });
            foreach (var port in ports)
            {
                object o = port.Properties["System.ItemNameDisplay"];
                Assert.IsNotNull(o);
            }
        }

        [TestMethod]
        public void TestReadOnlyDictionaryLookup()
        {
            Assert.IsTrue(LookupPorts().Wait(5000));
        }

#if NET
        async Task InvokeStreamWriteZeroBytes()
        {
            var random = new Random(42);
            byte[] data = new byte[256];
            random.NextBytes(data);

            using var stream = new InMemoryRandomAccessStream().AsStream();
            await stream.WriteAsync(data, 0, 0);
            await stream.WriteAsync(data, data.Length, 0);
        }

        [TestMethod]
        public void TestStreamWriteZeroByte()
        {
            Assert.IsTrue(InvokeStreamWriteZeroBytes().Wait(1000));
        }

        async Task InvokeStreamWriteAsync()
        {
            using var fileStream = File.OpenWrite("TestFile.txt");
            using var winRTStream = fileStream.AsOutputStream();

            var winRTBuffer = new Windows.Storage.Streams.Buffer(capacity: 0);

            await winRTStream.WriteAsync(winRTBuffer);
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestStreamWriteAsync()
        {
            Assert.IsTrue(InvokeStreamWriteAsync().Wait(1000));
        }

        [TestMethod]
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
            Assert.IsTrue(read.SequenceEqual(data));
        }

        [TestMethod]
        public void TestStreamWriteAndRead()
        {
            Assert.IsTrue(InvokeStreamWriteAndReadAsync().Wait(1000));
        }

        /* TODO
        [TestMethod]
        public void TestDynamicInterfaceCastingOnValidInterface()
        {
            var agileObject = (IAgileObject)(IWinRTObject)TestObject;
            Assert.IsNotNull(agileObject);
        }
        */

        [TestMethod]
        public void TestDynamicInterfaceCastingOnInvalidInterface()
        {
            Assert.ThrowsException<System.InvalidCastException>(() => (IStringableInterop)(WindowsRuntimeObject)TestObject);
        }

        [TestMethod]
        public void TestBuffer()
        {
            var arr1 = new byte[] { 0x01, 0x02 };
            var buff = arr1.AsBuffer();
            var arr2 = buff.ToArray(0, 2);
            Assert.IsTrue(arr1[0] == arr2[0]);
            Assert.IsTrue(arr1[1] == arr2[1]);
        }

#endif

        async Task TestStorageFileAsync()
        {
            var folderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            StorageFile file = await StorageFile.GetFileFromPathAsync(folderPath + "\\UnitTest.dll");
            var handle = WindowsRuntimeStorageExtensions.CreateSafeFileHandle(file, FileAccess.Read);
            Assert.IsNotNull(handle);
        }

        [TestMethod]
        public void TestStorageFile()
        {
            Assert.IsTrue(TestStorageFileAsync().Wait(5000));
        }

        async Task TestStorageFolderAsync()
        {
            var folderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(folderPath);
            var handle = WindowsRuntimeStorageExtensions.CreateSafeFileHandle(folder, "UnitTest.dll", FileMode.Open, FileAccess.Read);
            Assert.IsNotNull(handle);
        }

        [TestMethod]
        public void TestStorageFolder()
        {
            Assert.IsTrue(TestStorageFolderAsync().Wait(5000));
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

        [TestMethod]
        public void TestWriteBuffer()
        {
            Assert.IsTrue(InvokeWriteBufferAsync().Wait(1000));
        }

        [TestMethod]
        public unsafe void TestUri()
        {
            var base_uri = "https://github.com";
            var relative_uri = "microsoft/CsWinRT";
            var full_uri = base_uri + "/" + relative_uri;
            var managedUri = new Uri(full_uri);

            using WindowsRuntimeObjectReferenceValue uriObjectRefValue = ABI.System.UriMarshaller.ConvertToUnmanaged(managedUri);
            var uri1 = ABI.System.UriMarshaller.ConvertToManaged(uriObjectRefValue.GetThisPtrUnsafe());
            var str1 = uri1.ToString();
            Assert.AreEqual(full_uri, str1);

            var expected = new Uri("http://expected");
            TestObject.UriProperty = expected;
            Assert.AreEqual(expected, TestObject.UriProperty);

            TestObject.CallForUri(() => managedUri);
            TestObject.UriPropertyChanged +=
                (object sender, Uri value) => Assert.AreEqual(managedUri, value);
            TestObject.RaiseUriChanged();

            var uri2 = WindowsRuntimeMarshal.ConvertToManaged(WindowsRuntimeMarshal.ConvertToUnmanaged(managedUri));
            var str2 = uri2.ToString();
            Assert.AreEqual(full_uri, str2);
        }

        [TestMethod]
        public void TestNulls()
        {
            TestObject.StringProperty = null;
            Assert.AreEqual("", TestObject.StringProperty);
            TestObject.CallForString(() => null);
            TestObject.StringPropertyChanged +=
                (Class sender, string value) => Assert.AreEqual("", value);
            TestObject.RaiseStringChanged();

            TestObject.UriProperty = null;
            Assert.IsNull(TestObject.UriProperty);
            TestObject.CallForUri(() => null);
            TestObject.UriPropertyChanged +=
                (object sender, Uri value) => Assert.IsNull(value);
            TestObject.RaiseUriChanged();

            TestObject.ObjectProperty = null;
            Assert.IsNull(TestObject.ObjectProperty);
            TestObject.CallForObject(() => null);
            TestObject.ObjectPropertyChanged +=
                (object sender, Object value) => Assert.IsNull(value);
            TestObject.RaiseObjectChanged();

            // todo: arrays, delegates, event args, mapped types...
        }

        [TestMethod]
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
                Assert.IsInstanceOfType<Class>(sender);
            };
            TestObject.InvokeEvent1(TestObject);
            events_expected++;

            int int0 = 42;
            TestObject.Event2 += (Class sender, int arg0) =>
            {
                events_received++;
                Assert.AreEqual(arg0, int0);
            };
            TestObject.InvokeEvent2(TestObject, int0);
            events_expected++;

            string string1 = "foo";
            TestObject.Event3 += (Class sender, int arg0, string arg1) =>
            {
                events_received++;
                Assert.AreEqual(arg1, string1);
            };
            TestObject.InvokeEvent3(TestObject, int0, string1);
            events_expected++;

            int[] ints = { 1, 2, 3 };
            TestObject.NestedEvent += (object sender, IList<int> arg0) =>
            {
                events_received++;
                Assert.IsTrue(arg0.SequenceEqual(ints));
            };
            TestObject.InvokeNestedEvent(TestObject, ints);
            events_expected++;

            TestObject.ReturnEvent += (int arg0) =>
            {
                events_received++;
                return arg0;
            };
            Assert.AreEqual(42, TestObject.InvokeReturnEvent(42));
            events_expected++;

            var collection0 = new int[] { 42, 1729 };
            var collection1 = new Dictionary<int, string> { [1] = "foo", [2] = "bar" };
            TestObject.CollectionEvent += (Class sender, IList<int> arg0, IDictionary<int, string> arg1) =>
            {
                events_received++;
                Assert.IsTrue(arg0.SequenceEqual(collection0));
                Assert.IsTrue(arg1.SequenceEqual(collection1));
            };
            TestObject.InvokeCollectionEvent(TestObject, collection0, collection1);
            events_expected++;

            Assert.AreEqual(events_received, events_expected);
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
                Assert.AreEqual(new Uri("http://github.com"), TestObject.UriProperty);
            }
        }

        [TestMethod]
        public void TestDelegateUnwrapping()
        {
            var obj = TestObject.GetUriDelegate();
            TestObject.CallForUri(obj);
            Assert.AreEqual(new Uri("http://microsoft.com"), TestObject.UriProperty);

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

        [TestMethod]
        public void TestCustomProjections()
        {
            // INotifyDataErrorsInfo
            string propertyName = "";
            TestObject.ErrorsChanged += (object sender, System.ComponentModel.DataErrorsChangedEventArgs e) =>
            {
                propertyName = e.PropertyName;
            };
            TestObject.RaiseDataErrorChanged();
            Assert.AreEqual("name", propertyName);

            bool eventCalled = false;
            TestObject.CanExecuteChanged += (object sender, EventArgs e) =>
            {
                eventCalled = true;
            };

            TestObject.RaiseCanExecuteChanged();
            Assert.IsTrue(eventCalled);

            // IXamlServiceProvider <-> IServiceProvider
            /* TODO
            var serviceProvider = Class.ServiceProvider.As<IServiceProviderInterop>();
            IntPtr service;
            serviceProvider.GetService(IntPtr.Zero, out service);
            Assert.AreEqual(new IntPtr(42), service);
            */

            // Ensure robustness with bad runtime class names (parsing errors, type not found, etc)
            var badRuntimeClassName = Class.BadRuntimeClassName;
            Assert.IsNotNull(badRuntimeClassName);
        }

        [TestMethod]
        public void TestKeyValuePair()
        {
            var expected = new KeyValuePair<string, string>("key", "value");
            TestObject.StringPairProperty = expected;
            Assert.AreEqual(expected, TestObject.StringPairProperty);

            expected = new KeyValuePair<string, string>("foo", "bar");
            TestObject.CallForStringPair(() => expected);
            TestObject.StringPairPropertyChanged +=
                (object sender, KeyValuePair<string, string> value) => Assert.AreEqual(expected, value);
            TestObject.RaiseStringPairChanged();

            var expected2 = new KeyValuePair<EnumValue, EnumStruct>(EnumValue.Two, new EnumStruct() { value = EnumValue.One });
            TestObject.EnumPairProperty = expected2;
            Assert.AreEqual(expected2, TestObject.EnumPairProperty);
        }

        [TestMethod]
        public void TestObjectCasting()
        {
            object expected_uri = new Uri("http://aka.ms/cswinrt");
            TestObject.ObjectProperty = expected_uri;
            Assert.AreEqual(expected_uri, TestObject.UriProperty);
            Assert.AreEqual(expected_uri, TestObject.ObjectProperty);

            var expected = new KeyValuePair<string, string>("key", "value");
            TestObject.ObjectProperty = expected;
            var out_pair = (KeyValuePair<string, string>)TestObject.ObjectProperty;
            Assert.AreEqual(expected, out_pair);

            var nested = new KeyValuePair<KeyValuePair<int, int>, KeyValuePair<string, string>>(
                new KeyValuePair<int, int>(42, 1729),
                new KeyValuePair<string, string>("key", "value")
            );
            TestObject.ObjectProperty = nested;
            var out_nested = (KeyValuePair<KeyValuePair<int, int>, KeyValuePair<string, string>>)TestObject.ObjectProperty;
            Assert.AreEqual(nested, out_nested);

            // Test projected types in generic of KeyValuePair
            TestObject.ObjectProperty = new KeyValuePair<string, IProperties1>("one", new Class());
            TestObject.ObjectProperty = new KeyValuePair<string, Class>("two", new Class());
            TestObject.ObjectProperty = new KeyValuePair<string, IDisposable>("two", new CustomDisposableTest());

            // Test non-projected types in generic of KeyValuePair
            TestObject.ObjectProperty = new KeyValuePair<string, PlatformID>("two", PlatformID.Win32NT);
            TestObject.ObjectProperty = new KeyValuePair<string, Random>("two", new Random());

            var strings_in = new[] { "hello", "world" };
            TestObject.StringsProperty = strings_in;
            var strings_out = TestObject.StringsProperty;
            Assert.IsTrue(strings_out.SequenceEqual(strings_in));

            TestObject.ObjectProperty = strings_in;
            strings_out = (string[])TestObject.ObjectProperty;
            Assert.IsTrue(strings_out.SequenceEqual(strings_in));

            var eventHandler = new EventHandler<int>((sender, args) => { });
            TestObject.ObjectProperty = eventHandler;
            Assert.AreEqual(eventHandler, TestObject.ObjectProperty);

            var objects = new List<ManagedType>() { new ManagedType(), new ManagedType() };
            var query = from item in objects select item;
            TestObject.ObjectIterableProperty = query;

            TestObject.ObjectProperty = "test";
            Assert.AreEqual("test", TestObject.ObjectProperty);

            var objectArray = new ManagedType[] { new ManagedType(), new ManagedType() };
            TestObject.ObjectIterableProperty = objectArray;
            Assert.IsTrue(TestObject.ObjectIterableProperty.SequenceEqual(objectArray));

            var strArray = new string[] { "str1", "str2", "str3" };
            TestObject.ObjectIterableProperty = strArray;
            Assert.IsTrue(TestObject.ObjectIterableProperty.SequenceEqual(strArray));

            var uriArray = new Uri[] { new Uri("http://aka.ms/cswinrt"), new Uri("http://github.com") };
            TestObject.ObjectIterableProperty = uriArray;
            Assert.IsTrue(TestObject.ObjectIterableProperty.SequenceEqual(uriArray));

            var objectUriArray = new object[] { new Uri("http://github.com") };
            TestObject.ObjectIterableProperty = objectUriArray;
            Assert.IsTrue(TestObject.ObjectIterableProperty.SequenceEqual(objectUriArray));
        }

        [TestMethod]
        public void TestStringMap()
        {
            var map = new Dictionary<string, string> { ["foo"] = "bar", ["hello"] = "world" };
            var stringMap = new Windows.Foundation.Collections.StringMap();
            foreach (var item in map)
            {
                stringMap[item.Key] = item.Value;
            }
            Assert.AreEqual(map.Count, stringMap.Count);
            foreach (var item in map)
            {
                Assert.AreEqual(stringMap[item.Key], item.Value);
            }
            KeyValuePair<string, string>[] pairs = new KeyValuePair<string, string>[2];
            stringMap.CopyTo(pairs, 0);
            Assert.AreEqual(2, pairs.Length);
        }

        [TestMethod]
        public void TestPropertySet()
        {
            var map = new Dictionary<string, string> { ["foo"] = "bar", ["hello"] = "world" };
            var propertySet = new Windows.Foundation.Collections.PropertySet();
            foreach (var item in map)
            {
                propertySet[item.Key] = item.Value;
            }
            Assert.AreEqual(map.Count, propertySet.Count);
            foreach (var item in map)
            {
                Assert.AreEqual(propertySet[item.Key], item.Value);
            }
        }

        [TestMethod]
        public void TestValueSet()
        {
            var map = new Dictionary<string, string> { ["foo"] = "bar", ["hello"] = "world" };
            var valueSet = new Windows.Foundation.Collections.ValueSet();
            foreach (var item in map)
            {
                valueSet[item.Key] = item.Value;
            }
            Assert.AreEqual(map.Count, valueSet.Count);
            foreach (var item in map)
            {
                Assert.AreEqual(valueSet[item.Key], item.Value);
            }
        }

        [TestMethod]
        public void TestValueSetArrays()
        {
            var map = new Dictionary<string, long[]>
            {
                ["foo"] = new long[] { 1, 2, 3 },
                ["hello"] = new long[0],
                ["world"] = new long[] { 1, 2, 3 },
                ["bar"] = new long[0]
            };
            var valueSet = new Windows.Foundation.Collections.ValueSet();
            foreach (var item in map)
            {
                valueSet[item.Key] = item.Value;
            }
            Assert.AreEqual(map.Count, valueSet.Count);
            foreach (var item in map)
            {
                Assert.AreEqual(valueSet[item.Key], item.Value);
            }
        }

        [TestMethod]
        public void TestFactories()
        {
            var cls1 = new Class();

            var cls2 = new Class(42);
            Assert.AreEqual(42, cls2.IntProperty);

            var cls3 = new Class(42, "foo");
            Assert.AreEqual(42, cls3.IntProperty);
            Assert.AreEqual("foo", cls3.StringProperty);
        }

        [TestMethod]
        public void TestFactoriesWithExplicitlyImplementedIUnknown()
        {
            var cls1 = new ClassWithExplicitIUnknown();
            Assert.AreEqual(0, cls1.Value);
            cls1.Value = 42;
            Assert.AreEqual(42, cls1.Value);

            var cls2 = new ClassWithExplicitIUnknown(42);
            Assert.AreEqual(42, cls2.Value);
            cls2.Value = 22;
            Assert.AreEqual(22, cls2.Value);
        }

        [TestMethod]
        public void TestStaticMembers()
        {
            Class.StaticIntProperty = 42;
            Assert.AreEqual(42, Class.StaticIntProperty);

            Class.StaticStringProperty = "foo";
            Assert.AreEqual("foo", Class.StaticStringProperty);
        }

        [TestMethod]
        public void TestInterfaces()
        {
            var expected = "hello";
            TestObject.StringProperty = expected;

            // projected wrapper
            Assert.AreEqual(expected, TestObject.ToString());

            // implicit cast
            var str = (IStringable)TestObject;
            Assert.AreEqual(expected, str.ToString());

            var str2 = TestObject as IStringable;
            Assert.AreEqual(expected, str2.ToString());

            Assert.IsInstanceOfType<IStringable>(TestObject);
        }

        [TestMethod]
        public void TestAsync()
        {
            TestObject.IntProperty = 42;
            var async_get_int = TestObject.GetIntAsync();
            int async_int = 0;
            async_get_int.Completed = (info, status) => async_int = info.GetResults();
            async_get_int.GetResults();
            Assert.AreEqual(42, async_int);

            TestObject.StringProperty = "foo";
            var async_get_string = TestObject.GetStringAsync();
            string async_string = "";
            async_get_string.Completed = (info, status) => async_string = info.GetResults();
            int async_progress;
            async_get_string.Progress = (info, progress) => async_progress = progress;
            async_get_string.GetResults();
            Assert.AreEqual("foo", async_string);
        }

        [TestMethod]
        public void TestPrimitives()
        {
            var test_int = 21;
            TestObject.IntPropertyChanged += (object sender, Int32 value) =>
            {
                Assert.IsInstanceOfType<Class>(sender);
                var c = (Class)sender;
                Assert.AreEqual(value, test_int);
            };
            TestObject.IntProperty = test_int;

            var expectedVal = true;
            var hits = 0;
            TestObject.BoolPropertyChanged += (object sender, bool value) =>
            {
                Assert.AreEqual(expectedVal, value);
                ++hits;
            };

            TestObject.BoolProperty = true;
            Assert.AreEqual(1, hits);

            expectedVal = false;
            TestObject.CallForBool(() => false);
            Assert.AreEqual(2, hits);

            TestObject.RaiseBoolChanged();
            Assert.AreEqual(3, hits);
        }

        [TestMethod]
        public void TestStrings()
        {
            string test_string = "x";
            string test_string2 = "y";

            // In hstring from managed->native implicitly creates hstring reference
            TestObject.StringProperty = test_string;

            // Out hstring from native->managed only creates System.String on demand
            var sp = TestObject.StringProperty;
            Assert.AreEqual(sp, test_string);

            // Out hstring from managed->native always creates HString from System.String
            TestObject.CallForString(() => test_string2);
            Assert.AreEqual(TestObject.StringProperty, test_string2);

            // In hstring from native->managed only creates System.String on demand
            TestObject.StringPropertyChanged += (Class sender, string value) => sender.StringProperty2 = value;
            TestObject.RaiseStringChanged();
            Assert.AreEqual(TestObject.StringProperty2, test_string2);
        }

        [TestMethod]
        public void TestBlittableStruct()
        {
            // Property setter/getter
            var val = new BlittableStruct() { i32 = 42 };
            TestObject.BlittableStructProperty = val;
            Assert.AreEqual(42, TestObject.BlittableStructProperty.i32);

            // Manual getter
            Assert.AreEqual(42, TestObject.GetBlittableStruct().i32);

            // Manual setter
            val.i32 = 8;
            TestObject.SetBlittableStruct(val);
            Assert.AreEqual(8, TestObject.BlittableStructProperty.i32);

            // Output argument
            val = default;
            TestObject.OutBlittableStruct(out val);
            Assert.AreEqual(8, val.i32);
        }

        [TestMethod]
        public void TestComposedBlittableStruct()
        {
            // Property setter/getter
            var val = new ComposedBlittableStruct() { blittable = new BlittableStruct() { i32 = 42 } };
            TestObject.ComposedBlittableStructProperty = val;
            Assert.AreEqual(42, TestObject.ComposedBlittableStructProperty.blittable.i32);

            // Manual getter
            Assert.AreEqual(42, TestObject.GetComposedBlittableStruct().blittable.i32);

            // Manual setter
            var blittable = val.blittable;
            blittable.i32 = 8;
            val.blittable = blittable;
            TestObject.SetComposedBlittableStruct(val);
            Assert.AreEqual(8, TestObject.ComposedBlittableStructProperty.blittable.i32);

            // Output argument
            val = default;
            TestObject.OutComposedBlittableStruct(out val);
            Assert.AreEqual(8, val.blittable.i32);
        }

        [TestMethod]
        public void TestNonBlittableStringStruct()
        {
            // Property getter/setter
            var val = new NonBlittableStringStruct() { str = "I like tacos" };
            TestObject.NonBlittableStringStructProperty = val;
            Assert.AreEqual("I like tacos", TestObject.NonBlittableStringStructProperty.str.ToString());

            // Manual getter
            Assert.AreEqual("I like tacos", TestObject.GetNonBlittableStringStruct().str.ToString());

            // Manual setter
            val.str = "Hello, world";
            TestObject.SetNonBlittableStringStruct(val);
            Assert.AreEqual("Hello, world", TestObject.NonBlittableStringStructProperty.str.ToString());

            // Output argument
            val = default;
            TestObject.OutNonBlittableStringStruct(out val);
            Assert.AreEqual("Hello, world", val.str.ToString());
        }

        [TestMethod]
        public void TestNonBlittableBoolStruct()
        {
            // Property getter/setter
            var val = new NonBlittableBoolStruct() { w = true, x = false, y = true, z = false };
            TestObject.NonBlittableBoolStructProperty = val;
            Assert.IsTrue(TestObject.NonBlittableBoolStructProperty.w);
            Assert.IsFalse(TestObject.NonBlittableBoolStructProperty.x);
            Assert.IsTrue(TestObject.NonBlittableBoolStructProperty.y);
            Assert.IsFalse(TestObject.NonBlittableBoolStructProperty.z);

            // Manual getter
            Assert.IsTrue(TestObject.GetNonBlittableBoolStruct().w);
            Assert.IsFalse(TestObject.GetNonBlittableBoolStruct().x);
            Assert.IsTrue(TestObject.GetNonBlittableBoolStruct().y);
            Assert.IsFalse(TestObject.GetNonBlittableBoolStruct().z);

            // Manual setter
            val.w = false;
            val.x = true;
            val.y = false;
            val.z = true;
            TestObject.SetNonBlittableBoolStruct(val);
            Assert.IsFalse(TestObject.NonBlittableBoolStructProperty.w);
            Assert.IsTrue(TestObject.NonBlittableBoolStructProperty.x);
            Assert.IsFalse(TestObject.NonBlittableBoolStructProperty.y);
            Assert.IsTrue(TestObject.NonBlittableBoolStructProperty.z);

            // Output argument
            val = default;
            TestObject.OutNonBlittableBoolStruct(out val);
            Assert.IsFalse(val.w);
            Assert.IsTrue(val.x);
            Assert.IsFalse(val.y);
            Assert.IsTrue(val.z);
        }

        [TestMethod]
        public void TestNonBlittableRefStruct()
        {
            // Property getter/setter
            // TODO: Need to either support interface inheritance or project IReference/INullable for setter
            Assert.AreEqual(42, TestObject.NonBlittableRefStructProperty.ref32.Value);

            // Manual getter
            Assert.AreEqual(42, TestObject.GetNonBlittableRefStruct().ref32.Value);

            // TODO: Manual setter

            // Output argument
            NonBlittableRefStruct val;
            TestObject.OutNonBlittableRefStruct(out val);
            Assert.AreEqual(42, val.ref32.Value);
        }

        [TestMethod]
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
            Assert.AreEqual(42, TestObject.ComposedNonBlittableStructProperty.blittable.i32);
            Assert.AreEqual("I like tacos", TestObject.ComposedNonBlittableStructProperty.strings.str);
            Assert.IsTrue(TestObject.ComposedNonBlittableStructProperty.bools.w);
            Assert.IsFalse(TestObject.ComposedNonBlittableStructProperty.bools.x);
            Assert.IsTrue(TestObject.ComposedNonBlittableStructProperty.bools.y);
            Assert.IsFalse(TestObject.ComposedNonBlittableStructProperty.bools.z);

            // Manual getter
            Assert.AreEqual(42, TestObject.GetComposedNonBlittableStruct().blittable.i32);
            Assert.AreEqual("I like tacos", TestObject.GetComposedNonBlittableStruct().strings.str);
            Assert.IsTrue(TestObject.GetComposedNonBlittableStruct().bools.w);
            Assert.IsFalse(TestObject.GetComposedNonBlittableStruct().bools.x);
            Assert.IsTrue(TestObject.GetComposedNonBlittableStruct().bools.y);
            Assert.IsFalse(TestObject.GetComposedNonBlittableStruct().bools.z);

            // Manual setter
            var blittable = val.blittable;
            blittable.i32 = 8;
            val.blittable = blittable;
            var strings = val.strings;
            strings.str = "Hello, world";
            val.strings = strings;
            var bools = val.bools;
            bools.w = false;
            bools.x = true;
            bools.y = false;
            bools.z = true;
            val.bools = bools;
            TestObject.SetComposedNonBlittableStruct(val);
            Assert.AreEqual(8, TestObject.ComposedNonBlittableStructProperty.blittable.i32);
            Assert.AreEqual("Hello, world", TestObject.ComposedNonBlittableStructProperty.strings.str);
            Assert.IsFalse(TestObject.ComposedNonBlittableStructProperty.bools.w);
            Assert.IsTrue(TestObject.ComposedNonBlittableStructProperty.bools.x);
            Assert.IsFalse(TestObject.ComposedNonBlittableStructProperty.bools.y);
            Assert.IsTrue(TestObject.ComposedNonBlittableStructProperty.bools.z);

            // Output argument
            val = default;
            TestObject.OutComposedNonBlittableStruct(out val);
            Assert.AreEqual(8, val.blittable.i32);
            Assert.AreEqual("Hello, world", val.strings.str);
            Assert.IsFalse(val.bools.w);
            Assert.IsTrue(val.bools.x);
            Assert.IsFalse(val.bools.y);
            Assert.IsTrue(val.bools.z);
        }

        [TestMethod]
        public void TestBlittableArrays()
        {
            int[] arr = new[] { 2, 4, 6, 8 };
            TestObject.SetInts(arr);
            Assert.IsTrue(TestObject.GetInts().SequenceEqual(arr));

            TestObject.SetInts(null);
            Assert.IsTrue(TestObject.GetInts().SequenceEqual(null));
        }

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

        /* TODO
        [TestMethod]
        public unsafe void TestFactoryCast()
        {
            IntPtr hstr;

            // Access nonstatic class factory 
            var instanceFactory = Class.As<IStringableInterop>();
            instanceFactory.ToString(out hstr);
            Assert.AreEqual("Class", HStringMarshaller.ConvertToManaged((void*)hstr));

            // Access static class factory
            var staticFactory = ComImports.As<IStringableInterop>();
            staticFactory.ToString(out hstr);
            Assert.AreEqual("ComImports", HStringMarshaller.ConvertToManaged((void*)hstr));
        }
        */

        [TestMethod]
        public unsafe void TestMarshalString_FromAbiUnsafe()
        {
            // The span must be empty and point to a null-terminated buffer (HSTRING-s are null-terminated too)
            var span = HStringMarshaller.ConvertToManagedUnsafe(null);
            Assert.AreEqual(0, span.Length);
            Assert.IsTrue(MemoryMarshal.GetReference(span) == '\0');

            // Same thing but with round-tripping from a null string
            var hstr = HStringMarshaller.ConvertToUnmanaged(null);
            span = HStringMarshaller.ConvertToManagedUnsafe(hstr);
            Assert.AreEqual(0, span.Length);
            Assert.IsTrue(MemoryMarshal.GetReference(span) == '\0');
            HStringMarshaller.Free(hstr);

            // Same thing but with an empty string (equivalent to null)
            hstr = HStringMarshaller.ConvertToUnmanaged("");
            span = HStringMarshaller.ConvertToManagedUnsafe(hstr);
            Assert.AreEqual(0, span.Length);
            Assert.IsTrue(MemoryMarshal.GetReference(span) == '\0');
            HStringMarshaller.Free(hstr);

            // Marshal from some non-null, non-empty string. We want to check that both the span has the expected content,
            // but also that it's correctly null-terminated (outside of its bounds). This is always safe to access, like
            // before, because the memory should point to the HSTRING buffer, which is always null-terminated as well.
            hstr = HStringMarshaller.ConvertToUnmanaged(nameof(TestMarshalString_FromAbiUnsafe));
            span = HStringMarshaller.ConvertToManagedUnsafe(hstr);
            Assert.IsTrue(span.SequenceEqual(nameof(TestMarshalString_FromAbiUnsafe)));
            Assert.IsTrue(Unsafe.Add(ref MemoryMarshal.GetReference(span), span.Length) == '\0');
            HStringMarshaller.Free(hstr);
        }

        [TestMethod]
        public void TestFundamentalGeneric()
        {
            var ints = TestObject.GetIntVector();
            Assert.AreEqual(10, ints.Count);
            for (int i = 0; i < 10; ++i)
            {
                Assert.AreEqual(i, ints[i]);
            }

            var bools = TestObject.GetBoolVector();
            Assert.AreEqual(4, bools.Count);
            for (int i = 0; i < 4; ++i)
            {
                Assert.AreEqual(i % 2 == 0, bools[i]);
            }
        }

        [TestMethod]
        public void TestStringGeneric()
        {
            var strings = TestObject.GetStringVector();
            Assert.AreEqual(5, strings.Count);
            for (int i = 0; i < 5; ++i)
            {
                Assert.AreEqual("String" + i, strings[i]);
            }
        }

        [TestMethod]
        public void TestStructGeneric()
        {
            var blittable = TestObject.GetBlittableStructVector();
            Assert.AreEqual(5, blittable.Count);
            for (int i = 0; i < 5; ++i)
            {
                Assert.AreEqual(i, blittable[i].blittable.i32);
            }

            var nonblittable = TestObject.GetNonBlittableStructVector();
            Assert.AreEqual(3, nonblittable.Count);
            for (int i = 0; i < 3; ++i)
            {
                var val = nonblittable[i];
                Assert.AreEqual(i, val.blittable.i32);
                Assert.AreEqual("String" + i, val.strings.str);
                Assert.AreEqual(i % 2 == 0, val.bools.w);
                Assert.AreEqual(i % 2 == 1, val.bools.x);
                Assert.AreEqual(i % 2 == 0, val.bools.y);
                Assert.AreEqual(i % 2 == 1, val.bools.z);
                Assert.AreEqual(i, val.refs.ref32.Value);
            }
        }

        [TestMethod]
        public void TestValueUnboxing()
        {
            var objs = TestObject.GetObjectVector();
            Assert.AreEqual(3, objs.Count);
            for (int i = 0; i < 3; ++i)
            {
                Assert.AreEqual(i, (int)objs[i]);
            }
        }

        [TestMethod]
        void TestInterfaceGeneric()
        {
            var objs = TestObject.GetInterfaceVector();
            Assert.AreEqual(3, objs.Count);
            TestObject.ReadWriteProperty = 42;
            for (int i = 0; i < 3; ++i)
            {
                var obj = objs[i];
                Assert.AreSame(obj, TestObject);
                Assert.AreEqual(42, obj.ReadWriteProperty);
            }
        }

        [TestMethod]
        public void TestIterable()
        {
            var ints_in = new int[] { 0, 1, 2 };
            TestObject.SetIntIterable(ints_in);
            var ints_out = TestObject.GetIntIterable();
            Assert.IsTrue(ints_in.SequenceEqual(ints_out));
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
                VectorChanged?.Invoke(this, _observation = new TObservation());
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

        [TestMethod]
        public void TestBindable()
        {
            var expected = new int[] { 0, 1, 2 };

            TestObject.BindableIterableProperty = expected;
            Assert.AreEqual(expected, TestObject.BindableIterableProperty);
            TestObject.CallForBindableIterable(() => expected);
            TestObject.BindableIterablePropertyChanged +=
                (object sender, IEnumerable value) => Assert.IsTrue(expected.SequenceEqual(value.Cast<int>()));
            TestObject.RaiseBindableIterableChanged();

            TestObject.BindableVectorProperty = expected;
            Assert.IsTrue(expected.SequenceEqual(TestObject.BindableVectorProperty.Cast<int>()));
            TestObject.CallForBindableVector(() => expected);
            TestObject.BindableVectorPropertyChanged +=
                (object sender, IList value) => Assert.IsTrue(expected.SequenceEqual(value.Cast<int>()) );
            TestObject.RaiseBindableVectorChanged();

            var observable = new ManagedBindableObservable(expected);
            TestObject.BindableObservableVectorProperty = observable;
            observable.Add(3);
            Assert.AreEqual(6, observable.Observation);
        }

        [TestMethod]
        public void TestClassGeneric()
        {
            var objs = TestObject.GetClassVector();
            Assert.AreEqual(3, objs.Count);
            for (int i = 0; i < 3; ++i)
            {
                var obj = objs[i];
                Assert.AreSame(obj, TestObject);
                Assert.AreEqual(TestObject, objs[i]);
            }
        }

        [TestMethod]
        public void TestSimpleCCWs()
        {
            var managedProperties = new ManagedProperties(42);
            TestObject.CopyProperties(managedProperties);
            Assert.AreEqual(managedProperties.ReadWriteProperty, TestObject.ReadWriteProperty);
        }

        [TestMethod]
        public unsafe void TestCCWMarshaler()
        {
            Guid IID_IMarshal = new Guid("00000003-0000-0000-c000-000000000046");
            var managedProperties = new ManagedProperties(42);
            using WindowsRuntimeObjectReferenceValue ccw = WindowsRuntimeInterfaceMarshaller<IProperties1>.ConvertToUnmanaged(managedProperties, typeof(IProperties1).GUID);
            Marshal.ThrowExceptionForHR(Marshal.QueryInterface((IntPtr)ccw.GetThisPtrUnsafe(), in IID_IMarshal, out var marshalCCW));
            Assert.AreNotEqual(IntPtr.Zero, marshalCCW);

            var array = new byte[] { 0x01 };
            var buff = array.AsBuffer();
            using WindowsRuntimeObjectReferenceValue ccw2 = WindowsRuntimeInterfaceMarshaller<IBuffer>.ConvertToUnmanaged(buff, typeof(IBuffer).GUID);
            Marshal.ThrowExceptionForHR(Marshal.QueryInterface((IntPtr)ccw2.GetThisPtrUnsafe(), in IID_IMarshal, out var marshalCCW2));
            Assert.AreNotEqual(IntPtr.Zero, marshalCCW2);
        }

#if NET
        [TestMethod]
        public void TestDelegateCCWMarshaler()
        {
            CreateAndValidateStreamedFile().Wait();
        }

        private async Task CreateAndValidateStreamedFile()
        {
            var storageFile = await StorageFile.CreateStreamedFileAsync("CreateAndValidateStreamedFile.txt", StreamedFileWriter, null);
            using var inputStream = await storageFile.OpenSequentialReadAsync();
            using var stream = inputStream.AsStreamForRead();
            byte[] buff = new byte[50];
            var numRead = stream.Read(buff, 0, 50);
            Assert.IsTrue(numRead > 0);
            var result = System.Text.Encoding.Default.GetString(buff, 0, numRead).TrimEnd(null);
            Assert.AreEqual("Success!", result);
        }

        private static async void StreamedFileWriter(StreamedFileDataRequest request)
        {
            try
            {
                using (var stream = request.AsStreamForWrite())
                using (var streamWriter = new StreamWriter(stream))
                {
                    await streamWriter.WriteLineAsync("Success!");
                }
                request.Dispose();
            }
            catch (Exception)
            {
                request.FailAndClose(StreamedFileFailureMode.Incomplete);
            }
        }
#endif

        [TestMethod]
        public void TestWeakReference()
        {
            var managedProperties = new ManagedProperties(42);
            TestObject.CopyPropertiesViaWeakReference(managedProperties);
            Assert.AreEqual(managedProperties.ReadWriteProperty, TestObject.ReadWriteProperty);
        }

        [TestMethod]
        public unsafe void TestCCWIdentity()
        {
            var managedProperties = new ManagedProperties(42);
            using WindowsRuntimeObjectReferenceValue ccw1 = WindowsRuntimeInterfaceMarshaller<IProperties1>.ConvertToUnmanaged(managedProperties, typeof(IProperties1).GUID);
            using WindowsRuntimeObjectReferenceValue ccw2 = WindowsRuntimeInterfaceMarshaller<IProperties1>.ConvertToUnmanaged(managedProperties, typeof(IProperties1).GUID);
            Assert.AreEqual((nint)ccw1.GetThisPtrUnsafe(), (nint)ccw2.GetThisPtrUnsafe());
        }

        [TestMethod]
        public void TestInterfaceCCWLifetime()
        {
            unsafe static (WeakReference, WindowsRuntimeObjectReference) CreateCCW()
            {
                var managedProperties = new ManagedProperties(42);
                using WindowsRuntimeObjectReferenceValue ccw1Value = WindowsRuntimeInterfaceMarshaller<IProperties1>.ConvertToUnmanaged(managedProperties, typeof(IProperties1).GUID);
                WindowsRuntimeObjectReference ccw1 = WindowsRuntimeComWrappersMarshal.CreateObjectReferenceUnsafe(ccw1Value.GetThisPtrUnsafe(), typeof(IProperties1).GUID, out _);
                return (new WeakReference(managedProperties), ccw1);
            }

            static (WeakReference obj, WeakReference ccw) GetWeakReferenceToObjectAndCCW()
            {
                var (reference, ccw) = CreateCCW();

                GC.Collect();
                GC.WaitForPendingFinalizers();

                Assert.IsTrue(reference.IsAlive);
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
            Assert.IsFalse(obj.IsAlive);
        }

        [TestMethod]
        public void TestDelegateCCWLifetime()
        {
            unsafe static (WeakReference, WindowsRuntimeObjectReference) CreateCCW(Action<object, int> action)
            {
                EventHandler<object, int> eventHandler = (o, i) => action(o, i);
                void* eventHandlerCcwPtr = WindowsRuntimeMarshal.ConvertToUnmanaged(eventHandler);

                try
                {
                    WindowsRuntimeObjectReference ccw1 = WindowsRuntimeComWrappersMarshal.CreateObjectReferenceUnsafe(eventHandlerCcwPtr, WellKnownInterfaceIIDs.IID_IInspectable, out _);
                    return (new WeakReference(eventHandler), ccw1);
                }
                finally
                {
                    WindowsRuntimeMarshal.Free(eventHandlerCcwPtr);
                }
            }

            static (WeakReference obj, WeakReference ccw) GetWeakReferenceToObjectAndCCW(Action<object, int> action)
            {
                var (reference, ccw) = CreateCCW(action);

                GC.Collect();
                GC.WaitForPendingFinalizers();

                Assert.IsTrue(reference.IsAlive);
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
            Assert.IsFalse(obj.IsAlive);
        }

        [TestMethod]
        public void TestCCWIdentityThroughRefCountZero()
        {
            unsafe static (WeakReference, IntPtr) CreateCCWReference(IProperties1 properties)
            {
                using WindowsRuntimeObjectReferenceValue ccwValue = WindowsRuntimeInterfaceMarshaller<IProperties1>.ConvertToUnmanaged(properties, typeof(IProperties1).GUID);
                WindowsRuntimeObjectReference ccw = WindowsRuntimeComWrappersMarshal.CreateObjectReferenceUnsafe(ccwValue.GetThisPtrUnsafe(), typeof(IProperties1).GUID, out _);
                return (new WeakReference(ccw), (IntPtr)ccw.GetThisPtrUnsafe());
            }

            var obj = new ManagedProperties(42);

            var (ccwWeakReference, ptr) = CreateCCWReference(obj);

            GC.Collect();
            GC.WaitForPendingFinalizers();

            Assert.IsFalse(ccwWeakReference.IsAlive);

            var (_, ptr2) = CreateCCWReference(obj);

            Assert.AreEqual(ptr, ptr2);
        }

        [TestMethod]
        public void TestExceptionPropagation_Managed()
        {
            var exceptionToThrow = new ArgumentNullException("foo");
            var properties = new ThrowingManagedProperties(exceptionToThrow);
            var ex = Assert.ThrowsException<ArgumentNullException>(() => TestObject.CopyProperties(properties));
            Assert.AreEqual("foo", ex.ParamName);

            var properties2 = new ThrowingManagedProperties2(TestObject);
            var ex2 = Assert.ThrowsException<ArgumentNullException>(() => TestObject.CopyProperties(properties2));
            Assert.AreEqual("foo", ex2.ParamName);
        }

        [TestMethod]
        public void TestExceptionPropagation()
        {
            VerifyException<ArgumentException>(() => TestObject.ThrowExceptionWithMessage("Parameter1", false), "Parameter1");
            VerifyException<COMException>(() => TestObject.ThrowExceptionWithMessage("Error message", true), "Error message");

            void callProperties()
            {
                var properties = new ThrowingManagedProperties(TestObject);
                TestObject.CopyProperties(properties);
            }
            VerifyException<ArgumentException>(callProperties, "Property threw");

            VerifyException<ArgumentException>(() => TestObject.OriginateAndThrowExceptionWithMessage("Parameter3"), "Parameter3");

            void callProperties2()
            {
                var properties = new ThrowingManagedProperties(TestObject, true);
                TestObject.CopyProperties(properties);
            }
            VerifyException<ArgumentException>(callProperties2, "Property threw with language exception");

            static void VerifyException<T>(Action action, string expectedMessage) where T : Exception
            {
                try
                {
                    action();
                    Assert.IsTrue(false);
                }
                catch (T ex)
                {
                    Assert.IsTrue(ex.Message.Contains(expectedMessage));
                }
                catch (Exception)
                {
                    Assert.IsTrue(false);
                }
            }
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

            public ThrowingManagedProperties(Class instance, bool includeLanguageException = false)
            {
                Instance = instance;
                IncludeLanguageException = includeLanguageException;
            }

            public Exception ExceptionToThrow { get; }

            public Class Instance { get; }

            public bool IncludeLanguageException { get; }

            public int ReadWriteProperty
            {
                get
                {
                    if (Instance is not null)
                    {
                        if (IncludeLanguageException)
                        {
                            return Instance.OriginateAndThrowExceptionWithMessage("Property threw with language exception").Length;
                        }
                        else
                        {
                            return Instance.ThrowExceptionWithMessage("Property threw", false).Length;
                        }
                    }
                    else
                    {
                        throw ExceptionToThrow;
                    }
                }
            }
        }

        class ThrowingManagedProperties2 : IProperties1
        {
            public ThrowingManagedProperties2(Class instance)
            {
                Instance = instance;
            }

            public Class Instance { get; }

            public int ReadWriteProperty
            {
                get
                {
                    var exceptionToThrow = new ArgumentNullException("foo");
                    var properties = new ThrowingManagedProperties(exceptionToThrow);
                    Instance.CopyProperties(properties);
                    return 1;
                }
            }
        }


        readonly int E_FAIL = -2147467259;

        async Task InvokeDoitAsync()
        {
            await TestObject.DoitAsync();
        }

        [TestMethod]
        public void TestAsyncAction()
        {
            var task = InvokeDoitAsync();
            Assert.IsFalse(task.Wait(25));
            TestObject.CompleteAsync();
            Assert.IsTrue(task.Wait(5000));
            Assert.AreEqual(TaskStatus.RanToCompletion, task.Status);

            task = InvokeDoitAsync();
            Assert.IsFalse(task.Wait(25));
            TestObject.CompleteAsync(E_FAIL);
            var e = Assert.ThrowsException<AggregateException>(() => task.Wait(5000));
            Assert.AreEqual(E_FAIL, e.InnerException.HResult);
            Assert.AreEqual(TaskStatus.Faulted, task.Status);

            var src = new CancellationTokenSource();
            task = TestObject.DoitAsync().AsTask(src.Token);
            Assert.IsFalse(task.Wait(25));
            src.Cancel();
            e = Assert.ThrowsException<AggregateException>(() => task.Wait(5000));
            Assert.IsTrue(e.InnerException is TaskCanceledException);
            Assert.AreEqual(TaskStatus.Canceled, task.Status);
        }

        [TestMethod]
        public void TestAsyncActionWait()
        {
            var asyncAction = TestObject.DoitAsync();
            TestObject.CompleteAsync();
            asyncAction.AsTask().Wait();
            Assert.AreEqual(AsyncStatus.Completed, asyncAction.Status);

            asyncAction = TestObject.DoitAsync();
            TestObject.CompleteAsync(E_FAIL);
            var e = Assert.ThrowsException<AggregateException>(() => asyncAction.AsTask().Wait());
            Assert.AreEqual(E_FAIL, e.InnerException.HResult);
            Assert.AreEqual(AsyncStatus.Error, asyncAction.Status);

            asyncAction = TestObject.DoitAsync();
            asyncAction.Cancel();
            e = Assert.ThrowsException<AggregateException>(() => asyncAction.AsTask().Wait());
            Assert.IsTrue(e.InnerException is TaskCanceledException);
            Assert.AreEqual(AsyncStatus.Canceled, asyncAction.Status);
        }

        [TestMethod]
        public void TestAsyncActionRoundTrip()
        {
            var task = InvokeDoitAsync().AsAsyncAction().AsTask();
            Assert.IsFalse(task.Wait(25));
            TestObject.CompleteAsync();
            Assert.IsTrue(task.Wait(5000));
            Assert.AreEqual(TaskStatus.RanToCompletion, task.Status);

            task = InvokeDoitAsync().AsAsyncAction().AsTask();
            Assert.IsFalse(task.Wait(25));
            TestObject.CompleteAsync(E_FAIL);
            var e = Assert.ThrowsException<AggregateException>(() => task.Wait(5000));
            Assert.AreEqual(E_FAIL, e.InnerException.HResult);
            Assert.AreEqual(TaskStatus.Faulted, task.Status);

            var src = new CancellationTokenSource();
            task = InvokeDoitAsync().AsAsyncAction().AsTask(src.Token);
            Assert.IsFalse(task.Wait(25));
            src.Cancel();
            e = Assert.ThrowsException<AggregateException>(() => task.Wait(5000));
            Assert.IsTrue(e.InnerException is TaskCanceledException);
            Assert.AreEqual(TaskStatus.Canceled, task.Status);
        }

        async Task InvokeDoitAsyncWithProgress()
        {
            await TestObject.DoitAsyncWithProgress();
        }

        [TestMethod]
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
                Assert.IsTrue(evt.WaitOne(5000));
                Assert.AreEqual(10 * i, progress);
            }

            TestObject.CompleteAsync();
            Assert.IsTrue(task.Wait(5000));
            Assert.AreEqual(TaskStatus.RanToCompletion, task.Status);

            task = InvokeDoitAsyncWithProgress();
            TestObject.CompleteAsync(E_FAIL);
            var e = Assert.ThrowsException<AggregateException>(() => task.Wait(5000));
            Assert.AreEqual(E_FAIL, e.InnerException.HResult);
            Assert.AreEqual(TaskStatus.Faulted, task.Status);

            var src = new CancellationTokenSource();
            task = TestObject.DoitAsyncWithProgress().AsTask(src.Token);
            Assert.IsFalse(task.Wait(25));
            src.Cancel();
            e = Assert.ThrowsException<AggregateException>(() => task.Wait(5000));
            Assert.IsTrue(e.InnerException is TaskCanceledException);
            Assert.AreEqual(TaskStatus.Canceled, task.Status);
        }

        [TestMethod]
        public void TestAsyncActionWithProgressWait()
        {
            var asyncAction = TestObject.DoitAsyncWithProgress();
            TestObject.CompleteAsync();
            asyncAction.AsTask().Wait();
            Assert.AreEqual(AsyncStatus.Completed, asyncAction.Status);

            asyncAction = TestObject.DoitAsyncWithProgress();
            TestObject.CompleteAsync(E_FAIL);
            var e = Assert.ThrowsException<AggregateException>(() => asyncAction.AsTask().Wait());
            Assert.AreEqual(E_FAIL, e.InnerException.HResult);
            Assert.AreEqual(AsyncStatus.Error, asyncAction.Status);

            asyncAction = TestObject.DoitAsyncWithProgress();
            asyncAction.Cancel();
            e = Assert.ThrowsException<AggregateException>(() => asyncAction.AsTask().Wait());
            Assert.IsTrue(e.InnerException is TaskCanceledException);
            Assert.AreEqual(AsyncStatus.Canceled, asyncAction.Status);
        }

        async Task<int> InvokeAddAsync(int lhs, int rhs)
        {
            return await TestObject.AddAsync(lhs, rhs);
        }

        [TestMethod]
        public void TestAsyncOperation()
        {
            var task = InvokeAddAsync(42, 8);
            Assert.IsFalse(task.Wait(25));
            TestObject.CompleteAsync();
            Assert.IsTrue(task.Wait(10000));
            Assert.AreEqual(TaskStatus.RanToCompletion, task.Status);
            Assert.AreEqual(50, task.Result);

            task = InvokeAddAsync(0, 0);
            Assert.IsFalse(task.Wait(25));
            TestObject.CompleteAsync(E_FAIL);
            var e = Assert.ThrowsException<AggregateException>(() => task.Wait(5000));
            Assert.AreEqual(E_FAIL, e.InnerException.HResult);
            Assert.AreEqual(TaskStatus.Faulted, task.Status);

            var src = new CancellationTokenSource();
            task = TestObject.AddAsync(0, 0).AsTask(src.Token);
            Assert.IsFalse(task.Wait(25));
            src.Cancel();
            e = Assert.ThrowsException<AggregateException>(() => task.Wait(5000));
            Assert.IsTrue(e.InnerException is TaskCanceledException);
            Assert.AreEqual(TaskStatus.Canceled, task.Status);
        }

        [TestMethod]
        public void TestAsyncOperationWait()
        {
            var asyncOperation = TestObject.AddAsync(42, 8);
            TestObject.CompleteAsync();
            asyncOperation.AsTask().Wait();
            Assert.AreEqual(AsyncStatus.Completed, asyncOperation.Status);

            asyncOperation = TestObject.AddAsync(42, 8);
            TestObject.CompleteAsync(E_FAIL);
            var e = Assert.ThrowsException<AggregateException>(() => asyncOperation.AsTask().Wait());
            Assert.AreEqual(E_FAIL, e.InnerException.HResult);
            Assert.AreEqual(AsyncStatus.Error, asyncOperation.Status);

            asyncOperation = TestObject.AddAsync(42, 8);
            asyncOperation.Cancel();
            e = Assert.ThrowsException<AggregateException>(() => asyncOperation.AsTask().Wait());
            Assert.IsTrue(e.InnerException is TaskCanceledException);
            Assert.AreEqual(AsyncStatus.Canceled, asyncOperation.Status);
        }


        [TestMethod]
        public void TestAsyncOperationRoundTrip()
        {
            var task = InvokeAddAsync(42, 8).AsAsyncOperation().AsTask();
            Assert.IsFalse(task.Wait(25));
            TestObject.CompleteAsync();
            Assert.IsTrue(task.Wait(5000));
            Assert.AreEqual(TaskStatus.RanToCompletion, task.Status);
            Assert.AreEqual(50, task.Result);

            task = InvokeAddAsync(0, 0).AsAsyncOperation().AsTask();
            Assert.IsFalse(task.Wait(25));
            TestObject.CompleteAsync(E_FAIL);
            var e = Assert.ThrowsException<AggregateException>(() => task.Wait(5000));
            Assert.AreEqual(E_FAIL, e.InnerException.HResult);
            Assert.AreEqual(TaskStatus.Faulted, task.Status);

            var src = new CancellationTokenSource();
            task = InvokeAddAsync(0, 0).AsAsyncOperation().AsTask(src.Token);
            Assert.IsFalse(task.Wait(25));
            src.Cancel();
            e = Assert.ThrowsException<AggregateException>(() => task.Wait(5000));
            Assert.IsTrue(e.InnerException is TaskCanceledException);
            Assert.AreEqual(TaskStatus.Canceled, task.Status);
        }

        async Task<int> InvokeAddAsyncWithProgress(int lhs, int rhs)
        {
            return await TestObject.AddAsyncWithProgress(lhs, rhs);
        }

        [TestMethod]
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
                Assert.IsTrue(evt.WaitOne(5000));
                Assert.AreEqual(10 * i, progress);
            }

            TestObject.CompleteAsync();
            Assert.IsTrue(task.Wait(5000));
            Assert.AreEqual(TaskStatus.RanToCompletion, task.Status);
            Assert.AreEqual(50, task.Result);

            task = InvokeAddAsyncWithProgress(0, 0);
            TestObject.CompleteAsync(E_FAIL);
            var e = Assert.ThrowsException<AggregateException>(() => task.Wait(5000));
            Assert.AreEqual(E_FAIL, e.InnerException.HResult);
            Assert.AreEqual(TaskStatus.Faulted, task.Status);

            var src = new CancellationTokenSource();
            task = TestObject.AddAsyncWithProgress(0, 0).AsTask(src.Token);
            Assert.IsFalse(task.Wait(25));
            src.Cancel();
            e = Assert.ThrowsException<AggregateException>(() => task.Wait(5000));
            Assert.IsTrue(e.InnerException is TaskCanceledException);
            Assert.AreEqual(TaskStatus.Canceled, task.Status);
        }

        [TestMethod]
        public void TestAsyncOperationWithProgressWait()
        {
            var asyncOperation = TestObject.AddAsyncWithProgress(42, 8);
            TestObject.CompleteAsync();
            asyncOperation.AsTask().Wait();
            Assert.AreEqual(AsyncStatus.Completed, asyncOperation.Status);

            asyncOperation = TestObject.AddAsyncWithProgress(42, 8);
            TestObject.CompleteAsync(E_FAIL);
            var e = Assert.ThrowsException<AggregateException>(() => asyncOperation.AsTask().Wait());
            Assert.AreEqual(E_FAIL, e.InnerException.HResult);
            Assert.AreEqual(AsyncStatus.Error, asyncOperation.Status);

            asyncOperation = TestObject.AddAsyncWithProgress(42, 8);
            asyncOperation.Cancel();
            e = Assert.ThrowsException<AggregateException>(() => asyncOperation.AsTask().Wait());
            Assert.IsTrue(e.InnerException is TaskCanceledException);
            Assert.AreEqual(AsyncStatus.Canceled, asyncOperation.Status);
        }

        [TestMethod]
        public void TestPointTypeMapping()
        {
            var pt = new Point { X = 3.14F, Y = 42 };
            TestObject.PointProperty = pt;
            Assert.AreEqual(pt.X, TestObject.PointProperty.X);
            Assert.AreEqual(pt.Y, TestObject.PointProperty.Y);
            Assert.IsTrue(TestObject.PointProperty == pt);
            Assert.AreEqual(pt, TestObject.GetPointReference().Value);

            var vector2 = TestObject.PointProperty.ToVector2();
            Assert.AreEqual(pt.X, vector2.X);
            Assert.AreEqual(pt.Y, vector2.Y);

            TestObject.PointProperty = vector2.ToPoint();
            Assert.AreEqual(pt.X, TestObject.PointProperty.X);
            Assert.AreEqual(pt.Y, TestObject.PointProperty.Y);
        }

        [TestMethod]
        public void TestRectTypeMapping()
        {
            var rect = new Rect { X = 3.14F, Y = 42, Height = 3.14F, Width = 42 };
            TestObject.RectProperty = rect;
            Assert.AreEqual(rect.X, TestObject.RectProperty.X);
            Assert.AreEqual(rect.Y, TestObject.RectProperty.Y);
            Assert.AreEqual(rect.Height, TestObject.RectProperty.Height);
            Assert.AreEqual(rect.Width, TestObject.RectProperty.Width);
            Assert.IsTrue(TestObject.RectProperty == rect);
        }

        [TestMethod]
        public void TestSizeTypeMapping()
        {
            var size = new Size { Height = 3.14F, Width = 42 };
            TestObject.SizeProperty = size;
            Assert.AreEqual(size.Height, TestObject.SizeProperty.Height);
            Assert.AreEqual(size.Width, TestObject.SizeProperty.Width);
            Assert.IsTrue(TestObject.SizeProperty == size);

            var vector2 = TestObject.SizeProperty.ToVector2();
            Assert.AreEqual(size.Width, vector2.X);
            Assert.AreEqual(size.Height, vector2.Y);

            TestObject.SizeProperty = vector2.ToSize();
            Assert.AreEqual(size.Width, TestObject.SizeProperty.Width);
            Assert.AreEqual(size.Height, TestObject.SizeProperty.Height);
        }

        [TestMethod]
        public void TestColorTypeMapping()
        {
            var color = new Color { A = 0x20, R = 0x40, G = 0x60, B = 0x80 };
            TestObject.ColorProperty = color;
            Assert.AreEqual(color.A, TestObject.ColorProperty.A);
            Assert.AreEqual(color.R, TestObject.ColorProperty.R);
            Assert.AreEqual(color.G, TestObject.ColorProperty.G);
            Assert.AreEqual(color.B, TestObject.ColorProperty.B);
            Assert.IsTrue(TestObject.ColorProperty == color);
        }

        [TestMethod]
        public void TestCornerRadiusTypeMapping()
        {
            var cornerRadius = new CornerRadius { TopLeft = 1, TopRight = 2, BottomRight = 3, BottomLeft = 4 };
            TestObject.CornerRadiusProperty = cornerRadius;
            Assert.AreEqual(cornerRadius.TopLeft, TestObject.CornerRadiusProperty.TopLeft);
            Assert.AreEqual(cornerRadius.TopRight, TestObject.CornerRadiusProperty.TopRight);
            Assert.AreEqual(cornerRadius.BottomRight, TestObject.CornerRadiusProperty.BottomRight);
            Assert.AreEqual(cornerRadius.BottomLeft, TestObject.CornerRadiusProperty.BottomLeft);
            Assert.IsTrue(TestObject.CornerRadiusProperty == cornerRadius);
        }

        [TestMethod]
        public void TestDurationTypeMapping()
        {
            var duration = new Duration(TimeSpan.FromTicks(42));
            TestObject.DurationProperty = duration;
            Assert.AreEqual(duration.TimeSpan, TestObject.DurationProperty.TimeSpan);
            Assert.IsTrue(TestObject.DurationProperty == duration);
        }

        [TestMethod]
        public void TestGridLengthTypeMapping()
        {
            var gridLength = new GridLength(42, GridUnitType.Pixel);
            TestObject.GridLengthProperty = gridLength;
            Assert.AreEqual(gridLength.GridUnitType, TestObject.GridLengthProperty.GridUnitType);
            Assert.AreEqual(gridLength.Value, TestObject.GridLengthProperty.Value);
            Assert.IsTrue(TestObject.GridLengthProperty == gridLength);
        }

        [TestMethod]
        public void TestThicknessTypeMapping()
        {
            var thickness = new Thickness { Left = 1, Top = 2, Right = 3, Bottom = 4 };
            TestObject.ThicknessProperty = thickness;
            Assert.AreEqual(thickness.Left, TestObject.ThicknessProperty.Left);
            Assert.AreEqual(thickness.Top, TestObject.ThicknessProperty.Top);
            Assert.AreEqual(thickness.Right, TestObject.ThicknessProperty.Right);
            Assert.AreEqual(thickness.Bottom, TestObject.ThicknessProperty.Bottom);
            Assert.IsTrue(TestObject.ThicknessProperty == thickness);
        }

        [TestMethod]
        public void TestGeneratorPositionTypeMapping()
        {
            var generatorPosition = new GeneratorPosition { Index = 1, Offset = 2 };
            TestObject.GeneratorPositionProperty = generatorPosition;
            Assert.AreEqual(generatorPosition.Index, TestObject.GeneratorPositionProperty.Index);
            Assert.AreEqual(generatorPosition.Offset, TestObject.GeneratorPositionProperty.Offset);
            Assert.IsTrue(TestObject.GeneratorPositionProperty == generatorPosition);
        }

        [TestMethod]
        public void TestMatrixTypeMapping()
        {
            var matrix = new Matrix { M11 = 11, M12 = 12, M21 = 21, M22 = 22, OffsetX = 3, OffsetY = 4 };
            TestObject.MatrixProperty = matrix;
            Assert.AreEqual(matrix.M11, TestObject.MatrixProperty.M11);
            Assert.AreEqual(matrix.M12, TestObject.MatrixProperty.M12);
            Assert.AreEqual(matrix.M21, TestObject.MatrixProperty.M21);
            Assert.AreEqual(matrix.M22, TestObject.MatrixProperty.M22);
            Assert.AreEqual(matrix.OffsetX, TestObject.MatrixProperty.OffsetX);
            Assert.AreEqual(matrix.OffsetY, TestObject.MatrixProperty.OffsetY);
            Assert.IsTrue(TestObject.MatrixProperty == matrix);
        }

        [TestMethod]
        public void TestKeyTimeTypeMapping()
        {
            var keyTime = KeyTime.FromTimeSpan(TimeSpan.FromTicks(42));
            TestObject.KeyTimeProperty = keyTime;
            Assert.AreEqual(keyTime.TimeSpan, TestObject.KeyTimeProperty.TimeSpan);
            Assert.IsTrue(TestObject.KeyTimeProperty == keyTime);
        }

        [TestMethod]
        public void TestRepeatBehaviorTypeMapping()
        {
            var repeatBehavior = new RepeatBehavior
            {
                Count = 1,
                Duration = TimeSpan.FromTicks(42),
                Type = RepeatBehaviorType.Forever
            };
            TestObject.RepeatBehaviorProperty = repeatBehavior;
            Assert.AreEqual(repeatBehavior.Count, TestObject.RepeatBehaviorProperty.Count);
            Assert.AreEqual(repeatBehavior.Duration, TestObject.RepeatBehaviorProperty.Duration);
            Assert.AreEqual(repeatBehavior.Type, TestObject.RepeatBehaviorProperty.Type);
            Assert.IsTrue(TestObject.RepeatBehaviorProperty == repeatBehavior);
        }

        [TestMethod]
        public void TestMatrix3DTypeMapping()
        {
            var matrix3D = new Matrix3D
            {
                M11 = 11,
                M12 = 12,
                M13 = 13,
                M14 = 14,
                M21 = 21,
                M22 = 22,
                M23 = 23,
                M24 = 24,
                M31 = 31,
                M32 = 32,
                M33 = 33,
                M34 = 34,
                OffsetX = 41,
                OffsetY = 42,
                OffsetZ = 43,
                M44 = 44
            };

            TestObject.Matrix3DProperty = matrix3D;
            Assert.AreEqual(matrix3D.M11, TestObject.Matrix3DProperty.M11);
            Assert.AreEqual(matrix3D.M12, TestObject.Matrix3DProperty.M12);
            Assert.AreEqual(matrix3D.M13, TestObject.Matrix3DProperty.M13);
            Assert.AreEqual(matrix3D.M14, TestObject.Matrix3DProperty.M14);
            Assert.AreEqual(matrix3D.M21, TestObject.Matrix3DProperty.M21);
            Assert.AreEqual(matrix3D.M22, TestObject.Matrix3DProperty.M22);
            Assert.AreEqual(matrix3D.M23, TestObject.Matrix3DProperty.M23);
            Assert.AreEqual(matrix3D.M24, TestObject.Matrix3DProperty.M24);
            Assert.AreEqual(matrix3D.M31, TestObject.Matrix3DProperty.M31);
            Assert.AreEqual(matrix3D.M32, TestObject.Matrix3DProperty.M32);
            Assert.AreEqual(matrix3D.M33, TestObject.Matrix3DProperty.M33);
            Assert.AreEqual(matrix3D.M34, TestObject.Matrix3DProperty.M34);
            Assert.AreEqual(matrix3D.OffsetX, TestObject.Matrix3DProperty.OffsetX);
            Assert.AreEqual(matrix3D.OffsetY, TestObject.Matrix3DProperty.OffsetY);
            Assert.AreEqual(matrix3D.OffsetZ, TestObject.Matrix3DProperty.OffsetZ);
            Assert.AreEqual(matrix3D.M44, TestObject.Matrix3DProperty.M44);
            Assert.IsTrue(TestObject.Matrix3DProperty == matrix3D);
        }

        [TestMethod]
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
            Assert.AreEqual(matrix3x2.M11, TestObject.Matrix3x2Property.M11);
            Assert.AreEqual(matrix3x2.M12, TestObject.Matrix3x2Property.M12);
            Assert.AreEqual(matrix3x2.M21, TestObject.Matrix3x2Property.M21);
            Assert.AreEqual(matrix3x2.M22, TestObject.Matrix3x2Property.M22);
            Assert.AreEqual(matrix3x2.M31, TestObject.Matrix3x2Property.M31);
            Assert.AreEqual(matrix3x2.M32, TestObject.Matrix3x2Property.M32);
            Assert.IsTrue(TestObject.Matrix3x2Property == matrix3x2);
        }

        [TestMethod]
        public void TestMatrix4x4TypeMapping()
        {
            var matrix4x4 = new Matrix4x4
            {
                M11 = 11,
                M12 = 12,
                M13 = 13,
                M14 = 14,
                M21 = 21,
                M22 = 22,
                M23 = 23,
                M24 = 24,
                M31 = 31,
                M32 = 32,
                M33 = 33,
                M34 = 34,
                M41 = 41,
                M42 = 42,
                M43 = 43,
                M44 = 44
            };
            TestObject.Matrix4x4Property = matrix4x4;
            Assert.AreEqual(matrix4x4.M11, TestObject.Matrix4x4Property.M11);
            Assert.AreEqual(matrix4x4.M12, TestObject.Matrix4x4Property.M12);
            Assert.AreEqual(matrix4x4.M13, TestObject.Matrix4x4Property.M13);
            Assert.AreEqual(matrix4x4.M14, TestObject.Matrix4x4Property.M14);
            Assert.AreEqual(matrix4x4.M21, TestObject.Matrix4x4Property.M21);
            Assert.AreEqual(matrix4x4.M22, TestObject.Matrix4x4Property.M22);
            Assert.AreEqual(matrix4x4.M23, TestObject.Matrix4x4Property.M23);
            Assert.AreEqual(matrix4x4.M24, TestObject.Matrix4x4Property.M24);
            Assert.AreEqual(matrix4x4.M31, TestObject.Matrix4x4Property.M31);
            Assert.AreEqual(matrix4x4.M32, TestObject.Matrix4x4Property.M32);
            Assert.AreEqual(matrix4x4.M33, TestObject.Matrix4x4Property.M33);
            Assert.AreEqual(matrix4x4.M34, TestObject.Matrix4x4Property.M34);
            Assert.AreEqual(matrix4x4.M41, TestObject.Matrix4x4Property.M41);
            Assert.AreEqual(matrix4x4.M42, TestObject.Matrix4x4Property.M42);
            Assert.AreEqual(matrix4x4.M43, TestObject.Matrix4x4Property.M43);
            Assert.AreEqual(matrix4x4.M44, TestObject.Matrix4x4Property.M44);
            Assert.IsTrue(TestObject.Matrix4x4Property == matrix4x4);
        }

        [TestMethod]
        public void TestPlaneTypeMapping()
        {
            var plane = new Plane { D = 3.14F, Normal = new Vector3(1, 2, 3) };
            TestObject.PlaneProperty = plane;
            Assert.AreEqual(plane.D, TestObject.PlaneProperty.D);
            Assert.AreEqual(plane.Normal, TestObject.PlaneProperty.Normal);
            Assert.IsTrue(TestObject.PlaneProperty == plane);
        }

        [TestMethod]
        public void TestQuaternionTypeMapping()
        {
            var quaternion = new Quaternion { W = 3.14F, X = 1, Y = 42, Z = 1729 };
            TestObject.QuaternionProperty = quaternion;
            Assert.AreEqual(quaternion.W, TestObject.QuaternionProperty.W);
            Assert.AreEqual(quaternion.X, TestObject.QuaternionProperty.X);
            Assert.AreEqual(quaternion.Y, TestObject.QuaternionProperty.Y);
            Assert.AreEqual(quaternion.Z, TestObject.QuaternionProperty.Z);
            Assert.IsTrue(TestObject.QuaternionProperty == quaternion);
        }

        [TestMethod]
        public void TestVector2TypeMapping()
        {
            var vector2 = new Vector2 { X = 1, Y = 42 };
            TestObject.Vector2Property = vector2;
            Assert.AreEqual(vector2.X, TestObject.Vector2Property.X);
            Assert.AreEqual(vector2.Y, TestObject.Vector2Property.Y);
            Assert.IsTrue(TestObject.Vector2Property == vector2);
        }

        [TestMethod]
        public void TestVector3TypeMapping()
        {
            var vector3 = new Vector3 { X = 1, Y = 42, Z = 1729 };
            TestObject.Vector3Property = vector3;
            Assert.AreEqual(vector3.X, TestObject.Vector3Property.X);
            Assert.AreEqual(vector3.Y, TestObject.Vector3Property.Y);
            Assert.AreEqual(vector3.Z, TestObject.Vector3Property.Z);
            Assert.IsTrue(TestObject.Vector3Property == vector3);

            TestObject.Vector3NullableProperty = Vector3.Zero;
            Assert.AreEqual(0, TestObject.Vector3Property.X);
            Assert.AreEqual(0, TestObject.Vector3Property.Y);
            Assert.AreEqual(0, TestObject.Vector3Property.Z);
            Assert.AreEqual(Vector3.Zero, TestObject.Vector3NullableProperty);
        }

        [TestMethod]
        public void TestVector4TypeMapping()
        {
            var vector4 = new Vector4 { W = 3.14F, X = 1, Y = 42, Z = 1729 };
            TestObject.Vector4Property = vector4;
            Assert.AreEqual(vector4.W, TestObject.Vector4Property.W);
            Assert.AreEqual(vector4.X, TestObject.Vector4Property.X);
            Assert.AreEqual(vector4.Y, TestObject.Vector4Property.Y);
            Assert.AreEqual(vector4.Z, TestObject.Vector4Property.Z);
            Assert.IsTrue(TestObject.Vector4Property == vector4);
        }

        [TestMethod]
        public void TestTimeSpanMapping()
        {
            var ts = TimeSpan.FromSeconds(42);
            TestObject.TimeSpanProperty = ts;
            Assert.AreEqual(ts, TestObject.TimeSpanProperty);
            Assert.AreEqual(ts, TestObject.GetTimeSpanReference().Value);
            Assert.AreEqual(ts, Class.FromSeconds(42));
        }

        [TestMethod]
        public void TestDateTimeMapping()
        {
            var now = DateTimeOffset.Now;
            var ticks = (Class.Now() - now).Ticks;
            Assert.IsTrue(ticks >= -TimeSpan.TicksPerSecond && ticks <= TimeSpan.TicksPerSecond,
                $"Expected value between {-TimeSpan.TicksPerSecond} and {TimeSpan.TicksPerSecond}, but got {ticks}");
            TestObject.DateTimeProperty = now;
            Assert.AreEqual(now, TestObject.DateTimeProperty);
            Assert.AreEqual(now, TestObject.GetDateTimeProperty().Value);
        }

        [TestMethod]
        public void TestDateTimeMappingNegative()
        {
            var time = new DateTimeOffset(1501, 1, 1, 0, 0, 0, TimeSpan.Zero);
            TestObject.DateTimeProperty = time;
            Assert.AreEqual(time, TestObject.DateTimeProperty);
            Assert.AreEqual(time, TestObject.GetDateTimeProperty().Value);
        }

        [TestMethod]
        public void TestExceptionMapping()
        {
            var ex = new ArgumentOutOfRangeException();

            TestObject.HResultProperty = ex;

            Assert.IsInstanceOfType<ArgumentOutOfRangeException>(TestObject.HResultProperty);

            TestObject.HResultProperty = null;

            Assert.IsNull(TestObject.HResultProperty);
        }

        //[LibraryImport("api-ms-win-core-winrt-string-l1-1-0.dll")]
        //[UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        //private static unsafe partial char* WindowsGetStringRawBuffer(void* hstring, uint* length);

        //[LibraryImport("api-ms-win-core-winrt-string-l1-1-0.dll")]
        //[UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        //private static unsafe partial int WindowsDeleteString(void* hstring);

        //static unsafe string GetRuntimeClassName(void* ptr)
        //{
        //    Marshal.ThrowExceptionForHR(Marshal.QueryInterface((nint)ptr, WellKnownInterfaceIIDs.IID_IInspectable, out nint inspectablePtr));

        //    void* __retval = default;
        //    try
        //    {
        //        Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, void**, int>)(*(void***)inspectablePtr)[4])((void*)inspectablePtr, &__retval));

        //        uint length;
        //        char* buffer = WindowsGetStringRawBuffer(__retval, &length);
        //        return new string(buffer, 0, (int)length);
        //    }
        //    finally
        //    {
        //        WindowsDeleteString(__retval);
        //        WindowsRuntimeMarshal.Free((void*)inspectablePtr);
        //    }
        //}


        //[TestMethod]
        //public unsafe void TestGeneratedRuntimeClassName()
        //{
        //    void* ptr = WindowsRuntimeMarshal.ConvertToUnmanaged(new ManagedProperties(2));

        //    try
        //    {
        //        Assert.AreEqual(typeof(IProperties1).FullName, GetRuntimeClassName(ptr));
        //    }
        //    finally
        //    {
        //        WindowsRuntimeMarshal.Free(ptr);
        //    }
        //}

        [TestMethod]
        public void TestGetPropertyType()
        {
            Array arr = new[] { E.A, E.B, E.C };
            Array arr2 = new[] { new Estruct(), new Estruct() };
            Array arr3 = new int[] { 1, 2, 3 };
            IList<E> arr4 = new List<E>() { E.A, E.B, E.C };
            Array arr5 = new PropertyType[] { PropertyType.UInt8, PropertyType.Int16, PropertyType.UInt16 };

            Assert.AreEqual(-1, Class.GetPropertyType(arr));
            Assert.AreEqual(-1, Class.GetPropertyType(arr2));
            Assert.AreEqual((int)PropertyType.Int32Array, Class.GetPropertyType(arr3));
            Assert.AreEqual(-1, Class.GetPropertyType(arr4));
            Assert.AreEqual((int)PropertyType.OtherTypeArray, Class.GetPropertyType(arr5));
            Assert.AreEqual(-1, Class.GetPropertyType(arr.GetValue(0)));
            Assert.AreEqual(-1, Class.GetPropertyType(arr2.GetValue(0)));
            Assert.AreEqual((int)PropertyType.Int32, Class.GetPropertyType(arr3.GetValue(0)));
            Assert.AreEqual(-1, Class.GetPropertyType(arr4[0]));
            Assert.AreEqual((int)PropertyType.OtherType, Class.GetPropertyType(arr5.GetValue(0)));
        }

        [TestMethod]
        public void TestGetRuntimeClassName()
        {
            Array arr = new[] { E.A, E.B, E.C };
            Array arr2 = new[] { new Estruct(), new Estruct() };
            Array arr3 = new int[] { 1, 2, 3 };
            IList<E> arr4 = new List<E>() { E.A, E.B, E.C };
            Array arr5 = new PropertyType[] { PropertyType.UInt8, PropertyType.Int16, PropertyType.UInt16 };

            Assert.AreEqual("Microsoft.UI.Xaml.Interop.IBindableVector", Class.GetName(arr));
            Assert.AreEqual("Microsoft.UI.Xaml.Interop.IBindableVector", Class.GetName(arr2));
            Assert.AreEqual("Windows.Foundation.IReferenceArray`1<Int32>", Class.GetName(arr3));
            Assert.AreEqual("Microsoft.UI.Xaml.Interop.IBindableVector", Class.GetName(arr4));
            Assert.AreEqual("Windows.Foundation.IReferenceArray`1<Windows.Foundation.PropertyType>", Class.GetName(arr5));
            Assert.AreEqual("Object", Class.GetName(arr.GetValue(0)));
            Assert.AreEqual("Object", Class.GetName(arr2.GetValue(0)));
            Assert.AreEqual("Windows.Foundation.IReference`1<Int32>", Class.GetName(arr3.GetValue(0)));
            Assert.AreEqual("Object", Class.GetName(arr4[0]));
            Assert.AreEqual("Windows.Foundation.IReference`1<Windows.Foundation.PropertyType>", Class.GetName(arr5.GetValue(0)));

            Assert.AreEqual("Windows.Foundation.IReference`1<Windows.UI.Xaml.Interop.TypeName>", Class.GetName(typeof(IProperties1)));
            Assert.AreEqual("Windows.Foundation.IReference`1<Windows.UI.Xaml.Interop.TypeName>", Class.GetName(typeof(Type)));
        }

        //[TestMethod]
        //public unsafe void TestGeneratedRuntimeClassName_Primitive()
        //{
        //    void* ptr = WindowsRuntimeMarshal.ConvertToUnmanaged(2);

        //    try
        //    {
        //        Assert.AreEqual("Windows.Foundation.IReference`1<Int32>", GetRuntimeClassName(ptr));
        //    }
        //    finally
        //    {
        //        WindowsRuntimeMarshal.Free(ptr);
        //    }
        //}

        //[TestMethod]
        //public unsafe void TestGeneratedRuntimeClassName_Array()
        //{
        //    void* ptr = WindowsRuntimeMarshal.ConvertToUnmanaged(new int[0]);

        //    try
        //    {
        //        Assert.AreEqual("Windows.Foundation.IReferenceArray`1<Int32>", GetRuntimeClassName(ptr));
        //    }
        //    finally
        //    {
        //        WindowsRuntimeMarshal.Free(ptr);
        //    }
        //}

        [TestMethod]
        public void TestValueBoxing()
        {
            int i = 42;
            Assert.AreEqual(i, Class.UnboxInt32(i));

            bool b = true;
            Assert.AreEqual(b, Class.UnboxBoolean(b));

            string s = "Hello World!";
            Assert.AreEqual(s, Class.UnboxString(s));

            ProvideInt intHandler = () => 42;
            Assert.AreEqual(intHandler, Class.UnboxDelegate(intHandler));

            EnumValue enumValue = EnumValue.Two;
            Assert.AreEqual(enumValue, Class.UnboxEnum(enumValue));

            var type = typeof(EnumValue);
            Assert.AreEqual(type, Class.UnboxType(type));

            Assert.AreEqual(typeof(Class), Class.BoxedType);
        }

        [TestMethod]
        public void TestArrayBoxing()
        {
            int[] i = new[] { 42, 1, 4, 50, 0, -23 };
            Assert.IsTrue(i.SequenceEqual(Class.UnboxInt32Array(i)));

            bool[] b = new[] { true, false, true, true, false };
            Assert.IsTrue(b.SequenceEqual(Class.UnboxBooleanArray(b)));

            string[] s = new[] { "Hello World!", "WinRT", "C#", "Boxing" };
            Assert.IsTrue(s.SequenceEqual(Class.UnboxStringArray(s)));
        }

        [TestMethod]
        public void TestArrayUnboxing()
        {
            int[] i = new[] { 42, 1, 4, 50, 0, -23 };

            var obj = PropertyValue.CreateInt32Array(i);
            Assert.IsInstanceOfType<int[]>(obj);
            Assert.IsTrue(i.SequenceEqual((IEnumerable<int>)obj));
        }

        [TestMethod]
        public void TestUnboxingUsingPropertyValue()
        {
            int i = 24;
            Assert.AreEqual(i, Class.UnboxInt32UsingPropertyValue(i));

            uint j = 42;
            Assert.AreEqual((int)j, Class.UnboxInt32UsingPropertyValue(j));

            System.Nullable<int> k = new System.Nullable<int>(34);
            Assert.AreEqual(k, Class.UnboxInt32UsingPropertyValue(k));

            string s = "Hello!";
            Assert.AreEqual(s, Class.UnboxStringUsingPropertyValue(s));

            Guid guid = new("36AA48DD-ACBB-4570-B12A-86BF71D09A12");
            Assert.AreEqual("36AA48DD-ACBB-4570-B12A-86BF71D09A12", Class.UnboxStringUsingPropertyValue(guid), true);

            Assert.ThrowsException<InvalidCastException>(() => Class.UnboxInt32UsingPropertyValue(s));

            Rect rect = new Rect(1, 2, 3, 4);
            Assert.AreEqual(rect, Class.UnboxRectUsingPropertyValue(rect));

            int[] iArr = new[] { 42, 0, -23 };
            Assert.IsTrue(iArr.SequenceEqual((IEnumerable<int>)Class.UnboxInt32ArrayUsingPropertyValue(iArr)));

            bool[] bArr = new[] { true, false, false };
            Assert.IsTrue(Class.UnboxBooleanArrayUsingPropertyValue(bArr).SequenceEqual((IEnumerable<bool>)bArr));

            Point[] pArr = new[] { new Point(1, 3), new Point(2, 4) };
            Assert.IsTrue(Class.UnboxPointArrayUsingPropertyValue(pArr).SequenceEqual((IEnumerable<Point>)pArr));
        }

        [TestMethod]
        public void TestListOfTypes()
        {
            var types = Class.ListOfTypes;
            Assert.AreEqual(2, types.Count);
            Assert.AreEqual(typeof(Class), types[0]);
            Assert.AreEqual(typeof(int?), types[1]);
        }

        [TestMethod]
        public void PrimitiveTypeInfo()
        {
            Assert.AreEqual(typeof(int), Class.Int32Type);
            Assert.IsTrue(Class.VerifyTypeIsInt32Type(typeof(int)));
        }

        [TestMethod]
        public void WinRTTypeInfo()
        {
            Assert.AreEqual(typeof(Class), Class.ThisClassType);
            Assert.IsTrue(Class.VerifyTypeIsThisClassType(typeof(Class)));
        }

        [TestMethod]
        public void ProjectedTypeInfo()
        {
            Assert.AreEqual(typeof(int?), Class.ReferenceInt32Type);
            Assert.IsTrue(Class.VerifyTypeIsReferenceInt32Type(typeof(int?)));
        }

        [TestMethod]
        public void TypeInfoGenerics()
        {
            var typeName = Class.GetTypeNameForType(typeof(IList<int>));
            Assert.AreEqual("Windows.Foundation.Collections.IVector`1<Int32>", typeName);
        }

        [TestMethod]
        public void TypeInfoType()
        {
            var typeName = Class.GetTypeNameForType(typeof(Type));
            Assert.AreEqual("Windows.UI.Xaml.Interop.TypeName", typeName);
        }

        [TestMethod]
        public void TestStringUnboxing()
        {
            var str1 = Class.EmptyString;
            var str2 = Class.EmptyString;
            Assert.IsInstanceOfType<string>(str1);
            Assert.IsInstanceOfType<string>(str2);
            Assert.AreEqual(string.Empty, (string)str1);
            Assert.AreEqual(string.Empty, (string)str2);
        }

        [TestMethod]
        public void TestDelegateUnboxing()
        {
            var del = Class.BoxedDelegate;
            Assert.IsInstanceOfType<ProvideUri>(del);
            var provideUriDel = (ProvideUri)del;
            Assert.AreEqual(new Uri("http://microsoft.com"), provideUriDel());
        }

        [TestMethod]
        public void TestEnumUnboxing()
        {
            var enumVal = Class.BoxedEnum;
            Assert.IsInstanceOfType<EnumValue>(enumVal);
            Assert.AreEqual(EnumValue.Two, enumVal);
        }

        internal class ManagedType { }

        [TestMethod]
        public unsafe void CCWOfListOfManagedType()
        {
            void* ptr = WindowsRuntimeMarshal.ConvertToUnmanaged(new List<ManagedType>());
            Guid IID_IEnumerableObject = new("092b849b-60b1-52be-a44a-6fe8e933cbe4");
            Marshal.ThrowExceptionForHR(Marshal.QueryInterface((nint)ptr, IID_IEnumerableObject, out nint _));
        }

        [TestMethod]
        public void TestForIterableObject()
        {
            // Make sure for collections of value types that they don't project IEnumerable<object>
            // as it isn't a covariant interface.
            Assert.IsFalse(TestObject.CheckForBindableObjectInterface(new List<int>()));
            Assert.IsFalse(TestObject.CheckForBindableObjectInterface(new List<EnumValue>()));
            Assert.IsFalse(TestObject.CheckForBindableObjectInterface(new List<System.DateTimeOffset>()));
            Assert.IsFalse(TestObject.CheckForBindableObjectInterface(new Dictionary<string, System.DateTimeOffset>()));

            // Make sure for collections of object types that they do project IEnumerable<object>
            // as it is an covariant interface.
            Assert.IsTrue(TestObject.CheckForBindableObjectInterface(new List<object>()));
            Assert.IsTrue(TestObject.CheckForBindableObjectInterface(new List<Class>()));
            Assert.IsTrue(TestObject.CheckForBindableObjectInterface(new List<IProperties1>()));
            Assert.IsTrue(TestObject.CheckForBindableObjectInterface(new List<ManagedType2>()));
        }

        internal class ManagedType2 : List<ManagedType2> { }

        internal class ManagedType3 : List<ManagedType3>, IDisposable
        {
            public void Dispose()
            {
            }
        }

        [TestMethod]
        public unsafe void CCWOfListOfManagedType2()
        {
            void* ptr = WindowsRuntimeMarshal.ConvertToUnmanaged(new ManagedType2());
            Guid IID_IEnumerableObject = new("092b849b-60b1-52be-a44a-6fe8e933cbe4");
            Marshal.ThrowExceptionForHR(Marshal.QueryInterface((nint)ptr, IID_IEnumerableObject, out nint _));
        }

        [TestMethod]
        public unsafe void CCWOfListOfManagedType3()
        {
            void* ptr = WindowsRuntimeMarshal.ConvertToUnmanaged(new ManagedType3());

            Guid IID_IEnumerableObject = new("092b849b-60b1-52be-a44a-6fe8e933cbe4");
            Marshal.ThrowExceptionForHR(Marshal.QueryInterface((nint)ptr, IID_IEnumerableObject, out nint _));

            Guid IID_IEnumerable_IDisposable = new("44da7ecf-b8cf-5def-8bf1-664578a8fb16");
            Marshal.ThrowExceptionForHR(Marshal.QueryInterface((nint)ptr, IID_IEnumerable_IDisposable, out nint _));
        }

        [TestMethod]
        public void WeakReferenceOfManagedObject()
        {
            var properties = new ManagedProperties(42);
            WeakReference<IProperties1> weakReference = new WeakReference<IProperties1>(properties);
            Assert.IsTrue(weakReference.TryGetTarget(out var propertiesStrong));
            Assert.AreSame(properties, propertiesStrong);
        }

        [TestMethod]
        public void WeakReferenceOfNativeObject()
        {
            var weakReference = new WeakReference<Class>(TestObject);
            Assert.IsTrue(weakReference.TryGetTarget(out var classStrong));
            Assert.AreSame(TestObject, classStrong);
        }

        [TestMethod]
        public void WeakReferenceOfNativeObjectRehydratedAfterWrapperIsCollected()
        {
            unsafe static (WeakReference<Class> winrt, WeakReference net, WindowsRuntimeObjectReference objRef) GetWeakReferences()
            {
                var obj = new Class();
                Assert.IsTrue(WindowsRuntimeComWrappersMarshal.TryUnwrapObjectReference(obj, out WindowsRuntimeObjectReference? objRef));
                return (new WeakReference<Class>(obj), new WeakReference(obj), objRef);
            }

            var (winrt, net, objRef) = GetWeakReferences();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            Assert.IsFalse(net.IsAlive);
            Assert.IsTrue(winrt.TryGetTarget(out _));
            GC.KeepAlive(objRef);
        }

        [TestMethod]
        public unsafe void TestUnwrapInspectable()
        {
            Assert.IsTrue(WindowsRuntimeMarshal.TryGetNativeObject(TestObject, out _));

            using WindowsRuntimeObjectReferenceValue objRefValue = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(TestObject);
            Assert.IsFalse(objRefValue.IsNull);
        }

        /* TODO
        [TestMethod]
        public void TestManagedAgileObject()
        {
            using var testObjectAgileRef = TestObject.AsAgile();
            var agileTestObject = testObjectAgileRef.Get();
            Assert.AreEqual(TestObject, agileTestObject);

            IProperties1 properties = new ManagedProperties(42);
            using var propertiesAgileRef = properties.AsAgile();
            var agileProperties = propertiesAgileRef.Get();
            Assert.AreEqual(properties.ReadWriteProperty, agileProperties.ReadWriteProperty);

            var agileObject = TestObject.As<IAgileObject>();
            Assert.IsNotNull(agileObject);

            IProperties1 properties2 = null;
            using var properties2AgileRef = properties2.AsAgile();
            var agileProperties2 = properties2AgileRef.Get();
            Assert.IsNull(agileProperties2);
        }

        class NonAgileClassCaller
        {
            public void AcquireObject()
            {
                Assert.AreEqual(ApartmentState.STA, Thread.CurrentThread.GetApartmentState());
                nonAgileObject = new Windows.UI.Popups.PopupMenu();
                nonAgileObject.Commands.Add(new Windows.UI.Popups.UICommand("test"));
                nonAgileObject.Commands.Add(new Windows.UI.Popups.UICommand("test2"));
                Assert.ThrowsException<System.Exception>(() => nonAgileObject.As<IAgileObject>());

                agileReference = nonAgileObject.AsAgile();
                objectAcquired.Set();
                valueAcquired.WaitOne();

                // Object gets proxied to the apartment.
                Assert.AreEqual(2, proxyObject.Commands.Count);
                agileReference.Dispose();
            }

            public void CheckValue()
            {
                objectAcquired.WaitOne();
                Assert.AreEqual(ApartmentState.MTA, Thread.CurrentThread.GetApartmentState());
                proxyObject = agileReference.Get();
                Assert.AreEqual(2, proxyObject.Commands.Count);

                valueAcquired.Set();
            }

            public void CallProxyObject()
            {
                // Call to a proxy object which we internally use an agile reference
                // to resolve after the apartment is gone should throw.
                Assert.ThrowsException<System.Exception>(() => proxyObject.Commands);
            }

            private Windows.UI.Popups.PopupMenu nonAgileObject;
            private Windows.UI.Popups.PopupMenu proxyObject;
            private AgileReference<Windows.UI.Popups.PopupMenu> agileReference, agileReference2;
            private readonly AutoResetEvent objectAcquired = new AutoResetEvent(false);
            private readonly AutoResetEvent valueAcquired = new AutoResetEvent(false);
        }


        [TestMethod]
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

            // Spin another STA thread after the other 2 threads are done and try to
            // access one of the proxied objects.  They should fail as there is no context
            // to switch to in order to marshal it to the current apartment.
            Thread anotherStaThread = new Thread(new ThreadStart(caller.CallProxyObject));
            anotherStaThread.SetApartmentState(ApartmentState.STA);
            anotherStaThread.Start();
            anotherStaThread.Join();
        }
        */

        [TestMethod]
        public void TestNonAgileDelegateCall()
        {
            var expected = new int[] { 0, 1, 2 };
            var observable = new ManagedBindableObservable(expected);
            var nonAgileClass = new NonAgileClass();
            nonAgileClass.Observe(observable);
            observable.Add(3);
            Assert.AreEqual(6, observable.Observation);
        }

        //[GeneratedComInterface]
        //[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        //[Guid("EECDBF0E-BAE9-4CB6-A68E-9598E1CB57BB")]
        //internal partial interface IWindowNative
        //{
        //    IntPtr get_WindowHandle();
        //}

        //[GeneratedComInterface]
        //[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        //[Guid("3E68D4BD-7135-4D10-8018-9FB6D9F33FA1")]
        //internal partial interface IInitializeWithWindow
        //{
        //    void Initialize(IntPtr hwnd);
        //}

        //[TestMethod]
        //unsafe public void TestComImports()
        //{
        //    static Object MakeObject()
        //    {
        //        Assert.AreEqual(0, ComImports.NumObjects);
        //        var obj = ComImports.MakeObject();
        //        Assert.AreEqual(1, ComImports.NumObjects);
        //        return obj;
        //    }

        //    static void TestObject() => MakeObject();

        //    static (IInitializeWithWindow, IWindowNative) MakeImports()
        //    {
        //        var obj = MakeObject();
        //        var initializeWithWindow = (IInitializeWithWindow)obj;
        //        var windowNative = (IWindowNative)obj;
        //        return (initializeWithWindow, windowNative);
        //    }

        //    static void TestImports()
        //    {
        //        var (initializeWithWindow, windowNative) = MakeImports();

        //        GC.Collect();
        //        GC.WaitForPendingFinalizers();

        //        var hwnd = new IntPtr(0x12345678);
        //        initializeWithWindow.Initialize(hwnd);
        //        Assert.AreEqual(windowNative.get_WindowHandle(), hwnd);
        //    }

        //    TestObject();
        //    GC.Collect();
        //    GC.WaitForPendingFinalizers();
        //    Assert.AreEqual(0, ComImports.NumObjects);

        //    TestImports();
        //    GC.Collect();
        //    GC.WaitForPendingFinalizers();
        //    Assert.AreEqual(0, ComImports.NumObjects);
        //}

        [TestMethod]
        public void TestInterfaceObjectMarshalling()
        {
            var nativeProperties = Class.NativeProperties1;

            TestObject.CopyProperties(nativeProperties);

            Assert.AreEqual(TestObject.ReadWriteProperty, nativeProperties.ReadWriteProperty);
        }

        [TestMethod]
        public void TestSetPropertyAcrossProjections()
        {
            var setPropertyClass = new TestComponentCSharp.AnotherAssembly.SetPropertyClass();
            setPropertyClass.ReadWriteProperty = 4;
            Assert.AreEqual(4, setPropertyClass.ReadWriteProperty);

            IProperties1 property = setPropertyClass;
            Assert.AreEqual(4, property.ReadWriteProperty);
        }

        [TestMethod]
        public void TestStaticPropertyImplementedAcrossInterfaces()
        {
            // Testing call doesn't fail.
            WarningStatic.ReadWriteProperty = 4; // expected warning CA1416
            _ = WarningStatic.ReadWriteProperty;
        }

        // Test scenario where type reported by runtimeclass name is not a valid type (i.e. internal type).
        [TestMethod]
        public void TestNonProjectedRuntimeClass()
        {
            string key = ".....";
            IBuffer keyMaterial = CryptographicBuffer.ConvertStringToBinary(key, BinaryStringEncoding.Utf8);
            MacAlgorithmProvider mac = MacAlgorithmProvider.OpenAlgorithm(MacAlgorithmNames.HmacSha1);
            CryptographicKey cryptoKey = mac.CreateKey(keyMaterial);
            Assert.IsNotNull(cryptoKey);
        }

        [TestMethod]
        public void TestIBindableIterator()
        {
            CustomBindableIteratorTest bindableIterator = new CustomBindableIteratorTest();
            Assert.IsTrue(bindableIterator.MoveNext());
            Assert.AreEqual(27861, bindableIterator.Current);
        }

        [TestMethod]
        public void TestIDisposable()
        {
            CustomDisposableTest disposable = new CustomDisposableTest();
            disposable.Dispose();
        }

        [TestMethod]
        public void TestIBindableVector()
        {
            CustomBindableVectorTest vector = new CustomBindableVectorTest();
            Assert.IsNotNull(vector);
            Assert.AreEqual(1, vector.Count);
            Assert.IsFalse(vector.IsSynchronized);
            Assert.IsNotNull(vector.SyncRoot);
            Assert.AreEqual(1, vector[0]);

            var enumerator = ((IEnumerable)vector).GetEnumerator();
            Assert.IsNotNull(enumerator);
        }

        [TestMethod]
        public void TestBindableObservableVector()
        {
            CustomBindableObservableVectorTest vector = new CustomBindableObservableVectorTest();
            Assert.AreEqual(1, vector.Count);
            Assert.IsFalse(vector.IsSynchronized);
            Assert.IsNotNull(vector.SyncRoot);
            Assert.AreEqual(1, vector[0]);
            vector.Clear();
        }

        [TestMethod]
        public void TestNonProjectedBindableObservableVector()
        {
            var expected = new int[] { 0, 1, 2 };
            var observable = new ManagedBindableObservable(expected);
            var nativeObservable = TestObject.GetBindableObservableVector(observable);
            Assert.AreEqual(3, ((ICollection)(object)nativeObservable).Count);
            Assert.AreEqual(3, nativeObservable.Count);
            Assert.IsNotNull(nativeObservable.SyncRoot);
            Assert.AreEqual(0, nativeObservable[0]);
            nativeObservable.Clear();
            Assert.AreEqual(0, nativeObservable.Count);
        }

        [TestMethod]
        public void TestIterator()
        {
            CustomIteratorTest iterator = new CustomIteratorTest();
            iterator.MoveNext();
            Assert.AreEqual(2, iterator.Current);
            Assert.AreEqual(2, ((IEnumerator)iterator).Current);
        }

        [TestMethod]
        public void TestCovariance()
        {
            var listOfListOfPoints = new List<List<Point>>() {
                new List<Point>{ new Point(1, 1), new Point(1, 2), new Point(1, 3) },
                new List<Point>{ new Point(2, 1), new Point(2, 2), new Point(2, 3) },
                new List<Point>{ new Point(3, 1), new Point(3, 2), new Point(3, 3) }
            };
            TestObject.IterableOfPointIterablesProperty = listOfListOfPoints;
            Assert.IsTrue(TestObject.IterableOfPointIterablesProperty.SequenceEqual(listOfListOfPoints));

            var listOfListOfUris = new List<List<Uri>>() {
                new List<Uri>{ new Uri("http://aka.ms/cswinrt"), new Uri("http://github.com") },
                new List<Uri>{ new Uri("http://aka.ms/cswinrt") },
                new List<Uri>{ new Uri("http://aka.ms/cswinrt"), new Uri("http://microsoft.com") }
            };
            TestObject.IterableOfObjectIterablesProperty = listOfListOfUris;
            Assert.IsTrue(TestObject.IterableOfObjectIterablesProperty.SequenceEqual(listOfListOfUris));
        }

        (System.WeakReference<Class>, System.WeakReference<EventHandlerClass>) TestEventDelegateCleanup()
        {
            // Both WinRT object and handler class alive.
            var eventCalled = false;
            var eventHandlerClass = new EventHandlerClass(() => eventCalled = true);
            var classInstance = new Class();
            classInstance.IntPropertyChanged += eventHandlerClass.IntPropertyChanged;
            GC.Collect(2, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();
            classInstance.IntProperty = 3;
            Assert.IsTrue(eventCalled);

            // No strong reference to handler class, but delegate is still registered on
            // the WinRT object keeping it alive.
            eventCalled = false;
            var weakEventHandlerClass = new System.WeakReference<EventHandlerClass>(eventHandlerClass);
            eventHandlerClass = null;
            GC.Collect(2, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();
            classInstance.IntProperty = 3;
            Assert.IsTrue(eventCalled);
            Assert.IsTrue(weakEventHandlerClass.TryGetTarget(out var _));

            // No strong reference to WinRT object.  It should no longer be alive
            // and should also cause for the event handler class to be no longer alive.
            var weakClassInstance = new System.WeakReference<Class>(classInstance);
            classInstance = null;
            return (weakClassInstance, weakEventHandlerClass);
        }

        // Ensure that event subscription state is properly cached to enable later unsubscribes
        [TestMethod]
        public void TestEventSourceCaching()
        {
            bool eventCalled = false;
            void Class_StaticIntPropertyChanged(object sender, int e) => eventCalled = (e == 3);
            bool eventCalled2 = false;
            void Class_StaticIntPropertyChanged2(object sender, int e) => eventCalled2 = (e == 3);

            // Test static codegen-based EventSource caching
            Class.StaticIntPropertyChanged += Class_StaticIntPropertyChanged;
            GC.Collect(2, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();
            Class.StaticIntPropertyChanged -= Class_StaticIntPropertyChanged;
            Class.StaticIntProperty = 3;
            Assert.IsFalse(eventCalled);
            Class.StaticIntPropertyChanged += Class_StaticIntPropertyChanged;
            GC.Collect(2, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();
            Class.StaticIntProperty = 3;
            Assert.IsTrue(eventCalled);
            eventCalled = false;

            // Test adding another delegate to validate COM reference tracking in EventSource
            Class.StaticIntPropertyChanged += Class_StaticIntPropertyChanged2;
            Class.StaticIntProperty = 3;
            Assert.IsTrue(eventCalled);
            Assert.IsTrue(eventCalled2);
            GC.Collect(2, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();
            eventCalled = false;
            eventCalled2 = false;
            Class.StaticIntPropertyChanged -= Class_StaticIntPropertyChanged;
            Class.StaticIntProperty = 3;
            Assert.IsFalse(eventCalled);
            Assert.IsTrue(eventCalled2);

            // Test dynamic WeakRef-based EventSource caching
            eventCalled = false;
            static void Subscribe(EventHandler<int> handler) => Singleton.Instance.IntPropertyChanged += handler;
            static void Unsubscribe(EventHandler<int> handler) => Singleton.Instance.IntPropertyChanged -= handler;
            static void Assign(int value) => Singleton.Instance.IntProperty = value;
            Subscribe(Class_StaticIntPropertyChanged);
            GC.Collect(2, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();
            Unsubscribe(Class_StaticIntPropertyChanged);
            Assign(3);
            Assert.IsFalse(eventCalled);
            Subscribe(Class_StaticIntPropertyChanged);
            GC.Collect(2, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();
            Assign(3);
            Assert.IsTrue(eventCalled);

            // Test that event delegates don't leak when not unsubscribed.
            // Test runs into a different function as the finalizer wasn't
            // getting triggered otherwise with a weak reference.
            (System.WeakReference<Class> weakClassInstance, System.WeakReference<EventHandlerClass> weakEventHandlerClass) =
                TestEventDelegateCleanup();
            GC.Collect(2, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();
            Assert.IsFalse(weakClassInstance.TryGetTarget(out _));
            Assert.IsFalse(weakEventHandlerClass.TryGetTarget(out _));
        }

        class EventHandlerClass
        {
            private readonly Action eventCalled;

            public EventHandlerClass(Action eventCalled)
            {
                this.eventCalled = eventCalled;
            }

            public void IntPropertyChanged(object sender, int e) => eventCalled();
        }

        // Test scenario where events may be removed by the native event source without an unsubscribe.
        [TestMethod]
        public void TestEventRemovalByEventSource()
        {
            bool eventCalled = false;
            void Class_IntPropertyChanged(object sender, int e) => eventCalled = (e == 3);
            bool eventCalled2 = false;
            void Class_IntPropertyChanged2(object sender, int e) => eventCalled2 = (e == 3);

            var classInstance = new Class();
            classInstance.IntPropertyChanged += Class_IntPropertyChanged;
            classInstance.IntProperty = 3;
            Assert.IsTrue(eventCalled);
            Assert.IsFalse(eventCalled2);
            eventCalled = false;
            classInstance.RemoveLastIntPropertyChangedHandler();
            classInstance.IntPropertyChanged += Class_IntPropertyChanged2;
            classInstance.IntProperty = 3;
            Assert.IsFalse(eventCalled);
            Assert.IsTrue(eventCalled2);
            eventCalled2 = false;

            classInstance.RemoveLastIntPropertyChangedHandler();
            GC.Collect(2, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();
            classInstance.IntPropertyChanged += Class_IntPropertyChanged;
            classInstance.IntProperty = 3;
            Assert.IsTrue(eventCalled);
            Assert.IsFalse(eventCalled2);
            eventCalled = false;

            classInstance.IntPropertyChanged += Class_IntPropertyChanged2;
            classInstance.IntProperty = 3;
            Assert.IsTrue(eventCalled);
            Assert.IsTrue(eventCalled2);
        }

        //[TestMethod]
        //public unsafe void TestProxiedDelegate()
        //{
        //    var obj = new OOPAsyncAction();
        //    var factory = new WinRTClassFactory<OOPAsyncAction>(
        //        () => obj,
        //        new Dictionary<Guid, Func<object, IntPtr>>()
        //        {
        //            { typeof(IAsyncAction).GUID, obj => (IntPtr)WindowsRuntimeInterfaceMarshaller<IAsyncAction>.ConvertToUnmanaged((IAsyncAction) obj, typeof(IAsyncAction).GUID).GetThisPtr() },
        //        });

        //    WinRTClassFactory<OOPAsyncAction>.RegisterClass<OOPAsyncAction>(factory);

        //    var currentExecutingDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        //    var launchExePath = $"{currentExecutingDir}\\OOPExe.exe";
        //    var proc = Process.Start(launchExePath);
        //    Thread.Sleep(5000);
        //    obj.Close();
        //    Assert.IsTrue(obj.delegateCalled);

        //    try
        //    {
        //        proc.Kill();
        //    }
        //    catch (Exception)
        //    {
        //    }
        //}

        [TestMethod]
        private async Task TestPnpPropertiesInLoop()
        {
            for (int i = 0; i < 10; i++)
            {
                await TestPnpPropertiesAsync();
            }
        }

        private async Task TestPnpPropertiesAsync()
        {
            var requestedDeviceProperties = new List<string>()
                {
                    "System.Devices.ClassGuid",
                    "System.Devices.ContainerId",
                    "System.Devices.DeviceHasProblem",
                    "System.Devices.DeviceInstanceId",
                    "System.Devices.Parent",
                    "System.Devices.Present",
                    "System.ItemNameDisplay",
                    "System.Devices.Children",
                };
            var devicefilter = "System.Devices.Present:System.StructuredQueryType.Boolean#True";
            var presentDevices = (await PnpObject.FindAllAsync(PnpObjectType.Device, requestedDeviceProperties, devicefilter).AsTask().ConfigureAwait(false)).Select(pnpObject =>
            {
                var prop = pnpObject.Properties;
                // Iterating through each key is necessary for this test even though we do not use each key directly
                // This makes it more probable for a native pointer to get repeated and a value type to be cached and seen again.
                foreach (var key in prop.Keys)
                {
                    var val = prop[key];
                    if (string.CompareOrdinal(key, "System.Devices.ContainerId") == 0 && val != null)
                    {
                        var val4 = pnpObject.Properties[key];
                        if (val is not Guid || val4 is not Guid)
                        {
                            throw new Exception("Incorrect value type Guid. Actual type: " + val.GetType() + "  " + val4.GetType());
                        }
                    }
                    if (string.CompareOrdinal(key, "System.Devices.Parent") == 0 && val != null)
                    {
                        var val4 = pnpObject.Properties[key];
                        if (val is not string || val4 is not string)
                        {
                            throw new Exception("Incorrect value type string Actual type: " + val.GetType() + "  " + val4.GetType());
                        }
                    }

                }
                return pnpObject;
            }).ToList();
        }

        [TestComponentCSharp.Warning]  // NO warning CA1416
        class WarningManaged { };

        class WarningSubclass : WarningClass
        {
            void InvokeOverridableWarnings()
            {
                WarningOverridableMethod(); // warning CA1416
                WarningOverridableProperty = 0; // warning CA1416
                // see https://github.com/microsoft/cppwinrt/issues/782
                //WarningOverridableEvent += (object s, Int32 v) => { }; // warning CA1416
            }
        }

        // Manual for now - verify that all APIs targeting 19041 generate a warning
        private void TestSupportedOSPlatformWarnings()
        {
            // Types
            var a = new WarningAttribute();    // warning CA1416
            Assert.IsNotNull(a);
            var w = new WarningStruct { i32 = 0 }; // warning CA1416
            Assert.AreEqual(0, w.i32);     // warning CA1416
            var v = WarningEnum.Value;
            Assert.AreNotEqual(WarningEnum.WarningValue, v);   // warning CA1416

            // Members
            var o = new WarningClass();    // warning CA1416
            o = new WarningClass(WarningEnum.Value);    // warning CA1416
            o.WarningMethod();     // warning CA1416
            var p = o.WarningProperty; // warning CA1416
            o.WarningProperty = 0; // warning CA1416
            p = o.WarningPropertySetter;
            o.WarningPropertySetter = 0;   // warning CA1416
            o.WarningEvent += (object s, Int32 v) => { }; // warning CA1416
            o.WarningInterfaceMethod();     // warning CA1416
            p = o.WarningInterfaceProperty; // warning CA1416
            o.WarningInterfaceProperty = 0; // warning CA1416
            p = o.WarningInterfacePropertySetter;
            o.WarningInterfacePropertySetter = 0;   // warning CA1416
            o.WarningInterfaceEvent += (object s, Int32 v) => { }; // warning CA1416

            // Attributed statics
            WarningStatic.WarningMethod(); // warning CA1416
            WarningStatic.WarningProperty = 0; // warning CA1416
            WarningStatic.WarningEvent += (object s, Int32 v) => { }; // warning CA1416
        }

        [TestMethod]
        public void TestObjectFunctions()
        {
            CustomEquals first = new()
            {
                Value = 2
            };
            CustomEquals second = new()
            {
                Value = 4
            };
            CustomEquals third = new()
            {
                Value = 2
            };

            Assert.IsFalse(first.Equals(second));
            Assert.IsTrue(first.Equals(third));
            Assert.IsTrue(first.Equals(first));
            Assert.IsTrue(Object.Equals(first, second));
            Assert.IsTrue(Object.Equals(second, third));
            Assert.AreEqual(5, first.GetHashCode());
            Assert.AreEqual(5, second.GetHashCode());

            Class fourth = new();
            Class fifth = new();
            Assert.IsTrue(fourth.Equals(fourth));
            Assert.IsFalse(fourth.Equals(fifth));
            Assert.IsFalse(Object.Equals(fourth, fifth));
            Assert.IsTrue(Object.Equals(fifth, fifth));
            fourth.GetHashCode();

            CustomEquals2 sixth = new()
            {
                Value = 4
            };
            Assert.AreEqual(4, sixth.Equals(sixth));
            Assert.AreEqual(4, sixth.Equals(fifth));
            Assert.IsFalse(object.Equals(sixth, fifth));
            Assert.IsTrue(object.Equals(sixth, sixth));

            UnSealedCustomEquals seventh = new()
            {
                Value = 2
            };
            DerivedCustomEquals eighth = new()
            {
                Value = 2
            };
            Assert.AreEqual(10, eighth.GetHashCode());
            // Uses Equals defined on derived.
            Assert.IsTrue(eighth.Equals(seventh));
            Assert.IsFalse(seventh.Equals(eighth));
        }

        // Manually verify warning for experimental.
        private void TestExperimentAttribute()
        {
            CustomExperimentClass custom = new CustomExperimentClass();
            custom.f();
        }

        void OnDeviceAdded(DeviceWatcher sender, DeviceInformation args)
        {
        }

        void OnDeviceUpdated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
        }

        [TestMethod]
        public void TestWeakReferenceEventsFromMultipleContexts()
        {
            SemaphoreSlim semaphore = new SemaphoreSlim(0);
            DeviceWatcher watcher = null;

            Thread staThread = new Thread(() =>
            {
                Assert.IsTrue(Thread.CurrentThread.GetApartmentState() == ApartmentState.STA);

                watcher = DeviceInformation.CreateWatcher();


                try
                {
                    watcher.Added += OnDeviceAdded;
                }
                catch (Exception ex)
                {
                    Assert.Fail("Expected no exception, but got: " + ex);
                }


                Thread mtaThread = new Thread(() =>
                {
                    Assert.IsTrue(Thread.CurrentThread.GetApartmentState() == ApartmentState.MTA);

                    try
                    {
                        watcher.Updated += OnDeviceUpdated;
                    }
                    catch (Exception ex)
                    {
                        Assert.Fail("Expected no exception, but got: " + ex);
                    }
                });
                mtaThread.SetApartmentState(ApartmentState.MTA);
                mtaThread.Start();
                mtaThread.Join();
            });
            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start();
            staThread.Join();
        }

        [TestMethod]
        public void TestActivationFactoriesFromMultipleContexts()
        {
            Exception exception = null;

            Thread staThread = new Thread(() =>
            {
                Assert.IsTrue(Thread.CurrentThread.GetApartmentState() == ApartmentState.STA);

                try
                {
                    var xmlDoc = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText01);
                    _ = new ToastNotification(xmlDoc);
                }
                catch (Exception ex)
                {
                    Assert.Fail("Expected no exception, but got: " + ex);
                }

            });
            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start();
            staThread.Join();

            Assert.IsNull(exception);

            Thread mtaThread = new Thread(() =>
            {
                Assert.IsTrue(Thread.CurrentThread.GetApartmentState() == ApartmentState.MTA);

                try
                {
                    var xmlDoc = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText01);
                    _ = new ToastNotification(xmlDoc);
                }
                catch (Exception ex)
                {
                    Assert.Fail("Expected no exception, but got: " + ex);
                }
            });
            mtaThread.SetApartmentState(ApartmentState.MTA);
            mtaThread.Start();
            mtaThread.Join();

            Assert.IsNull(exception);
        }

        /* TODO

        [Guid("59C7966B-AE52-5283-AD7F-A1B9E9678ADD")]
        [global::WinRT.WindowsRuntimeType("Windows.Foundation.UniversalApiContract")]
        [global::WinRT.WindowsRuntimeHelperType(typeof(ICustomGuidHelperStatics))]
        interface ICustomGuidHelperStatics
        {
            public static readonly IntPtr AbiToProjectionVftablePtr;
            static unsafe ICustomGuidHelperStatics()
            {
                AbiToProjectionVftablePtr = ComWrappersSupport.AllocateVtableMemory(typeof(ICustomGuidHelperStatics), sizeof(IInspectable.Vftbl) + sizeof(IntPtr) * 3);
                *(IInspectable.Vftbl*)AbiToProjectionVftablePtr = IInspectable.Vftbl.AbiToProjectionVftable;
                ((delegate* unmanaged[Stdcall]<IntPtr, Guid*, int>*)AbiToProjectionVftablePtr)[6] = &Do_Abi_CreateNewGuid_0;
                ((delegate* unmanaged[Stdcall]<IntPtr, Guid*, int>*)AbiToProjectionVftablePtr)[7] = &Do_Abi_get_Empty_1;
                ((delegate* unmanaged[Stdcall]<IntPtr, Guid*, Guid*, byte*, int>*)AbiToProjectionVftablePtr)[8] = &Do_Abi_Equals_2;
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
            private static unsafe int Do_Abi_CreateNewGuid_0(IntPtr thisPtr, Guid* result)
            {

                Guid __result = default;

                *result = default;

                try
                {
                    __result = global::WinRT.ComWrappersSupport.FindObject<ICustomGuidHelperStatics>(thisPtr).CreateNewGuid();
                    *result = __result;

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
            private static unsafe int Do_Abi_Equals_2(IntPtr thisPtr, Guid* target, Guid* value, byte* result)
            {

                bool __result = default;

                *result = default;

                try
                {
                    __result = global::WinRT.ComWrappersSupport.FindObject<ICustomGuidHelperStatics>(thisPtr).Equals(*target, *value);
                    *result = (byte)(__result ? 1 : 0);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
            private static unsafe int Do_Abi_get_Empty_1(IntPtr thisPtr, Guid* value)
            {

                Guid __value = default;

                *value = default;

                try
                {
                    __value = global::WinRT.ComWrappersSupport.FindObject<ICustomGuidHelperStatics>(thisPtr).Empty;
                    *value = __value;

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }

            Guid CreateNewGuid();
            bool Equals(in Guid target, in Guid value);
            Guid Empty { get; }
        }

        class CustomGuidHelper : ICustomGuidHelperStatics
        {
            public static Guid Mock { get; set; }

            public Guid Empty => Mock;

            public Guid CreateNewGuid()
            {
                return Mock;
            }

            public bool Equals(in Guid target, in Guid value)
            {
                return false;
            }
        };

        [TestMethod]
        public unsafe void TestActivationHandler()
        {
            WindowsRuntimeActivationFactory.SetWindowsRuntimeActivationHandler((string name, in Guid iid, out void* activationFactory) =>
            {
                Assert.AreEqual("Windows.Foundation.GuidHelper", name);
                Assert.AreEqual(typeof(IGuidHelperStatics).GUID, iid);

                activationFactory = WindowsRuntimeInterfaceMarshaller<ICustomGuidHelperStatics>.ConvertToUnmanaged(new CustomGuidHelper(), typeof(ICustomGuidHelperStatics).GUID).GetThisPtrUnsafe();
                return 0;
            });

            CustomGuidHelper.Mock = new Guid("78872A91-C365-4DDB-9509-1CCA002B6FD9");

            Guid guid = GuidHelper.CreateNewGuid();
            Assert.AreEqual(CustomGuidHelper.Mock, guid);

            WindowsRuntimeActivationFactory.SetWindowsRuntimeActivationHandler(null);
        }
        */

        [TestMethod]
        public void TestDictionary()
        {
            var intToIntDict = TestObject.GetIntToIntDictionary();
            Assert.AreEqual(8, intToIntDict[2]);
            Assert.AreEqual(8, intToIntDict[2]);
            Assert.AreEqual(12, intToIntDict[3]);

            var stringToBlittableDict = TestObject.GetStringToBlittableDictionary();
            Assert.AreEqual(5, stringToBlittableDict["alpha"].blittable.i32);
            Assert.AreEqual(7, stringToBlittableDict["charlie"].blittable.i32);
            Assert.AreEqual(5, stringToBlittableDict["alpha"].blittable.i32);

            var stringToNonBlittableDict = TestObject.GetStringToNonBlittableDictionary();
            Assert.AreEqual(1, stringToNonBlittableDict["String1"].blittable.i32);
            Assert.AreEqual("String1", stringToNonBlittableDict["String1"].strings.str);
            Assert.IsFalse(stringToNonBlittableDict["String1"].bools.w);
            Assert.IsTrue(stringToNonBlittableDict["String1"].bools.x);

            var blittableToObjectDict = TestObject.GetBlittableToObjectDictionary();
            ComposedBlittableStruct key = new();
            BlittableStruct blittable = new()
            {
                i32 = 4
            };
            key.blittable = blittable;
            Assert.AreEqual("box", (string)blittableToObjectDict[key]);
            Assert.AreEqual("box", (string)blittableToObjectDict[key]);
        }

        [TestMethod]
        public void TestTimeSpanDictionary()
        {
            IDictionary<TimeSpan, TimeSpan> timeSpanDict = TestObject.GetTimeSpanToTimeSpanDictionary();

            Assert.IsFalse(timeSpanDict.ContainsKey(TimeSpan.FromSeconds(1)));
            Assert.IsTrue(timeSpanDict.ContainsKey(TimeSpan.FromSeconds(6)));
            Assert.IsTrue(timeSpanDict.ContainsKey(TimeSpan.FromHours(7)));

            Assert.AreEqual(TimeSpan.FromTicks(7), timeSpanDict[TimeSpan.FromHours(7)]);
            Assert.AreEqual(TimeSpan.FromTicks(5), timeSpanDict[TimeSpan.FromTicks(4)]);

            Assert.IsTrue(timeSpanDict.Remove(TimeSpan.FromHours(7)));
            Assert.IsFalse(timeSpanDict.ContainsKey(TimeSpan.FromHours(7)));

            Assert.AreEqual(2, timeSpanDict.Keys.Count);

            timeSpanDict.Clear();

            Assert.IsFalse(timeSpanDict.ContainsKey(TimeSpan.FromSeconds(6)));
            Assert.IsTrue(timeSpanDict.Keys.SequenceEqual(Enumerable.Empty<TimeSpan>()));

            // Key - value entries are 1 minute apart as it is used for validation later.
            timeSpanDict.Add(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2));
            timeSpanDict[TimeSpan.FromMinutes(3)] = TimeSpan.FromMinutes(4);

            Assert.IsTrue(timeSpanDict.TryGetValue(TimeSpan.FromMinutes(1), out TimeSpan value1));
            Assert.AreEqual(TimeSpan.FromMinutes(2), value1);
            Assert.IsFalse(timeSpanDict.TryGetValue(TimeSpan.FromMinutes(6), out TimeSpan _));

            Assert.AreEqual(2, timeSpanDict.Keys.Count);

            foreach (var entries in timeSpanDict)
            {
                Assert.AreEqual(TimeSpan.FromMinutes(1), entries.Value - entries.Key);
            }
        }

        [TestMethod]
        public void TestNontProjectedClassAsBaseClass()
        {
            UnSealedCustomEquals customEquals = Class.NonProjectedClassInstance;
            customEquals.Value = 3;
            // Non projected class changes the behavior of the Value property to double it.
            Assert.AreEqual(6, customEquals.Value);
        }
    }
}