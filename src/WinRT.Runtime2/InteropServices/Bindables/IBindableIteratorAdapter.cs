// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A stateful adapter for <see cref="IEnumerator"/>, to be exposed as <c>Windows.UI.Xaml.Interop.IBindableIterator</c>.
/// </summary>
internal sealed class IBindableIteratorAdapter
{
    /// <summary>
    /// The wrapped <see cref="IEnumerator"/> instance.
    /// </summary>
    private readonly IEnumerator _enumerator;

    /// <summary>
    /// Whether the item being retrieved is the first one, so <see cref="MoveNext"/> should be called first.
    /// </summary>
    private bool _firstItem = true;

    /// <summary>
    /// Whether there is a current item, i.e. whether the collection has no items left.
    /// </summary>
    private bool _hasCurrent;

    /// <summary>
    /// Creates a <see cref="IBindableIteratorAdapter"/> instance with the specified parameters.
    /// </summary>
    /// <param name="enumerator">The wrapped <see cref="IEnumerator"/> instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="enumerator"/> is <see langword="null"/>.</exception>
    private IBindableIteratorAdapter(IEnumerator enumerator)
    {
        ArgumentNullException.ThrowIfNull(enumerator);

        _enumerator = enumerator;
    }

    /// <summary>
    /// Gets an <see cref="IBindableIteratorAdapter"/> instance associated to a given <see cref="IEnumerator"/> object.
    /// </summary>
    /// <param name="enumerator">The input <see cref="IEnumerator"/> object.</param>
    /// <returns>The <see cref="IBindableIteratorAdapter"/> instance associated to <paramref name="enumerator"/>.</returns>
    public static IBindableIteratorAdapter GetInstance(IEnumerator enumerator)
    {
        return IBindableIteratorAdapterTable.Table.GetValue(enumerator, static enumerator => new IBindableIteratorAdapter(enumerator));
    }

    /// <summary>
    /// Gets the current item in the collection.
    /// </summary>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindableiterator.current"/>
    public object? Current
    {
        get
        {
            // Equivalent logic to 'IEnumeratorAdapter<T>', see comments there for the whole type
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
    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindableiterator.hascurrent"/>
    public bool HasCurrent
    {
        get
        {
            if (_firstItem)
            {
                _firstItem = false;

                _ = MoveNext();
            }

            return _hasCurrent;
        }
    }

    /// <summary>
    /// Advances the iterator to the next item in the collection.
    /// </summary>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindableiterator.movenext"/>
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
}

/// <summary>
/// Mapping table for <see cref="IBindableIteratorAdapter"/> instances.
/// </summary>
file static class IBindableIteratorAdapterTable
{
    /// <summary>
    /// The <see cref="ConditionalWeakTable{TKey, TValue}"/> instance for the mapping table.
    /// </summary>
    public static readonly ConditionalWeakTable<IEnumerator, IBindableIteratorAdapter> Table = [];
}
