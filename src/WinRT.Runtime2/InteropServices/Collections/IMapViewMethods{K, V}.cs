// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// An interface for implementations of <c>Windows.Foundation.Collections.IMapView&lt;K, V&gt;</c> types.
/// </summary>
/// <typeparam name="K">The type of keys in the map.</typeparam>
/// <typeparam name="V">The type of values in the map.</typeparam>
/// <remarks>
/// This type should only be used by generated code.
/// </remarks>
[Obsolete("This type is an implementation detail, and it's only meant to be consumed by 'cswinrtgen'")]
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IMapViewMethods<K, V>
{
    /// <summary>
    /// Returns the item at the specified key in the map view.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <param name="key">The key to locate in the map view.</param>
    /// <returns>The value, if an item with the specified key exists. Use the <see cref="HasKey"/> method to determine whether the key exists.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.imapview-2.lookup"/>
    static abstract V Lookup(WindowsRuntimeObjectReference thisReference, K key);

    /// <summary>
    /// Determines whether the map view contains the specified key.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <param name="key">The key to locate in the map view.</param>
    /// <returns><see langword="true"/> if the key is found; otherwise, <see langword="false"/>.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.imapview-2.haskey"/>
    static abstract bool HasKey(WindowsRuntimeObjectReference thisReference, K key);
}
