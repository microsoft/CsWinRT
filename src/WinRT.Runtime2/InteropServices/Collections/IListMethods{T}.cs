// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable CS1573

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Methods for implementations of <see cref="System.Collections.Generic.IList{T}"/> types.
/// </summary>
/// <typeparam name="T">The type of objects to enumerate.</typeparam>
/// <remarks>
/// This type should only be used by generated code.
/// </remarks>
[Obsolete("This type is an implementation detail, and it's only meant to be consumed by 'cswinrtgen'")]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IListMethods<T>
{
    /// <inheritdoc cref="System.Collections.Generic.IList{T}.this"/>
    /// <typeparam name="TMethods">The <see cref="IVectorMethods{T}"/> implementation to use.</typeparam>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    public static T Item<TMethods>(WindowsRuntimeObjectReference thisReference, int index)
        where TMethods : IVectorMethods<T>
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        try
        {
            // Same implementation as 'IReadOnlyListMethods<T>.Item<TMethods>(...)', but for 'IList<T>'
            return TMethods.GetAt(thisReference, (uint)index);
        }
        catch (Exception e) when (e.HResult == WellKnownErrorCodes.E_BOUNDS)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    /// <inheritdoc cref="System.Collections.Generic.IList{T}.this"/>
    /// <typeparam name="TMethods">The <see cref="IVectorMethods{T}"/> implementation to use.</typeparam>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <param name="item">The item to set.</param>
    public static void Item<TMethods>(WindowsRuntimeObjectReference thisReference, int index, T item)
        where TMethods : IVectorMethods<T>
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        try
        {
            // Defer the bounds checks to the native implementation
            TMethods.SetAt(thisReference, (uint)index, item);
        }
        catch (Exception e) when (e.HResult == WellKnownErrorCodes.E_BOUNDS)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    /// <inheritdoc cref="System.Collections.Generic.ICollection{T}.Add"/>
    /// <typeparam name="TMethods">The <see cref="IVectorMethods{T}"/> implementation to use.</typeparam>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    public static void Add<TMethods>(WindowsRuntimeObjectReference thisReference, T item)
        where TMethods : IVectorMethods<T>
    {
        TMethods.Append(thisReference, item);
    }

    /// <inheritdoc cref="System.Collections.Generic.ICollection{T}.Contains"/>
    /// <typeparam name="TMethods">The <see cref="IVectorMethods{T}"/> implementation to use.</typeparam>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    public static bool Contains<TMethods>(WindowsRuntimeObjectReference thisReference, T item)
        where TMethods : IVectorMethods<T>
    {
        return TMethods.IndexOf(thisReference, item, out _);
    }

    /// <inheritdoc cref="System.Collections.Generic.ICollection{T}.CopyTo"/>
    /// <typeparam name="TMethods">The <see cref="IVectorMethods{T}"/> implementation to use.</typeparam>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    public static void CopyTo<TMethods>(WindowsRuntimeObjectReference thisReference, T[] array, int arrayIndex)
        where TMethods : IVectorMethods<T>
    {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);

        int count = IListMethods.Count(thisReference);

        if (array.Length <= arrayIndex && count > 0)
        {
            throw new ArgumentException("Argument_IndexOutOfArrayBounds");
        }

        if (array.Length - arrayIndex < count)
        {
            throw new ArgumentException("Argument_InsufficientSpaceToCopyCollection");
        }

        // Copy all items into the target array, at the specified starting offset
        for (int i = 0; i < count; i++)
        {
            array[i + arrayIndex] = Item<TMethods>(thisReference, i);
        }
    }

    /// <inheritdoc cref="System.Collections.Generic.ICollection{T}.Remove"/>
    /// <typeparam name="TMethods">The <see cref="IVectorMethods{T}"/> implementation to use.</typeparam>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    public static bool Remove<TMethods>(WindowsRuntimeObjectReference thisReference, T item)
        where TMethods : IVectorMethods<T>
    {
        int index = IndexOf<TMethods>(thisReference, item);

        // If the item is not in the collection, stop here
        if (index == -1)
        {
            return false;
        }

        // We can now actually remove the item at the index we retrieved
        IListMethods.RemoveAt(thisReference, index);

        return true;
    }

    /// <inheritdoc cref="System.Collections.Generic.IList{T}.IndexOf"/>
    /// <typeparam name="TMethods">The <see cref="IVectorMethods{T}"/> implementation to use.</typeparam>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    public static int IndexOf<TMethods>(WindowsRuntimeObjectReference thisReference, T item)
        where TMethods : IVectorMethods<T>
    {
        // If the item is not in the collection, stop here
        if (!TMethods.IndexOf(thisReference, item, out uint index))
        {
            return -1;
        }

        // Ensure the index is within the valid range for .NET collections
        if (index > int.MaxValue)
        {
            [DoesNotReturn]
            static void ThrowInvalidOperationException()
                => throw new InvalidOperationException("InvalidOperation_CollectionBackingListTooLarge");

            ThrowInvalidOperationException();
        }

        return (int)index;
    }

    /// <inheritdoc cref="System.Collections.Generic.IList{T}.Insert"/>
    /// <typeparam name="TMethods">The <see cref="IVectorMethods{T}"/> implementation to use.</typeparam>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    public static void Insert<TMethods>(WindowsRuntimeObjectReference thisReference, int index, T item)
        where TMethods : IVectorMethods<T>
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        try
        {
            // Defer the bounds checks to the native implementation
            TMethods.InsertAt(thisReference, (uint)index, item);
        }
        catch (Exception e) when (e.HResult == WellKnownErrorCodes.E_BOUNDS)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }
}
