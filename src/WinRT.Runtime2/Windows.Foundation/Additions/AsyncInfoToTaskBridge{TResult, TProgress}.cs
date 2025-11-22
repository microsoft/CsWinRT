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

        // The logic for this constructor should be kept in sync with the one without 'TResult'
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

        // If we're already completed, unregister everything again
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
            Debug.Assert(asyncInfo.Status == asyncStatus, "The input async status doesn't match what 'IAsyncInfo' is reporting.");

            // Validate that the async status is valid for this call
            if (asyncStatus is not (AsyncStatus.Completed or AsyncStatus.Canceled or AsyncStatus.Error))
            {
                Debug.Fail("The async operation should be in a terminal state.");

                throw new InvalidOperationException("The asynchronous operation could not be completed.");
            }

            TResult? result = default;
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
            else if (asyncStatus == AsyncStatus.Completed)
            {
                // Callers of this method are validating that the 'TProgress'
                // value being used is correct for the interface type in use.
                // So here we can avoid the interface casts to reduce overhead.
                try
                {
                    result = typeof(TProgress) == typeof(VoidValueTypeParameter)
                        ? Unsafe.As<IAsyncOperation<TResult>>(asyncInfo).GetResults()
                        : Unsafe.As<IAsyncOperationWithProgress<TResult, TProgress>>(asyncInfo).GetResults();
                }
                catch (Exception e)
                {
                    // According to the WinRT team, this can happen in some egde cases,
                    // such as marshalling errors in calls to native 'GetResults()' methods.
                    error = e;
                    asyncStatus = AsyncStatus.Error;
                }
            }

            bool success = false;

            // Complete the task based on the previously retrieved results
            switch (asyncStatus)
            {
                case AsyncStatus.Completed:
                    success = TrySetResult(result!);
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