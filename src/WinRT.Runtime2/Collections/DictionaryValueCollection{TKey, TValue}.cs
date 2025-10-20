// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace WindowsRuntime;

/// <summary>
/// Provides an implementation for <see cref="IDictionary{TKey, TValue}.Values"/> for some <see cref="IDictionary{TKey, TValue}"/> type.
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
/// <remarks>
/// This type should only be used by generated code.
/// </remarks>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class DictionaryValueCollection<TKey, TValue> : ICollection<TValue>
{
    /// <summary>
    /// The wrapped <see cref="IDictionary{TKey, TValue}"/> instance.
    /// </summary>
    private readonly IDictionary<TKey, TValue> _dictionary;

    /// <summary>
    /// Creates a <see cref="DictionaryValueCollection{TKey, TValue}"/> instance with the specified parameters.
    /// </summary>
    /// <param name="dictionary">The <see cref="IDictionary{TKey, TValue}"/> instance to wrap.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="dictionary"/> is <see langword="null"/>.</exception>
    public DictionaryValueCollection(IDictionary<TKey, TValue> dictionary)
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        _dictionary = dictionary;
    }

    /// <inheritdoc/>
    public int Count => _dictionary.Count;

    /// <inheritdoc/>
    public bool IsReadOnly => true;

    /// <inheritdoc/>
    public void Add(TValue item)
    {
        throw new NotSupportedException("NotSupported_KeyCollectionSet");
    }

    /// <inheritdoc/>
    public void Clear()
    {
        throw new NotSupportedException("NotSupported_KeyCollectionSet");
    }

    /// <inheritdoc/>
    public bool Contains(TValue item)
    {
        // There's no efficient way to check if a value is present in a dictionary
        foreach ((_, TValue value) in _dictionary)
        {
            if (EqualityComparer<TValue>.Default.Equals(value, item))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public void CopyTo(TValue[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);

        int count = Count;

        if (arrayIndex >= array.Length && count > 0)
        {
            throw new ArgumentException("Arg_IndexOutOfRangeException");
        }

        if (array.Length - arrayIndex < count)
        {
            throw new ArgumentException("Argument_InsufficientSpaceToCopyCollection");
        }

        int i = arrayIndex;

        // Manually copy all values to the target array
        foreach ((_, TValue value) in _dictionary)
        {
            array[i++] = value;
        }
    }

    /// <inheritdoc/>
    public bool Remove(TValue item)
    {
        throw new NotSupportedException("NotSupported_KeyCollectionSet");
    }

    /// <inheritdoc/>
    public IEnumerator<TValue> GetEnumerator()
    {
        return new Enumerator(_dictionary);
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// The <see cref="IEnumerator{T}"/> implementation for <see cref="DictionaryValueCollection{TKey, TValue}"/>.
    /// </summary>
    private sealed class Enumerator : IEnumerator<TValue>
    {
        /// <summary>
        /// The underlying <see cref="IEnumerable{T}"/> instance that contains the key-value pairs of the read-only dictionary.
        /// </summary>
        private readonly IEnumerable<KeyValuePair<TKey, TValue>> _enumerable;

        /// <summary>
        /// The <see cref="IEnumerator{T}"/> instance currently in use.
        /// </summary>
        private IEnumerator<KeyValuePair<TKey, TValue>> _enumerator;

        /// <summary>
        /// Creates a <see cref="DictionaryValueCollection{TKey, TValue}"/> instance with the specified parameters.
        /// </summary>
        /// <param name="enumerable">The <see cref="IEnumerator{T}"/> instance to wrap.</param>
        public Enumerator(IEnumerable<KeyValuePair<TKey, TValue>> enumerable)
        {
            _enumerable = enumerable;
            _enumerator = enumerable.GetEnumerator();
        }

        /// <inheritdoc/>
        public TValue Current => _enumerator!.Current.Value;

        /// <inheritdoc/>
        object IEnumerator.Current => Current!;

        /// <inheritdoc/>
        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        /// <inheritdoc/>
        public void Reset()
        {
            _enumerator = _enumerable.GetEnumerator();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _enumerator.Dispose();
        }
    }
}
