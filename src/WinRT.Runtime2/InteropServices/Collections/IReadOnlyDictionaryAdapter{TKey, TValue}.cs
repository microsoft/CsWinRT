// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A stateless adapter for <see cref="IReadOnlyDictionary{TKey, TValue}"/>, to be exposed as <c>Windows.Foundation.Collections.IMapView&lt;K, V&gt;</c>.
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.imapview-2"/>
public static class IReadOnlyDictionaryAdapter<TKey, TValue>
{
    /// <summary>
    /// Returns the item at the specified key in the map view.
    /// </summary>
    /// <param name="dictionary">The wrapped <see cref="IReadOnlyDictionary{TKey, TValue}"/> instance.</param>
    /// <param name="key">The key to locate in the map view.</param>
    /// <returns>The value, if an item with the specified key exists.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.imapview-2.lookup"/>
    public static TValue Lookup(IReadOnlyDictionary<TKey, TValue> dictionary, TKey key)
    {
        // Try to lookup the key, and throw the right exception if it's not found.
        // This is more efficient than using the indexer and then catching the
        // exception if the lookup fails, adjusting the 'HRESUT', and re-throwing.
        if (!dictionary.TryGetValue(key, out TValue? value))
        {
            KeyNotFoundException.ThrowKeyNotFound();
        }

        return value;
    }

    /// <summary>
    /// Gets the number of elements in the map.
    /// </summary>
    /// <param name="dictionary">The wrapped <see cref="IReadOnlyDictionary{TKey, TValue}"/> instance.</param>
    /// <returns>The number of elements in the map.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.imapview-2.size"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Size(IReadOnlyDictionary<TKey, TValue> dictionary)
    {
        return (uint)dictionary.Count;
    }

    /// <summary>
    /// Splits the map view into two views.
    /// </summary>
    /// <param name="dictionary">The wrapped <see cref="IReadOnlyDictionary{TKey, TValue}"/> instance.</param>
    /// <param name="first">One half of the original map.</param>
    /// <param name="second">The second half of the original map.</param>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.imapview-2.split"/>
    public static void Split(
        IReadOnlyDictionary<TKey, TValue> dictionary,
        [NotNullIfNotNull(nameof(second))] out IReadOnlyDictionary<TKey, TValue>? first,
        [NotNullIfNotNull(nameof(first))] out IReadOnlyDictionary<TKey, TValue>? second)
    {
        // If the input dictionary doesn't have enough items, just set both halves to 'null'
        if (dictionary.Count < 2)
        {
            first = null;
            second = null;

            return;
        }

        // Get the split adapter, if we don't have one already. We can reuse the data
        // from the first one we create, so we don't re-allocate and re-sort the full
        // set of key-value pairs from the original input dictionary.
        if (dictionary is not IReadOnlyDictionarySplitAdapter<TKey, TValue> splitAdapter)
        {
            splitAdapter = new IReadOnlyDictionarySplitAdapter<TKey, TValue>(dictionary);
        }

        splitAdapter.Split(out first, out second);
    }
}
#endif
