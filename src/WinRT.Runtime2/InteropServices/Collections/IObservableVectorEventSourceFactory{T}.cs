// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY

namespace WindowsRuntime.InteropServices;

/// <summary>
/// An interface for implementations of <see cref="Windows.Foundation.Collections.IObservableVector{T}"/> types.
/// </summary>
/// <typeparam name="T">The type of elements in the observable vector.</typeparam>
public interface IObservableVectorEventSourceFactory<T>
{
    /// <summary>
    /// Creates a new <see cref="EventSource{T}"/> instance to be used with <see cref="Windows.Foundation.Collections.IObservableVector{T}.VectorChanged"/>.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <returns>The <see cref="EventSource{T}"/> instance to be used with <see cref="Windows.Foundation.Collections.IObservableVector{T}.VectorChanged"/>.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.iobservablevector-1.vectorchanged"/>
    static abstract ABI.Windows.Foundation.Collections.VectorChangedEventHandlerEventSource<T> VectorChanged(WindowsRuntimeObjectReference thisReference);
}
#endif
