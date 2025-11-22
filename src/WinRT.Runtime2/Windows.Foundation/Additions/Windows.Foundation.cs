// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System;

using System.Runtime.Versioning;
using global::System.Diagnostics;
using global::System.Runtime.CompilerServices;
using global::System.Runtime.InteropServices;
using global::System.Threading;
using global::System.Threading.Tasks;
using global::Windows.Foundation;
using WindowsRuntime.InteropServices;

#if NET
[global::System.Runtime.Versioning.SupportedOSPlatform("windows10.0.10240.0")]
#endif
#if EMBED
internal
#else 
public
#endif
static class WindowsRuntimeSystemExtensions
{
    public static Task AsTask(this IAsyncAction source)
    {
        return AsTask(source, CancellationToken.None);
    }

    public static Task AsTask(this IAsyncAction source, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source is AsyncInfoAdapter { Task: Task task })
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

        AsyncInfoToTaskBridge<VoidValueTypeParameter> bridge = new(source, cancellationToken);

        source.Completed = bridge.Complete;

        return bridge.Task;
    }

    public static TaskAwaiter GetAwaiter(this IAsyncAction source)
    {
        return AsTask(source).GetAwaiter();
    }

    public static void Wait(this IAsyncAction source)
    {
        AsTask(source).Wait();
    }

    public static Task<TResult> AsTask<TResult>(this IAsyncOperation<TResult> source)
    {
        return AsTask(source, CancellationToken.None);
    }

    public static Task<TResult> AsTask<TResult>(this IAsyncOperation<TResult> source, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source is AsyncInfoAdapter { Task: Task<TResult> task })
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

        AsyncInfoToTaskBridge<TResult, VoidValueTypeParameter> bridge = new(source, cancellationToken);

        source.Completed = bridge.Complete;

        return bridge.Task;
    }

    public static TaskAwaiter<TResult> GetAwaiter<TResult>(this IAsyncOperation<TResult> source)
    {
        return AsTask(source).GetAwaiter();
    }

    public static void Wait<TResult>(this IAsyncOperation<TResult> source)
    {
        AsTask(source).Wait();
    }

    public static Task AsTask<TProgress>(this IAsyncActionWithProgress<TProgress> source, CancellationToken cancellationToken, IProgress<TProgress> progress)
    {
        ArgumentNullException.ThrowIfNull(source);

        // fast path is underlying asyncInfo is Task and no IProgress provided
        if (source is AsyncInfoAdapter { Task: Task task } && progress is null)
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
                return Task.FromException(source.ErrorCode);

            case AsyncStatus.Canceled:
                return Task.FromCanceled(cancellationToken.IsCancellationRequested ? cancellationToken : new CancellationToken(true));
        }

        if (progress != null)
        {
            SetProgress(source, progress);
        }

        var bridge = new AsyncInfoToTaskBridge<TProgress>(source, cancellationToken);
        source.Completed = bridge.Complete;
        return bridge.Task;
    }

    private static void SetProgress<TProgress>(IAsyncActionWithProgress<TProgress> source, IProgress<TProgress> sink)
    {
        // This is separated out into a separate method so that we only pay the costs of compiler-generated closure if progress is non-null.
        source.Progress = new AsyncActionProgressHandler<TProgress>((_, info) => sink.Report(info));
    }

    public static Task AsTask<TProgress>(this IAsyncActionWithProgress<TProgress> source)
    {
        return AsTask(source, CancellationToken.None, null);
    }

    public static Task AsTask<TProgress>(this IAsyncActionWithProgress<TProgress> source, CancellationToken cancellationToken)
    {
        return AsTask(source, cancellationToken, null);
    }

    public static Task AsTask<TProgress>(this IAsyncActionWithProgress<TProgress> source, IProgress<TProgress> progress)
    {
        return AsTask(source, CancellationToken.None, progress);
    }

    public static TaskAwaiter GetAwaiter<TProgress>(this IAsyncActionWithProgress<TProgress> source)
    {
        return AsTask(source).GetAwaiter();
    }

    public static void Wait<TProgress>(this IAsyncActionWithProgress<TProgress> source)
    {
        AsTask(source).Wait();
    }

    public static Task<TResult> AsTask<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source, CancellationToken cancellationToken, IProgress<TProgress> progress)
    {
        ArgumentNullException.ThrowIfNull(source);

        // fast path is underlying asyncInfo is Task and no IProgress provided
        if (source is AsyncInfoAdapter { Task: Task<TResult> task } && progress is null)
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
                return Task.FromException<TResult>(source.ErrorCode);

            case AsyncStatus.Canceled:
                return Task.FromCanceled<TResult>(cancellationToken.IsCancellationRequested ? cancellationToken : new CancellationToken(true));
        }

        if (progress != null)
        {
            SetProgress(source, progress);
        }

        var bridge = new AsyncInfoToTaskBridge<TResult, TProgress>(source, cancellationToken);
        source.Completed = bridge.Complete;
        return bridge.Task;
    }

    private static void SetProgress<TResult, TProgress>(IAsyncOperationWithProgress<TResult, TProgress> source, IProgress<TProgress> sink)
    {
        // This is separated out into a separate method so that we only pay the costs of compiler-generated closure if progress is non-null.
        source.Progress = new AsyncOperationProgressHandler<TResult, TProgress>((_, info) => sink.Report(info));
    }

    public static Task<TResult> AsTask<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source)
    {
        return AsTask(source, CancellationToken.None, null);
    }

    public static Task<TResult> AsTask<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source, CancellationToken cancellationToken)
    {
        return AsTask(source, cancellationToken, null);
    }

    public static Task<TResult> AsTask<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source, IProgress<TProgress> progress)
    {
        return AsTask(source, CancellationToken.None, progress);
    }

    public static TaskAwaiter<TResult> GetAwaiter<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source)
    {
        return AsTask(source).GetAwaiter();
    }

    public static void Wait<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source)
    {
        AsTask(source).Wait();
    }

    public static IAsyncAction AsAsyncAction(this Task source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new AsyncActionAdapter(source, cancellationTokenSource: null);
    }

    public static IAsyncOperation<TResult> AsAsyncOperation<TResult>(this Task<TResult> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new AsyncOperationAdapter<TResult>(source, cancellationTokenSource: null);
    }
}

