// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Windows.Storage.Streams;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Provides a way to create managed instances of the <see cref="IBuffer"/> interface.
/// </summary>
public static class WindowsRuntimeBuffer
{
    /// <summary>
    /// Creates an empty <see cref="IBuffer"/> instance with a given maximum capacity, and its contents set to zero.
    /// </summary>
    /// <param name="capacity">The maximum number of bytes the buffer can hold.</param>
    /// <returns>The resulting <see cref="IBuffer"/> instance (with the specified capacity and <see cref="IBuffer.Length"/> equal to <c>0</c>).</returns>
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
    /// <returns>The resulting <see cref="IBuffer"/> instance (with <see cref="IBuffer.Capacity"/> and <see cref="IBuffer.Length"/> equal to the length of <paramref name="data"/>).</returns>
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
