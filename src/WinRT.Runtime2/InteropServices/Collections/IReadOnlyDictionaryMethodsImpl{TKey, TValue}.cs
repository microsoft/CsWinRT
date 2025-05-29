// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable CS1573

namespace WindowsRuntime.InteropServices;

/// <summary>
/// An interface for implementations of <see cref="IReadOnlyDictionary{TKey, TValue}"/> types.
/// </summary>
/// <typeparam name="TKey">The type of keys in the read-only dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the read-only dictionary.</typeparam>
/// <remarks>
/// This type should only be used by generated code.
/// </remarks>
[Obsolete("This type is an implementation detail, and it's only meant to be consumed by 'cswinrtgen'")]
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IReadOnlyDictionaryMethodsImpl<TKey, TValue>
{
    /// <inheritdoc cref="IReadOnlyDictionary{TKey, TValue}.this"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    static abstract TValue Item(WindowsRuntimeObjectReference thisReference, TKey key);

    /// <inheritdoc cref="IReadOnlyDictionary{TKey, TValue}.ContainsKey"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    static abstract bool ContainsKey(WindowsRuntimeObjectReference thisReference, TKey key);

    /// <inheritdoc cref="IReadOnlyDictionary{TKey, TValue}.TryGetValue"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    static abstract bool TryGetValue(WindowsRuntimeObjectReference thisReference, TKey key, [MaybeNullWhen(false)] out TValue value);
}
