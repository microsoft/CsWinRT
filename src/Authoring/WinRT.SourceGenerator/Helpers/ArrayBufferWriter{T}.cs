// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace Generator;

/// <summary>
/// A helper type to build sequences of values with pooled buffers.
/// </summary>
/// <typeparam name="T">The type of items to create sequences for.</typeparam>
internal sealed class ArrayBufferWriter<T> : IList<T>, IReadOnlyList<T>
{
    /// <summary>
    /// The underlying <typeparamref name="T"/> array.
    /// </summary>
    private T[] _array;

    /// <summary>
    /// The starting offset within <see cref="_array"/>.
    /// </summary>
    private int _index;

    /// <summary>
    /// Creates a new <see cref="ArrayBufferWriter{T}"/> instance with the specified parameters.
    /// </summary>
    public ArrayBufferWriter()
    {
        if (typeof(T) == typeof(char))
        {
            _array = new T[1024];
        }
        else
        {
            _array = new T[8];
        }

        _index = 0;
    }

    /// <inheritdoc/>
    public int Count => _index;

    /// <summary>
    /// Gets the data written to the underlying buffer so far, as a <see cref="ReadOnlySpan{T}"/>.
    /// </summary>
    public ReadOnlySpan<T> WrittenSpan
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_array, 0, _index);
    }

    /// <inheritdoc/>
    bool ICollection<T>.IsReadOnly => true;

    /// <inheritdoc/>
    T IReadOnlyList<T>.this[int index] => WrittenSpan[index];

    /// <inheritdoc/>
    T IList<T>.this[int index]
    {
        get => WrittenSpan[index];
        set => throw new NotSupportedException();
    }

    /// <summary>
    /// Advances the current writer and gets a <see cref="Span{T}"/> to the requested memory area.
    /// </summary>
    /// <param name="requestedSize">The requested size to advance by.</param>
    /// <returns>A <see cref="Span{T}"/> to the requested memory area.</returns>
    /// <remarks>
    /// No other data should be written to the builder while the returned <see cref="Span{T}"/>
    /// is in use, as it could invalidate the memory area wrapped by it, if resizing occurs.
    /// </remarks>
    public Span<T> Advance(int requestedSize)
    {
        EnsureCapacity(requestedSize);

        Span<T> span = _array.AsSpan(_index, requestedSize);

        _index += requestedSize;

        return span;
    }

    /// <inheritdoc cref="ImmutableArray{T}.Builder.Add(T)"/>
    public void Add(T value)
    {
        EnsureCapacity(1);

        _array[_index++] = value;
    }

    // <summary>
    /// Adds the specified items to the end of the array.
    /// </summary>
    /// <param name="items">The items to add at the end of the array.</param>
    public void AddRange(ReadOnlySpan<T> items)
    {
        EnsureCapacity(items.Length);

        items.CopyTo(_array.AsSpan(_index));

        _index += items.Length;
    }

    /// <summary>
    /// Inserts an item to the builder at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
    /// <param name="item">The object to insert into the current instance.</param>
    public void Insert(int index, T item)
    {
        if (index < 0 || index > _index)
        {
            ImmutableArrayBuilder.ThrowArgumentOutOfRangeExceptionForIndex();
        }

        EnsureCapacity(1);

        if (index < _index)
        {
            Array.Copy(_array, index, _array, index + 1, _index - index);
        }

        _array[index] = item;
        _index++;
    }

    /// <summary>
    /// Clears the items in the current writer.
    /// </summary>
    public void Clear()
    {
        if (typeof(T) != typeof(byte) &&
            typeof(T) != typeof(char) &&
            typeof(T) != typeof(int))
        {
            _array.AsSpan(0, _index).Clear();
        }

        _index = 0;
    }

    /// <summary>
    /// Ensures that <see cref="_array"/> has enough free space to contain a given number of new items.
    /// </summary>
    /// <param name="requestedSize">The minimum number of items to ensure space for in <see cref="_array"/>.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureCapacity(int requestedSize)
    {
        if (requestedSize > _array.Length - _index)
        {
            ResizeBuffer(requestedSize);
        }
    }

    /// <summary>
    /// Resizes <see cref="_array"/> to ensure it can fit the specified number of new items.
    /// </summary>
    /// <param name="sizeHint">The minimum number of items to ensure space for in <see cref="_array"/>.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ResizeBuffer(int sizeHint)
    {
        int minimumSize = _index + sizeHint;
        int requestedSize = Math.Max(_array.Length * 2, minimumSize);

        T[] newArray = new T[requestedSize];

        Array.Copy(_array, newArray, _index);

        _array = newArray;
    }

    /// <inheritdoc/>
    int IList<T>.IndexOf(T item)
    {
        return Array.IndexOf(_array, item, 0, _index);
    }

    /// <inheritdoc/>
    void IList<T>.RemoveAt(int index)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc/>
    bool ICollection<T>.Contains(T item)
    {
        return Array.IndexOf(_array, item, 0, _index) >= 0;
    }

    /// <inheritdoc/>
    void ICollection<T>.CopyTo(T[] array, int arrayIndex)
    {
        Array.Copy(_array, 0, array, arrayIndex, _index);
    }

    /// <inheritdoc/>
    bool ICollection<T>.Remove(T item)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc/>
    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        T?[] array = _array!;
        int length = _index;

        for (int i = 0; i < length; i++)
        {
            yield return array[i]!;
        }
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable<T>)this).GetEnumerator();
    }
}

/// <summary>
/// Private helpers for the <see cref="ImmutableArrayBuilder{T}"/> type.
/// </summary>
file static class ImmutableArrayBuilder
{
    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException"/> for <c>"index"</c>.
    /// </summary>
    public static void ThrowArgumentOutOfRangeExceptionForIndex()
    {
        throw new ArgumentOutOfRangeException("index");
    }
}