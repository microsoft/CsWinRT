// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

#pragma warning disable IDE0045, IDE0046

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Extensions for the <see cref="IReadOnlyDictionaryAdapter{TKey, TValue}"/> type.
/// </summary>
[WindowsRuntimeImplementationOnlyMember]
public static class IReadOnlyDictionaryAdapterExtensions
{
    extension<TValue>(IReadOnlyDictionaryAdapter<string, TValue>)
    {
        /// <inheritdoc cref="IReadOnlyDictionaryAdapter{TKey, TValue}.Lookup"/>
        /// <remarks>
        /// This overload can be used to avoid a <see cref="string"/> allocation on the caller side.
        /// </remarks>
        public static TValue Lookup(IReadOnlyDictionary<string, TValue> dictionary, ReadOnlySpan<char> key)
        {
            bool found;
            TValue? value;

            // Try to lookup via an alternate comparer, if we can get one. This allows us to avoid materializing
            // the 'string' value for the key, whenever possible. In practice, these cases should pretty much
            // cover almost all scenarios. Custom dictionaries are rare. We can't use an 'is' check for
            // 'Dictionary<TKey, TValue' and 'ConcurrentDictionary<TKey, TValue>', because we could have some
            // user-defined type that reimplemented 'IReadOnlyDictionary<TKey, TValue>.TryGetValue' with a
            // different implementation that's not guaranteed to match what the alternate lookup does. So in
            // those cases, we have to fallback to that method instead, hence the explicit type checks here.
            // This is not needed for 'FrozenDictionary<TKey, TValue>', as only the BCL can instantiate it.
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
        /// Determines whether the map view contains the specified key.
        /// </summary>
        /// <param name="dictionary">The wrapped <see cref="IReadOnlyDictionary{TKey, TValue}"/> instance.</param>
        /// <param name="key">The key to locate in the map view.</param>
        /// <returns>Whether the key was found.</returns>
        /// <remarks>
        /// This overload can be used to avoid a <see cref="string"/> allocation on the caller side.
        /// </remarks>
        /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.imapview-2.haskey"/>
        public static bool HasKey(IReadOnlyDictionary<string, TValue> dictionary, ReadOnlySpan<char> key)
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
    }
}
