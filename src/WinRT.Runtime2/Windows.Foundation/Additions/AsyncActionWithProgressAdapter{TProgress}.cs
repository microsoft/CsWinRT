// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Implements the Windows Runtime <see cref="IAsyncActionWithProgress{TProgress}"/> interface by wrapping a <see cref="Task"/> instance.
/// </summary>
/// <typeparam name="TProgress">The type of progress information.</typeparam>
[SupportedOSPlatform("windows10.0.10240.0")]
internal sealed class AsyncActionWithProgressAdapter<TProgress> : TaskToAsyncInfoAdapter<
    VoidValueTypeParameter,
    TProgress,
    AsyncActionWithProgressCompletedHandler<TProgress>,
    AsyncActionProgressHandler<TProgress>>,
    IAsyncActionWithProgress<TProgress>
{
    /// <summary>
    /// Creates a new <see cref="AsyncActionWithProgressAdapter{TProgress}"/> instance with the specified parameters.
    /// </summary>
    /// <param name="factory">The function to invoke to create the <see cref="Task"/> instance to wrap.</param>
    public AsyncActionWithProgressAdapter(Func<CancellationToken, IProgress<TProgress>, Task> factory)
         : base(factory)
    {
    }

    /// <summary>
    /// Creates a new <see cref="AsyncActionWithProgressAdapter{TProgress}"/> instance with the specified parameters.
    /// </summary>
    /// <param name="_">The <see cref="CompletedTaskPlaceholder"/> value to select this overload.</param>
    public AsyncActionWithProgressAdapter(CompletedTaskPlaceholder _)
        : base(default(VoidValueTypeParameter))
    {
    }

    /// <summary>
    /// Creates a new <see cref="AsyncActionWithProgressAdapter{TProgress}"/> instance with the specified parameters.
    /// </summary>
    /// <param name="_">The <see cref="CanceledTaskPlaceholder"/> value to select this overload.</param>
    public AsyncActionWithProgressAdapter(CanceledTaskPlaceholder _)
        : base(default(CanceledTaskPlaceholder))
    {
    }

    /// <summary>
    /// Creates a new <see cref="AsyncActionWithProgressAdapter{TProgress}"/> instance with the specified parameters.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to use to set the error state for the resulting instance.</param>
    public AsyncActionWithProgressAdapter(Exception exception)
        : base(exception)
    {
    }

    /// <inheritdoc/>
    public void GetResults()
    {
        _ = GetResultsCore();
    }

    /// <inheritdoc/>
    internal override void OnCompleted(AsyncActionWithProgressCompletedHandler<TProgress> handler, AsyncStatus asyncStatus)
    {
        handler(this, asyncStatus);
    }

    /// <inheritdoc/>
    internal override void OnProgress(AsyncActionProgressHandler<TProgress> handler, TProgress progressInfo)
    {
        handler(this, progressInfo);
    }
}