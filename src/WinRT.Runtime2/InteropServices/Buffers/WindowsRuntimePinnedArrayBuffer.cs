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

    /// <summary>
    /// Gets the array of bytes in the buffer.
    /// </summary>
    /// <see href="https://learn.microsoft.com/windows/win32/api/robuffer/nf-robuffer-ibufferbyteaccess-buffer"/>
    internal unsafe byte* Buffer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => &((byte*)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(_pinnedData)))[(uint)_offset];
    }

    /// <summary>
    /// Gets an <see cref="ArraySegment{T}"/> value for the underlying data in this buffer.
    /// </summary>
    /// <returns>The resulting <see cref="ArraySegment{T}"/> value.</returns>
    internal ArraySegment<byte> GetArraySegment()
    {
        return new(_pinnedData, _offset, _length);
    }
}
