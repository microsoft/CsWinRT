// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A stateless adapter for <see cref="IReadOnlyList{T}"/>, to be exposed as <c>Windows.Foundation.Collections.IVectorView&lt;T&gt;</c>.
/// </summary>
/// <typeparam name="T">The type of objects to enumerate.</typeparam>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivectorview-1"/>
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
        ArgumentOutOfRangeException.ThrowIfIndexLargerThanMaxValue(index, list.Count);

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
    /// Gets the number of items in the vector view.
    /// </summary>
    /// <param name="list">The wrapped <see cref="IReadOnlyList{T}"/> instance.</param>
    /// <returns>The number of items in the vector view.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivectorview-1.size"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Size(IReadOnlyList<T> list)
    {
        return (uint)list.Count;
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
}
#endif
