// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable CS1573

namespace WindowsRuntime.InteropServices;

/// <summary>
/// An interface for implementations of <see cref="IDictionary{TKey, TValue}"/> types.
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
/// <remarks>
/// This type should only be used by generated code.
/// </remarks>
[Obsolete("This type is an implementation detail, and it's only meant to be consumed by 'cswinrtgen'")]
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDictionaryMethodsImpl<TKey, TValue>
{
    /// <inheritdoc cref="IDictionary{TKey, TValue}.this"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    static abstract TValue Item(WindowsRuntimeObjectReference thisReference, TKey key);

    /// <inheritdoc cref="IDictionary{TKey, TValue}.this"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    static abstract void Item(WindowsRuntimeObjectReference thisReference, TKey key, TValue value);

    /// <inheritdoc cref="IDictionary{TKey, TValue}.Add"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    static abstract void Add(WindowsRuntimeObjectReference thisReference, TKey key, TValue value);

    /// <inheritdoc cref="IDictionary{TKey, TValue}.ContainsKey"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    static abstract bool ContainsKey(WindowsRuntimeObjectReference thisReference, TKey key);

    /// <inheritdoc cref="IDictionary{TKey, TValue}.Remove"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    static abstract bool Remove(WindowsRuntimeObjectReference thisReference, TKey key);

    /// <inheritdoc cref="IDictionary{TKey, TValue}.TryGetValue"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    static abstract bool TryGetValue(WindowsRuntimeObjectReference thisReference, TKey key, [MaybeNullWhen(false)] out TValue value);
}
