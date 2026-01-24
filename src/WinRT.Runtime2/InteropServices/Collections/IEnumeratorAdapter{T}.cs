// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

#pragma warning disable IDE0032

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A stateful adapter for <see cref="IEnumerator{T}"/>, to be exposed as <c>Windows.Foundation.Collections.IIterator&lt;T&gt;</c>.
/// </summary>
/// <typeparam name="T">The type of objects to enumerate.</typeparam>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.iiterator-1"/>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class IEnumeratorAdapter<T> : IEnumerator<T>
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
    internal IEnumeratorAdapter(IEnumerator<T> enumerator)
    {
        ArgumentNullException.ThrowIfNull(enumerator);

        _enumerator = enumerator;
    }

    /// <summary>
    /// Gets an <see cref="IEnumeratorAdapter{T}"/> instance associated to a given <see cref="IEnumerator{T}"/> object.
    /// </summary>
    /// <param name="enumerator">The input <see cref="IEnumerator{T}"/> object.</param>
    /// <returns>The <see cref="IEnumeratorAdapter{T}"/> instance associated to <paramref name="enumerator"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumeratorAdapter<T> GetInstance(IEnumerator<T> enumerator)
    {
        // Check if the input enumerator is already an 'IEnumeratorAdapter<T>' instance. If so,
        // we can just unwrap the original enumerator and return it. Otherwise, we create a new
        // adapter for it. The input value could potentially be an adapter already if has been
        // previously marshalled to native on a managed type that had no marshalling info available
        // for it. See additional notes in 'IEnumerableAdapter<T>.First' for more context on this.
        return enumerator as IEnumeratorAdapter<T> ?? IEnumeratorAdapterTable<T>.Table.GetOrAdd(enumerator, IEnumeratorAdapterFactory<T>.Callback);
    }

    /// <summary>
    /// Gets the wrapped <see cref="IEnumerator{T}"/> instance.
    /// </summary>
    internal IEnumerator<T> Enumerator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _enumerator;
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

    /// <inheritdoc/>
    T IEnumerator<T>.Current => _enumerator.Current;

    /// <inheritdoc/>
    object IEnumerator.Current => _enumerator.Current!;

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
        _enumerator.Dispose();
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

/// <summary>
/// A factory type for <see cref="IEnumeratorAdapter{T}"/> instances.
/// </summary>
/// <typeparam name="T">The type of objects to enumerate.</typeparam>
file sealed class IEnumeratorAdapterFactory<T>
{
    /// <summary>
    /// The singleton <see cref="IEnumeratorAdapterFactory{T}"/> instance.
    /// </summary>
    private static readonly IEnumeratorAdapterFactory<T> Instance = new();

    /// <summary>
    /// The singleton <see cref="Func{T, TResult}"/> callback instance for the factory.
    /// </summary>
    public static readonly Func<IEnumerator<T>, IEnumeratorAdapter<T>> Callback = new(Instance.Create);

    /// <inheritdoc cref="IEnumeratorAdapter{T}.IEnumeratorAdapter(IEnumerator{T})"/>
    private IEnumeratorAdapter<T> Create(IEnumerator<T> enumerator)
    {
        return new(enumerator);
    }
}