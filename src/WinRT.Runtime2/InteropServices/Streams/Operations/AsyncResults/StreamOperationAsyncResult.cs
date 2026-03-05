// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using System.Runtime.Versioning;
using System.Threading;
using Windows.Foundation;
using Windows.Storage.Streams;

#pragma warning disable IDE0032, IDE0270

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A base class providing support for implementing <see cref="IAsyncResult"/> for asynchronous Windows Runtime stream operations.
/// </summary>
[SupportedOSPlatform("windows10.0.10240.0")]
internal abstract class StreamOperationAsyncResult : IAsyncResult
{
    /// <summary>
    /// The user-provided completion callback, if available.
    /// </summary>
    private readonly AsyncCallback? _userCompletionCallback;

    /// <summary>
    /// The user-provided state object, if available.
    /// </summary>
    private readonly object? _userAsyncStateInfo;

    /// <summary>
    /// Indicates whether to process the completed operation in the callback.
    /// </summary>
    private readonly bool _processCompletedOperationInCallback;

    /// <summary>
    /// The asynchronous operation to wrap.
    /// </summary>
    /// <remarks>
    /// This will be set to <see langword="null"/> when the operation is closed or canceled.
    /// </remarks>
    private IAsyncInfo? _asyncStreamOperation;

    /// <summary>
    /// Indicates whether the operation is completed.
    /// </summary>
    private volatile bool _completed;

    /// <summary>
    /// Indicates whether <see cref="_userCompletionCallback"/> has been invoked.
    /// </summary>
    private volatile bool _callbackInvoked;

    /// <summary>
    /// The wait handle to use to implement <see cref="IAsyncResult.AsyncWaitHandle"/>.
    /// </summary>
    private volatile ManualResetEvent? _waitHandle;

    /// <summary>
    /// The number of bytes processed (by calling <see cref="ProcessCompletedOperation()"/>.
    /// </summary>
    private long _numberOfBytesProcessed;

    /// <summary>
    /// The cached <see cref="ExceptionDispatchInfo"/> instance, if the operation failed.
    /// </summary>
    private ExceptionDispatchInfo? _errorInfo;

    /// <summary>
    /// The completed operation to wrap.
    /// </summary>
    /// <remarks>
    /// This will be the same instance as <see cref="_asyncStreamOperation"/>, but only set upon completion.
    /// </remarks>
    private IAsyncInfo? _completedOperation;

    /// <summary>
    /// Creates a new <see cref="StreamOperationAsyncResult"/> instance with the specified parameters.
    /// </summary>
    /// <param name="asyncOperation">The asynchronous operation to wrap.</param>
    /// <param name="userCompletionCallback">The user-provided completion callback, if available.</param>
    /// <param name="userAsyncStateInfo">The user-provided state object, if available.</param>
    /// <param name="processCompletedOperationInCallback">Indicates whether to process the completed operation in the callback.</param>
    protected StreamOperationAsyncResult(
        IAsyncInfo asyncOperation,
        AsyncCallback? userCompletionCallback,
        object? userAsyncStateInfo,
        bool processCompletedOperationInCallback)
    {
        Debug.Assert(!processCompletedOperationInCallback || userCompletionCallback is not null);

        _userCompletionCallback = userCompletionCallback;
        _userAsyncStateInfo = userAsyncStateInfo;
        _processCompletedOperationInCallback = processCompletedOperationInCallback;
        _asyncStreamOperation = asyncOperation;
    }

    /// <summary>
    /// Finalizes the current operation object.
    /// </summary>
    ~StreamOperationAsyncResult()
    {
        // This finalisation is not critical (we're not directly responsible for any unmanaged resources),
        // but we can still make an effort to notify the underlying Windows Runtime stream object that we
        // are not any longer interested in the results of the operation.
        CancelStreamOperation();
    }

    /// <inheritdoc/>
    public object? AsyncState => _userAsyncStateInfo;

    /// <inheritdoc/>
    public WaitHandle AsyncWaitHandle
    {
        get
        {
            ManualResetEvent? waitHandle = _waitHandle;

            // Just return the existing handle if we have already created one
            if (waitHandle is not null)
            {
                return waitHandle;
            }

            // Create a new wait handle passing the flag indicating whether the current operation
            // has completed. This ensures things will work fine if someone will retrieve this
            // wait handle through the public property and then wait on it. That is, if the task
            // is already completed, the wait handle will also already be in the signaled state.
            waitHandle = new ManualResetEvent(_completed);

            // Try to atomically set the new wait handle
            ManualResetEvent? otherHandle = Interlocked.CompareExchange(ref _waitHandle, waitHandle, null);

            // If the original value was not 'null', it means we raced against another thread
            // and lost. In that case, we can just dispose this handle and return the other one.
            if (otherHandle is not null)
            {
                waitHandle.Dispose();

                return otherHandle;
            }

            // If we got here, we won the race, so we can directly return our local wait handle
            return waitHandle;
        }
    }

