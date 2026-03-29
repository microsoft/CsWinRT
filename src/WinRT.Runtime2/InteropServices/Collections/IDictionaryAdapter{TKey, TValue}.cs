// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable CS8714

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A stateless adapter for <see cref="IDictionary{TKey, TValue}"/>, to be exposed as <c>Windows.Foundation.Collections.IMap&lt;K, V&gt;</c>.
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.imapview-2"/>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IDictionaryAdapter<TKey, TValue>
{
    /// <summary>
    /// Returns the item at the specified key in the map.
    /// </summary>
    /// <param name="dictionary">The wrapped <see cref="IDictionary{TKey, TValue}"/> instance.</param>
    /// <param name="key">The key associated with the item to locate.</param>
    /// <returns>The value, if an item with the specified key exists.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.imap-2.lookup"/>
    public static TValue Lookup(IDictionary<TKey, TValue> dictionary, TKey key)
    {
        // Same logic as in 'IReadOnlyDictionaryAdapter<TKey, TValue>.Lookup'
        if (!dictionary.TryGetValue(key, out TValue? value))
        {
            KeyNotFoundException.ThrowKeyNotFound();
        }

        return value;
    }

    /// <summary>
    /// Gets the number of items in the map.
    /// </summary>
    /// <param name="dictionary">The wrapped <see cref="IReadOnlyDictionary{TKey, TValue}"/> instance.</param>
    /// <returns>The number of elements in the map.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.imap-2.size"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Size(IDictionary<TKey, TValue> dictionary)
    {
        return (uint)dictionary.Count;
    }

    /// <summary>
    /// Returns an immutable view of the map.
    /// </summary>
    /// <param name="dictionary">The wrapped <see cref="IReadOnlyDictionary{TKey, TValue}"/> instance.</param>
    /// <returns>The view of the map.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.imap-2.getview"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IReadOnlyDictionary<TKey, TValue> GetView(IDictionary<TKey, TValue> dictionary)
    {
        // Analogous implementation to 'IListAdapter<T>.GetView', see additional notes there
        return dictionary as IReadOnlyDictionary<TKey, TValue> ?? new ReadOnlyDictionary<TKey, TValue>(dictionary);
    }

    /// <summary>
    /// Inserts or replaces an item in the map.
    /// </summary>
    /// <param name="dictionary">The wrapped <see cref="IDictionary{TKey, TValue}"/> instance.</param>
    /// <param name="key">The key associated with the item to insert.</param>
    /// <param name="value">The item to insert.</param>
    /// <returns>Whether an item with the specified key is an existing item that was replaced.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.imap-2.insert"/>
    public static bool Insert(IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
    {
        // If the target dictionary is a 'Dictionary<TKey, TValue>' instance, we can optimize the
        // insertion by avoiding the duplicate lookup. We only do this for exactly this type, as
        // derived implementations could otherwise alter the standard indexer behavior. We don't
        // need to worry about concurrent operations here: same result as using the indexer.
        // The 'CollectionsMarshal' APIs already guarantee that concurrent operations might
        // possibly throw or result in invalid results, but that type safety violations can't happen.
        if (dictionary.GetType() == typeof(Dictionary<TKey, TValue>))
        {
            CollectionsMarshal.GetValueRefOrAddDefault(
                dictionary: Unsafe.As<Dictionary<TKey, TValue>>(dictionary),
                key: key,
                exists: out bool exists) = value;

            return exists;
        }

        bool replacing = dictionary.ContainsKey(key);

        dictionary[key] = value;

        return replacing;
    }

    /// <summary>
    /// Removes an item from the map.
    /// </summary>
    /// <param name="dictionary">The wrapped <see cref="IDictionary{TKey, TValue}"/> instance.</param>
    /// <param name="key">The key associated with the item to remove.</param>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.imap-2.remove"/>
    public static void Remove(IDictionary<TKey, TValue> dictionary, TKey key)
    {
        bool removed = dictionary.Remove(key);

        if (!removed)
        {
            KeyNotFoundException.ThrowKeyNotFound();
        }
    }
}
#endif
