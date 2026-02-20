// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.Storage.Streams;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Provides a managed implementation of the <see cref="IBuffer"/> interface backed by an external array.
/// </summary>
[WindowsRuntimeManagedOnlyType]
internal sealed unsafe class WindowsRuntimeExternalArrayBuffer : IBuffer
{
    /// <summary>
    /// The external array to use.
    /// </summary>
    private readonly byte[] _data;

    /// <summary>
    /// The offset in <see cref="_data"/>.
    /// </summary>
    private readonly int _offset;

    /// <summary>
    /// The number of bytes that can be read or written in <see cref="_data"/>.
    /// </summary>
    private int _length;

    /// <summary>
    /// The capacity of the buffer.
    /// </summary>
    private readonly int _capacity;

    /// <summary>
    /// The pinned GC handle to pin <see cref="_data"/>.
    /// </summary>
    private PinnedGCHandle<byte[]> _pinnedHandle;

    /// <summary>
    /// The address of the pinned data, at the right offset, if <see cref="_pinnedHandle"/> has been allocated.
    /// </summary>
    private nint _pinnedDataPtr;

    /// <summary>
    /// Creates a <see cref="WindowsRuntimeExternalArrayBuffer"/> instance with the specified parameters.
    /// </summary>
    /// <param name="data">The external array to wrap.</param>
    /// <param name="offset">The offset in <paramref name="data"/>.</param>
    /// <param name="length">The number of bytes.</param>
    /// <param name="capacity">The maximum number of bytes the buffer can hold.</param>
    /// <remarks>This constructor doesn't validate any of its parameters.</remarks>
    public WindowsRuntimeExternalArrayBuffer(byte[] data, int offset, int length, int capacity)
    {
        Debug.Assert(data is not null);
        Debug.Assert(offset >= 0);
        Debug.Assert(length >= 0);
        Debug.Assert(capacity >= 0);
        Debug.Assert(data.Length - offset >= length);
        Debug.Assert(data.Length - offset >= capacity);
        Debug.Assert(capacity >= length);

        _data = data;
        _offset = offset;
        _length = length;
        _capacity = capacity;
        _pinnedHandle = default;
        _pinnedDataPtr = 0;
    }

    /// <summary>
    /// Finalizes the current <see cref="WindowsRuntimeExternalArrayBuffer"/> instance.
    /// </summary>
    ~WindowsRuntimeExternalArrayBuffer()
    {
        _pinnedHandle.Dispose();
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

            _length = unchecked((int)value);
        }
    }

    /// <inheritdoc cref="WindowsRuntimePinnedArrayBuffer.Buffer"/>
    public byte* Buffer()
    {
        // Try to get the (already computed) pointer to the offset data
        nint pinnedDataPtr = Volatile.Read(ref _pinnedDataPtr);

        // If the pointer is not 'null', it means another caller has already
        // called this method, and subsequently caused the array to be pinned.
        if (pinnedDataPtr != 0)
        {
            return (byte*)pinnedDataPtr;
        }

        // If we're potentially the first ones to get here, then try to pin the array.
        // This method will return the right pointer with the offset already computed.
        return PinData();
    }

    /// <inheritdoc cref="WindowsRuntimePinnedArrayBuffer.GetArraySegment"/>
    public ArraySegment<byte> GetArraySegment()
    {
        return new(_data, _offset, _length);
    }

    /// <summary>
    /// Pins <see cref="_data"/> and returns the address of its data at the right offset.
    /// </summary>
    /// <returns>The address of the pinned data to use.</returns>
    private byte* PinData()
    {
        PinnedGCHandle<byte[]> pinnedHandle = default;
        nint pinnedDataPtr;
        bool wasPinnedDataPtrWritten = false;

        try
        {
            // Pin the data array
            pinnedHandle = new PinnedGCHandle<byte[]>(_data);
            pinnedDataPtr = (nint)(&pinnedHandle.GetAddressOfArrayData()[_offset]);

            // Store the address only if it hasn't been assigned yet. This operation is why we
            // have to use 'nint' for the field instead of 'byte*', as no overload supports it.
            wasPinnedDataPtrWritten = Interlocked.CompareExchange(
                location1: ref _pinnedDataPtr,
                value: pinnedDataPtr,
                comparand: 0) == 0;
        }
        finally
        {
            // Track the pinned GC handle so that we can release it from the finalizer
            if (wasPinnedDataPtrWritten)
            {
                _pinnedHandle = pinnedHandle;
            }
            else
            {
                // If we have a race with another thread also trying to pin the array, and that thread is the
                // first one to assign the pinned data pointer, than just stop and dispose the pinned GC handle
                // we just created. It's not needed anyway, as the one from the other thread will remain active.
                pinnedHandle.Dispose();
            }
        }

        // Return the address we retrieved, it will point to the pinned array data
        return (byte*)pinnedDataPtr;
    }
}
