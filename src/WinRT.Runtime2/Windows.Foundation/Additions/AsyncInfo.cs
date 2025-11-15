// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

namespace System.Runtime.InteropServices.WindowsRuntime;

/// <summary>
/// Provides factory methods to construct Windows Runtime representations of asynchronous operations.
/// </summary>
/// <remarks>
/// The factory methods take as inputs functions (delegates) that provide managed <see cref="Task"/> objects.
/// Different factory methods return different <see cref="IAsyncInfo"/>-derived interface instantiations.
/// When an asynchronous operation created by this factory is started (which happens right at construction),
/// the specified <see cref="Task"/>-provider delegate will be invoked to create the resulting instant
/// that will be wrapped by the returned Windows Runtime adapter.
/// </remarks>
[SupportedOSPlatform("windows10.0.10240.0")]
public static class AsyncInfo
{
    /// <summary>
    /// Creates and starts an <see cref="IAsyncAction"/> instance from a function that generates a <see cref="Task"/>.
    /// </summary>
    /// <param name="factory">The function to invoke to create the <see cref="Task"/> when the <see cref="IAsyncInfo"/> is started.</param>
    /// <returns>The (already started) <see cref="IAsyncAction"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="factory"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// Use this overload if your task supports cancellation in order to hook-up the <see cref="IAsyncInfo.Cancel"/>
    /// mechanism exposed by the created asynchronous action and the cancellation of your task.
    /// </para>
    /// <para>
    /// The <paramref name="factory"/> delegate is passed a <see cref="CancellationToken"/> that the task may monitor
    /// to be notified of a cancellation request. You may ignore the it if your task does not support cancellation.
    /// </para>
    /// </remarks>
    public static IAsyncAction Run(Func<CancellationToken, Task> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        return new TaskToAsyncActionAdapter(factory);
    }

    /// <summary>
    /// Creates and starts an <see cref="IAsyncActionWithProgress{TProgress}"/> instance from a function
    /// that generates a <see cref="System.Threading.Tasks.Task"/>.
    /// Use this overload if your task supports cancellation and progress monitoring is order to:
    /// (1) hook-up the <code>Cancel</code> mechanism of the created asynchronous action and the cancellation of your task,
    /// and (2) hook-up the <code>Progress</code> update delegate exposed by the created async action and the progress updates
    /// published by your task.</summary>
    /// <param name="factory">The function to invoke to create the task when the IAsyncInfo is started.
    /// The function is passed a <see cref="System.Threading.CancellationToken"/> that the task may monitor
    /// to be notified of a cancellation request;
    /// you may ignore the <code>CancellationToken</code> if your task does not support cancellation.
    /// It is also passed a <see cref="System.IProgress{TProgress}"/> instance to which progress updates may be published;
    /// you may ignore the <code>IProgress</code> if your task does not support progress reporting.</param>
    /// <returns>An unstarted <see cref="IAsyncActionWithProgress{TProgress}"/> instance.</returns>
    public static IAsyncActionWithProgress<TProgress> Run<TProgress>(Func<CancellationToken, IProgress<TProgress>, Task> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        return new TaskToAsyncActionWithProgressAdapter<TProgress>(factory);
    }

    /// <summary>
    /// Creates and starts  an <see cref="IAsyncOperation{TResult}"/> instance from a function
    /// that generates a <see cref="System.Threading.Tasks.Task{TResult}"/>.
    /// Use this overload if your task supports cancellation in order to hook-up the <code>Cancel</code>
    /// mechanism exposed by the created asynchronous operation and the cancellation of your task.</summary>
    /// <param name="factory">The function to invoke to create the task when the IAsyncInfo is started.
    /// The function is passed a <see cref="System.Threading.CancellationToken"/> that the task may monitor
    /// to be notified of a cancellation request;
    /// you may ignore the <code>CancellationToken</code> if your task does not support cancellation.</param>
    /// <returns>An unstarted <see cref="IAsyncOperation{TResult}"/> instance.</returns>
    public static IAsyncOperation<TResult> Run<TResult>(Func<CancellationToken, Task<TResult>> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        return new TaskToAsyncOperationAdapter<TResult>(factory);
    }

