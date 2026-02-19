// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Storage.Streams;
using WindowsRuntime.InteropServices;

namespace Windows.Storage.Buffers;

/// <summary>
/// Provides a way to create managed instances of the <see cref="IBuffer"/> interface.
/// </summary>
public static class WindowsRuntimeBuffer
{
    /// <summary>
    /// Creates an empty <see cref="IBuffer"/> instance with a given maximum capacity, and its contents set to zero.
    /// </summary>
    /// <param name="capacity">The maximum number of bytes the buffer can hold.</param>
    /// <returns>The resulting <see cref="IBuffer"/> instance (with the specified capacity and <see cref="Length"/> equal to <c>0</c>).</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="capacity"/> is less than <c>0</c>.</exception>
    public static IBuffer Create(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);

        return new WindowsRuntimePinnedArrayBuffer(capacity);
    }

    /// <summary>
    /// Creates an <see cref="IBuffer"/> instance that contains a specified range of bytes copied from an input <see cref="ReadOnlySpan{T}"/> value.
    /// </summary>
    /// <param name="data">The <see cref="ReadOnlySpan{T}"/> value to copy from.</param>
    /// <returns>The resulting <see cref="IBuffer"/> instance (with <see cref="Capacity"/> and <see cref="Length"/> equal to the length of <paramref name="data"/>).</returns>
    public static IBuffer Create(ReadOnlySpan<byte> data)
    {
        byte[] pinnedData = GC.AllocateArray<byte>(data.Length, pinned: true);

        data.CopyTo(pinnedData);

        return new WindowsRuntimePinnedArrayBuffer(pinnedData, offset: 0, data.Length, data.Length);
    }

    /// <summary>
    /// Creates an <see cref="IBuffer"/> instance that contains a specified range of bytes copied from an input <see cref="ReadOnlySpan{T}"/>
    /// value. If the specified capacity is greater than the number of bytes copied, the rest of the buffer is zero-filled.
    /// </summary>
    /// <param name="data">The <see cref="ReadOnlySpan{T}"/> value to copy from.</param>
    /// <param name="capacity">The maximum number of bytes the buffer can hold (if this is greater than the length of <paramref name="data"/>, the rest of the bytes in the buffer are initialized to <c>0</c>).</param>
    /// <returns>The resulting <see cref="IBuffer"/> instance that contains the specified range of bytes (if the specified capacity is greater than the length of <paramref name="data"/>, the rest of the buffer is zero-filled).</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="capacity"/> is less than <c>0</c>.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="capacity"/> is less than the length of <paramref name="data"/>.</exception>
    public static IBuffer Create(ReadOnlySpan<byte> data, int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        //if (capacity < length) throw new ArgumentException(global::Windows.Foundation.SR.Argument_InsufficientBufferCapacity);

        byte[] pinnedData = GC.AllocateArray<byte>(capacity, pinned: true);

        data.CopyTo(pinnedData);

        return new WindowsRuntimePinnedArrayBuffer(pinnedData, offset: 0, data.Length, capacity);
    }

    /// <summary>
    /// Creates an <see cref="IBuffer"/> instance that contains a specified range of bytes copied from an input array.
    /// If the specified capacity is greater than the number of bytes copied, the rest of the buffer is zero-filled.
    /// </summary>
    /// <param name="data">The array to copy from.</param>
    /// <param name="offset">The offset in <paramref name="data"/> from which copying begins.</param>
    /// <param name="length">The number of bytes to copy.</param>
    /// <param name="capacity">The maximum number of bytes the buffer can hold (if this is greater than the length of <paramref name="data"/>, the rest of the bytes in the buffer are initialized to <c>0</c>).</param>
    /// <returns>The resulting <see cref="IBuffer"/> instance that contains the specified range of bytes (if the specified capacity is greater than the length of <paramref name="data"/>, the rest of the buffer is zero-filled).</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="data"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="offset"/>, <paramref name="length"/>, or <paramref name="capacity"/> are less than <c>0</c>.</exception>
    /// <exception cref="ArgumentException">Thrown if the specified range is not valid, or if <paramref name="capacity"/> is less than the specified range.</exception>
    public static IBuffer Create(byte[] data, int offset, int length, int capacity)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        //if (data.Length - offset < length) throw new ArgumentException(global::Windows.Foundation.SR.Argument_InsufficientArrayElementsAfterOffset);
        //if (data.Length - offset < capacity) throw new ArgumentException(global::Windows.Foundation.SR.Argument_InsufficientArrayElementsAfterOffset);
        //if (capacity < length) throw new ArgumentException(global::Windows.Foundation.SR.Argument_InsufficientBufferCapacity);

        byte[] pinnedData = GC.AllocateArray<byte>(capacity, pinned: true);

        Array.Copy(
            sourceArray: data,
            sourceIndex: offset,
            destinationArray: pinnedData,
            destinationIndex: 0,
            length: length);

        return new WindowsRuntimePinnedArrayBuffer(pinnedData, offset: 0, length, capacity);
    }
}

