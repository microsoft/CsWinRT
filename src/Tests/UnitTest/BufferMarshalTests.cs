// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Windows.Foundation;
using Windows.Storage.Streams;
using WindowsRuntime.InteropServices;

namespace UnitTest;

[TestClass]
public class BufferMarshalTests
{
    [TestMethod]
    [DataRow(0)]
    [DataRow(3)]
    public unsafe void GetDataUnsafe_NativeBuffer_ReturnsUnderlyingData(int length)
    {
        byte[] expected = new byte[length];
        Array.Fill(expected, (byte)0x42);
        IBuffer buffer = new Windows.Storage.Streams.Buffer((uint)length);
        expected.CopyTo(0, buffer, 0, length);

        byte* data = WindowsRuntimeBufferMarshal.GetDataUnsafe(buffer);

        Assert.IsTrue(WindowsRuntimeBufferMarshal.TryGetDataUnsafe(buffer, out byte* expectedData));
        Assert.IsTrue(data == expectedData);
        CollectionAssert.AreEqual(expected, new ReadOnlySpan<byte>(data, length).ToArray());

        GC.KeepAlive(buffer);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public unsafe void GetDataUnsafe_ManagedBuffer_ReturnsUnderlyingData(bool pinned)
    {
        byte[] source = [1, 2, 3, 4];
        IBuffer buffer = pinned
            ? WindowsRuntimeBuffer.Create(source, 1, 2, 3)
            : source.AsBuffer(1, 2, 3);

        byte* data = WindowsRuntimeBufferMarshal.GetDataUnsafe(buffer);

        Assert.IsTrue(WindowsRuntimeBufferMarshal.TryGetDataUnsafe(buffer, out byte* expectedData));
        Assert.IsTrue(data == expectedData);
        CollectionAssert.AreEqual(new byte[] { 2, 3 }, new ReadOnlySpan<byte>(data, 2).ToArray());

        data[0] = 42;

        Assert.AreEqual((byte)42, buffer.GetByte(0));

        GC.KeepAlive(buffer);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public unsafe void GetDataUnsafe_EmptyManagedBuffer_ReturnsUnderlyingData(bool pinned)
    {
        IBuffer buffer = pinned
            ? WindowsRuntimeBuffer.Create(0)
            : Array.Empty<byte>().AsBuffer();

        byte* data = WindowsRuntimeBufferMarshal.GetDataUnsafe(buffer);

        Assert.IsTrue(WindowsRuntimeBufferMarshal.TryGetDataUnsafe(buffer, out byte* expectedData));
        Assert.IsTrue(data == expectedData);

        GC.KeepAlive(buffer);
    }

    [TestMethod]
    public unsafe void GetDataUnsafe_NullBuffer_ThrowsArgumentNullException()
    {
        Assert.IsFalse(WindowsRuntimeBufferMarshal.TryGetDataUnsafe(null, out byte* data));
        Assert.IsTrue(data == null);

        ArgumentNullException exception = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            WindowsRuntimeBufferMarshal.GetDataUnsafe(null!);
        });

        Assert.AreEqual("buffer", exception.ParamName);
    }

    [TestMethod]
    public unsafe void GetDataUnsafe_UnsupportedBuffer_ThrowsArgumentException()
    {
        IBuffer buffer = new UnsupportedBuffer();

        Assert.IsFalse(WindowsRuntimeBufferMarshal.TryGetDataUnsafe(buffer, out byte* data));
        Assert.IsTrue(data == null);

        ArgumentException exception = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            WindowsRuntimeBufferMarshal.GetDataUnsafe(buffer);
        });

        Assert.AreEqual("buffer", exception.ParamName);
    }

    [TestMethod]
    [DataRow(0u)]
    [DataRow(256u)]
    public unsafe void GetDataUnsafe_MemoryBufferReference_ReturnsUnderlyingDataAndCapacity(uint expectedCapacity)
    {
        using var buffer = new MemoryBuffer(expectedCapacity);
        using var reference = buffer.CreateReference();

        byte* data = WindowsRuntimeBufferMarshal.GetDataUnsafe(reference, out uint capacity);

        Assert.AreEqual(expectedCapacity, capacity);
        Assert.IsTrue(WindowsRuntimeBufferMarshal.TryGetDataUnsafe(reference, out byte* expectedData, out uint capacityFromTryGet));
        Assert.IsTrue(data == expectedData);
        Assert.AreEqual(expectedCapacity, capacityFromTryGet);

        if (capacity > 0)
        {
            data[0] = 42;
            data[capacity - 1] = 84;

            Assert.AreEqual((byte)42, expectedData[0]);
            Assert.AreEqual((byte)84, expectedData[capacity - 1]);
        }

        GC.KeepAlive(reference);
    }

    [TestMethod]
    public unsafe void GetDataUnsafe_NullMemoryBufferReference_ThrowsArgumentNullException()
    {
        Assert.IsFalse(WindowsRuntimeBufferMarshal.TryGetDataUnsafe(null, out byte* data, out uint capacity));
        Assert.IsTrue(data == null);
        Assert.AreEqual(0u, capacity);

        ArgumentNullException exception = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            WindowsRuntimeBufferMarshal.GetDataUnsafe(null!, out _);
        });

        Assert.AreEqual("buffer", exception.ParamName);
    }

    [TestMethod]
    public unsafe void GetDataUnsafe_UnsupportedMemoryBufferReference_ThrowsArgumentException()
    {
        using IMemoryBufferReference reference = new UnsupportedMemoryBufferReference();

        Assert.IsFalse(WindowsRuntimeBufferMarshal.TryGetDataUnsafe(reference, out byte* data, out uint capacity));
        Assert.IsTrue(data == null);
        Assert.AreEqual(0u, capacity);

        ArgumentException exception = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            WindowsRuntimeBufferMarshal.GetDataUnsafe(reference, out _);
        });

        Assert.AreEqual("buffer", exception.ParamName);
    }

    [WindowsRuntimeManagedOnlyType]
    private sealed class UnsupportedBuffer : IBuffer
    {
        public uint Capacity => 0;

        public uint Length { get; set; }
    }

    [WindowsRuntimeManagedOnlyType]
    private sealed class UnsupportedMemoryBufferReference : IMemoryBufferReference
    {
        public uint Capacity => 0;

        public event EventHandler<IMemoryBufferReference, object> Closed
        {
            add { }
            remove { }
        }

        public void Dispose()
        {
        }
    }
}
