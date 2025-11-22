// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using WindowsRuntime.InteropServices;

namespace Windows.Foundation.Tasks;

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
/// <seealso cref="IAsyncInfo"/>
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
    /// to be notified of a cancellation request. You may ignore it if your task does not support cancellation.
    /// </para>
    /// </remarks>
    public static IAsyncAction Run(Func<CancellationToken, Task> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        return new AsyncActionAdapter(factory);
    }

    /// <summary>
    /// Creates and starts an <see cref="IAsyncActionWithProgress{TProgress}"/> instance from a function that generates a <see cref="Task"/>.
    /// </summary>
    /// <typeparam name="TProgress">The type of progress information.</typeparam>
    /// <param name="factory">The function to invoke to create the <see cref="Task"/> when the <see cref="IAsyncActionWithProgress{TProgress}"/> is started.</param>
    /// <returns>The (already started) <see cref="IAsyncActionWithProgress{TProgress}"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="factory"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// Use this overload if your task supports cancellation and progress monitoring in order to:
    /// <list type="bullet">
    ///   <item>Hook-up the <see cref="IAsyncInfo.Cancel"/> mechanism of the created asynchronous action and the cancellation of your task.</item>
    ///   <item>
    ///     Hook-up the <see cref="IAsyncActionWithProgress{TProgress}.Progress"/> update delegate exposed by the created
    ///     async action and the progress updates published by your task.
    ///   </item>
    /// </list>
    /// </para>
    /// <para>
    /// The <paramref name="factory"/> delegate is passed a <see cref="CancellationToken"/> that the task may monitor
    /// to be notified of a cancellation request. You may ignore it if your task does not support cancellation.
    /// </para>
    /// <para>
    /// It is also passed an <see cref="IProgress{TProgress}"/> instance to which progress updates may be published.
    /// You may ignore it if your task does not support progress reporting.
    /// </para>
    /// </remarks>
    public static IAsyncActionWithProgress<TProgress> Run<TProgress>(Func<CancellationToken, IProgress<TProgress>, Task> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        return new AsyncActionWithProgressAdapter<TProgress>(factory);
    }

    /// <summary>
    /// Creates and starts an <see cref="IAsyncOperation{TResult}"/> instance from a function that generates a <see cref="Task{TResult}"/>.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="factory">The function to invoke to create the <see cref="Task{TResult}"/> when the <see cref="IAsyncOperation{TResult}"/> is started.</param>
    /// <returns>The (already started) <see cref="IAsyncOperation{TResult}"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="factory"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// Use this overload if your task supports cancellation in order to hook-up the <see cref="IAsyncInfo.Cancel"/>
    /// mechanism exposed by the created asynchronous operation and the cancellation of your task.
    /// </para>
    /// <para>
    /// The function is passed a <see cref="CancellationToken"/> that the task may monitor to be notified
    /// of a cancellation request. You may ignore it if your task does not support cancellation.
    /// </para>
    /// </remarks>
    public static IAsyncOperation<TResult> Run<TResult>(Func<CancellationToken, Task<TResult>> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        return new AsyncOperationAdapter<TResult>(factory);
    }

    /// <summary>
    /// Creates and starts an <see cref="IAsyncOperationWithProgress{TResult, TProgress}"/> instance from a function that generates a <see cref="Task{TResult}"/>.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <typeparam name="TProgress">The type of progress information.</typeparam>
    /// <param name="factory">The function to invoke to create the <see cref="Task{TResult}"/> when the <see cref="IAsyncOperationWithProgress{TResult, TProgress}"/> is started.</param>
    /// <returns>The (already started) <see cref="IAsyncOperationWithProgress{TResult, TProgress}"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="factory"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// Use this overload if your task supports cancellation and progress monitoring in order to:
    /// <list type="bullet">
    ///   <item>Hook-up the <see cref="IAsyncInfo.Cancel"/> mechanism of the created asynchronous operation and the cancellation of your task.</item>
    ///   <item>
    ///     Hook-up the <see cref="IAsyncOperationWithProgress{TResult, TProgress}.Progress"/> update delegate exposed
    ///     by the created async operation and the progress updates published by your task.
    ///   </item>
    ///   <item>
    ///     Hook-up the <see cref="IAsyncOperationWithProgress{TResult, TProgress}.Progress"/> update delegate
    ///     exposed by the created async operation and the progress updates published by your task.
    ///   </item>
    /// </list>
    /// </para>
    /// <para>
    /// The function is passed a <see cref="CancellationToken"/> that the task may monitor to be notified
    /// of a cancellation request. You may ignore it if your task does not support cancellation.
    /// </para>
    /// <para>
    /// It is also passed a <see cref="IProgress{TProgress}"/> instance to which progress updates
    /// may be published. You may ignore it if your task does not support progress reporting.
    /// </para>
    /// </remarks>
    public static IAsyncOperationWithProgress<TResult, TProgress> Run<TResult, TProgress>(
        Func<CancellationToken, IProgress<TProgress>, Task<TResult>> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        return new AsyncOperationProgressAdapter<TResult, TProgress>(factory);
    }

    /// <summary>
    /// Creates an <see cref="IAsyncAction"/> instance in the completed state.
    /// </summary>
    /// <returns>The resulting <see cref="IAsyncAction"/> instance.</returns>
    public static IAsyncAction CompletedAction()
    {
        return new AsyncActionAdapter(default(CompletedTaskPlaceholder));
    }

    /// <summary>
    /// Creates an <see cref="IAsyncActionWithProgress{TProgress}"/> instance in the completed state.
    /// </summary>
    /// <typeparam name="TProgress">The type of progress information.</typeparam>
    /// <returns>The resulting <see cref="IAsyncActionWithProgress{TProgress}"/> instance.</returns>
    public static IAsyncActionWithProgress<TProgress> CompletedActionWithProgress<TProgress>()
    {
        return new AsyncActionWithProgressAdapter<TProgress>(default(CompletedTaskPlaceholder));
    }

    /// <summary>
    /// Creates an <see cref="IAsyncOperation{TResult}"/> instance in the completed state, with a specified result.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="result">The result to wrap.</param>
    /// <returns>The resulting <see cref="IAsyncOperation{TResult}"/> instance.</returns>
    public static IAsyncOperation<TResult> FromResult<TResult>(TResult result)
    {
        return new AsyncOperationAdapter<TResult>(result);
    }

    /// <summary>
    /// Creates an <see cref="IAsyncOperationWithProgress{TResult, TProgress}"/> instance in the completed state, with a specified result.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <typeparam name="TProgress">The type of progress information.</typeparam>
    /// <param name="result">The result to wrap.</param>
    /// <returns>The resulting <see cref="IAsyncOperationWithProgress{TResult, TProgress}"/> instance.</returns>
    public static IAsyncOperationWithProgress<TResult, TProgress> FromResultWithProgress<TResult, TProgress>(TResult result)
    {
        return new AsyncOperationProgressAdapter<TResult, TProgress>(result);
    }

    /// <summary>
    /// Creates an <see cref="IAsyncAction"/> instance in the error state.
    /// </summary>
    /// <param name="exception">The exception representing the error state.</param>
    /// <returns>The resulting <see cref="IAsyncAction"/> instance.</returns>
    public static IAsyncAction FromException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return new AsyncActionAdapter(exception);
    }

    /// <summary>
    /// Creates an <see cref="IAsyncActionWithProgress{TProgress}"/> instance in the error state.
    /// </summary>
    /// <typeparam name="TProgress">The type of progress information.</typeparam>
    /// <param name="exception">The exception representing the error state.</param>
    /// <returns>The resulting <see cref="IAsyncActionWithProgress{TProgress}"/> instance.</returns>
    public static IAsyncActionWithProgress<TProgress> FromExceptionWithProgress<TProgress>(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return new AsyncActionWithProgressAdapter<TProgress>(exception);
    }

    /// <summary>
    /// Creates an <see cref="IAsyncOperation{TResult}"/> instance in the error state.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="exception">The exception representing the error state.</param>
    /// <returns>The resulting <see cref="IAsyncOperation{TResult}"/> instance.</returns>
    public static IAsyncOperation<TResult> FromException<TResult>(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return new AsyncOperationAdapter<TResult>(exception);
    }

    /// <summary>
    /// Creates an <see cref="IAsyncOperationWithProgress{TResult, TProgress}"/> instance in the error state.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <typeparam name="TProgress">The type of progress information.</typeparam>
    /// <param name="exception">The exception representing the error state.</param>
    /// <returns>The resulting <see cref="IAsyncOperationWithProgress{TResult, TProgress}"/> instance.</returns>
    public static IAsyncOperationWithProgress<TResult, TProgress> FromExceptionWithProgress<TResult, TProgress>(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return new AsyncOperationProgressAdapter<TResult, TProgress>(exception);
    }

    /// <summary>
    /// Creates an <see cref="IAsyncAction"/> instance in the canceled state.
    /// </summary>
    /// <returns>The resulting <see cref="IAsyncAction"/> instance.</returns>
    public static IAsyncAction CanceledAction()
    {
        return new AsyncActionAdapter(default(CanceledTaskPlaceholder));
    }

    /// <summary>
    /// Creates an <see cref="IAsyncActionWithProgress{TProgress}"/> instance in the canceled state.
    /// </summary>
    /// <typeparam name="TProgress">The type of progress information.</typeparam>
    /// <returns>The resulting <see cref="IAsyncActionWithProgress{TProgress}"/> instance.</returns>
    public static IAsyncActionWithProgress<TProgress> CanceledActionWithProgress<TProgress>()
    {
        return new AsyncActionWithProgressAdapter<TProgress>(default(CanceledTaskPlaceholder));
    }

    /// <summary>
    /// Creates an <see cref="IAsyncOperation{TResult}"/> instance in the canceled state.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <returns>The resulting <see cref="IAsyncOperation{TResult}"/> instance.</returns>
    public static IAsyncOperation<TResult> CanceledOperation<TResult>()
    {
        return new AsyncOperationAdapter<TResult>(default(CanceledTaskPlaceholder));
    }

    /// <summary>
    /// Creates an <see cref="IAsyncOperationWithProgress{TResult, TProgress}"/> instance in the canceled state.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <typeparam name="TProgress">The type of progress information.</typeparam>
    /// <returns>The resulting <see cref="IAsyncOperationWithProgress{TResult, TProgress}"/> instance.</returns>
    public static IAsyncOperationWithProgress<TResult, TProgress> CanceledOperationWithProgress<TResult, TProgress>()
    {
        return new AsyncOperationProgressAdapter<TResult, TProgress>(default(CanceledTaskPlaceholder));
    }
}