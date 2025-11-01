// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace WindowsRuntime;

/// <summary>
/// Provides an implementation for <see cref="IReadOnlyDictionary{TKey, TValue}.Values"/> for some <see cref="IEnumerable{T}"/> type.
/// </summary>
/// <typeparam name="TKey">The type of keys in the read-only dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the read-only dictionary.</typeparam>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class ReadOnlyDictionaryValueCollection<TKey, TValue> : IEnumerable<TValue>
{
    /// <summary>
    /// The wrapped <see cref="IEnumerable{T}"/> instance that contains the key-value pairs of the read-only dictionary.
    /// </summary>
    private readonly IEnumerable<KeyValuePair<TKey, TValue>> _enumerable;

    /// <summary>
    /// Creates a <see cref="ReadOnlyDictionaryValueCollection{TKey, TValue}"/> instance with the specified parameters.
    /// </summary>
    /// <param name="enumerable">The <see cref="IEnumerable{T}"/> instance to wrap.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="enumerable"/> is <see langword="null"/>.</exception>
    public ReadOnlyDictionaryValueCollection(IEnumerable<KeyValuePair<TKey, TValue>> enumerable)
    {
        ArgumentNullException.ThrowIfNull(enumerable);

        _enumerable = enumerable;
    }

    /// <inheritdoc/>
    public IEnumerator<TValue> GetEnumerator()
    {
        return new Enumerator(_enumerable);
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// The <see cref="IEnumerator{T}"/> implementation for <see cref="ReadOnlyDictionaryValueCollection{TKey, TValue}"/>.
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
        /// Creates a <see cref="ReadOnlyDictionaryValueCollection{TKey, TValue}"/> instance with the specified parameters.
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
