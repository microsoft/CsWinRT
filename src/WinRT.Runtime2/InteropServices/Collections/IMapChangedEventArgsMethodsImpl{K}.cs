// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// An interface for implementations of <c>Windows.Foundation.Collections.IMapChangedEventArgs&lt;K&gt;</c> types.
/// </summary>
/// <typeparam name="K">The type of keys in the map.</typeparam>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IMapChangedEventArgsMethodsImpl<K>
{
    /// <summary>
    /// Returns the key of the item that changed.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <returns>The key of the item that changed.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.imapchangedeventargs-1.index"/>
    static abstract K Key(WindowsRuntimeObjectReference thisReference);
}