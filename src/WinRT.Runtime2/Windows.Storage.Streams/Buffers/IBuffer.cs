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
/// Provides a managed implementation of the <see cref="IBuffer"/> interface.
/// </summary>
[WindowsRuntimeManagedOnlyType]
public sealed class WindowsRuntimeBuffer : IBuffer
{
    public static IBuffer Create(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);

        return new WindowsRuntimeBuffer(capacity);
    }

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

        return new WindowsRuntimeBuffer(pinnedData, offset: 0, length, capacity);
    }

    private readonly byte[] _pinnedData;
    private readonly int _offset;
    private int _length;
    private readonly int _capacity;

    private WindowsRuntimeBuffer(int capacity)
    {
        Debug.Assert(capacity >= 0);

        _pinnedData = GC.AllocateArray<byte>(capacity, pinned: true);
        _offset = 0;
        _length = 0;
        _capacity = capacity;
    }

    private WindowsRuntimeBuffer(byte[] pinnedData, int offset, int length, int capacity)
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
