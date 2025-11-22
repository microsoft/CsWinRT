// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

#pragma warning disable CS0420

namespace WindowsRuntime.InteropServices;

/// <inheritdoc cref="TaskToAsyncInfoAdapter{TResult, TProgress, TCompletedHandler, TProgressHandler}"/>
internal partial class TaskToAsyncInfoAdapter<
    TResult,
    TProgress,
    TCompletedHandler,
    TProgressHandler>
{
    /// <inheritdoc/>
    public uint Id
    {
        get
        {
            EnsureNotClosed();

            return AsyncInfoIdGenerator.EnsureInitialized(ref _id);
        }
    }

    /// <inheritdoc/>
    public AsyncStatus Status
    {
        get
        {
            EnsureNotClosed();

            return GetStatus(_state);
        }
    }

    /// <inheritdoc/>
    public Exception? ErrorCode
    {
        get
        {
            EnsureNotClosed();

            // If the task is faulted, hand back an error representing its first exception.
            // Otherwise, hand back 'S_OK' (which is just a 'null' exception, with no error code).
            if (!IsInErrorState)
            {
                return null;
            }

            Exception? error = _error;

            // If we have an exception object, then it means we are in a terminal state. So if we do
            // have an instance, we can just return it. If we completed synchronously, we return the
            // current error, given that even if it is 'null' we do not expect this to change.
            if (error is not null || CompletedSynchronously)
            {
                return error;
            }

            // The '_dataContainer' field is always set to the wrapped 'Task' from the constructor,
            // if the one not marking the 'IAsyncInfo' object as completed synchronously was used.
            Task task = (Task)_dataContainer;

            // By spec, if 'Task.IsFaulted' is 'true', then 'Task.Exception' must not be 'null', and
            // its 'InnerException' property must also not be 'null'. However, in case something is
            // unexpected on the 'Task' side of the road, let's be defensive instead of failing with
            // an inexplicable 'NullReferenceException'.
            error = task.Exception is AggregateException aggregateException
                ? aggregateException.InnerException ?? aggregateException
                : new Exception(SR.WinRtCOM_Error) { HResult = WellKnownErrorCodes.E_FAIL };

            // If '_error' was set concurrently, the previous value will be not 'null'. If we see that,
            // then we return that value, as it would be the first error that was set on this instance.
            // Otherwise if the old value was still 'null', it means we won the race, and can return.
            return Interlocked.CompareExchange(ref _error, error, null) ?? error;
        }
    }

    /// <inheritdoc/>
    public void Cancel()
    {
        // 'Cancel' will be ignored in any terminal state, including closed.
        // In other words, it is ignored in any state except the started one.
        _ = SetAsyncState(
            newAsyncState: STATE_CANCELLATION_REQUESTED,
            conditionBitMask: STATE_STARTED,
            conditionFailed: out bool stateWasNotStarted);

        // If the state was different than 'STATE_STARTED'
        if (!stateWasNotStarted)
        {
            _cancelTokenSource?.Cancel();
        }
    }

    /// <inheritdoc/>
    public void Close()
    {
        if (IsInClosedState)
        {
            return;
        }

        // 'Close' can only be invoked from a terminal state
        if (!IsInTerminalState)
        {
            // If 'STATE_NOT_INITIALIZED' is set, the we probably threw from the constructor.
            // The finalizer will be called anyway and we need to free this partially
            // constructed object correctly. So we avoid throwing when we are in this scenario.
            // In other words, we throw only if some async state is set correctly.
            if ((_state & STATEMASK_SELECT_ANY_ASYNC_STATE) != 0)
            {
                InvalidOperationException exception = new(SR.InvalidOperation_IllegalStateChange)
                {
                    HResult = WellKnownErrorCodes.E_ILLEGAL_STATE_CHANGE
                };

                throw exception;
            }
        }

        TransitionToClosed();
    }

    /// <summary>
    /// Gets or sets the completed handler.
    /// </summary>
    /// <remarks>
    /// The completion handler even when this <see cref="IAsyncInfo"/> instance is already started (no other choice).
    /// If we the completion handler is set before this <see cref="IAsyncInfo"/> instance completes, then the handler
    /// will be called upon completion as normal. If we the completion handler is set after this <see cref="IAsyncInfo"/>
    /// instance completes, then this setter will invoke the handler synchronously on the current context.
    /// </remarks>
    public TCompletedHandler? Completed
    {
        get
        {
            TCompletedHandler? handler = _completedHandler;

            EnsureNotClosed();

            return handler;
        }
        set
        {
            EnsureNotClosed();

            // Try setting completion handler, but only if this has not yet been done. We allow setting 'Completed' to 'null'
            // multiple times if and only if it has not yet been set to anything else than 'null'. Some other Windows Runtime
            // projection languages do not allow setting the 'Completed' handler more than once, even if it is set to 'null'.
            // We could do the same by introducing a new 'STATEFLAG_COMPLETION_HNDL_SET' bit-flag constant and saving a this
            // state into the '_state' field to indicate that the completion handler has been set previously, but we choose
            // not to do this, which gives more flexibility to users of this class.
            TCompletedHandler? handlerBefore = Interlocked.CompareExchange(ref _completedHandler, value, null);

            if (handlerBefore is not null)
            {
                InvalidOperationException exception = new(SR.InvalidOperation_CannotSetCompletionHanlderMoreThanOnce)
                {
                    HResult = WellKnownErrorCodes.E_ILLEGAL_DELEGATE_ASSIGNMENT
                };

                throw exception;
            }

            // The handler was 'null' and it's being set to 'null' again, so nothing to do
            if (value is null)
            {
                return;
            }

            // If 'STATEFLAG_MUST_RUN_COMPLETION_HNDL_WHEN_SET' is not set, then we are done (i.e. no need to invoke the handler synchronously)
            if ((_state & STATEFLAG_MUST_RUN_COMPLETION_HNDL_WHEN_SET) == 0)
            {
                return;
            }

            // We have changed the handler and at some point this 'IAsyncInfo' may have transitioned to the closed state.
            // This is fine, but if this happened we need to ensure that we only leave a 'null' handler behind.
            if (IsInClosedState)
            {
                Interlocked.Write(ref _completedHandler, null);

                return;
            }

            Debug.Assert(IsInTerminalState);

            // The 'STATEFLAG_MUST_RUN_COMPLETION_HNDL_WHEN_SET' flag was set, so we need to call the completion handler now
            OnCompletedInvoker(Status);
        }
    }

    /// <summary>
    /// Gets or sets the progress handler.
    /// </summary>
    public TProgressHandler? Progress
    {
        get
        {
            TProgressHandler? handler = _progressHandler;

            EnsureNotClosed();

            return handler;
        }
        set
        {
            EnsureNotClosed();

            Interlocked.Write(ref _progressHandler, null);

            // If we transitioned into the closed state after the above check, we will need to reset the handler
            if (IsInClosedState)
            {
                Debug.Assert(IsInTerminalState);

                Interlocked.Write(ref _progressHandler, null);
            }
        }
    }
}