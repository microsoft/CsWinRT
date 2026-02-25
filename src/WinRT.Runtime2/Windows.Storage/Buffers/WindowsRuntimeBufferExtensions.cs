// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Windows.Storage.Streams;
using WindowsRuntime.InteropServices;

namespace Windows.Storage.Buffers;

/// <summary>
/// Provides extension methods that expose operations on <see cref="IBuffer"/> objects.
/// </summary>
public static class WindowsRuntimeBufferExtensions
{
    /// <summary>
    /// Returns an <see cref="IBuffer"/> instance that represents the specified byte array.
    /// </summary>
    /// <param name="source">The byte array to represent.</param>
    /// <returns>The resulting <see cref="IBuffer"/> instance.</returns>
    /// <remarks>
    /// The returned <see cref="IBuffer"/> instance will have <see cref="IBuffer.Capacity"/> and <see cref="IBuffer.Length"/> equal to the length of <paramref name="source"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
    public static IBuffer AsBuffer(this byte[] source)
    {
        return AsBuffer(source, 0, source.Length, source.Length);
    }

    /// <summary>
    /// Returns an <see cref="IBuffer"/> instance that represents a range of bytes in the specified byte array.
    /// </summary>
    /// <param name="source">The byte array to represent.</param>
    /// <param name="offset">The offset in <paramref name="source"/> where the range begins.</param>
    /// <param name="length">The length of the range that is represented by the <see cref="IBuffer"/> instance.</param>
    /// <returns>The resulting <see cref="IBuffer"/> instance that represents the specified range of bytes in <paramref name="source"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="offset"/> or <paramref name="length"/> are less than <c>0</c>.</exception>
    /// <exception cref="ArgumentException">Thrown if the specified range is not valid.</exception>
    public static IBuffer AsBuffer(this byte[] source, int offset, int length)
    {
        return AsBuffer(source, offset, length, length);
    }

    /// <summary>
    /// Returns an <see cref="IBuffer"/> instance that represents a range of bytes in the specified byte array and has a specified capacity.
    /// </summary>
    /// <param name="source">The byte array to represent.</param>
    /// <param name="offset">The offset in <paramref name="source"/> where the range begins.</param>
    /// <param name="length">The length of the range that is represented by the <see cref="IBuffer"/> instance.</param>
    /// <param name="capacity">The value to use for the <see cref="IBuffer.Capacity"/> property on the returned instance.</param>
    /// <returns>The resulting <see cref="IBuffer"/> instance that represents the specified range of bytes in <paramref name="source"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="offset"/>, <paramref name="length"/>, or <paramref name="capacity"/> are less than <c>0</c>.</exception>
    /// <exception cref="ArgumentException">Thrown if the specified range is not valid, or if <paramref name="capacity"/> is less than the specified range.</exception>
    public static IBuffer AsBuffer(this byte[] source, int offset, int length, int capacity)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        //if (source.Length - offset < length) throw new ArgumentException(global::Windows.Storage.Streams.SR.Argument_InsufficientArrayElementsAfterOffset);
        //if (source.Length - offset < capacity) throw new ArgumentException(global::Windows.Storage.Streams.SR.Argument_InsufficientArrayElementsAfterOffset);
        //if (capacity < length) throw new ArgumentException(global::Windows.Storage.Streams.SR.Argument_InsufficientBufferCapacity);

        return new WindowsRuntimeExternalArrayBuffer(source, offset, length, capacity);
    }
}
