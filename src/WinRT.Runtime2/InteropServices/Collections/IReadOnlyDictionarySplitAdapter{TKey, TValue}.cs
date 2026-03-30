// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable IDE0046

namespace WindowsRuntime.InteropServices;

/// <summary>
/// An adapter for <see cref="IReadOnlyDictionary{TKey, TValue}"/> with keys sorted in ascending order.
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.imapview-2"/>
public sealed class IReadOnlyDictionarySplitAdapter<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
{
    /// <summary>
    /// The array of key-value pairs stored in the adapter.
    /// </summary>
    /// <remarks>
    /// These items are sorted with <see cref="KeyValuePairComparer"/>.
    /// </remarks>
    private readonly KeyValuePair<TKey, TValue>[] _items;

    /// <summary>
    /// The index of the first item in the array that can be used by this instance.
    /// </summary>
    private readonly int _firstItemIndex;

    /// <summary>
    /// The index of the last item in the array that can be used by this instance.
    /// </summary>
    private readonly int _lastItemIndex;

    /// <summary>
    /// The <see cref="ReadOnlyDictionaryKeyCollection{TKey, TValue}"/> instance, if initialized.
    /// </summary>
    private ReadOnlyDictionaryKeyCollection<TKey, TValue>? _keys;

    /// <summary>
    /// The <see cref="ReadOnlyDictionaryValueCollection{TKey, TValue}"/> instance, if initialized.
    /// </summary>
    private ReadOnlyDictionaryValueCollection<TKey, TValue>? _values;

    /// <summary>
    /// Creates a new <see cref="IReadOnlyDictionarySplitAdapter{TKey, TValue}"/> instance with the specified parameters.
    /// </summary>
    /// <param name="items">The array of key-value pairs to store in the adapter.</param>
    /// <param name="firstItemIndex">The index of the first item in the array that can be used by this instance.</param>
    /// <param name="lastItemIndex">The index of the last item in the array that can be used by this instance.</param>
    private IReadOnlyDictionarySplitAdapter(KeyValuePair<TKey, TValue>[] items, int firstItemIndex, int lastItemIndex)
    {
        _items = items;
        _firstItemIndex = firstItemIndex;
        _lastItemIndex = lastItemIndex;
    }

    /// <summary>
    /// Creates a new <see cref="IReadOnlyDictionarySplitAdapter{TKey, TValue}"/> instance with the specified parameters.
    /// </summary>
    /// <param name="dictionary">The source <see cref="IDictionary{TKey, TValue}"/> instance.</param>
    internal IReadOnlyDictionarySplitAdapter(IReadOnlyDictionary<TKey, TValue> dictionary)
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        _firstItemIndex = 0;
        _lastItemIndex = dictionary.Count - 1;
        _items = CreateKeyValueArray(dictionary);
    }

    /// <inheritdoc/>
    public IEnumerable<TKey> Keys => _keys ??= new ReadOnlyDictionaryKeyCollection<TKey, TValue>(this);

    /// <inheritdoc/>
    public IEnumerable<TValue> Values => _values ??= new ReadOnlyDictionaryValueCollection<TKey, TValue>(this);

    /// <inheritdoc/>
    public int Count => _lastItemIndex - _firstItemIndex + 1;

    /// <inheritdoc/>
    public TValue this[TKey key]
    {
        get
        {
            // Try to lookup the key, and throw the right exception if it's not found.
            // We don't need to adjust the 'HRESULT' here, it'll be done separately.
            if (!TryGetValue(key, out TValue? value))
            {
                throw KeyNotFoundException.GetKeyNotFoundException();
            }

            return value;
        }
    }

    /// <summary>
    /// Splits the dictionary into two instances, each representing a sorted half.
    /// </summary>
    /// <param name="first">One half of the original dictionary.</param>
    /// <param name="second">The second half of the original dictionary.</param>
    public void Split(
        [NotNullIfNotNull(nameof(second))] out IReadOnlyDictionary<TKey, TValue>? first,
        [NotNullIfNotNull(nameof(first))] out IReadOnlyDictionary<TKey, TValue>? second)
    {
        if (Count < 2)
        {
            first = null;
            second = null;

            return;
        }

        int pivot = (int)(uint)(((ulong)(uint)_firstItemIndex + (uint)_lastItemIndex) / 2UL);

        first = new IReadOnlyDictionarySplitAdapter<TKey, TValue>(_items, _firstItemIndex, pivot);
        second = new IReadOnlyDictionarySplitAdapter<TKey, TValue>(_items, pivot + 1, _lastItemIndex);
    }

    /// <inheritdoc/>
    public bool ContainsKey(TKey key)
    {
        return TryGetValue(key, out _);
    }

    /// <inheritdoc/>
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        KeyValuePair<TKey, TValue> searchKey = new(key, default!);

        // Do a binary search for the input key (the array is sorted upon construction)
        int index = Array.BinarySearch(
            array: _items,
            index: _firstItemIndex,
            length: Count,
            value: searchKey,
            comparer: KeyValuePairComparer.Instance);

        // If the key was not found, just stop here
        if (index < 0)
        {
            value = default;

            return false;
        }

        // Lookup the found pair in the array and return the value
        value = _items[index].Value;

        return true;
    }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        // The returned 'ArraySegment<KeyValuePair<TKey, TValue>>.Enumerator' type requires special handling in
        // 'cswinrtinteropgen' to be tracked correctly. If this implementation is changed, it needs to be kept in sync.
        return new ArraySegment<KeyValuePair<TKey, TValue>>(_items, _firstItemIndex, Count).GetEnumerator();
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Prepares the sorted array of key-value pairs from an input dictionary.
    /// </summary>
    /// <param name="dictionary">The source <see cref="IDictionary{TKey, TValue}"/> instance.</param>
    /// <returns>The resulting sorted array of key-value pairs from an input dictionary.</returns>
    private static KeyValuePair<TKey, TValue>[] CreateKeyValueArray(IReadOnlyDictionary<TKey, TValue> dictionary)
    {
        KeyValuePair<TKey, TValue>[] array = new KeyValuePair<TKey, TValue>[dictionary.Count];

        // Get all key-value pairs from the dictionary (we avoid 'ToArray' to avoid using LINQ)
        using (IEnumerator<KeyValuePair<TKey, TValue>> enumerator = dictionary.GetEnumerator())
        {
            int i = 0;

            while (enumerator.MoveNext())
            {
                array[i++] = enumerator.Current;
            }
        }

        // Sort the items based on their keys
        Array.Sort(array, KeyValuePairComparer.Instance);

        return array;
    }

    /// <summary>
    /// A comparer for <see cref="KeyValuePair{TKey, TValue}"/> that only checks <see cref="KeyValuePair{TKey, TValue}.Key"/>.
    /// </summary>
    private sealed class KeyValuePairComparer : IComparer<KeyValuePair<TKey, TValue>>
    {
        /// <summary>
        /// The singleton <see cref="KeyValuePairComparer"/> instance.
        /// </summary>
        public static readonly KeyValuePairComparer Instance = new();

        /// <inheritdoc/>
        public int Compare(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y)
        {
            return Comparer<TKey>.Default.Compare(x.Key, y.Key);
        }
    }
}
#endif
