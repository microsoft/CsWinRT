﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A stateless adapter for <see cref="IReadOnlyList{T}"/>, to be exposed as <c>Windows.Foundation.Collections.IVectorView&lt;T&gt;</c>.
/// </summary>
/// <typeparam name="T">The type of objects to enumerate.</typeparam>
/// <remarks>
/// This type should only be used by generated code.
/// </remarks>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivectorview-1"/>
[Obsolete("This type is an implementation detail, and it's only meant to be consumed by 'cswinrtgen'")]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IReadOnlyListAdapter<T>
{
    /// <summary>
    /// Returns the item at the specified index in the vector view.
    /// </summary>
    /// <param name="list">The wrapped <see cref="IReadOnlyList{T}"/> instance.</param>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The item at the specified index.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivectorview-1.getat"/>
    public static T GetAt(IReadOnlyList<T> list, uint index)
    {
        IReadOnlyListAdapter.EnsureIndexInt32(index, list.Count);

        try
        {
            return list[(int)index];
        }
        catch (ArgumentOutOfRangeException e)
        {
            e.HResult = WellKnownErrorCodes.E_BOUNDS;

            throw;
        }
    }

    /// <summary>
    /// Retrieves the index of a specified item in the vector view.
    /// </summary>
    /// <param name="list">The wrapped <see cref="IReadOnlyList{T}"/> instance.</param>
    /// <param name="value">The item to find in the vector view.</param>
    /// <param name="index">If the item is found, this is the zero-based index of the item; otherwise, this parameter is <c>0</c>.</param>
    /// <returns><see langword="true"/> if the item is found; otherwise, <see langword="false"/>.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivectorview-1.indexof"/>
    public static bool IndexOf(IReadOnlyList<T> list, T value, out uint index)
    {
        int count = list.Count;

        // Scan the list and look for the target item
        for (int i = 0; i < count; i++)
        {
            if (EqualityComparer<T>.Default.Equals(value, list[i]))
            {
                index = (uint)i;

                return true;
            }
        }

        // If we didn't find the item, 'IndexOf' expects 'index' to be set to '0' (not to '-1' like in .NET)
        index = 0;

        return false;
    }

    /// <summary>
    /// Retrieves multiple items from the vector view beginning at the given index.
    /// </summary>
    /// <param name="list">The wrapped <see cref="IReadOnlyList{T}"/> instance.</param>
    /// <param name="startIndex">The zero-based index to start at.</param>
    /// <param name="items">The target <see cref="Span{T}"/> to write items into.</param>
    /// <returns>The number of items that were retrieved. This value can be less than the size of <paramref name="items"/> if the end of the list is reached.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivectorview-1.getmany"/>
    public static int GetMany(IReadOnlyList<T> list, uint startIndex, Span<T> items)
    {
        int count = list.Count;

        // The spec says:
        //   "Calling 'GetMany' with 'startIndex' equal to the length of the vector (last valid index + 1)
        //   and any specified capacity will succeed and return zero actual elements".
        if (startIndex == count)
        {
            return 0;
        }

        IReadOnlyListAdapter.EnsureIndexInt32(startIndex, count);

        // Empty spans are supported, we just stop immediately
        if (items.IsEmpty)
        {
            return 0;
        }

        int itemCount = int.Min(items.Length, count - (int)startIndex);

        // Copy all items to the target span
        for (int i = 0; i < itemCount; ++i)
        {
            items[i] = list[i + (int)startIndex];
        }

        return itemCount;
    }
}

/// <summary>
/// Non-generic helpers for <see cref="IReadOnlyListAdapter{T}"/>.
/// </summary>
file static class IReadOnlyListAdapter
{
    /// <summary>
    /// Ensures that the specified index is within the valid range for a collection of the specified count.
    /// </summary>
    /// <param name="index">The input index.</param>
    /// <param name="count">The size of the collection.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is out of range.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void EnsureIndexInt32(uint index, int count)
    {
        if (index >= (uint)count)
        {
            [DoesNotReturn]
            static void ThrowArgumentOutOfRangeException()
            {
                throw new ArgumentOutOfRangeException(nameof(index), "ArgumentOutOfRange_IndexLargerThanMaxValue")
                {
                    HResult = WellKnownErrorCodes.E_BOUNDS
                };
            }

            ThrowArgumentOutOfRangeException();
        }
    }
}