/// <summary>
/// Provides a managed implementation of the <see cref="IBuffer"/> interface backed by a pinned array.
/// </summary>
internal sealed class WindowsRuntimePinnedArrayBuffer : IBuffer
{
    /// <summary>
    /// The pinned array to use.
    /// </summary>
    private readonly byte[] _pinnedData;

    /// <summary>
    /// The offset in <see cref="_pinnedData"/>.
    /// </summary>
    private readonly int _offset;

    /// <summary>
    /// The number of bytes that can be read or written in <see cref="_pinnedData"/>.
    /// </summary>
    private int _length;

    /// <summary>
    /// The capacity of the buffer.
    /// </summary>
    private readonly int _capacity;

    /// <summary>
    /// Creates a <see cref="WindowsRuntimePinnedArrayBuffer"/> instance with the specified parameters.
    /// </summary>
    /// <param name="capacity">The maximum number of bytes the buffer can hold.</param>
    /// <remarks>This constructor doesn't validate any of its parameters.</remarks>
    public WindowsRuntimePinnedArrayBuffer(int capacity)
    {
        Debug.Assert(capacity >= 0);

        _pinnedData = GC.AllocateArray<byte>(capacity, pinned: true);
        _offset = 0;
        _length = 0;
        _capacity = capacity;
    }

    /// <summary>
    /// Creates a <see cref="WindowsRuntimePinnedArrayBuffer"/> instance with the specified parameters.
    /// </summary>
    /// <param name="pinnedData">The pinned array to wrap.</param>
    /// <param name="offset">The offset in <paramref name="pinnedData"/>.</param>
    /// <param name="length">The number of bytes.</param>
    /// <param name="capacity">The maximum number of bytes the buffer can hold.</param>
    /// <remarks>This constructor doesn't validate any of its parameters.</remarks>
    public WindowsRuntimePinnedArrayBuffer(byte[] pinnedData, int offset, int length, int capacity)
    {
        Debug.Assert(pinnedData is not null);
        Debug.Assert(offset >= 0);
        Debug.Assert(length >= 0);
        Debug.Assert(capacity >= 0);
        Debug.Assert(pinnedData.Length - offset >= length);
        Debug.Assert(pinnedData.Length - offset >= capacity);
        Debug.Assert(capacity >= length);

        _pinnedData = pinnedData;
        _offset = offset;
        _length = length;
        _capacity = capacity;
    }

    /// <inheritdoc/>
    public uint Capacity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (uint)_capacity;
    }

    /// <inheritdoc/>
    public uint Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (uint)_length;
        set
        {
            if (value > Capacity)
            {
                ArgumentOutOfRangeException ex = new(nameof(value), global::Windows.Foundation.SR.Argument_BufferLengthExceedsCapacity)
                {
                    HResult = WellKnownErrorCodes.E_BOUNDS
                };

                throw ex;
            }

            // Capacity is ensured to not exceed Int32.MaxValue, so Length is within this limit and this cast is safe:
            Debug.Assert(Capacity <= int.MaxValue);
            _length = unchecked((int)value);
        }
    }

    internal unsafe byte* Buffer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => &((byte*)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(_pinnedData)))[(uint)_offset];
    }

    internal void GetUnderlyingData(out byte[] underlyingDataArray, out int underlyingDataArrayStartOffset)
    {
        underlyingDataArray = _pinnedData;
        underlyingDataArrayStartOffset = _offset;
    }
}
