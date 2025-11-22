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