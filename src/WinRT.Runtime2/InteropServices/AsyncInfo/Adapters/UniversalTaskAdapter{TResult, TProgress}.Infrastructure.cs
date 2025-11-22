// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

#pragma warning disable IDE0072

namespace WindowsRuntime.InteropServices;

/// <inheritdoc cref="UniversalTaskAdapter{TResult, TProgress, TCompletedHandler, TProgressHandler}"/>
internal partial class UniversalTaskAdapter<
    TResult,
    TProgress,
    TCompletedHandler,
    TProgressHandler>
{
    /// <summary>
    /// Retrieves the result of the asynchronous operation, throwing any error encountered during execution.
    /// </summary>
    /// <returns>The result of the asynchronous operation.</returns>
    /// <remarks>
    /// This method should only be called when the operation has completed. If the operation completed with an
    /// error, the corresponding exception is thrown. If the operation was canceled or is not yet complete, an
    /// <see cref="InvalidOperationException"/> is thrown. For asynchronous operations, any exception thrown by
    /// the underlying task is propagated to the caller.
    /// </remarks>
    protected TResult GetResultsCore()
    {
        EnsureNotClosed();

        // If this 'IAsyncInfo' has actually faulted, 'GetResults' will throw the same error as returned by 'ErrorCode'
        if (IsInErrorState)
        {
            Exception error = ErrorCode;

            Debug.Assert(error is not null);

            ExceptionDispatchInfo.Capture(error).Throw();
        }

        // 'IAsyncInfo' throws 'E_ILLEGAL_METHOD_CALL' when called in a state other than completed (successfully)
        if (!IsInRunToCompletionState)
        {
            throw CreateCannotGetResultsFromIncompleteOperationException(null);
        }


        // If this is a synchronous operation, use the cached result
        if (CompletedSynchronously)
        {
            return (TResult)_dataContainer!;
        }

        // The operation is asynchronous, so we should have a 'Task' instance here.
        // Since 'CompletedSynchronously' is 'false' at this point, and our call to
        // 'EnsureNotClosed' did not throw, the task can only be 'null' if:
        //  - This 'IAsyncInfo' instance has completed synchronously, however we checked for this above
        //  - The safe cast to 'Task<TResult>' failed, which means we have a non-generic Task. In that case we cannot get a result.
        if (_dataContainer is not Task<TResult> task)
        {
            return default!;
        }

        Debug.Assert(IsInRunToCompletionState);

        // Pull out the task result and return. Any exceptions thrown in the task will be rethrown.
        // If this exception is a cancelation exception, meaning there was actually no error except
        // for being cancelled, we return an error code appropriate for Windows Runtime instead.
        // That is, we'll throw 'InvalidOperation' with an error code of 'E_ILLEGAL_METHOD_CALL'.
        try
        {
            return task.GetAwaiter().GetResult();
        }
        catch (TaskCanceledException tcEx)
        {
            throw CreateCannotGetResultsFromIncompleteOperationException(tcEx);
        }
    }

    /// <summary>
    /// Invoked when the asynchronous operation has completed, allowing derived classes to handle the completion logic.
    /// </summary>
    /// <param name="handler">The delegate or handler to be invoked upon completion of the asynchronous operation.</param>
    /// <param name="asyncStatus">The final status of the asynchronous operation.</param>
    /// <remarks>This method is called once per asynchronous operation, after the operation has reached a terminal state.</remarks>
    protected abstract void OnCompleted(TCompletedHandler handler, AsyncStatus asyncStatus);

    /// <summary>
    /// Raises a progress notification event to the specified handler with the provided progress information.
    /// </summary>
    /// <param name="handler">The delegate to invoke when reporting progress.</param>
    /// <param name="progressInfo">The progress data to pass to the handler.</param>
    /// <remarks>
    /// The default implementation does not support progress notifications and should
    /// be overridden in derived classes that provide progress reporting.
    /// </remarks>
    protected virtual void OnProgress(TProgressHandler handler, TProgress progressInfo)
    {
        Debug.Fail($"This 'IAsyncInfo' adapter type ('{GetType()}') doesn't support progress notifications.");
    }

    /// <summary>
    /// Invokes the <see cref="Delegate"/> passed when constructing the current instance, to initialize it.
    /// </summary>
    /// <param name="factory">The task generator to use for creating the task.</param>
    private Task? InvokeTaskFactory(Delegate factory)
    {
        if (factory is Func<Task> taskFactory)
        {
            return taskFactory();
        }

        if (factory is Func<CancellationToken, Task> cancelableTaskFactory)
        {
            _cancelTokenSource = new CancellationTokenSource();

            return cancelableTaskFactory(_cancelTokenSource.Token);
        }

        if (factory is Func<IProgress<TProgress>, Task> taskFactoryWithProgress)
        {
            return taskFactoryWithProgress(this);
        }

        if (factory is Func<CancellationToken, IProgress<TProgress>, Task> cancelableTaskFactoryWithProgress)
        {
            _cancelTokenSource = new CancellationTokenSource();

            return cancelableTaskFactoryWithProgress(_cancelTokenSource.Token, this);
        }

        Debug.Fail($"Invalid task factory of type '{factory}'.");

        return null;
    }

    /// <summary>
    /// Gets the <see cref="AsyncStatus"/> value for a given state bitmask.
    /// </summary>
    /// <param name="state">The input state flags to convert.</param>
    /// <returns>The resulting <see cref="AsyncStatus"/> value.</returns>
    private static AsyncStatus GetStatus(int state)
    {
        int asyncState = state & STATEMASK_SELECT_ANY_ASYNC_STATE;

        CheckUniqueAsyncState(asyncState);

        switch (asyncState)
        {
            case STATE_NOT_INITIALIZED:
                Debug.Fail("'STATE_NOT_INITIALIZED' should only occur when this object was not fully constructed, meaning this code shouldn't be reachable.");
                return AsyncStatus.Error;
            case STATE_STARTED:
                return AsyncStatus.Started;
            case STATE_RUN_TO_COMPLETION:
                return AsyncStatus.Completed;
            case STATE_CANCELLATION_REQUESTED:
            case STATE_CANCELLATION_COMPLETED:
                return AsyncStatus.Canceled;
            case STATE_ERROR:
                return AsyncStatus.Error;
            case STATE_CLOSED:
                Debug.Fail("This method should never be called is this 'IAsyncInfo' instance is closed.");
                return AsyncStatus.Error;
            default:
                Debug.Fail("'GetStatus' is missing an 'AsyncStatus' value to handle.");
                return AsyncStatus.Error;
        }
    }

    /// <summary>
    /// Transitions the operation to a terminal state and invokes the completion handler,
    /// ensuring that any exceptions thrown by the handler are rethrown asynchronously.
    /// </summary>
    /// <remarks>
    /// This method should be called when the operation has completed, either successfully or with an
    /// error. If the completion handler throws an exception, the exception is rethrown on the thread
    /// pool to ensure proper diagnostic reporting and to avoid being silently swallowed by the task
    /// infrastructure. The method is thread-safe with respect to concurrent closure of the operation.
    /// </remarks>
    private void TaskCompleted()
    {
        int terminalState = TransitionToTerminalState();

        Debug.Assert(IsInTerminalState);

        // We transitioned into a terminal state, so it became legal to close us concurrently.
        // So we use data from this stack and not from '_state' to get the completion status.
        // On this code path we will also fetch '_completedHandler', however that race is benign
        // because in the closed state the handler can only change to 'null', so it won't be
        // invoked, which is appropriate for this scenario.
        AsyncStatus terminationStatus = GetStatus(terminalState);

        // Try calling completed handler synchronusly (always on this synchronization context).
        // If the user callback throws an exception, it will bubble up through here. If we let
        // it though, it will be caught and swallowed by the 'Task' subsystem, which is just
        // below us on the stack. Instead we follow the same pattern as 'Task' and other parallel
        // libs and re-throw the excpetion on the thread pool to ensure a diagnostic message and
        // a fail-fast-like teardown.
        try
        {
            OnCompletedInvoker(terminationStatus);
        }
        catch (Exception ex)
        {
            ExceptionDispatchInfo.ThrowAsync(ex, synchronizationContext: null);
        }
    }

    /// <summary>
    /// Invokes the completion handler for the asynchronous operation, passing the specified status.
    /// </summary>
    /// <param name="status">The final status of the asynchronous operation to provide to the completion handler.</param>
    /// <remarks>
    /// This method ensures that the completion handler is invoked exactly once, even in the presence
    /// of concurrent state changes. If the handler is not set when this method is called, it will be
    /// invoked when the handler is later assigned.
    /// </remarks>
    private void OnCompletedInvoker(AsyncStatus status)
    {
        // Get the current handler value
        TCompletedHandler? handler = _completedHandler;

        // If we might not run the handler now, we need to remember that if it is set later, it will need to be run then
        if (handler is null)
        {
            // Remember to run the handler when it is set
            _ = SetState(
                newStateSetMask: STATEFLAG_MUST_RUN_COMPLETION_HNDL_WHEN_SET,
                newStateIgnoreMask: ~STATEFLAG_MUST_RUN_COMPLETION_HNDL_WHEN_SET);

            // The handler may have been set concurrently before we managed to call 'SetState', so check for it again
            handler = _completedHandler;

            // If handler was not set cuncurrently after all, then no worries
            if (handler is null)
            {
                return;
            }
        }

        // This method might be running cuncurrently. Create a block by emulating an interlocked reset of the
        // 'STATEFLAG_COMPLETION_HNDL_NOT_YET_INVOKED' flag in the '_state' field. Only the thread that wins
        // the race for unsetting this bit, wins, others give up.
        _ = SetState(
            newStateSetMask: 0,
            newStateIgnoreMask: ~STATEFLAG_COMPLETION_HNDL_NOT_YET_INVOKED,
            conditionBitMask: STATEFLAG_COMPLETION_HNDL_NOT_YET_INVOKED,
            conditionFailed: out bool conditionFailed);

        // We lost the race, just stop here
        if (conditionFailed)
        {
            return;
        }

        // Invoke the user handler
        OnCompleted(handler, status);
    }

    /// <summary>
    /// Implements the logic for <see cref="IProgress{T}.Report"/> for the current instance.
    /// </summary>
    /// <param name="value">The progress value to report.</param>
    private void ReportProgress(TProgress value)
    {
        TProgressHandler? handler = _progressHandler;

        // If no progress handler is set, there is nothing to do
        if (handler is null)
        {
            return;
        }

        // Try calling progress handler on the current synchronization context (we only have that).
        // If the user callback throws an exception, it will bubble up through here and reach the
        // user worker code running as this task. The user should catch it. If the user does not
        // catch it, it will be treated just as any other exception coming from the async execution
        // code: this 'IAsyncInfo' instance will be faulted.
        //
        // The 'IAsyncInfo' instance is not expected to trigger the 'Completed' and 'Progress' handlers
        // on the same context as the async operation. Instead the awaiter (i.e. using 'await') is expected
        // to make sure the handler runs on the context that the caller wants it to based on how it was configured.
        // For instance, in C#, by default the 'await' runs on the same context, but a caller can use 'ConfigureAwait'
        // to say it doesn't want to run on the same context and that should be respected which it is by allowing
        // the awaiter to decide the context.
        OnProgress(handler, value);
    }

    /// <summary>
    /// Handler for <see cref="Progress{T}.ProgressChanged"/>.
    /// </summary>
    private void OnReportChainedProgress(object? sender, TProgress progressInfo)
    {
        ReportProgress(progressInfo);
    }

    /// <summary>
    /// Transitions the current instance to a terminal state based on the completion status of the underlying task.
    /// </summary>
    /// <returns>The new async state of the current instance after the transition occurred.</returns>
    /// <remarks>
    /// This method should be called when the underlying task has completed. The resulting state reflects whether
    /// the operation ran to completion, was canceled, or faulted, based on the final status of the wrapped task.
    /// Subsequent calls after the operation has reached a terminal state have no effect on the state.
    /// </remarks>
    private int TransitionToTerminalState()
    {
        Debug.Assert(IsInRunningState);
        Debug.Assert(!CompletedSynchronously);

        Task task = (Task)_dataContainer;

        Debug.Assert(task is not null);
        Debug.Assert(task.IsCompleted);

        // If the switch below defaults, we have an erroneous implementation
        Debug.Assert(task.Status is TaskStatus.RanToCompletion or TaskStatus.Canceled or TaskStatus.Faulted);

        // Recall that 'STATE_CANCELLATION_REQUESTED' and 'STATE_CANCELLATION_COMPLETED' flags both map to the public
        // canceled state. So, we are either started or canceled. We will ask the task how it completed, and possibly
        // transition out of the canceled state. This may happen if cancellation was requested while in the started
        // state, but the task does not support cancellation, or if it can support cancellation in principle, but the
        // 'Cancel' request came in while still in the started state, but after the last opportunity to cancel.
        //
        // If the underlying operation was not able to react to the cancellation request and instead either run to
        // completion or faulted, then the state will transition into completed or faulted accordingly. If the operation
        // was really cancelled, the state will remain canceled.
        int terminalAsyncState = task.Status switch
        {
            TaskStatus.RanToCompletion => STATE_RUN_TO_COMPLETION,
            TaskStatus.Canceled => STATE_CANCELLATION_COMPLETED,
            TaskStatus.Faulted => STATE_ERROR,
            _ => STATE_ERROR
        };

        // Update the async state for this instance to represent the transition that should occur
        int newState = SetAsyncState(
            newAsyncState: terminalAsyncState,
            conditionBitMask: STATEMASK_SELECT_ANY_ASYNC_STATE,
            conditionFailed: out _);

        Debug.Assert((newState & STATEMASK_SELECT_ANY_ASYNC_STATE) == terminalAsyncState);
        Debug.Assert((_state & STATEMASK_SELECT_ANY_ASYNC_STATE) == terminalAsyncState || IsInClosedState);

        return newState;
    }

    /// <summary>
    /// Ensures that the current instance is not in the closed state.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown if the current instance is in the closed state.</exception>
    private void EnsureNotClosed()
    {
        if (!IsInClosedState)
        {
            return;
        }

        ObjectDisposedException ex = new(SR.ObjectDisposed_AsyncInfoIsClosed)
        {
            HResult = WellKnownErrorCodes.E_ILLEGAL_METHOD_CALL
        };

        throw ex;
    }

    /// <summary>
    /// Transitions the current instance to the closed state.
    /// </summary>
    private void TransitionToClosed()
    {
        // From the finalizer we always call this method, since finalization can happen any time,
        // even when in the started state (e.g. if the process ends), and we do not want to throw
        // in those cases. We always go to the closed state, even from 'STATE_NOT_INITIALIZED'. Any
        // checking whether it is legal to call this in the current state, should occur in 'Close'.
        _ = SetAsyncState(STATE_CLOSED);

        _cancelTokenSource = null;
        _dataContainer = null;
        _error = null;
        _completedHandler = null;
        _progressHandler = null;
    }
}