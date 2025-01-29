// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#nullable enable

namespace WinRT
{
    /// <summary>
    /// An event registration token table stores mappings from delegates to event tokens, in order to support
    /// sourcing WinRT style events from managed code. This only supports events for CCW objects.
    /// </summary>
    /// <typeparam name="T">The event handler type to use in the table.</typeparam>
#if EMBED
    internal
#else
    public
#endif
    sealed class EventRegistrationTokenTable<T>
        where T : Delegate
    {
        /// <summary>
        /// The hashcode of the delegate type, being set in the upper 32 bits of the registration tokens.
        /// </summary>
        private static readonly int TypeOfTHashCode = GetTypeOfTHashCode();

        private static int GetTypeOfTHashCode()
        {
            int hashCode = typeof(T).GetHashCode();

            // There is a minimal but non-zero chance that the hashcode of the T type argument will be 0.
            // If that is the case, it means that it is possible for an event registration token to just
            // be 0, which will happen when the low 32 bits also wrap around and go through 0. Such a
            // registration token is not valid as per the WinRT spec, see:
            // https://learn.microsoft.com/uwp/api/windows.foundation.eventregistrationtoken.value.
            // To work around this, we just check for this edge case and return a magic constant instead.
            if (hashCode == 0)
            {
                return 0x5FC74196;
            }

            return hashCode;
        }

        // Note this dictionary is also used as the synchronization object for this table
        private readonly Dictionary<int, object> m_tokens = new();

        // The current counter used for the low 32 bits of the registration tokens.
        // We explicit use [int.MinValue, int.MaxValue] as the range, as this value
        // is expected to eventually wrap around, and we don't want to lose the
        // additional possible range of negative values (there's no reason for that).
        private int m_low32Bits =
#if NET8_0_OR_GREATER
            Random.Shared.Next(int.MinValue, int.MaxValue);
#else
            new Random().Next(int.MinValue, int.MaxValue);
#endif
        /// <summary>
        /// Adds an event handler to the table and retrieves the <see cref="EventRegistrationToken"/> value for it.
        /// </summary>
        /// <param name="handler">The handler to add to the table.</param>
        /// <returns>The <see cref="EventRegistrationToken"/> value for the new handler.</returns>
        /// <remarks>
        /// <para>Handler can be registered multiple times, and they will use a different token each time.</para>
        /// <para>If the input handler is <see langword="null"/>, the resulting token will be 0.</para>
        /// </remarks>
        public EventRegistrationToken AddEventHandler(T? handler)
        {
            // Windows Runtime allows null handlers. Assign those the default token (token value 0) for simplicity
            if (handler is null)
            {
                return default;
            }

            lock (m_tokens)
            {
                // Get a registration token, making sure that we haven't already used the value. This should be quite
                // rare, but in the case it does happen, just keep trying until we find one that's unused. Note that
                // this mutable part of the token is just 32 bit wide (the lower 32 bits). The upper 32 bits are fixed.
                //
                // Note that:
                //   - If there is a handler assigned to the generated initial token value, it is not necessarily
                //     this handler.
                //   - If there is no handler assigned to the generated initial token value, the handler may still
                //     be registered under a different token.
                //
                // Effectively the only reasonable thing to do with this value is to use it as a good starting point
                // for generating a token for handler.
                //
                // We want to generate a token value that has the following properties:
                //   1. Is quickly obtained from the handler instance (in this case, it doesn't depend on it at all).
                //   2. Uses bits in the upper 32 bits of the 64 bit value, in order to avoid bugs where code
                //      may assume the value is really just 32 bits.
                //   3. Uses bits in the bottom 32 bits of the 64 bit value, in order to ensure that code doesn't
                //      take a dependency on them always being 0.
                //
                // The simple algorithm chosen here is to simply assign the upper 32 bits the metadata token of the
                // event handler type, and the lower 32 bits to an incremental counter starting from some arbitrary
                // constant. Using the metadata token for the upper 32 bits gives us at least a small chance of being
                // able to identify a totally corrupted token if we ever come across one in a minidump or other scenario.
                //
                // We should feel free to change this algorithm as other requirements / optimizations become available.
                // This implementation is sufficiently random that code cannot simply guess the value to take a dependency
                // upon it. (Simply applying the hash-value algorithm directly won't work in the case of collisions,
                // where we'll use a different token value).
                int tokenLow32Bits;

#if NET8_0_OR_GREATER
                do
                {
                    // When on .NET 6+, just iterate on TryAdd, which allows skipping the extra
                    // lookup on the last iteration (as the handler is added rigth away instead).
                    //
                    // We're doing this do-while loop here and incrementing 'm_low32Bits' on every failed insertion to work
                    // around one possible (theoretical) performance problem. Suppose the candidate token was somehow already
                    // used (not entirely clear when that would happen in practice). Incrementing only the local value from the
                    // loop would mean we could "race past" the value in 'm_low32Bits', meaning that all subsequent registrations
                    // would then also go through unnecessary extra lookups as the value of those lower 32 bits "catches up" to
                    // the one that ended up being used here. So we can avoid that by simply incrementing both of them every time.
                    tokenLow32Bits = m_low32Bits++;
                }
                while (!m_tokens.TryAdd(tokenLow32Bits, handler));
#else
                do
                {
                    tokenLow32Bits = m_low32Bits++;
                }
                while (m_tokens.ContainsKey(tokenLow32Bits));
                m_tokens[tokenLow32Bits] = handler;
#endif
                // The real event registration token is composed this way:
                //   - The upper 32 bits are the hashcode of the T type argument.
                //   - The lower 32 bits are the valid token computed above.
                return new() { Value = (long)(((ulong)(uint)TypeOfTHashCode << 32) | (uint)tokenLow32Bits) };
            }
        }

        /// <summary>
        /// Removes an event handler from the table and retrieves the delegate associated with the input token, if it exists.
        /// </summary>
        /// <param name="token">The registration token to use to remove the event handler.</param>
        /// <param name="handler">The resulting delegate associated with <paramref name="token"/>, if it exists.</param>
        /// <returns>Whether or not a registered event handler could be retrieved and removed from the table.</returns>
        public bool RemoveEventHandler(
            EventRegistrationToken token,
#if NET8_0_OR_GREATER
            [NotNullWhen(true)]
#endif
            out T? handler)
        {
            // If the token doesn't have the upper 32 bits set to the hashcode of the delegate
            // type in use, we know that the token cannot possibly have a registered handler.
            //
            // Note that both here right after the right shift by 32 bits (since we want to read
            // the upper 32 bits to compare against the T hashcode) and below (where we want to
            // read the lower 32 bits to use as lookup index into our dictionary), we're just
            // casting to int as a simple and efficient way of truncating the input 64 bit value.
            // That is, '(int)i64' is the same as '(int)(i64 & 0xFFFFFFFF)', but more readable.
            if ((int)((ulong)token.Value >> 32) != TypeOfTHashCode)
            {
                handler = null;

                return false;
            }

            lock (m_tokens)
            {
#if NET8_0_OR_GREATER
                // On .NET 6 and above, we can use a single lookup to both check whether the token
                // exists in the table, remove it, and also retrieve the removed handler to return.
                if (m_tokens.Remove((int)token.Value, out object? obj))
                {
                    handler = Unsafe.As<T>(obj);

                    return true;
                }
#else
                if (m_tokens.TryGetValue((int)token.Value, out object? obj))
                {
                    m_tokens.Remove((int)token.Value);

                    handler = Unsafe.As<T>(obj);

                    return true;
                }
#endif
            }

            handler = null;

            return false;
        }
    }
}

// Restore in case this file is merged with others.
#nullable restore