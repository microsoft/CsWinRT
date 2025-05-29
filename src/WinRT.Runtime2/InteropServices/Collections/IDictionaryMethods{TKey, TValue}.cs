// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable CS1573

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Methods for implementations of <see cref="IDictionary{TKey, TValue}"/> types.
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
/// <remarks>
/// This type should only be used by generated code.
/// </remarks>
[Obsolete("This type is an implementation detail, and it's only meant to be consumed by 'cswinrtgen'")]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IDictionaryMethods<TKey, TValue>
{
    /// <inheritdoc cref="IDictionary{TKey, TValue}.this"/>
    /// <typeparam name="TMethods">The <see cref="IMapMethodsImpl{K, V}"/> implementation to use.</typeparam>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    public static TValue Item<TMethods>(WindowsRuntimeObjectReference thisReference, TKey key)
        where TMethods : IMapMethodsImpl<TKey, TValue>
    {
        ArgumentNullException.ThrowIfNull(key);

        try
        {
            return TMethods.Lookup(thisReference, key);
        }
        catch (Exception e) when (e.HResult == WellKnownErrorCodes.E_BOUNDS)
        {
            throw new KeyNotFoundException("Arg_KeyNotFoundWithKey", e);
        }
    }

    /// <inheritdoc cref="IDictionary{TKey, TValue}.this"/>
    /// <typeparam name="TMethods">The <see cref="IMapMethodsImpl{K, V}"/> implementation to use.</typeparam>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    public static void Item<TMethods>(WindowsRuntimeObjectReference thisReference, TKey key, TValue value)
        where TMethods : IMapMethodsImpl<TKey, TValue>
    {
        ArgumentNullException.ThrowIfNull(key);

        // The semantics of the setter are to either insert or replace, so we can ignore the result here
        _ = TMethods.Insert(thisReference, key, value);
    }

    /// <inheritdoc cref="IDictionary{TKey, TValue}.Add"/>
    /// <typeparam name="TMethods">The <see cref="IMapMethodsImpl{K, V}"/> implementation to use.</typeparam>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    public static void Add<TMethods>(WindowsRuntimeObjectReference thisReference, TKey key, TValue value)
        where TMethods : IMapMethodsImpl<TKey, TValue>
    {
        ArgumentNullException.ThrowIfNull(key);

        // Skip the insertion if the key already exists, just throw an exception directly
        if (ContainsKey<TMethods>(thisReference, key))
        {
            throw new ArgumentException("Argument_AddingDuplicate");
        }

        // We can ignore the result here too, as we expect the insertion to succeed
        _ = TMethods.Insert(thisReference, key, value);
    }

    /// <inheritdoc cref="IDictionary{TKey, TValue}.ContainsKey"/>
    /// <typeparam name="TMethods">The <see cref="IMapMethodsImpl{K, V}"/> implementation to use.</typeparam>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    public static bool ContainsKey<TMethods>(WindowsRuntimeObjectReference thisReference, TKey key)
        where TMethods : IMapMethodsImpl<TKey, TValue>
    {
        ArgumentNullException.ThrowIfNull(key);

        return TMethods.HasKey(thisReference, key);
    }

    /// <inheritdoc cref="IDictionary{TKey, TValue}.Remove"/>
    /// <typeparam name="TMethods">The <see cref="IMapMethodsImpl{K, V}"/> implementation to use.</typeparam>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    public static bool Remove<TMethods>(WindowsRuntimeObjectReference thisReference, TKey key)
        where TMethods : IMapMethodsImpl<TKey, TValue>
    {
        ArgumentNullException.ThrowIfNull(key);

        // If the key does not exist, we can just return 'false' and stop.
        // We do this check to avoid throwing and catching in case the key
        // is not present. This is the same we do for 'TryGetValue' as well.
        if (!ContainsKey<TMethods>(thisReference, key))
        {
            return false;
        }

        try
        {
            TMethods.Remove(thisReference, key);

            return true;
        }
        catch (Exception e) when (e.HResult == WellKnownErrorCodes.E_BOUNDS)
        {
            // The key is not present (e.g. due to a concurrent mutation).
            // In this case, we translate this 'HRESULT' to just 'false'.
            return false;
        }
    }

    /// <inheritdoc cref="IDictionary{TKey, TValue}.TryGetValue"/>
    /// <typeparam name="TMethods">The <see cref="IMapMethodsImpl{K, V}"/> implementation to use.</typeparam>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    public static bool TryGetValue<TMethods>(WindowsRuntimeObjectReference thisReference, TKey key, [MaybeNullWhen(false)] out TValue value)
        where TMethods : IMapViewMethodsImpl<TKey, TValue>
    {
        ArgumentNullException.ThrowIfNull(key);

        // Manual check to avoid throwing and catching, like in 'Remove' above
        if (!TMethods.HasKey(thisReference, key))
        {
            value = default;

            return false;
        }

        try
        {
            // Try to retrieve the item (we assume it should exist at this point)
            value = TMethods.Lookup(thisReference, key);

            return true;
        }
        catch (Exception e) when (e.HResult == WellKnownErrorCodes.E_BOUNDS)
        {
            // The map was probably mutated concurrently, so we just return 'false'
            value = default;

            return false;
        }
    }
}
