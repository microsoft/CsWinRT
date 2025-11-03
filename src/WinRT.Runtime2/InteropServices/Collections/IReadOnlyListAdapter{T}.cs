// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A stateless adapter for <see cref="IReadOnlyList{T}"/>, to be exposed as <c>Windows.Foundation.Collections.IVectorView&lt;T&gt;</c>.
/// </summary>
/// <typeparam name="T">The type of objects to enumerate.</typeparam>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivectorview-1"/>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
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
        IReadOnlyListAdapterHelpers.EnsureIndexInValidRange(index, list.Count);

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
        //   and any specified capacity will succeed and return zero actual elements."
        if (startIndex == count)
        {
            return 0;
        }

        IReadOnlyListAdapterHelpers.EnsureIndexInValidRange(startIndex, count);

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
