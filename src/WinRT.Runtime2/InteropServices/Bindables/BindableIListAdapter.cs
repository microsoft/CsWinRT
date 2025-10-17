// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A stateless adapter for <see cref="IList"/>.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindablevector"/>.
internal static class BindableIListAdapter
{
    /// <summary>
    /// Returns the item at the specified index in the vector.
    /// </summary>
    /// <param name="list">The wrapped <see cref="IList"/> instance.</param>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The item at the specified index.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindablevector.getat"/>
    public static object? GetAt(IList list, uint index)
    {
        // The validation logic is the same as for 'IReadOnlyList<T>'
        IReadOnlyListAdapterHelpers.EnsureIndexInValidRange(index, list.Count);

        try
        {
            return list[(int)index];
        }
        catch (ArgumentOutOfRangeException e)
        {
            throw new Exception("ArgumentOutOfRange_Index", e) { HResult = WellKnownErrorCodes.E_BOUNDS };
        }
    }

    /// <summary>
    /// Gets the number of items in the vector.
    /// </summary>
    /// <param name="list">The wrapped <see cref="IList"/> instance.</param>
    /// <returns>The number of items in the vector.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindablevector.size"/>
    public static uint Size(IList list)
    {
        return (uint)list.Count;
    }

    /// <summary>
    /// Returns an immutable view of the vector.
    /// </summary>
    /// <param name="list">The wrapped <see cref="IList"/> instance.</param>
    /// <returns>The view of the vector.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindablevector.getview"/>
    public static BindableIReadOnlyListAdapter GetView(IList list)
    {
        return new(list);
    }

    /// <summary>
    /// Returns the index of a specified item in the vector.
    /// </summary>
    /// <param name="list">The wrapped <see cref="IList"/> instance.</param>
    /// <param name="value">The item to find in the vector.</param>
    /// <param name="index">The zero-based index of the item if found.</param>
    /// <returns>Whether the item was found.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindablevector.indexof"/>
    public static bool IndexOf(IList list, object? value, out uint index)
    {
        int result = list.IndexOf(value);

        if (result == -1)
        {
            index = 0;

            return false;
        }

        index = (uint)result;

        return true;
    }

    /// <summary>
    /// Sets the item value at the specified index of the vector.
    /// </summary>
    /// <param name="list">The wrapped <see cref="IList"/> instance.</param>
    /// <param name="index">The zero-based index of the vector, at which to set the value.</param>
    /// <param name="value">The item value to set.</param>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindablevector.setat"/>
    public static void SetAt(IList list, uint index, object? value)
    {
        // The validation logic is the same as for 'IReadOnlyList<T>'
        IReadOnlyListAdapterHelpers.EnsureIndexInValidRange(index, list.Count);

        try
        {
            list[(int)index] = value;
        }
        catch (IndexOutOfRangeException e)
        {
            throw new Exception("ArgumentOutOfRange_Index", e) { HResult = WellKnownErrorCodes.E_BOUNDS };
        }
    }

    /// <summary>
    /// Inserts an item into a vector at a specified index.
    /// </summary>
    /// <param name="list">The wrapped <see cref="IList"/> instance.</param>
    /// <param name="index">The zero-based index of the vector, at which to insert the value.</param>
    /// <param name="value">The item value to insert.</param>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindablevector.insertat"/>
    public static void InsertAt(IList list, uint index, object? value)
    {
        // Inserting at an index one past the end of the list is equivalent to appending,
        // a new item, so we need to ensure that we're within the [0, count + 1) range.
        IReadOnlyListAdapterHelpers.EnsureIndexInValidRange(index, list.Count + 1);

        try
        {
            list.Insert((int)index, value);
        }
        catch (IndexOutOfRangeException e)
        {
            e.HResult = WellKnownErrorCodes.E_BOUNDS;

            throw;
        }
    }

    /// <summary>
    /// Removes the item at the specified index in the vector.
    /// </summary>
    /// <param name="list">The wrapped <see cref="IList"/> instance.</param>
    /// <param name="index">The zero-based index of the vector, at which to remove the item.</param>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindablevector.removeat"/>
    public static void RemoveAt(IList list, uint index)
    {
        // The validation logic is the same as for 'IReadOnlyList<T>'
        IReadOnlyListAdapterHelpers.EnsureIndexInValidRange(index, list.Count);

        try
        {
            list.RemoveAt((int)index);
        }
        catch (IndexOutOfRangeException e)
        {
            e.HResult = WellKnownErrorCodes.E_BOUNDS;

            throw;
        }
    }

    /// <summary>
    /// Appends an item to the end of the vector.
    /// </summary>
    /// <param name="list">The wrapped <see cref="IList"/> instance.</param>
    /// <param name="value">The item to append to the vector.</param>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindablevector.append"/>
    public static void Append(IList list, object? value)
    {
        _ = list.Add(value);
    }

    /// <summary>
    /// Removes the last item in the vector.
    /// </summary>
    /// <param name="list">The wrapped <see cref="IList"/> instance.</param>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindablevector.removeatend"/>
    public static void RemoveAtEnd(IList list)
    {
        if (list.Count == 0)
        {
            throw new InvalidOperationException("InvalidOperation_CannotRemoveLastFromEmptyCollection") { HResult = WellKnownErrorCodes.E_BOUNDS };
        }

        RemoveAt(list, (uint)list.Count - 1);
    }

    /// <summary>
    /// Removes all items from the vector.
    /// </summary>
    /// <param name="list">The wrapped <see cref="IList"/> instance.</param>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindablevector.clear"/>
    public static void Clear(IList list)
    {
        list.Clear();
    }
}
