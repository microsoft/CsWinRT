// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A stateless adapter for <see cref="IList{T}"/>, to be exposed as <c>Windows.Foundation.Collections.IVector&lt;T&gt;</c>.
/// </summary>
/// <typeparam name="T">The type of objects to enumerate.</typeparam>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivector-1"/>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IListAdapter<T>
{
    /// <summary>
    /// Returns the item at the specified index in the vector.
    /// </summary>
    /// <param name="list">The wrapped <see cref="IList{T}"/> instance.</param>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The item at the specified index.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivector-1.getat"/>
    public static T GetAt(IList<T> list, uint index)
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
    /// Gets the number of items in the vector.
    /// </summary>
    /// <param name="list">The wrapped <see cref="IList{T}"/> instance.</param>
    /// <returns>The number of items in the vector.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivector-1.size"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Size(IList<T> list)
    {
        return (uint)list.Count;
    }

    /// <summary>
    /// Returns an immutable view of the vector.
    /// </summary>
    /// <param name="list">The wrapped <see cref="IList{T}"/> instance.</param>
    /// <returns>The view of the vector.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivector-1.getview"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IReadOnlyList<T> GetView(IList<T> list)
    {
        // This list is not really read-only: once marshalled, native code could do
        // a 'QueryInterface' call back to 'IVector<T>', which would succeed, and would
        // return a modifiable reference for this view. We believe this is accetable, as
        // it allows us to gain some performance. For instance, in most situations (because
        // pretty much all built-in .NET collection types implementing 'IList<T>' also implement
        // 'IReadOnlyList<T>'), this allows us to not allocate anything. That is, when native
        // code calls 'GetView()', it would get back the same CCW instance, just through a
        // 'QueryInterface' call for 'IVectorView<T>' instead.
        return list as IReadOnlyList<T> ?? new ReadOnlyCollection<T>(list);
    }

    /// <summary>
    /// Retrieves the index of a specified item in the vector.
    /// </summary>
    /// <param name="list">The wrapped <see cref="IList{T}"/> instance.</param>
    /// <param name="value">The item to find in the vector.</param>
    /// <param name="index">If the item is found, this is the zero-based index of the item; otherwise, this parameter is <c>0</c>.</param>
    /// <returns><see langword="true"/> if the item is found; otherwise, <see langword="false"/>.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivector-1.indexof"/>
    public static bool IndexOf(IList<T> list, T value, out uint index)
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

        // Same as 'IndexOf' for 'IReadOnlyList<T>', see notes there
        index = 0;

        return false;
    }

    /// <summary>
    /// Retrieves multiple items from the vector view beginning at the given index.
    /// </summary>
    /// <param name="list">The wrapped <see cref="IList{T}"/> instance.</param>
    /// <param name="startIndex">The zero-based index to start at.</param>
    /// <param name="items">The target <see cref="Span{T}"/> to write items into.</param>
    /// <returns>The number of items that were retrieved. This value can be less than the size of <paramref name="items"/> if the end of the list is reached.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivector-1.getmany"/>
    public static int GetMany(IList<T> list, uint startIndex, Span<T> items)
    {
        int count = list.Count;

        // See notes in 'GetMany' for 'IReadOnlyList<T>'
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