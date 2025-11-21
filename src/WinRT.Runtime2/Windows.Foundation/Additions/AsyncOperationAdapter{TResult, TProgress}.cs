// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Implements the Windows Runtime <see cref="IAsyncOperationWithProgress{TResult, TProgress}"/> interface by wrapping a <see cref="Task{TResult}"/> instance.
/// </summary>
/// <typeparam name="TResult">The result type.</typeparam>
/// <typeparam name="TProgress">The type of progress information.</typeparam>
[SupportedOSPlatform("windows10.0.10240.0")]
internal sealed class AsyncOperationProgressAdapter<TResult, TProgress> : TaskToAsyncInfoAdapter<
    TResult,
    TProgress,
    AsyncOperationWithProgressCompletedHandler<TResult, TProgress>,
    AsyncOperationProgressHandler<TResult, TProgress>>,
    IAsyncOperationWithProgress<TResult, TProgress>
{
    /// <summary>
    /// Creates a new <see cref="AsyncOperationProgressAdapter{TResult, TProgress}"/> instance with the specified parameters.
    /// </summary>
    /// <param name="factory">The function to invoke to create the <see cref="Task{TResult}"/> instance to wrap.</param>
    public AsyncOperationProgressAdapter(Func<CancellationToken, IProgress<TProgress>, Task<TResult>> factory)

         : base(factory)
    {
    }

    /// <summary>
    /// Creates a new <see cref="AsyncOperationProgressAdapter{TResult, TProgress}"/> instance with the specified parameters.
    /// </summary>
    /// <param name="result">The result to wrap (which assumes the operation completed synchronously).</param>
    public AsyncOperationProgressAdapter(TResult result)
        : base(result)
    {
    }

    /// <summary>
    /// Creates a new <see cref="AsyncOperationProgressAdapter{TResult, TProgress}"/> instance with the specified parameters.
    /// </summary>
    /// <param name="isCanceled">Whether the resulting instance should be marked as canceled or completed.</param>
    public AsyncOperationProgressAdapter(bool isCanceled)
        : base(default(TResult)!)
    {
        if (isCanceled)
        {
            _ = DangerousSetCanceled();
        }
    }

    /// <summary>
    /// Creates a new <see cref="AsyncOperationProgressAdapter{TResult, TProgress}"/> instance with the specified parameters.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to use to set the error state for the resulting instance.</param>
    public AsyncOperationProgressAdapter(Exception exception)
        : base(default(TResult)!)
    {
        _ = DangerousSetError(exception);
    }

    /// <inheritdoc/>
    public TResult GetResults()
    {
        return GetResultsInternal();
    }

    /// <inheritdoc/>
    internal override void OnCompleted(AsyncOperationWithProgressCompletedHandler<TResult, TProgress> handler, AsyncStatus asyncStatus)
    {
        handler(this, asyncStatus);
    }

    /// <inheritdoc/>
    internal override void OnProgress(AsyncOperationProgressHandler<TResult, TProgress> handler, TProgress progressInfo)
    {
        handler(this, progressInfo);
    }
}