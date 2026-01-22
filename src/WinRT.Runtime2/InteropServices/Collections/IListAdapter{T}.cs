// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
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
        // return a modifiable reference for this view. We believe this is acceptable, as
        // it allows us to gain some performance. For instance, in most situations (because
        // pretty much all built-in .NET collection types implementing 'IList<T>' also implement
        // 'IReadOnlyList<T>'), this allows us to not allocate anything. That is, when native
        // code calls 'GetView()', it would get back the same CCW instance, just through a
        // 'QueryInterface' call for 'IVectorView<T>' instead.
        //
        // The returned 'ReadOnlyCollection<T>' type requires special handling in 'cswinrtgen' to
        // be tracked correctly. If this implementation is changed, it needs to be kept in sync.
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
    /// Sets the value at the specified index in the vector.
    /// </summary>
    /// <param name="list">The wrapped <see cref="IList{T}"/> instance.</param>
    /// <param name="index">The zero-based index at which to set the value.</param>
    /// <param name="value">The item to set.</param>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivector-1.setat"/>
    public static void SetAt(IList<T> list, uint index, T value)
    {
        IReadOnlyListAdapterHelpers.EnsureIndexInValidRange(index, list.Count);

        try
        {
            list[(int)index] = value;
        }
        catch (ArgumentOutOfRangeException e)
        {
            e.HResult = WellKnownErrorCodes.E_BOUNDS;

            throw;
        }
    }

    /// <summary>
    /// Inserts an item at a specified index in the vector.
    /// </summary>
    /// <param name="list">The wrapped <see cref="IList{T}"/> instance.</param>
    /// <param name="index">The zero-based index.</param>
    /// <param name="value">The item to insert.</param>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivector-1.insertat"/>
    public static void InsertAt(IList<T> list, uint index, T value)
    {
        // Inserting at an index one past the end of the list is equivalent to just
        // appending an item, so we need to ensure that we're within '[0, count + 1)'.
        IReadOnlyListAdapterHelpers.EnsureIndexInValidRange(index, list.Count + 1);

        try
        {
            list.Insert((int)index, value);
        }
        catch (ArgumentOutOfRangeException e)
        {
            e.HResult = WellKnownErrorCodes.E_BOUNDS;

            throw;
        }
    }

    /// <summary>
    /// Removes the item at the specified index in the vector.
    /// </summary>
    /// <param name="list">The wrapped <see cref="IList{T}"/> instance.</param>
    /// <param name="index">The zero-based index of the vector item to remove.</param>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivector-1.removeat"/>
    public static void RemoveAt(IList<T> list, uint index)
    {
        IReadOnlyListAdapterHelpers.EnsureIndexInValidRange(index, list.Count);

        try
        {
            list.RemoveAt((int)index);
        }
        catch (ArgumentOutOfRangeException e)
        {
            e.HResult = WellKnownErrorCodes.E_BOUNDS;

            throw;
        }
    }

    /// <summary>
    /// Removes the last item from the vector.
    /// </summary>
    /// <param name="list">The wrapped <see cref="IList{T}"/> instance.</param>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivector-1.removeatend"/>
    public static void RemoveAtEnd(IList<T> list)
    {
        // Manually hoist the count to avoid doing an interface dispatch call twice.
        // We don't need to protect against mutation here: the actual list type would
        // already either handle this, or the following 'RemoveAt' call might throw.
        int count = list.Count;

        // Check that the list isn't empty, as that would of course be invalid
        if (count == 0)
        {
            [DoesNotReturn]
            static void ThrowInvalidOperationException()
            {
                throw new InvalidOperationException("InvalidOperation_CannotRemoveLastFromEmptyCollection")
                {
                    HResult = WellKnownErrorCodes.E_BOUNDS
                };
            }

            ThrowInvalidOperationException();
        }

        try
        {
            list.RemoveAt(count - 1);
        }
        catch (ArgumentOutOfRangeException e)
        {
            e.HResult = WellKnownErrorCodes.E_BOUNDS;

            throw;
        }
    }
}