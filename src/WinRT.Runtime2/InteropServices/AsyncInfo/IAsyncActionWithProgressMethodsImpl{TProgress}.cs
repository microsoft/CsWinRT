// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using Windows.Foundation;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// An interface for implementations of <see cref="IAsyncActionWithProgress{TProgress}"/> types.
/// </summary>
/// <typeparam name="TProgress">The type of progress information.</typeparam>
/// <remarks>
/// This type should only be used by generated code.
/// </remarks>
[Obsolete("This type is an implementation detail, and it's only meant to be consumed by 'cswinrtgen'")]
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IAsyncActionWithProgressMethodsImpl<TProgress>
{
    /// <summary>
    /// Gets the callback method that receives progress notification.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <returns>The callback.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.iasyncactionwithprogress-1.progress"/>
    static abstract AsyncActionProgressHandler<TProgress>? Progress(WindowsRuntimeObjectReference thisReference);

    /// <summary>
    /// Sets the callback method that receives progress notification.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <param name="handler">The callback.</param>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.iasyncactionwithprogress-1.progress"/>
    static abstract void Progress(WindowsRuntimeObjectReference thisReference, AsyncActionProgressHandler<TProgress>? handler);

    /// <summary>
    /// Gets the delegate that is called when the action completes.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <returns>The delegate.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.iasyncactionwithprogress-1.completed"/>
    static abstract AsyncActionWithProgressCompletedHandler<TProgress>? Completed(WindowsRuntimeObjectReference thisReference);

    /// <summary>
    /// Sets the delegate that is called when the action completes.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <param name="handler">The delegate.</param>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.iasyncactionwithprogress-1.completed"/>
    static abstract void Completed(WindowsRuntimeObjectReference thisReference, AsyncActionWithProgressCompletedHandler<TProgress>? handler);
}
