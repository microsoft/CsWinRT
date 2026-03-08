// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Windows.Storage.Streams;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Provides a managed implementation of the <see cref="IBuffer"/> interface backed by a pinned
/// pointer to memory. This buffer does not own the underlying memory and can be invalidated to
/// prevent further access once the memory it points to is no longer guaranteed to be pinned.
/// </summary>
[WindowsRuntimeManagedOnlyType]
internal sealed unsafe class WindowsRuntimePinnedMemoryBuffer : IBuffer
{
    /// <summary>
    /// The pointer to the pinned memory.
    /// </summary>
    private volatile nint _data;

    /// <summary>
    /// The number of bytes that can be read or written in the buffer.
    /// </summary>
    private int _length;

    /// <summary>
    /// The capacity of the buffer.
    /// </summary>
    private readonly int _capacity;

    /// <summary>
    /// Creates a <see cref="WindowsRuntimePinnedMemoryBuffer"/> instance with the specified parameters.
    /// </summary>
    /// <param name="data">The pointer to the pinned memory.</param>
    /// <param name="length">The number of bytes.</param>
    /// <param name="capacity">The maximum number of bytes the buffer can hold.</param>
    /// <remarks>This constructor doesn't validate any of its parameters.</remarks>
    public WindowsRuntimePinnedMemoryBuffer(byte* data, int length, int capacity)
    {
        Debug.Assert(data is not null);
        Debug.Assert(length >= 0);
        Debug.Assert(capacity >= 0);
        Debug.Assert(capacity >= length);

        _data = (nint)data;
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

    /// <inheritdoc cref="WindowsRuntimePinnedArrayBuffer.Buffer"/>
    /// <exception cref="InvalidOperationException">Thrown if the buffer has been invalidated.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte* Buffer()
    {
        byte* data = (byte*)_data;

        InvalidOperationException.ThrowIfBufferIsInvalidated(data);

        return data;
    }

    /// <inheritdoc cref="WindowsRuntimePinnedArrayBuffer.GetSpanForCapacity"/>
    /// <exception cref="InvalidOperationException">Thrown if the buffer has been invalidated.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> GetSpanForCapacity()
    {
        byte* data = (byte*)_data;

        InvalidOperationException.ThrowIfBufferIsInvalidated(data);

        return new(data, _capacity);
    }

    /// <summary>
    /// Invalidates the buffer, preventing any further access to the underlying memory.
    /// </summary>
    /// <remarks>
    /// <para>
    /// After calling this method, any attempt to call <see cref="Buffer"/> will throw
    /// an <see cref="InvalidOperationException"/>. This is used to prevent use-after-free
    /// scenarios when the buffer wraps memory that is only temporarily pinned (e.g. a span).
    /// </para>
    /// <para>
    /// This type intentionally does not implement <see cref="IDisposable"/> to perform this
    /// invalidation. This is because <see cref="IDisposable"/> would end up in the CCW interface
    /// list for this <see cref="IBuffer"/> implementation, which is not desirable since this type
    /// is only meant to be used from the managed side in a specific, controlled context.
    /// </para>
    /// </remarks>
    public void Invalidate()
    {
        _data = default;
    }
}
