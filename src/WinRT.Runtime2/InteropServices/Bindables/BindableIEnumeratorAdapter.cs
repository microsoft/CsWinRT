// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

#pragma warning disable IDE0032

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A stateful adapter for <see cref="IEnumerator"/>, to be exposed as <c>Windows.UI.Xaml.Interop.IBindableIterator</c>.
/// </summary>
[WindowsRuntimeManagedOnlyType]
internal sealed class BindableIEnumeratorAdapter : IEnumerator<object>, IEnumeratorAdapter
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
    /// Creates a <see cref="BindableIEnumeratorAdapter"/> instance with the specified parameters.
    /// </summary>
    /// <param name="enumerator">The wrapped <see cref="IEnumerator"/> instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="enumerator"/> is <see langword="null"/>.</exception>
    public BindableIEnumeratorAdapter(IEnumerator enumerator)
    {
        ArgumentNullException.ThrowIfNull(enumerator);

        _enumerator = enumerator;
    }

    /// <summary>
    /// Gets an <see cref="BindableIEnumeratorAdapter"/> instance associated to a given <see cref="IEnumerator"/> object.
    /// </summary>
    /// <param name="enumerator">The input <see cref="IEnumerator"/> object.</param>
    /// <returns>The <see cref="BindableIEnumeratorAdapter"/> instance associated to <paramref name="enumerator"/>.</returns>
    public static BindableIEnumeratorAdapter GetInstance(IEnumerator enumerator)
    {
        // Handle unwrapping for the various cases we can encounter. We want to try to get the inner-most
        // adapter here, to avoid multiple interface dispatch calls adding overhead. The main cases we
        // can hit are the one for an 'IEnumerator' instance being marshalled directly (which may or may
        // not have needed an intermediate adapter), some 'IEnumerator<T>' adapter for an inner enumerator,
        // or an actual (unwrapped) enumerator that had its own marshalling info when passed to native.
        return enumerator switch
        {
            IEnumeratorAdapter<object> { Enumerator: BindableIEnumeratorAdapter adapter } => adapter,
            IEnumeratorAdapter<object> { Enumerator: IEnumerator inner }
                => BindableIEnumeratorAdapterTable.Table.GetOrAdd(inner, BindableIEnumeratorAdapterFactory.Callback),
            IEnumeratorAdapter { Enumerator: IEnumerator inner }
                => BindableIEnumeratorAdapterTable.Table.GetOrAdd(inner, BindableIEnumeratorAdapterFactory.Callback),
            _ => BindableIEnumeratorAdapterTable.Table.GetOrAdd(enumerator, BindableIEnumeratorAdapterFactory.Callback)
        };
    }

    /// <inheritdoc/>
    public IEnumerator Enumerator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _enumerator;
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

    /// <inheritdoc/>
    object IEnumerator<object>.Current => _enumerator.Current;

    /// <inheritdoc/>
    object IEnumerator.Current => _enumerator.Current;

    /// <inheritdoc/>
    bool IEnumerator.MoveNext()
    {
        return _enumerator.MoveNext();
    }

    /// <inheritdoc/>
    void IEnumerator.Reset()
    {
        _enumerator.Reset();
    }

    /// <inheritdoc/>
    void IDisposable.Dispose()
    {
    }
}

/// <summary>
/// Mapping table for <see cref="BindableIEnumeratorAdapter"/> instances.
/// </summary>
file static class BindableIEnumeratorAdapterTable
{
    /// <summary>
    /// The <see cref="ConditionalWeakTable{TKey, TValue}"/> instance for the mapping table.
    /// </summary>
    public static readonly ConditionalWeakTable<IEnumerator, BindableIEnumeratorAdapter> Table = [];
}

/// <summary>
/// A factory type for <see cref="BindableIEnumeratorAdapter"/> instances.
/// </summary>
file sealed class BindableIEnumeratorAdapterFactory
{
    /// <summary>
    /// The singleton <see cref="BindableIEnumeratorAdapterFactory"/> instance.
    /// </summary>
    private static readonly BindableIEnumeratorAdapterFactory Instance = new();

    /// <summary>
    /// The singleton <see cref="Func{T, TResult}"/> callback instance for the factory.
    /// </summary>
    public static readonly Func<IEnumerator, BindableIEnumeratorAdapter> Callback = new(Instance.Create);

    /// <inheritdoc cref="BindableIEnumeratorAdapter(IEnumerator)"/>
    private BindableIEnumeratorAdapter Create(IEnumerator enumerator)
    {
        return new(enumerator);
    }
}
#endif