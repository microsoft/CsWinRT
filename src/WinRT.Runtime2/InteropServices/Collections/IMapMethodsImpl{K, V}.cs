// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// An interface for implementations of <c>Windows.Foundation.Collections.IMap&lt;K, V&gt;</c> types.
/// </summary>
/// <typeparam name="K">The type of keys in the map.</typeparam>
/// <typeparam name="V">The type of values in the map.</typeparam>
/// <remarks>
/// This type should only be used by generated code.
/// </remarks>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IMapMethodsImpl<K, V>
{
    /// <summary>
    /// Returns the item at the specified key in the map view.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <param name="key">The key to locate in the map view.</param>
    /// <returns>The value, if an item with the specified key exists. Use the <see cref="HasKey"/> method to determine whether the key exists.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.imap-2.lookup"/>
    static abstract V Lookup(WindowsRuntimeObjectReference thisReference, K key);

    /// <summary>
    /// Determines whether the map view contains the specified key.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <param name="key">The key to locate in the map view.</param>
    /// <returns><see langword="true"/> if the key is found; otherwise, <see langword="false"/>.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.imap-2.haskey"/>
    static abstract bool HasKey(WindowsRuntimeObjectReference thisReference, K key);

    /// <summary>
    /// Inserts or replaces an item in the map.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <param name="key">The key associated with the item to insert.</param>
    /// <param name="value">The item to insert.</param>
    /// <returns><see langword="true"/> if an item with the specified key is an existing item that was replaced; otherwise, <see langword="false"/>.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.imap-2.insert"/>
    static abstract bool Insert(WindowsRuntimeObjectReference thisReference, K key, V value);

    /// <summary>
    /// Removes an item from the map.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <param name="key">The key associated with the item to remove.</param>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.imap-2.remove"/>
    static abstract void Remove(WindowsRuntimeObjectReference thisReference, K key);
}
