// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Storage.Streams;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Provides a managed implementation of the <see cref="IBuffer"/> interface backed by a pinned array.
/// </summary>
[WindowsRuntimeManagedOnlyType]
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
            ArgumentOutOfRangeException.ThrowIfBufferLengthExceedsCapacity(value, Capacity);

            _length = unchecked((int)value);
        }
    }

    /// <summary>
    /// Gets the array of bytes in the buffer.
    /// </summary>
    /// <returns>The byte array.</returns>
    /// <see href="https://learn.microsoft.com/windows/win32/api/robuffer/nf-robuffer-ibufferbyteaccess-buffer"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe byte* Buffer()
    {
        return &((byte*)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(_pinnedData)))[(uint)_offset];
    }

    /// <summary>
    /// Gets the underlying array for this buffer.
    /// </summary>
    /// <param name="offset">The offset in the returned array.</param>
    /// <returns>The underlying array.</returns>
    public byte[] GetArray(out int offset)
    {
        offset = _offset;

        return _pinnedData;
    }

    /// <summary>
    /// Gets an <see cref="ArraySegment{T}"/> value for the underlying data in this buffer.
    /// </summary>
    /// <returns>The resulting <see cref="ArraySegment{T}"/> value.</returns>
    public ArraySegment<byte> GetArraySegment()
    {
        return new(_pinnedData, _offset, _length);
    }

    /// <summary>
    /// Gets a <see cref="Span{T}"/> value for the underlying data in this buffer.
    /// </summary>
    /// <returns>The resulting <see cref="Span{T}"/> value.</returns>
    /// <remarks>
    /// The returned <see cref="Span{T}"/> value has a length equal to <see cref="Capacity"/>, not <see cref="Length"/>.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> GetSpanForCapacity()
    {
        ref byte pinnedData = ref MemoryMarshal.GetArrayDataReference(_pinnedData);

        // All parameters have been validated before constructing this object,
        // so we can avoid the overhead of checking the offset and capaciity.
        return MemoryMarshal.CreateSpan(ref Unsafe.Add(ref pinnedData, _offset), _capacity);
    }
}
