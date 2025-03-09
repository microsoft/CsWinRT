// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A pooled <see cref="IBufferWriter{T}"/> implementation for <see cref="ComWrappers.ComInterfaceEntry"/> values,
/// which can be used to efficiently prepare vtables for types to marshal to Windows Runtime APIs.
/// </summary>
internal sealed class PooledComInterfaceEntryBufferWriter : IBufferWriter<ComWrappers.ComInterfaceEntry>
{
    /// <summary>
    /// The default buffer size to use to expand empty arrays.
    /// </summary>
    private const int DefaultInitialBufferSize = 32;

    /// <summary>
    /// The underlying <see cref="ComWrappers.ComInterfaceEntry"/> array.
    /// </summary>
    private ComWrappers.ComInterfaceEntry[]? _array;

    /// <summary>
    /// The starting offset within <see cref="_array"/>.
    /// </summary>
    private int _index;

    /// <summary>
    /// Initializes a new instance of the <see cref="PooledComInterfaceEntryBufferWriter"/> class.
    /// </summary>
    public PooledComInterfaceEntryBufferWriter()
    {
        // Since we're using pooled arrays, we can rent the buffer with the default size immediately,
        // we don't need to use lazy initialization to save unnecessary memory allocations in this case.
        _array = ArrayPool<ComWrappers.ComInterfaceEntry>.Shared.Rent(DefaultInitialBufferSize);
        _index = 0;
    }

    /// <summary>
    /// Gets a <see cref="ReadOnlySpan{T}"/> value with all written elements.
    /// </summary>
    public ReadOnlySpan<ComWrappers.ComInterfaceEntry> WrittenSpan
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ComWrappers.ComInterfaceEntry[]? array = _array;

            ObjectDisposedException.ThrowIf(array is null, this);

            return array.AsSpan(0, _index);
        }
    }

    /// <inheritdoc/>
    public void Advance(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        ComWrappers.ComInterfaceEntry[]? array = _array;

        ObjectDisposedException.ThrowIf(array is null, this);

        if (_index > array!.Length - count)
        {
            // Throw helper for better codegen
            [DoesNotReturn]
            static void ThrowArgumentExceptionForAdvancedTooFar() => throw new ArgumentException("The buffer writer has advanced too far.");

            ThrowArgumentExceptionForAdvancedTooFar();
        }

        _index += count;
    }

    /// <inheritdoc/>
    public Memory<ComWrappers.ComInterfaceEntry> GetMemory(int sizeHint = 0)
    {
        CheckBufferAndEnsureCapacity(sizeHint);

        return _array.AsMemory(_index);
    }

    /// <inheritdoc/>
    public Span<ComWrappers.ComInterfaceEntry> GetSpan(int sizeHint = 0)
    {
        CheckBufferAndEnsureCapacity(sizeHint);

        return _array.AsSpan(_index);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        ComWrappers.ComInterfaceEntry[]? array = _array;

        if (array is null)
        {
            return;
        }

        _array = null;

        ArrayPool<ComWrappers.ComInterfaceEntry>.Shared.Return(array);
    }

    /// <summary>
    /// Ensures that <see cref="_array"/> has enough free space to contain a given number of new items.
    /// </summary>
    /// <param name="sizeHint">The minimum number of items to ensure space for in <see cref="_array"/>.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckBufferAndEnsureCapacity(int sizeHint)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sizeHint);

        ComWrappers.ComInterfaceEntry[]? array = _array;

        ObjectDisposedException.ThrowIf(array is null, this);

        if (sizeHint == 0)
        {
            sizeHint = 1;
        }

        if (sizeHint > array.Length - _index)
        {
            // Helper to grow the pooled array
            [MethodImpl(MethodImplOptions.NoInlining)]
            void ResizeBuffer(int sizeHint)
            {
                uint minimumSize = (uint)_index + (uint)sizeHint;

                ComWrappers.ComInterfaceEntry[] oldArray = _array!;
                ComWrappers.ComInterfaceEntry[] newArray = ArrayPool<ComWrappers.ComInterfaceEntry>.Shared.Rent((int)minimumSize);

                // Copy all written items to the new array
                Array.Copy(oldArray, newArray, _index);

                ArrayPool<ComWrappers.ComInterfaceEntry>.Shared.Return(oldArray);

                _array = newArray;
            }

            ResizeBuffer(sizeHint);
        }
    }
}
