// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Implements the Windows Runtime <see cref="IAsyncAction"/> interface by wrapping a <see cref="Task"/> instance.
/// </summary>
[SupportedOSPlatform("windows10.0.10240.0")]
internal sealed class AsyncActionAdapter : TaskToAsyncInfoAdapter<
    VoidValueTypeParameter,
    VoidValueTypeParameter,
    AsyncActionCompletedHandler,
    VoidReferenceTypeParameter>,
    IAsyncAction
{
    /// <summary>
    /// Creates a new <see cref="AsyncActionAdapter"/> instance with the specified parameters.
    /// </summary>
    /// <param name="factory">The function to invoke to create the <see cref="Task"/> instance to wrap.</param>
    public AsyncActionAdapter(Func<CancellationToken, Task> factory)
        : base(factory)
    {
    }

    /// <summary>
    /// Creates a new <see cref="AsyncActionAdapter"/> instance with the specified parameters.
    /// </summary>
    /// <param name="task">The <see cref="Task"/> instance to wrap.</param>
    /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/> instance to use for cancellation.</param>
    public AsyncActionAdapter(Task task, CancellationTokenSource? cancellationTokenSource)
        : base(task, cancellationTokenSource, progress: null)
    {
    }

    /// <summary>
    /// Creates a new <see cref="AsyncActionAdapter"/> instance with the specified parameters.
    /// </summary>
    /// <param name="isCanceled">Whether the resulting instance should be marked as canceled or completed.</param>
    public AsyncActionAdapter(bool isCanceled)
        : base(default(VoidValueTypeParameter))
    {
        if (isCanceled)
        {
            _ = DangerousSetCanceled();
        }
    }

    /// <summary>
    /// Creates a new <see cref="AsyncActionAdapter"/> instance with the specified parameters.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to use to set the error state for the resulting instance.</param>
    public AsyncActionAdapter(Exception exception)
        : base(exception)
    {
    }

    /// <inheritdoc/>
    public void GetResults()
    {
        _ = GetResultsInternal();
    }

    /// <inheritdoc/>
    internal override void OnCompleted(AsyncActionCompletedHandler handler, AsyncStatus asyncStatus)
    {
        handler(this, asyncStatus);
    }
}