    /// <summary>
    /// Creates and starts  an <see cref="IAsyncOperationWithProgress{TResult, TProgress}"/> instance
    /// from a function that generates a <see cref="System.Threading.Tasks.Task{TResult}"/>.<br />
    /// Use this overload if your task supports cancellation and progress monitoring is order to:
    /// (1) hook-up the <code>Cancel</code> mechanism of the created asynchronous operation and the cancellation of your task,
    /// and (2) hook-up the <code>Progress</code> update delegate exposed by the created async operation and the progress
    /// updates published by your task.</summary>
    /// <typeparam name="TResult">The result type of the task.</typeparam>
    /// <typeparam name="TProgress">The type used for progress notifications.</typeparam>
    /// <param name="factory">The function to invoke to create the task when the IAsyncOperationWithProgress is started.<br />
    /// The function is passed a <see cref="System.Threading.CancellationToken"/> that the task may monitor
    /// to be notified of a cancellation request;
    /// you may ignore the <code>CancellationToken</code> if your task does not support cancellation.
    /// It is also passed a <see cref="System.IProgress{TProgress}"/> instance to which progress updates may be published;
    /// you may ignore the <code>IProgress</code> if your task does not support progress reporting.</param>
    /// <returns>An unstarted <see cref="IAsyncOperationWithProgress{TResult, TProgress}"/> instance.</returns>
    public static IAsyncOperationWithProgress<TResult, TProgress> Run<TResult, TProgress>(
        Func<CancellationToken, IProgress<TProgress>, Task<TResult>> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        return new TaskToAsyncOperationWithProgressAdapter<TResult, TProgress>(factory);
    }

    public static IAsyncAction CompletedAction()
    {
        return new TaskToAsyncActionAdapter(isCanceled: false);
    }

    public static IAsyncActionWithProgress<TProgress> CompletedActionWithProgress<TProgress>()
    {
        return new TaskToAsyncActionWithProgressAdapter<TProgress>(isCanceled: false);
    }

    public static IAsyncOperation<TResult> FromResult<TResult>(TResult synchronousResult)
    {
        return new TaskToAsyncOperationAdapter<TResult>(synchronousResult);
    }

    public static IAsyncOperationWithProgress<TResult, TProgress> FromResultWithProgress<TResult, TProgress>(TResult synchronousResult)
    {
        return new TaskToAsyncOperationWithProgressAdapter<TResult, TProgress>(synchronousResult);
    }

    public static IAsyncAction FromException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        TaskToAsyncActionAdapter asyncInfo = new(isCanceled: false);

        asyncInfo.DangerousSetError(exception);
        Debug.Assert(asyncInfo.Status == AsyncStatus.Error);

        return asyncInfo;
    }

    public static IAsyncActionWithProgress<TProgress> FromExceptionWithProgress<TProgress>(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        TaskToAsyncActionWithProgressAdapter<TProgress> asyncInfo = new(isCanceled: false);

        asyncInfo.DangerousSetError(exception);
        Debug.Assert(asyncInfo.Status == AsyncStatus.Error);

        return asyncInfo;
    }

    public static IAsyncOperation<TResult> FromException<TResult>(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        TaskToAsyncOperationAdapter<TResult> asyncInfo = new(default(TResult));

        asyncInfo.DangerousSetError(exception);
        Debug.Assert(asyncInfo.Status == AsyncStatus.Error);

        return asyncInfo;
    }

    public static IAsyncOperationWithProgress<TResult, TProgress> FromExceptionWithProgress<TResult, TProgress>(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        TaskToAsyncOperationWithProgressAdapter<TResult, TProgress> asyncInfo = new(default(TResult));

        asyncInfo.DangerousSetError(exception);
        Debug.Assert(asyncInfo.Status == AsyncStatus.Error);

        return asyncInfo;
    }

    public static IAsyncAction CanceledAction()
    {
        return new TaskToAsyncActionAdapter(isCanceled: true);
    }

    public static IAsyncActionWithProgress<TProgress> CanceledActionWithProgress<TProgress>()
    {
        return new TaskToAsyncActionWithProgressAdapter<TProgress>(isCanceled: true);
    }

    public static IAsyncOperation<TResult> CanceledOperation<TResult>()
    {
        return new TaskToAsyncOperationAdapter<TResult>(isCanceled: true);
    }

    public static IAsyncOperationWithProgress<TResult, TProgress> CanceledOperationWithProgress<TResult, TProgress>()
    {
        return new TaskToAsyncOperationWithProgressAdapter<TResult, TProgress>(isCanceled: true);
    }
}