// Marker type since generic parameters cannot be 'void'
struct VoidValueTypeParameter { }

/// <summary>This can be used instead of <code>VoidValueTypeParameter</code> when a reference type is required.
/// In case of an actual instantiation (e.g. through <code>default(T)</code>),
/// using <code>VoidValueTypeParameter</code> offers better performance.</summary>
internal class VoidReferenceTypeParameter { }

/// <summary>
/// A <see cref="TaskCompletionSource"/> implementation backed by an <see cref="IAsyncInfo"/> object.
/// </summary>
/// <typeparam name="TProgress">The type of progress information.</typeparam>
[SupportedOSPlatform("windows10.0.10240.0")]
internal sealed class AsyncInfoToTaskBridge<TProgress> : TaskCompletionSource
{
    /// <summary>
    /// The input <see cref="CancellationToken"/> value.
    /// </summary>
    private readonly CancellationToken _cancellationToken;

    /// <summary>
    /// The cancellation registration from <see cref="_cancellationToken"/> canceling this <see cref="TaskCompletionSource"/> instance.
    /// </summary>
    private readonly CancellationTokenRegistration _registration;

    /// <summary>
    /// The cancellation registration from <see cref="_cancellationToken"/> canceling the input <see cref="IAsyncInfo"/> instance.
    /// </summary>
    private readonly CancellationTokenRegistration _asyncInfoRegistration;

    /// <summary>
    /// Creates a new <see cref="AsyncInfoToTaskBridge{TProgress}"/> instance with the specified parameters.
    /// </summary>
    /// <param name="asyncInfo">The <see cref="IAsyncInfo"/> instance to wrap.</param>
    /// <param name="cancellationToken">The input <see cref="CancellationToken"/> value.</param>
    public AsyncInfoToTaskBridge(IAsyncInfo asyncInfo, CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;

        if (_cancellationToken.CanBeCanceled)
        {
            // Register the cancellation from the input cancellation token
            _registration = _cancellationToken.Register(
                callback: static (@this, ct) => Unsafe.As<TaskCompletionSource>(@this!).TrySetCanceled(ct),
                state: this);

            // Register the cancellation from the async object as well
            try
            {
                _asyncInfoRegistration = _cancellationToken.Register(
                    callback: static asyncInfo => Unsafe.As<IAsyncInfo>(asyncInfo!).Cancel(),
                    state: asyncInfo);
            }
            catch (Exception ex)
            {
                // Handle exceptions from 'Cancel' if the token is already canceled
                if (!Task.IsFaulted)
                {
                    Debug.Fail($"Expected base task to already be faulted, but found it in state '{Task.Status}'.");

                    _ = TrySetException(ex);
                }
            }
        }

        // If we're already completed, unregister everything again.
        // The unregistration we perform is idempotent and thread-safe.
        if (Task.IsCompleted)
        {
            Dispose();
        }
    }

    /// <summary>
    /// Marks the current instance as completed.
    /// </summary>
    /// <param name="asyncInfo">The <see cref="IAsyncAction"/> instance that completed.</param>
    /// <param name="asyncStatus">The current <see cref="AsyncStatus"/> value (reported by <see cref="IAsyncAction.Completed"/>).</param>
    public void Complete(IAsyncAction asyncInfo, AsyncStatus asyncStatus)
    {
        CompleteCore(asyncInfo, asyncStatus);
    }

