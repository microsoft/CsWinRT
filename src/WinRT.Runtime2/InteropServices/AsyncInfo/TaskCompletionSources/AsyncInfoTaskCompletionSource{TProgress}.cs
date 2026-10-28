// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

#pragma warning disable IDE0010, IDE0055

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A <see cref="TaskCompletionSource"/> implementation backed by an <see cref="IAsyncInfo"/> object.
/// </summary>
/// <typeparam name="TProgress">The type of progress information.</typeparam>
internal sealed class AsyncInfoTaskCompletionSource<TProgress> : TaskCompletionSource
{
    /// <summary>
    /// The input <see cref="CancellationToken"/> value.
    /// </summary>
    private readonly CancellationToken _cancellationToken;

    /// <summary>
    /// The cancellation registration that handles cancellation of <see cref="_cancellationToken"/>.
    /// </summary>
    private readonly CancellationTokenRegistration _registration;

    /// <summary>
    /// Creates a new <see cref="AsyncInfoTaskCompletionSource{TProgress}"/> instance with the specified parameters.
    /// </summary>
    /// <param name="asyncInfo">The <see cref="IAsyncInfo"/> instance to wrap.</param>
    /// <param name="cancellationToken">The input <see cref="CancellationToken"/> value.</param>
    public AsyncInfoTaskCompletionSource(IAsyncInfo asyncInfo, CancellationToken cancellationToken)

        // Store the operation in 'Task.AsyncState' for the full lifetime of the task, even when cancellation is not supported.
        // This ensures that keeping only the task alive also keeps the native operation and its completion callbacks alive.
        : base(asyncInfo)
    {
        _cancellationToken = cancellationToken;

        if (_cancellationToken.CanBeCanceled)
        {
            // Register a single callback that cancels the underlying 'IAsyncInfo' and also completes the task. Handling both
            // responsibilities (forwarding the cancellation to native and completing the task) in one callback means that the
            // task is completed even if the native cancellation throws (e.g. 'RPC_E_DISCONNECTED' if the backing COM server
            // has died) or never reports completion, and doesn't depend on some other registration still running afterwards
            // (if the callback threw, 'CancellationTokenSource.Cancel()' would stop invoking the remaining ones).
            try
            {
                _registration = _cancellationToken.Register(
                    callback: static @this => Unsafe.As<AsyncInfoTaskCompletionSource<TProgress>>(@this!).OnCancellation(),
                    state: this);
            }
            catch (Exception e)
            {
                // An already-canceled token runs the callback during registration; its 'finally'
                // has already made the task terminal, so 'TrySetException' preserves that outcome.
                _ = TrySetException(e);
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
    /// Cancels the underlying <see cref="IAsyncInfo"/> and completes the task accordingly.
    /// </summary>
    private void OnCancellation()
    {
        try
        {
            // 'Task.AsyncState' is always the 'IAsyncInfo' passed to the base constructor, and it cannot change afterwards
            Unsafe.As<IAsyncInfo>(Task.AsyncState!).Cancel();
        }
        catch (Exception e) when (e is COMException { HResult:
            WellKnownErrorCodes.RPC_E_DISCONNECTED or
            WellKnownErrorCodes.RPC_S_SERVER_UNAVAILABLE or
            WellKnownErrorCodes.JSCRIPT_E_CANTEXECUTE })
        {
            // A disconnected COM server cannot deliver completion. Swallow these expected failures
            // so they do not escape from 'CancellationTokenSource.Cancel()' on the calling thread
            // (potentially a thread pool thread), where an unhandled exception could terminate the process.
            // The task is still completed as canceled by the 'finally' block below, so awaiters will
            // observe the cancellation as expected.
            //
            // Note that we're intentionally only handling well-known exceptions due to "valid"
            // infrastructure issues that can happen normally (e.g. a COM server disconnecting).
            // We are not handling all exceptions, as that could hide legitimate bugs. The set of
            // 'HRESULT'-s should be kept in sync with 'ExceptionDispatchInfoExtensions.ThrowAsync'.
        }
        finally
        {
            // Complete even if native cancellation never reports completion or throws and causes
            // 'CancellationTokenSource.Cancel(throwOnFirstException: true)' to stop invoking callbacks.
            _ = TrySetCanceled(_cancellationToken);
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
    }
}