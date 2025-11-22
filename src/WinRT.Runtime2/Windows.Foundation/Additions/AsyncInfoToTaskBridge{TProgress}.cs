// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

#pragma warning disable IDE0010

namespace WindowsRuntime.InteropServices;

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