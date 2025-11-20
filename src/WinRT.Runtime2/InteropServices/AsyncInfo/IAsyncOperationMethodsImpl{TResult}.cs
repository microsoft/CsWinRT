// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using Windows.Foundation;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// An interface for implementations of <see cref="IAsyncOperation{TResult}"/> types.
/// </summary>
/// <typeparam name="TResult">The result type.</typeparam>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IAsyncOperationMethodsImpl<TResult>
{
    /// <summary>
    /// Gets the delegate that is called when the action completes.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <returns>The delegate.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.iasyncoperation-1.completed"/>
    static abstract AsyncOperationCompletedHandler<TResult>? Completed(WindowsRuntimeObjectReference thisReference);

    /// <summary>
    /// Sets the delegate that is called when the action completes.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <param name="handler">The delegate.</param>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.iasyncoperation-1.completed"/>
    static abstract void Completed(WindowsRuntimeObjectReference thisReference, AsyncOperationCompletedHandler<TResult>? handler);

    /// <summary>
    /// Returns the results of the operation.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <returns>The results of the operation.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.iasyncoperation-1.getresults"/>
    static abstract TResult GetResults(WindowsRuntimeObjectReference thisReference);
}