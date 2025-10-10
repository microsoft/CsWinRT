// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A stateful adapter for <see cref="IEnumerator{T}"/>, to be exposed as <c>Windows.Foundation.Collections.IIterator&lt;T&gt;</c>.
/// </summary>
/// <typeparam name="T">The type of objects to enumerate.</typeparam>
/// <remarks>
/// This type should only be used by generated code.
/// </remarks>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.iiterator-1"/>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage)]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class IEnumeratorAdapter<T>
{
    /// <summary>
    /// The wrapped <see cref="IEnumerator{T}"/> instance.
    /// </summary>
    private readonly IEnumerator<T> _enumerator;

    /// <summary>
    /// Whether the item being retrieved is the first one, so <see cref="MoveNext"/> should be called first.
    /// </summary>
    private bool _firstItem = true;

    /// <summary>
    /// Whether there is a current item, i.e. whether the collection has no items left.
    /// </summary>
    private bool _hasCurrent;

    /// <summary>
    /// Creates a <see cref="IEnumeratorAdapter{T}"/> instance with the specified parameters.
    /// </summary>
    /// <param name="enumerator">The wrapped <see cref="IEnumerator{T}"/> instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="enumerator"/> is <see langword="null"/>.</exception>
    public IEnumeratorAdapter(IEnumerator<T> enumerator)
    {
        ArgumentNullException.ThrowIfNull(enumerator);

        _enumerator = enumerator;
    }

    /// <summary>
    /// Gets an <see cref="IEnumeratorAdapter{T}"/> instance associated to a given <see cref="IEnumerator{T}"/> object.
    /// </summary>
    /// <param name="enumerator">The input <see cref="IEnumerator{T}"/> object.</param>
    /// <returns>The <see cref="IEnumeratorAdapter{T}"/> instance associated to <paramref name="enumerator"/>.</returns>
    public static IEnumeratorAdapter<T> GetInstance(IEnumerator<T> enumerator)
    {
        return IEnumeratorAdapterTable<T>.Table.GetValue(enumerator, static enumerator => new IEnumeratorAdapter<T>(enumerator));
    }

    /// <summary>
    /// Gets the current item in the collection.
    /// </summary>
    /// <remarks>
    /// This method should directly implement the <c>Windows.Foundation.Collections.IIterator&lt;T&gt;.Current</c> property.
    /// </remarks>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.iiterator-1.current"/>
    public T Current
    {
        get
        {
            // Map no item being available to 'E_BOUNDS'
            if (!HasCurrent)
            {
                RestrictedErrorInfo.ThrowExceptionForHR(WellKnownErrorCodes.E_BOUNDS);
            }

            return _enumerator.Current;
        }
    }

    /// <summary>
    /// Gets a value that indicates whether the iterator refers to a current item or is at the end of the collection.
    /// </summary>
    /// <remarks>
    /// This method should directly implement the <c>Windows.Foundation.Collections.IIterator&lt;T&gt;.HasCurrent</c> property.
    /// </remarks>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.iiterator-1.hascurrent"/>
    public bool HasCurrent
    {
        get
        {
            // 'IEnumerator<T>' starts at item -1, while 'IIterator<T>' start at item 0. Therefore, if this
            // is the first access to the iterator, we need to advance to the first item.
            if (_firstItem)
            {
                _firstItem = false;

                // 'MoveNext' will set '_hasCurrent', so we can just read it after this call
                _ = MoveNext();
            }

            return _hasCurrent;
        }
    }

    /// <summary>
    /// Advances the iterator to the next item in the collection.
    /// </summary>
    /// <remarks>
    /// This method should directly implement the <c>Windows.Foundation.Collections.IIterator&lt;T&gt;.MoveNext</c> method.
    /// </remarks>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.iiterator-1.movenext"/>
    public bool MoveNext()
    {
        try
        {
            _hasCurrent = _enumerator.MoveNext();
        }
        catch (InvalidOperationException)
        {
            RestrictedErrorInfo.ThrowExceptionForHR(WellKnownErrorCodes.E_CHANGED_STATE);
        }

        return _hasCurrent;
    }

    /// <summary>
    /// Retrieves multiple items from the iterator.
    /// </summary>
    /// <param name="items">The target <see cref="Span{T}"/> to write items into.</param>
    /// <remarks>
    /// This method should directly implement the <c>Windows.Foundation.Collections.IIterator&lt;T&gt;.GetMany</c> method.
    /// </remarks>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.iiterator-1.getmany"/>
    public int GetMany(Span<T> items)
    {
        if (items.IsEmpty)
        {
            return 0;
        }

        int index = 0;

        // Copy all items into the target span
        for (; index < items.Length & HasCurrent; index++)
        {
            items[index] = Current;

            _ = MoveNext();
        }

        return index;
    }
}

/// <summary>
/// Mapping table for <see cref="IEnumeratorAdapter{T}"/> instances.
/// </summary>
/// <typeparam name="T">The type of objects to enumerate.</typeparam>
file static class IEnumeratorAdapterTable<T>
{
    /// <summary>
    /// The <see cref="ConditionalWeakTable{TKey, TValue}"/> instance for the mapping table.
    /// </summary>
    public static readonly ConditionalWeakTable<IEnumerator<T>, IEnumeratorAdapter<T>> Table = [];
}
