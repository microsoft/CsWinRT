// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using WindowsRuntime.InteropServices;

#pragma warning disable IDE0010

namespace Windows.Foundation.Tasks;

/// <summary>
/// Provides extensions for <see cref="IAsyncInfo"/> types to interoperate with <see cref="Task"/> types.
/// </summary>
[SupportedOSPlatform("windows10.0.10240.0")]
public static class WindowsRuntimeSystemExtensions
{
    /// <summary>
    /// Creates a <see cref="Task"/> for the asynchronous operation represented by the specified <see cref="IAsyncAction"/> instance.
    /// </summary>
    /// <param name="source">The input <see cref="IAsyncAction"/> instance.</param>
    /// <returns>The resulting <see cref="Task"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
    public static Task AsTask(this IAsyncAction source)
    {
        return AsTask(source, CancellationToken.None);
    }

    /// <summary>
    /// Creates a <see cref="Task"/> for the asynchronous operation represented by the specified <see cref="IAsyncAction"/> instance.
    /// </summary>
    /// <param name="source">The input <see cref="IAsyncAction"/> instance.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>The resulting <see cref="Task"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
    public static Task AsTask(this IAsyncAction source, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);

        // If the source is already an adapter over a 'Task', return the underlying 'Task' directly
        if (source is UniversalTaskAdapter { Task: Task task })
        {
            return cancellationToken.CanBeCanceled ?
                task.WaitAsync(cancellationToken) :
                task;
        }

        // Handle terminal states for the input async object
        switch (source.Status)
        {
            case AsyncStatus.Completed:
                return Task.CompletedTask;
            case AsyncStatus.Error:
                return Task.FromException(source.ErrorCode!);
            case AsyncStatus.Canceled:
                return Task.FromCanceled(cancellationToken.IsCancellationRequested ? cancellationToken : new CancellationToken(true));
        }

        // The input async object is in progress, so create a bridge to represent it as a 'Task' instance
        AsyncInfoTaskCompletionSource<ValueTypePlaceholder> bridge = new(source, cancellationToken);

        // Assign a completion handler to notify our bridge object of completion.
        // This will complete the 'Task' instance that we're returning to callers.
        source.Completed = bridge.Complete;

