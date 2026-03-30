// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

#pragma warning disable IDE0045, IDE0046

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Extensions for the <see cref="IDictionaryAdapter{TKey, TValue}"/> type.
/// </summary>
public static class IDictionaryAdapterExtensions
{
    extension<TValue>(IDictionaryAdapter<string, TValue>)
    {
        /// <inheritdoc cref="IDictionaryAdapter{TKey, TValue}.Lookup"/>
        /// <remarks>
        /// This overload can be used to avoid a <see cref="string"/> allocation on the caller side.
        /// </remarks>
        public static TValue Lookup(IDictionary<string, TValue> dictionary, ReadOnlySpan<char> key)
        {
            bool found;
            TValue? value;

            // Same logic as in 'IReadOnlyDictionaryAdapterExtensions.Lookup' for trying to avoid materializing the 'string' key
            if (dictionary.GetType() == typeof(Dictionary<string, TValue>) &&
                Unsafe.As<Dictionary<string, TValue>>(dictionary).TryGetAlternateLookup(out Dictionary<string, TValue>.AlternateLookup<ReadOnlySpan<char>> lookup1))
            {
                found = lookup1.TryGetValue(key, out value);
            }
            else if (dictionary.GetType() == typeof(ConcurrentDictionary<string, TValue>) &&
                     Unsafe.As<ConcurrentDictionary<string, TValue>>(dictionary).TryGetAlternateLookup(out ConcurrentDictionary<string, TValue>.AlternateLookup<ReadOnlySpan<char>> lookup2))
            {
                found = lookup2.TryGetValue(key, out value);
            }
            else if (dictionary is FrozenDictionary<string, TValue> candidate3 &&
                     candidate3.TryGetAlternateLookup(out FrozenDictionary<string, TValue>.AlternateLookup<ReadOnlySpan<char>> lookup3))
            {
                found = lookup3.TryGetValue(key, out value);
            }
            else
            {
                found = dictionary.TryGetValue(key.ToString(), out value);
            }

            // Throw the correct exception if the lookup failed
            if (!found)
            {
                KeyNotFoundException.ThrowKeyNotFound();
            }

            return value!;
        }

        /// <summary>
        /// Determines whether the map contains the specified key.
        /// </summary>
        /// <param name="dictionary">The wrapped <see cref="IReadOnlyDictionary{TKey, TValue}"/> instance.</param>
        /// <param name="key">The key to locate in the map.</param>
        /// <returns>Whether the key was found.</returns>
        /// <remarks>
        /// This overload can be used to avoid a <see cref="string"/> allocation on the caller side.
        /// </remarks>
        /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.imap-2.haskey"/>
        public static bool HasKey(IDictionary<string, TValue> dictionary, ReadOnlySpan<char> key)
        {
            // Same logic as in 'Lookup' above for trying to avoid materializing the 'string' key
            if (dictionary.GetType() == typeof(Dictionary<string, TValue>) &&
                Unsafe.As<Dictionary<string, TValue>>(dictionary).TryGetAlternateLookup(out Dictionary<string, TValue>.AlternateLookup<ReadOnlySpan<char>> lookup1))
            {
                return lookup1.ContainsKey(key);
            }

            if (dictionary.GetType() == typeof(ConcurrentDictionary<string, TValue>) &&
                Unsafe.As<ConcurrentDictionary<string, TValue>>(dictionary).TryGetAlternateLookup(out ConcurrentDictionary<string, TValue>.AlternateLookup<ReadOnlySpan<char>> lookup2))
            {
                return lookup2.ContainsKey(key);
            }

            if (dictionary is FrozenDictionary<string, TValue> candidate3 &&
                candidate3.TryGetAlternateLookup(out FrozenDictionary<string, TValue>.AlternateLookup<ReadOnlySpan<char>> lookup3))
            {
                return lookup3.ContainsKey(key);
            }

            return dictionary.ContainsKey(key.ToString());
        }

        /// <inheritdoc cref="IDictionaryAdapter{TKey, TValue}.Remove"/>
        /// <remarks>
        /// This overload can be used to avoid a <see cref="string"/> allocation on the caller side.
        /// </remarks>
        public static void Remove(IDictionary<string, TValue> dictionary, ReadOnlySpan<char> key)
        {
            bool removed;

            // Same logic as in 'IReadOnlyDictionaryAdapterExtensions.Lookup' for trying to avoid materializing the 'string' key.
            // We don't handle 'FrozenDictionary<TKey, TValue>' since instances of that type are immutable (removing would throw).
            if (dictionary.GetType() == typeof(Dictionary<string, TValue>) &&
                Unsafe.As<Dictionary<string, TValue>>(dictionary).TryGetAlternateLookup(out Dictionary<string, TValue>.AlternateLookup<ReadOnlySpan<char>> lookup1))
            {
                removed = lookup1.Remove(key);
            }
            else if (dictionary.GetType() == typeof(ConcurrentDictionary<string, TValue>) &&
                     Unsafe.As<ConcurrentDictionary<string, TValue>>(dictionary).TryGetAlternateLookup(out ConcurrentDictionary<string, TValue>.AlternateLookup<ReadOnlySpan<char>> lookup2))
            {
                removed = lookup2.TryRemove(key, out _);
            }
            else
            {
                removed = dictionary.Remove(key.ToString());
            }

            if (!removed)
            {
                KeyNotFoundException.ThrowKeyNotFound();
            }
        }
    }
}
#endif