    /// <inheritdoc/>
    public bool CompletedSynchronously => false;

    /// <inheritdoc/>
    public bool IsCompleted => _completed;

    /// <inheritdoc/>
    public bool ProcessCompletedOperationInCallback => _processCompletedOperationInCallback;

    /// <summary>
    /// Gets the number of bytes processed after a completed operation.
    /// </summary>
    /// <remarks>
    /// This property is updated by calling <see cref="ProcessCompletedOperation()"/>.
    /// </remarks>
    public long NumberOfBytesProcessed => _numberOfBytesProcessed;

    /// <summary>
    /// Gets whether the current asynchronous result failed with some error.
    /// </summary>
    [MemberNotNullWhen(true, nameof(_errorInfo))]
    public bool HasError => _errorInfo is not null;

    /// <summary>
    /// Synchronusly waits for the operation to be completed.
    /// </summary>
    public void Wait()
    {
        if (_completed)
        {
            return;
        }

        WaitHandle waitHandle = AsyncWaitHandle;

        // Keep waiting on the handle until the current task reaches the completed state
        while (!_completed)
        {
            _ = waitHandle.WaitOne();
        }
    }

    /// <summary>
    /// Processes a completed operation and updates the internal state accordingly.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the wrapped operation is not in the completed state, or if this method has been called already.</exception>
    /// <remarks>
    /// Callers should only invoke this method after checking that <see cref="ProcessCompletedOperationInCallback"/> is <see langword="false"/>.
    /// </remarks>
    public void ProcessCompletedOperation()
    {
        // The error handling is slightly tricky here. Before processing the I/O results, we are verifying some basic
        // assumptions. If they do not hold, we are throwing an 'InvalidOperationException'. However, by the time this
        // method is called, we might have already stored something into '_errorInfo', e.g. if an error occurred in
        // '_userCompletionCallback'. If that is the case, then that previous exception might include some important
        // info relevant for detecting the problem. So, we take that previous exception and attach it as the inner
        // exception to the 'InvalidOperationException' being thrown. In cases where we have a good understanding of
        // the previously saved error info, and we know for sure that it is the immediate reason for the state validation
        // to fail, we can avoid throwing the 'InvalidOperationException' altogether and only rethrow the error info.

        // Check that the 'Completed' handler has been set, meaning the wrapped operation has completed. This is set from
        // 'OnStreamOperationCompleted', which is set by each derived type as the completion handler upon construction.
        if (!_callbackInvoked)
        {
            ProcessCompletedOperation_InvalidOperationThrowHelper(SR.InvalidOperation_CannotCallThisMethodInCurrentState);
        }

        // Check that the the completion handler has actually ran to completion already (it might run concurrently)
        if (!_processCompletedOperationInCallback && !_completed)
        {
            ProcessCompletedOperation_InvalidOperationThrowHelper(SR.InvalidOperation_CannotCallThisMethodInCurrentState);
        }

        // If we don't have a completed operation, then we are in an invalid state and can't proceed
        if (_completedOperation is null)
        {
            ExceptionDispatchInfo? errorInfo = _errorInfo;

            // See if the error info is set because we observed a 'null' completed async operation previously.
            // We're explicitly checking that the message is an exact match to avoid any false positives here.
            if (errorInfo is { SourceException: ArgumentNullException { Message: SR.ArgumentNullReference_IOCompletionCallbackCannotProcessNullAsyncInfo } })
            {
                errorInfo.Throw();
            }
            else
            {
                throw InvalidOperationException.GetCannotCallThisMethodInCurrentStateException();
            }
        }

        // Ensure that the completed operation matches the one that we're currently wrapping
        if (_completedOperation.Id != _asyncStreamOperation!.Id)
        {
            ProcessCompletedOperation_InvalidOperationThrowHelper(SR.InvalidOperation_UnexpectedAsyncOperationID);
        }

        // If the completed operation is in an error state, we also can't proceed
        if (_completedOperation.Status is AsyncStatus.Error)
        {
            _numberOfBytesProcessed = 0;

            ThrowWithIOExceptionDispatchInfo(_completedOperation.ErrorCode!);
        }

        // The state is valid, so we can delegate to the derived implementation calling 'GetResults()'
        ProcessCompletedOperation(_completedOperation, out _numberOfBytesProcessed);
    }

    /// <summary>
    /// Closes the current operation.
    /// </summary>
    public void CloseStreamOperation()
    {
        try
        {
            _asyncStreamOperation?.Close();
        }
        catch
        {
        }

        _asyncStreamOperation = null;
    }

    /// <summary>
    /// Cancels the current operation.
    /// </summary>
    public void CancelStreamOperation()
    {
        if (_callbackInvoked)
        {
            return;
        }

        _asyncStreamOperation?.Cancel();
        _asyncStreamOperation = null;
    }

