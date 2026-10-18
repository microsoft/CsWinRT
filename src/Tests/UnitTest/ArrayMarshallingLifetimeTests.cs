// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;

namespace UnitTest;

[TestClass]
public unsafe class ArrayMarshallingLifetimeTests
{
    private const int BufferLength = 4;

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(3)]
    public void ObjectArray_FailedConversionClearsReleasedSlots(int failureIndex)
    {
        object[] values = Enumerable.Range(0, BufferLength).Select(i => (object)new Uri($"https://example.com/{i}")).ToArray();
        values[failureIndex] = new Uri("relative", UriKind.Relative);
        nint[] destination = new nint[BufferLength];
        fixed (nint* data = destination)
        {
            void** buffer = (void**)data;
            Assert.ThrowsExactly<ArgumentException>(() => WindowsRuntimeObjectArrayMarshaller.CopyToUnmanaged(values, BufferLength, buffer));
            Assert.IsTrue(destination.All(value => value == 0));
            WindowsRuntimeUnknownArrayMarshaller.Dispose(BufferLength, buffer);
        }
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(3)]
    public void NullableArray_InjectedConversionFailureClearsReleasedSlots(int failureIndex)
    {
        int?[] values = Enumerable.Range(1, BufferLength).Select(i => (int?)i).ToArray();
        FaultingNullableMarshaller.ConversionsBeforeFailure = failureIndex;
        nint[] destination = new nint[BufferLength];
        fixed (nint* data = destination)
        {
            void** buffer = (void**)data;
            Assert.ThrowsExactly<InvalidOperationException>(() =>
                WindowsRuntimeNullableTypeArrayMarshaller<int>.CopyToUnmanaged<FaultingNullableMarshaller>(values, BufferLength, buffer));
            Assert.IsTrue(destination.All(value => value == 0));
            WindowsRuntimeUnknownArrayMarshaller.Dispose(BufferLength, buffer);
        }
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(3)]
    public void KeyValuePairArray_InjectedConversionFailureClearsReleasedSlots(int failureIndex)
    {
        KeyValuePair<int, int>[] values = Enumerable.Range(1, BufferLength).Select(i => new KeyValuePair<int, int>(i, i)).ToArray();
        FaultingKeyValuePairMarshaller.ConversionsBeforeFailure = failureIndex;
        nint[] destination = new nint[BufferLength];
        fixed (nint* data = destination)
        {
            void** buffer = (void**)data;
            Assert.ThrowsExactly<InvalidOperationException>(() =>
                WindowsRuntimeKeyValuePairTypeArrayMarshaller<int, int>.CopyToUnmanaged<FaultingKeyValuePairMarshaller>(values, BufferLength, buffer));
            Assert.IsTrue(destination.All(value => value == 0));
            WindowsRuntimeUnknownArrayMarshaller.Dispose(BufferLength, buffer);
        }
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(3)]
    public void TypeArray_FailedConversionClearsReleasedSlots(int failureIndex)
    {
        Type[] values = Enumerable.Repeat(typeof(int), BufferLength).ToArray();
        values[failureIndex] = null;
        ABI.System.Type* destination = stackalloc ABI.System.Type[BufferLength];
        for (int i = 0; i < BufferLength; i++)
        {
            destination[i] = default;
        }
        Assert.ThrowsExactly<ArgumentNullException>(() => TypeArrayMarshaller.CopyToUnmanaged(values, BufferLength, destination));
        for (int i = 0; i < BufferLength; i++)
        {
            Assert.IsTrue(destination[i].Name == null);
        }
        TypeArrayMarshaller.Dispose(BufferLength, destination);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(3)]
    public void ReferenceArray_FailedConversionClearsReleasedSlots(int failureIndex)
    {
        Uri[] values = Enumerable.Range(0, BufferLength).Select(i => new Uri($"https://example.com/{i}")).ToArray();
        values[failureIndex] = new Uri("relative", UriKind.Relative);
        nint[] destination = new nint[BufferLength];
        fixed (nint* data = destination)
        {
            void** buffer = (void**)data;
            Assert.ThrowsExactly<ArgumentException>(() =>
                WindowsRuntimeReferenceTypeArrayMarshaller<Uri>.CopyToUnmanaged<UriElementMarshaller>(
                    values, BufferLength, buffer));
            Assert.IsTrue(destination.All(value => value == 0), "Released pointers must not be disposed again.");
            WindowsRuntimeUnknownArrayMarshaller.Dispose(BufferLength, buffer);
        }
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(3)]
    public void ManagedValueArray_InjectedConversionFailureDisposesResourcesExactlyOnce(int failureIndex)
    {
        ResourceMarshaller.Disposals = 0;
        ResourceMarshaller.ConversionsBeforeFailure = failureIndex;
        Resource[] values = Enumerable.Range(1, BufferLength).Select(i => new Resource(i)).ToArray();
        int[] destination = new int[BufferLength];
        fixed (int* data = destination)
        {
            int* buffer = data;
            Assert.ThrowsExactly<InvalidOperationException>(() =>
                WindowsRuntimeManagedValueTypeArrayMarshaller<Resource, int>.CopyToUnmanaged<ResourceMarshaller>(
                    values, BufferLength, buffer));
            Assert.AreEqual(failureIndex, ResourceMarshaller.Disposals);
            WindowsRuntimeManagedValueTypeArrayMarshaller<Resource, int>.Dispose<ResourceMarshaller>(BufferLength, buffer);
            Assert.AreEqual(failureIndex, ResourceMarshaller.Disposals, "Caller cleanup must not double-dispose resources.");
            Assert.IsTrue(destination.All(value => value == 0), "Released ABI values must be reset.");
        }
    }

    private readonly struct UriElementMarshaller : IWindowsRuntimeReferenceTypeArrayElementMarshaller<Uri>
    {
        public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(Uri value) =>
            ABI.System.UriMarshaller.ConvertToUnmanaged(value);

        public static Uri ConvertToManaged(void* value) => ABI.System.UriMarshaller.ConvertToManaged(value);
    }

    private readonly struct FaultingNullableMarshaller : IWindowsRuntimeNullableTypeArrayElementMarshaller<int>
    {
        [ThreadStatic]
        public static int ConversionsBeforeFailure;

        public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(int? value)
        {
            if (ConversionsBeforeFailure-- == 0)
            {
                throw new InvalidOperationException("Injected element conversion failure.");
            }
            return ABI.System.Int32Marshaller.BoxToUnmanaged(value);
        }

        public static int? ConvertToManaged(void* value) => throw new NotSupportedException();
    }

    private readonly struct FaultingKeyValuePairMarshaller : IWindowsRuntimeKeyValuePairTypeArrayElementMarshaller<int, int>
    {
        [ThreadStatic]
        public static int ConversionsBeforeFailure;

        public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(KeyValuePair<int, int> value)
        {
            if (ConversionsBeforeFailure-- == 0)
            {
                throw new InvalidOperationException("Injected element conversion failure.");
            }
            return ConvertPairToUnmanaged(null, value);
        }

        public static KeyValuePair<int, int> ConvertToManaged(void* value) => throw new NotSupportedException();

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "ConvertToUnmanaged")]
        private static extern WindowsRuntimeObjectReferenceValue ConvertPairToUnmanaged(
            [UnsafeAccessorType("ABI.System.Collections.Generic.<#corlib>KeyValuePair'2<int|int>Marshaller, WinRT.Interop")] object _,
            KeyValuePair<int, int> value);
    }

    private readonly record struct Resource(int Value);

    private readonly struct ResourceMarshaller : IWindowsRuntimeManagedValueTypeArrayElementMarshaller<Resource, int>
    {
        [ThreadStatic]
        public static int Disposals;

        [ThreadStatic]
        public static int ConversionsBeforeFailure;

        public static int ConvertToUnmanaged(Resource value)
        {
            if (ConversionsBeforeFailure-- == 0)
            {
                throw new InvalidOperationException("Injected element conversion failure.");
            }
            return value.Value;
        }

        public static Resource ConvertToManaged(int value) => new(value);

        public static void Dispose(int value)
        {
            if (value != 0)
            {
                Disposals++;
            }
        }
    }
}
