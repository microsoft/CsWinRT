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
internal sealed class TaskAdapter : UniversalTaskAdapter<
    ValueTypePlaceholder,
    ValueTypePlaceholder,
    AsyncActionCompletedHandler,
    ReferenceTypePlaceholder>,
    IAsyncAction
{
    /// <summary>
    /// Creates a new <see cref="TaskAdapter"/> instance with the specified parameters.
    /// </summary>
    /// <param name="factory">The function to invoke to create the <see cref="Task"/> instance to wrap.</param>
    public TaskAdapter(Func<CancellationToken, Task> factory)
        : base(factory)
    {
    }

    /// <summary>
    /// Creates a new <see cref="TaskAdapter"/> instance with the specified parameters.
    /// </summary>
    /// <param name="task">The <see cref="Task"/> instance to wrap.</param>
    /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/> instance to use for cancellation.</param>
    public TaskAdapter(Task task, CancellationTokenSource? cancellationTokenSource)
        : base(task, cancellationTokenSource, progress: null)
    {
    }

    /// <summary>
    /// Creates a new <see cref="TaskAdapter"/> instance with the specified parameters.
    /// </summary>
    /// <param name="_">The <see cref="CompletedTaskPlaceholder"/> value to select this overload.</param>
    public TaskAdapter(CompletedTaskPlaceholder _)
        : base(default(ValueTypePlaceholder))
    {
    }

    /// <summary>
    /// Creates a new <see cref="TaskAdapter"/> instance with the specified parameters.
    /// </summary>
    /// <param name="_">The <see cref="CanceledTaskPlaceholder"/> value to select this overload.</param>
    public TaskAdapter(CanceledTaskPlaceholder _)
        : base(default(CanceledTaskPlaceholder))
    {
    }

    /// <summary>
    /// Creates a new <see cref="TaskAdapter"/> instance with the specified parameters.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to use to set the error state for the resulting instance.</param>
    public TaskAdapter(Exception exception)
        : base(exception)
    {
    }

    /// <inheritdoc/>
    public void GetResults()
    {
        _ = GetResultsCore();
    }

    /// <inheritdoc/>
    protected override void OnCompleted(AsyncActionCompletedHandler handler, AsyncStatus asyncStatus)
    {
        handler(this, asyncStatus);
    }
}