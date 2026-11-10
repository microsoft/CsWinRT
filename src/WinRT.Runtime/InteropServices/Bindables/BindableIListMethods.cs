// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

#pragma warning disable CS1573

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Methods for <see cref="System.Collections.IList"/> types.
/// </summary>
internal static class BindableIListMethods
{
    /// <inheritdoc cref="System.Collections.ICollection.Count"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    public static int Count(WindowsRuntimeObjectReference thisReference)
    {
        uint count = IBindableVectorMethods.Size(thisReference);

        // Same validation as for 'IVectorView<T>'
        InvalidOperationException.ThrowIfCollectionBackingListTooLarge(count);

        return (int)count;
    }

    /// <inheritdoc cref="System.Collections.IList.this"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    public static object? Item(WindowsRuntimeObjectReference thisReference, int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        try
        {
            return IBindableVectorMethods.GetAt(thisReference, (uint)index);
        }
        catch (Exception e) when (e.HResult == WellKnownErrorCodes.E_BOUNDS)
        {
            throw ArgumentOutOfRangeException.GetArgumentOutOfRangeException(nameof(index));
        }
    }

    /// <inheritdoc cref="System.Collections.IList.this"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <param name="item">The item to set.</param>
    public static void Item(WindowsRuntimeObjectReference thisReference, int index, object? item)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        try
        {
            // Defer the bounds checks to the native implementation (same as for 'IVector<T>')
            IBindableVectorMethods.SetAt(thisReference, (uint)index, item);
        }
        catch (Exception e) when (e.HResult == WellKnownErrorCodes.E_BOUNDS)
        {
            throw ArgumentOutOfRangeException.GetArgumentOutOfRangeException(nameof(index));
        }
    }

    /// <inheritdoc cref="System.Collections.IList.Add"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    public static int Add(WindowsRuntimeObjectReference thisReference, object? item)
    {
        IBindableVectorMethods.Append(thisReference, item);

        return Count(thisReference) - 1;
    }

    /// <inheritdoc cref="System.Collections.IList.Clear"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    public static void Clear(WindowsRuntimeObjectReference thisReference)
    {
        IBindableVectorMethods.Clear(thisReference);
    }

    /// <inheritdoc cref="System.Collections.IList.Contains"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    public static bool Contains(WindowsRuntimeObjectReference thisReference, object? item)
    {
        return IBindableVectorMethods.IndexOf(thisReference, item, out _);
    }

    /// <inheritdoc cref="System.Collections.IList.IndexOf"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    public static int IndexOf(WindowsRuntimeObjectReference thisReference, object? item)
    {
        // If the item is not in the collection, stop here
        if (!IBindableVectorMethods.IndexOf(thisReference, item, out uint index))
        {
            return -1;
        }

        // Same validation as for 'IVector<T>'
        InvalidOperationException.ThrowIfCollectionBackingListTooLarge(index);

        return (int)index;
    }

    /// <inheritdoc cref="System.Collections.IList.Insert"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    public static void Insert(WindowsRuntimeObjectReference thisReference, int index, object? item)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        try
        {
            // Defer the bounds checks to the native implementation (same as for 'IVector<T>')
            IBindableVectorMethods.InsertAt(thisReference, (uint)index, item);
        }
        catch (Exception e) when (e.HResult == WellKnownErrorCodes.E_BOUNDS)
        {
            throw ArgumentOutOfRangeException.GetArgumentOutOfRangeException(nameof(index));
        }
    }

    /// <inheritdoc cref="System.Collections.IList.Remove"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    public static void Remove(WindowsRuntimeObjectReference thisReference, object? item)
    {
        int index = IndexOf(thisReference, item);

        if (index == -1)
        {
            return;
        }

        // Check above and removal here matches the logic for 'IList<T>.Remove'
        IListMethods.RemoveAt(thisReference, index);
    }

    /// <inheritdoc cref="System.Collections.IList.RemoveAt"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    public static void RemoveAt(WindowsRuntimeObjectReference thisReference, int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        try
        {
            // Defer the bounds checks to the native implementation
            IBindableVectorMethods.RemoveAt(thisReference, (uint)index);
        }
        catch (Exception e) when (e.HResult == WellKnownErrorCodes.E_BOUNDS)
        {
            throw ArgumentOutOfRangeException.GetArgumentOutOfRangeException(nameof(index));
        }
    }

    /// <inheritdoc cref="System.Collections.ICollection.CopyTo"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    public static void CopyTo(WindowsRuntimeObjectReference thisReference, Array array, int index)
    {
        ArgumentNullException.ThrowIfNull(array);

        // The destination array must be single-dimensional
        ArgumentException.ThrowIfRankMultiDimNotSupported(array.Rank);

        ArgumentOutOfRangeException.ThrowIfNegative(index);

        int arrayLowerBound = array.GetLowerBound(0);
        int arrayLength = array.GetLength(0);
        int sourceLength = Count(thisReference);

        // The index must be in range with respect to the lower bound of the array
        if (index < arrayLowerBound)
        {
            throw ArgumentOutOfRangeException.GetArgumentOutOfRangeException(nameof(index));
        }

        // Does the dimension in question have sufficient space to copy the expected number of entries?
        // We perform this check before valid index check to ensure the exception message is in sync with
        // the following snippet that uses regular framework code:
        //
        // ArrayList list = new();
        //
        // list.Add(1);
        //
        // Array items = Array.CreateInstance(typeof(object), [1], [-1]);
        //
        // list.CopyTo(items, 0);
        if (sourceLength > (arrayLength - (index - arrayLowerBound)))
        {
            ArgumentException.ThrowInsufficientSpaceToCopyCollection();
        }

        if (index - arrayLowerBound > arrayLength)
        {
            ArgumentException.ThrowIndexOutOfArrayBounds();
        }

        // Copy all items into the target array, at the specified starting offset
        for (int i = 0; i < sourceLength; i++)
        {
            array.SetValue(Item(thisReference, i), i + index);
        }
    }
}