    /// <summary>
    /// Throws the appropriate exception for a failed operation.
    /// </summary>
    /// <remarks>
    /// Callers should only invoke this method after checking <see cref="HasError"/>.
    /// </remarks>
    public void ThrowCachedError()
    {
        _errorInfo?.Throw();
    }

    /// <summary>
    /// Processes a completed operation and returns the number of bytes being processed.
    /// </summary>
    /// <param name="completedOperation">The completed asynchronous operation.</param>
    /// <param name="numberOfBytesProcessed">The number of bytes processed by the operation.</param>
    protected abstract void ProcessCompletedOperation(IAsyncInfo completedOperation, out long numberOfBytesProcessed);

    /// <summary>
    /// Notifies the current instance that the wrapped operation has completed, and processes the completion accordingly.
    /// </summary>
    /// <param name="asyncInfo"><inheritdoc cref="AsyncActionCompletedHandler" path="/param[@name='asyncInfo']/node()"/></param>
    /// <param name="asyncStatus"><inheritdoc cref="AsyncActionCompletedHandler" path="/param[@name='asyncStatus']/node()"/></param>
    /// <remarks>This method is meant to be set to <see cref="IAsyncAction.Completed"/> (and equivalent properties) upon construction.</remarks>
    [SuppressMessage("Style", "IDE0060", Justification = "The 'asyncStatus' parameter is required, as this method is used as a completion handler.")]
    protected void OnStreamOperationCompleted(IAsyncInfo asyncInfo, AsyncStatus asyncStatus)
    {
        try
        {
            // Ensure that this is the first time this callback is being invoked
            if (_callbackInvoked)
            {
                throw InvalidOperationException.GetMultipleIOCompletionCallbackInvocationException();
            }

            _callbackInvoked = true;

            // This happens in rare stress cases in 'Console' mode, and the Windows Runtime team said this is unlikely to be fixed.
            // Moreover, this can also happen if the underlying Windows Runtime stream just has a faulty user implementation. If we
            // did not do this check, we would either get the same exception without the explanation message when dereferencing the
            // input operation later, or we would get an 'InvalidOperationException' when processing the operation. With the check,
            // a much more useful exception will be reported to the user, to inform them of exactly what went wrong and why.
            if (asyncInfo is null)
            {
                throw ArgumentNullException.GetIOCompletionCallbackCannotProcessNullAsyncInfoException(nameof(asyncInfo));
            }

            _completedOperation = asyncInfo;

            // If '_processCompletedOperationInCallback' is not set, that indicates that the stream is doing a blocking wait on the
            // wait handle of this 'IAsyncResult' instance. In that case, calls on the completed operation object may deadlock if
            // the native object is not free threaded. By setting '_processCompletedOperationInCallback' to 'false' the stream that
            // created this 'IAsyncResult' instance indicated that it will call 'ProcessCompletedOperation' after the wait handle
            // is signalled to manually fetch the results, which avoids the risk of deadlocks.
            if (_processCompletedOperationInCallback)
            {
                ProcessCompletedOperation();
            }
        }
        catch (Exception ex)
        {
            _numberOfBytesProcessed = 0;
            _errorInfo = ExceptionDispatchInfo.Capture(ex);
        }
        finally
        {
            _completed = true;

            Interlocked.MemoryBarrier();

            // From this point on, trying to access 'AsyncWaitHandle' would create a handle that is already signaled as
            // completed (because that property checks the value of '_completed'), so we do not need to check if it is
            // being produced asynchronously. That is, we can signal it if it already exists (i.e. if someone tried to
            // access 'AsyncWaitHandle' before). If it doesn't exist yet, it will be auto-signaled on creation anyway.
            _ = _waitHandle?.Set();
        }

        // Finally invoke the user-provided callback, now that all other state has been updated
        _userCompletionCallback?.Invoke(this);
    }

    [DoesNotReturn]
    [StackTraceHidden]
    private void ProcessCompletedOperation_InvalidOperationThrowHelper(string errorMessage)
    {
        Exception? errorInfoSourceException = _errorInfo?.SourceException;

        // If we don't have an exception from the error info, just throw a generic 'InvalidOperationException' with the input message
        if (errorInfoSourceException is null)
        {
            throw new InvalidOperationException(errorMessage);
        }

        // Otherwise, we can wrap the original exception as inner to provide better context for the failure.
        // We still actually throw an 'InvalidOperationException' here, so the exeption type is consistent.
        throw new InvalidOperationException(errorMessage, errorInfoSourceException);
    }

    [DoesNotReturn]
    [StackTraceHidden]
    private static void ThrowWithIOExceptionDispatchInfo(Exception exception)
    {
        RestrictedErrorInfo.AttachErrorInfo(exception);

        WindowsRuntimeIOHelpers.GetExceptionDispatchInfo(exception).Throw();
    }
}