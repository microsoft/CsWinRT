// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// An interface for implementations of <see cref="Windows.Foundation.Collections.IObservableVector{T}"/> types.
/// </summary>
/// <typeparam name="T">The type of elements in the observable vector.</typeparam>
/// <remarks>
/// This type should only be used by generated code.
/// </remarks>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage, DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId)]
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IObservableVectorMethodsImpl<T>
{
    /// <summary>
    /// Returns the <see cref="EventSource{T}"/> instance associated with <see cref="Windows.Foundation.Collections.IObservableVector{T}.VectorChanged"/>.
    /// </summary>
    /// <param name="thisObject">The <see cref="WindowsRuntimeObject"/> instance to use.</param>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <returns>The <see cref="EventSource{T}"/> instance associated with <see cref="Windows.Foundation.Collections.IObservableVector{T}.VectorChanged"/>.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.iobservablevector-1.vectorchanged"/>
    static abstract VectorChangedEventHandlerEventSource<T> VectorChanged(WindowsRuntimeObject thisObject, WindowsRuntimeObjectReference thisReference);
}
