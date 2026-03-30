// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY

namespace WindowsRuntime.InteropServices;

/// <summary>
/// An interface for implementations of <see cref="Windows.Foundation.Collections.IObservableMap{K, V}"/> types.
/// </summary>
/// <typeparam name="TKey">The type of keys in the observable map.</typeparam>
/// <typeparam name="TValue">The type of values in the observable map.</typeparam>
public interface IObservableMapEventSourceFactory<TKey, TValue>
{
    /// <summary>
    /// Creates a new <see cref="EventSource{T}"/> instance to be used with <see cref="Windows.Foundation.Collections.IObservableMap{K, V}.MapChanged"/>.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <returns>The <see cref="EventSource{T}"/> instance to be used with <see cref="Windows.Foundation.Collections.IObservableMap{K, V}.MapChanged"/>.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.iobservablemap-2.mapchanged"/>
    static abstract ABI.Windows.Foundation.Collections.MapChangedEventHandlerEventSource<TKey, TValue> MapChanged(WindowsRuntimeObjectReference thisReference);
}
#endif