    /// <summary>
    /// Marks the current instance as completed.
    /// </summary>
    /// <param name="asyncInfo">The <see cref="IAsyncActionWithProgress{TProgress}"/> instance that completed.</param>
    /// <param name="asyncStatus">The current <see cref="AsyncStatus"/> value (reported by <see cref="IAsyncActionWithProgress{TProgress}.Completed"/>).</param>
    public void Complete(IAsyncActionWithProgress<TProgress> asyncInfo, AsyncStatus asyncStatus)
    {
        CompleteCore(asyncInfo, asyncStatus);
    }

    /// <summary>
    /// Marks the current instance as completed.
    /// </summary>
    /// <param name="asyncInfo">The <see cref="IAsyncInfo"/> instance that completed.</param>
    /// <param name="asyncStatus">The current <see cref="AsyncStatus"/> value (from some completion handler).</param>
    private void CompleteCore(IAsyncInfo asyncInfo, AsyncStatus asyncStatus)
    {
        ArgumentNullException.ThrowIfNull(asyncInfo);

        try
        {
            Debug.Assert(asyncInfo.Status == asyncStatus, "The input async status doesn't match what 'IAsyncInfo' is reporting.");

            // Validate that the async status is valid for this call
            if (asyncStatus is not (AsyncStatus.Completed or AsyncStatus.Canceled or AsyncStatus.Error))
            {
                Debug.Fail("The async operation should be in a terminal state.");

                throw new InvalidOperationException("The asynchronous operation could not be completed.");
            }

            Exception? error = null;

            // Retrieve the exception, if the async object is in the error state
            if (asyncStatus == AsyncStatus.Error)
            {
                error = asyncInfo.ErrorCode;

                // Defend against a faulty 'IAsyncInfo' implementation
                if (error is null)
                {
                    Debug.Fail("'IAsyncInfo.Status == Error', but 'ErrorCode' returns 'null' (implying 'S_OK').");

                    error = new InvalidOperationException("The asynchronous operation could not be completed.");
                }
            }

            bool success = false;

            // Complete the task based on the previously retrieved results
            switch (asyncStatus)
            {
                case AsyncStatus.Completed:
                    success = TrySetResult();
                    break;
                case AsyncStatus.Error:
                    success = TrySetException(error!);
                    break;
                case AsyncStatus.Canceled:
                    success = TrySetCanceled(_cancellationToken.IsCancellationRequested ? _cancellationToken : new CancellationToken(true));
                    break;
            }

            // Dispose the registrations, if we succeeded in completing the task
            if (success)
            {
                Dispose();
            }
        }
        catch (Exception e)
        {
            Debug.Fail($"Unexpected exception in 'CompleteCore': '{e}'.");

            // If we can't set the exception, we just re-throw it from here
            if (!TrySetException(e))
            {
                Debug.Fail("The task was already completed and thus the exception couldn't be stored.");

                throw;
            }

            Dispose();
        }
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    private void Dispose()
    {
        _registration.Dispose();
        _asyncInfoRegistration.Dispose();
    }
}

/// <summary>
/// A <see cref="TaskCompletionSource{TResult}"/> implementation backed by an <see cref="IAsyncInfo"/> object.
/// </summary>
/// <typeparam name="TResult">The result type.</typeparam>
/// <typeparam name="TProgress">The type of progress information.</typeparam>
[SupportedOSPlatform("windows10.0.10240.0")]
internal sealed class AsyncInfoToTaskBridge<TResult, TProgress> : TaskCompletionSource<TResult>
{
    /// <inheritdoc cref="AsyncInfoToTaskBridge{TProgress}._cancellationToken"/>
    private readonly CancellationToken _cancellationToken;

    /// <inheritdoc cref="AsyncInfoToTaskBridge{TProgress}._registration"/>
    private readonly CancellationTokenRegistration _registration;

    /// <inheritdoc cref="AsyncInfoToTaskBridge{TProgress}._asyncInfoRegistration"/>
    private readonly CancellationTokenRegistration _asyncInfoRegistration;

    /// <summary>
    /// Creates a new <see cref="AsyncInfoToTaskBridge{TResult, TProgress}"/> instance with the specified parameters.
    /// </summary>
    /// <param name="asyncInfo">The <see cref="IAsyncInfo"/> instance to wrap.</param>
    /// <param name="cancellationToken">The input <see cref="CancellationToken"/> value.</param>
    public AsyncInfoToTaskBridge(IAsyncInfo asyncInfo, CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        if (_cancellationToken.CanBeCanceled)
        {
            _registration = _cancellationToken.Register(static (b, ct) => ((TaskCompletionSource<TResult>)b).TrySetCanceled(ct), this);

            // Handle Exception from Cancel() if the token is already canceled.
            try
            {
                _asyncInfoRegistration = _cancellationToken.Register(static ai => ((IAsyncInfo)ai).Cancel(), asyncInfo);
            }
            catch (Exception ex)
            {
                if (!base.Task.IsFaulted)
                {
                    Debug.Fail($"Expected base task to already be faulted but found it in state {base.Task.Status}");
                    base.TrySetException(ex);
                }
            }
        }

        // If we're already completed, unregister everything again.  Unregistration is idempotent and thread-safe.
        if (Task.IsCompleted)
        {
            Dispose();
        }
    }