        return bridge.Task;
    }

    /// <summary>Gets an awaiter used to await this <see cref="IAsyncAction"/>.</summary>
    /// <param name="source">The input <see cref="IAsyncAction"/> instance.</param>
    /// <returns>An awaiter instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
    public static TaskAwaiter GetAwaiter(this IAsyncAction source)
    {
        return AsTask(source).GetAwaiter();
    }

    /// <summary>
    /// Creates a <see cref="Task{TResult}"/> for the asynchronous operation represented by the specified <see cref="IAsyncOperation{TResult}"/> instance.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="source">The input <see cref="IAsyncOperation{TResult}"/> instance.</param>
    /// <returns>The resulting <see cref="Task{TResult}"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
    public static Task<TResult> AsTask<TResult>(this IAsyncOperation<TResult> source)
    {
        return AsTask(source, CancellationToken.None);
    }

    /// <summary>
    /// Creates a <see cref="Task{TResult}"/> for the asynchronous operation represented by the specified <see cref="IAsyncOperation{TResult}"/> instance.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="source">The input <see cref="IAsyncOperation{TResult}"/> instance.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>The resulting <see cref="Task{TResult}"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
    public static Task<TResult> AsTask<TResult>(this IAsyncOperation<TResult> source, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source is UniversalTaskAdapter { Task: Task<TResult> task })
        {
            return cancellationToken.CanBeCanceled ?
                task.WaitAsync(cancellationToken) :
                task;
        }

        switch (source.Status)
        {
            case AsyncStatus.Completed:
                return Task.FromResult(source.GetResults());
            case AsyncStatus.Error:
                return Task.FromException<TResult>(source.ErrorCode!);
            case AsyncStatus.Canceled:
                return Task.FromCanceled<TResult>(cancellationToken.IsCancellationRequested ? cancellationToken : new CancellationToken(true));
        }

        AsyncInfoTaskCompletionSource<TResult, ValueTypePlaceholder> bridge = new(source, cancellationToken);

        source.Completed = bridge.Complete;

        return bridge.Task;
    }

    /// <summary>Gets an awaiter used to await this <see cref="IAsyncOperation{TResult}"/>.</summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="source">The input <see cref="IAsyncOperation{TResult}"/> instance.</param>
    /// <returns>An awaiter instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
    public static TaskAwaiter<TResult> GetAwaiter<TResult>(this IAsyncOperation<TResult> source)
    {
        return AsTask(source).GetAwaiter();
    }

    /// <summary>
    /// Creates a <see cref="Task"/> for the asynchronous operation represented by the specified <see cref="IAsyncActionWithProgress{TProgress}"/> instance.
    /// </summary>
    /// <typeparam name="TProgress">The type of progress information.</typeparam>
    /// <param name="source">The input <see cref="IAsyncActionWithProgress{TProgress}"/> instance.</param>
    /// <returns>The resulting <see cref="Task"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
    public static Task AsTask<TProgress>(this IAsyncActionWithProgress<TProgress> source)
    {
        return AsTask(source, CancellationToken.None);
    }

    /// <summary>
    /// Creates a <see cref="Task"/> for the asynchronous operation represented by the specified <see cref="IAsyncActionWithProgress{TProgress}"/> instance.
    /// </summary>
    /// <typeparam name="TProgress">The type of progress information.</typeparam>
    /// <param name="source">The input <see cref="IAsyncActionWithProgress{TProgress}"/> instance.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>The resulting <see cref="Task"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
    public static Task AsTask<TProgress>(this IAsyncActionWithProgress<TProgress> source, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source is UniversalTaskAdapter { Task: Task task })
        {
            return cancellationToken.CanBeCanceled ?
                task.WaitAsync(cancellationToken) :
                task;
        }

        switch (source.Status)
        {
            case AsyncStatus.Completed:
                return Task.CompletedTask;
            case AsyncStatus.Error:
                return Task.FromException(source.ErrorCode!);
            case AsyncStatus.Canceled:
                return Task.FromCanceled(cancellationToken.IsCancellationRequested ? cancellationToken : new CancellationToken(true));
        }

        AsyncInfoTaskCompletionSource<TProgress> bridge = new(source, cancellationToken);

        source.Completed = bridge.Complete;

        return bridge.Task;
    }

    /// <summary>
    /// Creates a <see cref="Task"/> for the asynchronous operation represented by the specified <see cref="IAsyncActionWithProgress{TProgress}"/> instance.
    /// </summary>
    /// <typeparam name="TProgress">The type of progress information.</typeparam>
    /// <param name="source">The input <see cref="IAsyncActionWithProgress{TProgress}"/> instance.</param>
    /// <param name="progress">The <see cref="IProgress{T}"/> instance to use to monitor progress on <paramref name="source"/>.</param>
    /// <returns>The resulting <see cref="Task"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> or <typeparamref name="TProgress"/> are <see langword="null"/>.</exception>
    public static Task AsTask<TProgress>(this IAsyncActionWithProgress<TProgress> source, IProgress<TProgress> progress)
    {
        return AsTask(source, progress, CancellationToken.None);
    }

    /// <summary>
    /// Creates a <see cref="Task"/> for the asynchronous operation represented by the specified <see cref="IAsyncActionWithProgress{TProgress}"/> instance.
    /// </summary>
    /// <typeparam name="TProgress">The type of progress information.</typeparam>
    /// <param name="source">The input <see cref="IAsyncActionWithProgress{TProgress}"/> instance.</param>
    /// <param name="progress">The <see cref="IProgress{T}"/> instance to use to monitor progress on <paramref name="source"/>.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>The resulting <see cref="Task"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> or <typeparamref name="TProgress"/> are <see langword="null"/>.</exception>
    public static Task AsTask<TProgress>(
        this IAsyncActionWithProgress<TProgress> source,
        IProgress<TProgress> progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(progress);

        switch (source.Status)
        {
            case AsyncStatus.Completed:
                return Task.CompletedTask;
            case AsyncStatus.Error:
                return Task.FromException(source.ErrorCode!);
            case AsyncStatus.Canceled:
                return Task.FromCanceled(cancellationToken.IsCancellationRequested ? cancellationToken : new CancellationToken(true));
        }

        AsyncInfoTaskCompletionSource<TProgress> bridge = new(source, cancellationToken);

        source.Progress = new AsyncActionProgressHandler<TProgress>((_, info) => progress.Report(info));
        source.Completed = bridge.Complete;

        return bridge.Task;
    }

    /// <summary>Gets an awaiter used to await this <see cref="IAsyncActionWithProgress{TProgress}"/>.</summary>
    /// <typeparam name="TProgress">The type of progress information.</typeparam>
    /// <param name="source">The input <see cref="IAsyncActionWithProgress{TProgress}"/> instance.</param>
    /// <returns>An awaiter instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
    public static TaskAwaiter GetAwaiter<TProgress>(this IAsyncActionWithProgress<TProgress> source)
    {
        return AsTask(source).GetAwaiter();
    }

    /// <summary>
    /// Creates a <see cref="Task{TResult}"/> for the asynchronous operation represented by the specified <see cref="IAsyncOperationWithProgress{TResult, TProgress}"/> instance.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <typeparam name="TProgress">The type of progress information.</typeparam>
    /// <param name="source">The input <see cref="IAsyncOperationWithProgress{TResult, TProgress}"/> instance.</param>
    /// <returns>The resulting <see cref="Task{TResult}"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
    public static Task<TResult> AsTask<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source)
    {
        return AsTask(source, CancellationToken.None);
    }

    /// <summary>
    /// Creates a <see cref="Task{TResult}"/> for the asynchronous operation represented by the specified <see cref="IAsyncOperationWithProgress{TResult, TProgress}"/> instance.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <typeparam name="TProgress">The type of progress information.</typeparam>
    /// <param name="source">The input <see cref="IAsyncOperationWithProgress{TResult, TProgress}"/> instance.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>The resulting <see cref="Task{TResult}"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
    public static Task<TResult> AsTask<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source is UniversalTaskAdapter { Task: Task<TResult> task })
        {
            return cancellationToken.CanBeCanceled ?
                task.WaitAsync(cancellationToken) :
                task;
        }

        switch (source.Status)
        {
            case AsyncStatus.Completed:
                return Task.FromResult(source.GetResults());
            case AsyncStatus.Error:
                return Task.FromException<TResult>(source.ErrorCode!);
            case AsyncStatus.Canceled:
                return Task.FromCanceled<TResult>(cancellationToken.IsCancellationRequested ? cancellationToken : new CancellationToken(true));
        }

        AsyncInfoTaskCompletionSource<TResult, TProgress> bridge = new(source, cancellationToken);

        source.Completed = bridge.Complete;

        return bridge.Task;
    }

    /// <summary>
    /// Creates a <see cref="Task{TResult}"/> for the asynchronous operation represented by the specified <see cref="IAsyncOperationWithProgress{TResult, TProgress}"/> instance.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <typeparam name="TProgress">The type of progress information.</typeparam>
    /// <param name="source">The input <see cref="IAsyncOperationWithProgress{TResult, TProgress}"/> instance.</param>
    /// <param name="progress">The <see cref="IProgress{T}"/> instance to use to monitor progress on <paramref name="source"/>.</param>
    /// <returns>The resulting <see cref="Task{TResult}"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> or <typeparamref name="TProgress"/> are <see langword="null"/>.</exception>
    public static Task<TResult> AsTask<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source, IProgress<TProgress> progress)
    {
        return AsTask(source, progress, CancellationToken.None);
    }

    /// <summary>
    /// Creates a <see cref="Task{TResult}"/> for the asynchronous operation represented by the specified <see cref="IAsyncOperationWithProgress{TResult, TProgress}"/> instance.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <typeparam name="TProgress">The type of progress information.</typeparam>
    /// <param name="source">The input <see cref="IAsyncOperationWithProgress{TResult, TProgress}"/> instance.</param>
    /// <param name="progress">The <see cref="IProgress{T}"/> instance to use to monitor progress on <paramref name="source"/>.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>The resulting <see cref="Task{TResult}"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> or <typeparamref name="TProgress"/> are <see langword="null"/>.</exception>
    public static Task<TResult> AsTask<TResult, TProgress>(
        this IAsyncOperationWithProgress<TResult, TProgress> source,
        IProgress<TProgress> progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(progress);

        switch (source.Status)
        {
            case AsyncStatus.Completed:
                return Task.FromResult(source.GetResults());
            case AsyncStatus.Error:
                return Task.FromException<TResult>(source.ErrorCode!);
            case AsyncStatus.Canceled:
                return Task.FromCanceled<TResult>(cancellationToken.IsCancellationRequested ? cancellationToken : new CancellationToken(true));
        }

        AsyncInfoTaskCompletionSource<TResult, TProgress> bridge = new(source, cancellationToken);

        source.Progress = new AsyncOperationProgressHandler<TResult, TProgress>((_, info) => progress.Report(info));
        source.Completed = bridge.Complete;

        return bridge.Task;
    }

    /// <summary>Gets an awaiter used to await this <see cref="IAsyncOperationWithProgress{TResult, TProgress}"/>.</summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <typeparam name="TProgress">The type of progress information.</typeparam>
    /// <param name="source">The input <see cref="IAsyncOperationWithProgress{TResult, TProgress}"/> instance.</param>
    /// <returns>An awaiter instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
    public static TaskAwaiter<TResult> GetAwaiter<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source)
    {
        return AsTask(source).GetAwaiter();
    }

    /// <summary>
    /// Creates a <see cref="IAsyncAction"/> to represent the specified <see cref="Task"/> instance.
    /// </summary>
    /// <param name="source">The input <see cref="Task"/> instance.</param>
    /// <returns>The resulting <see cref="IAsyncAction"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
    public static IAsyncAction AsAsyncAction(this Task source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new TaskAdapter(source, cancellationTokenSource: null);
    }

    /// <summary>
    /// Creates a <see cref="IAsyncOperation{TResult}"/> to represent the specified <see cref="Task{TResult}"/> instance.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="source">The input <see cref="Task{TResult}"/> instance.</param>
    /// <returns>The resulting <see cref="IAsyncOperation{TResult}"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
    public static IAsyncOperation<TResult> AsAsyncOperation<TResult>(this Task<TResult> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new TaskAdapter<TResult>(source, cancellationTokenSource: null);
    }
}