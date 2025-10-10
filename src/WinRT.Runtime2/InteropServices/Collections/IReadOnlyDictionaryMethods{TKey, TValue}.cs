// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable CS1573

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Methods for <see cref="IReadOnlyDictionary{TKey, TValue}"/> types.
/// </summary>
/// <typeparam name="TKey">The type of keys in the read-only dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the read-only dictionary.</typeparam>
/// <remarks>
/// This type should only be used by generated code.
/// </remarks>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IReadOnlyDictionaryMethods<TKey, TValue>
{
    /// <inheritdoc cref="IReadOnlyDictionary{TKey, TValue}.this"/>
    /// <typeparam name="TMethods">The <see cref="IMapViewMethodsImpl{K, V}"/> implementation to use.</typeparam>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    public static TValue Item<TMethods>(WindowsRuntimeObjectReference thisReference, TKey key)
        where TMethods : IMapViewMethodsImpl<TKey, TValue>
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

    /// <inheritdoc cref="IReadOnlyDictionary{TKey, TValue}.ContainsKey"/>
    /// <typeparam name="TMethods">The <see cref="IMapViewMethodsImpl{K, V}"/> implementation to use.</typeparam>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    public static bool ContainsKey<TMethods>(WindowsRuntimeObjectReference thisReference, TKey key)
        where TMethods : IMapViewMethodsImpl<TKey, TValue>
    {
        ArgumentNullException.ThrowIfNull(key);

        return TMethods.HasKey(thisReference, key);
    }

    /// <inheritdoc cref="IReadOnlyDictionary{TKey, TValue}.TryGetValue"/>
    /// <typeparam name="TMethods">The <see cref="IMapViewMethodsImpl{K, V}"/> implementation to use.</typeparam>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    public static bool TryGetValue<TMethods>(WindowsRuntimeObjectReference thisReference, TKey key, [MaybeNullWhen(false)] out TValue value)
        where TMethods : IMapViewMethodsImpl<TKey, TValue>
    {
        ArgumentNullException.ThrowIfNull(key);

        // It may be faster to call 'HasKey' and then 'Lookup' if we know the key is present. Otherwise, we
        // would have to throw and catch an exception in case of failure (i.e. if the key is not present).
        if (!TMethods.HasKey(thisReference, key))
        {
            value = default;

            return false;
        }

        // Do the normal lookup now (this might technically still fail in case of race conditions, but that's fine)
        value = Item<TMethods>(thisReference, key);

        return true;
    }
}
