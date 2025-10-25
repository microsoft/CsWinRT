// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// An interface for implementations of <see cref="Windows.Foundation.Collections.IObservableMap{K, V}"/> types.
/// </summary>
/// <typeparam name="TKey">The type of keys in the observable map.</typeparam>
/// <typeparam name="TValue">The type of values in the observable map.</typeparam>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IObservableMapMethodsImpl<TKey, TValue>
{
    /// <summary>
    /// Returns the <see cref="EventSource{T}"/> instance associated with <see cref="Windows.Foundation.Collections.IObservableMap{K, V}.MapChanged"/>.
    /// </summary>
    /// <param name="thisObject">The <see cref="WindowsRuntimeObject"/> instance to use.</param>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <returns>The <see cref="EventSource{T}"/> instance associated with <see cref="Windows.Foundation.Collections.IObservableMap{K, V}.MapChanged"/>.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.iobservablemap-2.mapchanged"/>
    static abstract MapChangedEventHandlerEventSource<TKey, TValue> MapChanged(WindowsRuntimeObject thisObject, WindowsRuntimeObjectReference thisReference);
}