    /// <summary>
    /// Marks the current instance as completed.
    /// </summary>
    /// <param name="asyncInfo">The <see cref="IAsyncOperation{TResult}"/> instance that completed.</param>
    /// <param name="asyncStatus">The current <see cref="AsyncStatus"/> value (reported by <see cref="IAsyncOperation{TResult}.Completed"/>).</param>
    internal void Complete(IAsyncOperation<TResult> asyncInfo, AsyncStatus asyncStatus)
    {
        Debug.Assert(typeof(TProgress) == typeof(VoidValueTypeParameter));

        CompleteCore(asyncInfo, asyncStatus);
    }

    /// <summary>
    /// Marks the current instance as completed.
    /// </summary>
    /// <param name="asyncInfo">The <see cref="IAsyncOperationWithProgress{TResult, TProgress}"/> instance that completed.</param>
    /// <param name="asyncStatus">The current <see cref="AsyncStatus"/> value (reported by <see cref="IAsyncOperationWithProgress{TResult, TProgress}.Completed"/>).</param>
    internal void Complete(IAsyncOperationWithProgress<TResult, TProgress> asyncInfo, AsyncStatus asyncStatus)
    {
        Debug.Assert(typeof(TProgress) != typeof(VoidValueTypeParameter));

        CompleteCore(asyncInfo, asyncStatus);
    }

    /// <inheritdoc cref="AsyncInfoToTaskBridge{TProgress}.CompleteCore"/>
    private void CompleteCore(IAsyncInfo asyncInfo, AsyncStatus asyncStatus)
    {
        ArgumentNullException.ThrowIfNull(asyncInfo);

        try
        {
            Debug.Assert(asyncInfo.Status == asyncStatus, "asyncInfo.Status does not match asyncStatus; are we dealing with a faulty IAsyncInfo implementation?");

            if (asyncStatus != AsyncStatus.Completed && asyncStatus != AsyncStatus.Canceled && asyncStatus != AsyncStatus.Error)
            {
                Debug.Fail("The async operation should be in a terminal state.");
                throw new InvalidOperationException("The asynchronous operation could not be completed.");
            }

            TResult result = default(TResult);
            Exception error = null;
            if (asyncStatus == AsyncStatus.Error)
            {
                error = asyncInfo.ErrorCode;

                // Defend against a faulty IAsyncInfo implementation
                if (error is null)
                {
                    Debug.Fail("IAsyncInfo.Status == Error, but ErrorCode returns a null Exception (implying S_OK).");
                    error = new InvalidOperationException("The asynchronous operation could not be completed.");
                }
            }
            else if (asyncStatus == AsyncStatus.Completed)
            {
                try
                {
                    result = typeof(TProgress) == typeof(VoidValueTypeParameter)
                        ? Unsafe.As<IAsyncOperation<TResult>>(asyncInfo).GetResults()
                        : Unsafe.As<IAsyncOperationWithProgress<TResult, TProgress>>(asyncInfo).GetResults();
                }
                catch (Exception resultsEx)
                {
                    // According to the WinRT team, this can happen in some egde cases, such as marshalling errors in GetResults.
                    error = resultsEx;
                    asyncStatus = AsyncStatus.Error;
                }
            }

            // Complete the task based on the previously retrieved results:
            bool success = false;
            switch (asyncStatus)
            {
                case AsyncStatus.Completed:
                    success = base.TrySetResult(result);
                    break;

                case AsyncStatus.Error:
                    Debug.Assert(error != null, "The error should have been retrieved previously.");
                    success = base.TrySetException(error);
                    break;

                case AsyncStatus.Canceled:
                    success = base.TrySetCanceled(_cancellationToken.IsCancellationRequested ? _cancellationToken : new CancellationToken(true));
                    break;
            }

            if (success)
            {
                Dispose();
            }
        }
        catch (Exception e)
        {
            Debug.Fail($"Unexpected exception in Complete: {e}");

            if (!base.TrySetException(e))
            {
                Debug.Fail("The task was already completed and thus the exception couldn't be stored.");
                throw;
            }
            else
            {
                Dispose();
            }
        }
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    private void Dispose()
    {
        _registration.Dispose();
        _asyncInfoRegistration.Dispose();
    }
}