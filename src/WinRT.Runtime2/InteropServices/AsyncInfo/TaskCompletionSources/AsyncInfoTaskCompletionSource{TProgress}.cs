// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

#pragma warning disable IDE0010, IDE0055

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A <see cref="TaskCompletionSource"/> implementation backed by an <see cref="IAsyncInfo"/> object.
/// </summary>
/// <typeparam name="TProgress">The type of progress information.</typeparam>
[SupportedOSPlatform("windows10.0.10240.0")]
internal sealed class AsyncInfoTaskCompletionSource<TProgress> : TaskCompletionSource
{
    /// <summary>
    /// The input <see cref="CancellationToken"/> value.
    /// </summary>
    private readonly CancellationToken _cancellationToken;

    /// <summary>
    /// The <see cref="IAsyncInfo"/> instance to cancel when <see cref="_cancellationToken"/> is canceled.
    /// </summary>
    private readonly IAsyncInfo? _asyncInfo;

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
    {
        _cancellationToken = cancellationToken;

        if (_cancellationToken.CanBeCanceled)
        {
            _asyncInfo = asyncInfo;

            // Register a single callback that cancels the underlying 'IAsyncInfo' and then
            // completes the task. We handle both responsibilities (forwarding the cancellation
            // to native and completing the task) inside one callback so that any exception
            // thrown by 'IAsyncInfo.Cancel()' (e.g. 'RPC_E_DISCONNECTED' if the backing COM
            // server has died) is observed and used to fault the task, rather than escaping
            // out of the cancellation callback. If the exception escaped, it would be re-thrown
            // from 'CancellationTokenSource.Cancel()' on whichever thread invoked it (often a
            // thread pool thread), potentially crashing the process.
            _registration = _cancellationToken.Register(
                callback: static @this => Unsafe.As<AsyncInfoTaskCompletionSource<TProgress>>(@this!).OnCancellation(),
                state: this);
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
        Debug.Assert(_asyncInfo is not null);

        // Mark the task as canceled first, so that the 'Canceled' status wins over any
        // failure from the native 'Cancel' call below. This matches the original ordering
        // (where the cancellation registration ran before the 'IAsyncInfo.Cancel' one).
        _ = TrySetCanceled(_cancellationToken);

        try
        {
            _asyncInfo.Cancel();
        }
        catch (Exception e) when (e is COMException { HResult:
            WellKnownErrorCodes.RPC_E_DISCONNECTED or
            WellKnownErrorCodes.RPC_S_SERVER_UNAVAILABLE or
            WellKnownErrorCodes.JSCRIPT_E_CANTEXECUTE })
        {
            // The native 'Cancel' call failed (e.g. the COM server hosting the async
            // operation has been disconnected). Swallow the exception here so it doesn't
            // propagate out of the cancellation callback, which would otherwise be
            // re-thrown by 'CancellationTokenSource.Cancel()' on whichever thread invoked
            // it (often a thread pool thread), potentially crashing the process. The task
            // has already been marked as canceled above, so awaiters will observe the
            // cancellation as expected.
            //
            // Note that we're intentionally only handling well-known exceptions due to "valid"
            // infrastructure issues that can happen normally (e.g. a COM server disconnecting).
            // We are not handling all exceptions, as that could hide legitimate bugs. The set of
            // 'HRESULT'-s should be kept in sync with 'ExceptionDispatchInfoExtensions.ThrowAsync'.
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