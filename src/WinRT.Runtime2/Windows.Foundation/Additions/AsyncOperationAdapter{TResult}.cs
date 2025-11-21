// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Implements the Windows Runtime <see cref="IAsyncOperation{TResult}"/> interface by wrapping a <see cref="Task{TResult}"/> instance.
/// </summary>
/// <typeparam name="TResult">The result type.</typeparam>
[SupportedOSPlatform("windows10.0.10240.0")]
internal sealed class AsyncOperationAdapter<TResult> : TaskToAsyncInfoAdapter<
    TResult,
    VoidValueTypeParameter,
    AsyncOperationCompletedHandler<TResult>,
    VoidReferenceTypeParameter>,
    IAsyncOperation<TResult>
{
    /// <summary>
    /// Creates a new <see cref="AsyncOperationAdapter{TResult}"/> instance with the specified parameters.
    /// </summary>
    /// <param name="factory">The function to invoke to create the <see cref="Task{TResult}"/> instance to wrap.</param>
    public AsyncOperationAdapter(Func<CancellationToken, Task<TResult>> factory)

         : base(factory)
    {
    }

    /// <summary>
    /// Creates a new <see cref="AsyncOperationAdapter{TResult}"/> instance with the specified parameters.
    /// </summary>
    /// <param name="task">The <see cref="Task"/> instance to wrap.</param>
    /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/> instance to use for cancellation.</param>
    public AsyncOperationAdapter(Task<TResult> task, CancellationTokenSource? cancellationTokenSource)

        : base(task, cancellationTokenSource, progress: null)
    {
    }

    /// <summary>
    /// Creates a new <see cref="AsyncOperationAdapter{TResult}"/> instance with the specified parameters.
    /// </summary>
    /// <param name="result">The result to wrap (which assumes the operation completed synchronously).</param>
    public AsyncOperationAdapter(TResult result)
        : base(result)
    {
    }

    /// <summary>
    /// Creates a new <see cref="AsyncOperationAdapter{TResult}"/> instance with the specified parameters.
    /// </summary>
    /// <param name="_">The <see cref="CanceledTaskPlaceholder"/> value to select this overload.</param>
    public AsyncOperationAdapter(CanceledTaskPlaceholder _)
        : base(default(CanceledTaskPlaceholder))
    {
    }

    /// <summary>
    /// Creates a new <see cref="AsyncOperationAdapter{TResult}"/> instance with the specified parameters.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to use to set the error state for the resulting instance.</param>
    public AsyncOperationAdapter(Exception exception)
        : base(exception)
    {
    }

    /// <inheritdoc/>
    public TResult GetResults()
    {
        return GetResultsCore();
    }

    /// <inheritdoc/>
    protected override void OnCompleted(AsyncOperationCompletedHandler<TResult> handler, AsyncStatus asyncStatus)
    {
        handler(this, asyncStatus);
    }
}