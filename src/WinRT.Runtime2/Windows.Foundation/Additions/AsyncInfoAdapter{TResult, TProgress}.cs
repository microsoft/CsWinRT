// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.Versioning;
using Windows.Foundation;
using WindowsRuntime.InteropServices;

#pragma warning disable CS0420, IDE0072

namespace System.Threading.Tasks;

/// <summary>
/// Implements a wrapper that allows to expose managed <see cref="Tasks.Task"/> objects
/// to Windows Runtime consumers, via the projected <see cref="IAsyncInfo"/> interface.
/// </summary>
/// <typeparam name="TResult">The result type.</typeparam>
/// <typeparam name="TProgress">The type of progress information.</typeparam>
/// <typeparam name="TCompletedHandler">The type of completed handler (e.g. for <see cref="IAsyncAction.Completed"/>).</typeparam>
/// <typeparam name="TProgressHandler">The type of progress handler (e.g. for <see cref="IAsyncActionWithProgress{TProgress}.Progress"/>).</typeparam>
[SupportedOSPlatform("windows10.0.10240.0")]
internal abstract class TaskToAsyncInfoAdapter<
    TResult,
    TProgress,
    TCompletedHandler,
    TProgressHandler> : AsyncInfoAdapter,
    IAsyncInfo,
    IProgress<TProgress>
    where TCompletedHandler : class
    where TProgressHandler : class
{
    #region Private Types, Statics and Constants

    // ! THIS DIAGRAM ILLUSTRATES THE CONSTANTS BELOW. UPDATE THIS IF UPDATING THE CONSTANTS BELOW!:
    //     3         2         1         0
    //    10987654321098765432109876543210
    //    X...............................   Reserved such that we can use Int32 and not worry about negative-valued state constants
    //    ..X.............................   STATEFLAG_COMPLETED_SYNCHRONOUSLY
    //    ...X............................   STATEFLAG_MUST_RUN_COMPLETION_HNDL_WHEN_SET
    //    ....X...........................   STATEFLAG_COMPLETION_HNDL_NOT_YET_INVOKED
    //    ................................   STATE_NOT_INITIALIZED
    //    ...............................X   STATE_STARTED
    //    ..............................X.   STATE_RUN_TO_COMPLETION
    //    .............................X..   STATE_CANCELLATION_REQUESTED
    //    ............................X...   STATE_CANCELLATION_COMPLETED
    //    ...........................X....   STATE_ERROR
    //    ..........................X.....   STATE_CLOSED
    //    ..........................XXXXXX   STATEMASK_SELECT_ANY_ASYNC_STATE
    //    XXXXXXXXXXXXXXXXXXXXXXXXXX......   STATEMASK_CLEAR_ALL_ASYNC_STATES
    //     3         2         1         0
    //    10987654321098765432109876543210

    // These STATE_XXXX constants describe the async state of this object.
    // Objects of this type are in exactly in one of these states at any given time:
    private const int STATE_NOT_INITIALIZED = 0;   // 0x00
    private const int STATE_STARTED = 1;   // 0x01
    private const int STATE_RUN_TO_COMPLETION = 2;   // 0x02
    private const int STATE_CANCELLATION_REQUESTED = 4;   // 0x04
    private const int STATE_CANCELLATION_COMPLETED = 8;   // 0x08
    private const int STATE_ERROR = 16;  // 0x10
    private const int STATE_CLOSED = 32;  // 0x20

    // The STATEFLAG_XXXX constants can be bitmasked with the states to describe additional
    // state info that cannot be easily inferred from the async state:
    private const int STATEFLAG_COMPLETED_SYNCHRONOUSLY = 0x20000000;
    private const int STATEFLAG_MUST_RUN_COMPLETION_HNDL_WHEN_SET = 0x10000000;
    private const int STATEFLAG_COMPLETION_HNDL_NOT_YET_INVOKED = 0x8000000;

    // These two masks are used to select any STATE_XXXX bits and clear all other (i.e. STATEFLAG_XXXX) bits.
    // It is set to (next power of 2 after the largest STATE_XXXX value) - 1.
    // !!! Make sure to update this if a new STATE_XXXX value is added above !!
    private const int STATEMASK_SELECT_ANY_ASYNC_STATE = 64 - 1;

    // These two masks are used to clear all STATE_XXXX bits and leave any STATEFLAG_XXXX bits.
    private const int STATEMASK_CLEAR_ALL_ASYNC_STATES = ~STATEMASK_SELECT_ANY_ASYNC_STATE;

    private static InvalidOperationException CreateCannotGetResultsFromIncompleteOperationException(Exception? cause)
    {
        InvalidOperationException ex = (cause is null)
                        ? new InvalidOperationException(SR.InvalidOperation_CannotGetResultsFromIncompleteOperation)
                        : new InvalidOperationException(SR.InvalidOperation_CannotGetResultsFromIncompleteOperation, cause);
        ex.HResult = WellKnownErrorCodes.E_ILLEGAL_METHOD_CALL;
        return ex;
    }

    #endregion Private Types, Statics and Constants


    #region Instance variables

    /// <summary>The token source used to cancel running operations.</summary>
    private volatile CancellationTokenSource? _cancelTokenSource;

    /// <summary>The async info's ID.</summary>
    /// <remarks>The <see cref="AsyncInfoIdGenerator.InvalidId"/> value stands for not yet been initialised.</remarks>
    private volatile uint _id = AsyncInfoIdGenerator.InvalidId;

    /// <summary>
    /// The cached error code used to avoid creating several exception objects
    /// if the <see cref="ErrorCode"/> property is accessed several times.
    /// </summary>
    /// <remarks>
    /// A <see langword="null"/> value indicates either no error or that <see cref="ErrorCode"/> has not yet been called.
    /// </remarks>
    private volatile Exception? _error;

    /// <summary>The state of the async info. Interlocked operations are used to manipulate this field.</summary>
    private volatile int _state = STATE_NOT_INITIALIZED;

    /// <summary>For IAsyncInfo instances that completed synchronously (at creation time) this field holds the result;
    /// for instances backed by an actual Task, this field holds a reference to the task generated by the task generator.
    /// Since we always know which of the above is the case, we can always cast this field to TResult in the former case
    /// or to one of Task or Task{TResult} in the latter case. This approach allows us to save a field on all IAsyncInfos.
    /// Notably, this makes us pay the added cost of boxing for synchronously completing IAsyncInfos where TResult is a
    /// value type, however, this is expected to occur rather rare compared to non-synchronously completed user-IAsyncInfos.</summary>
    private volatile object? _dataContainer;

    /// <summary>The registered completed handler.</summary>
    private volatile TCompletedHandler? _completedHandler;

    /// <summary>The registered progress handler.</summary>
    private volatile TProgressHandler? _progressHandler;

    #endregion Instance variables


    #region Constructors and Destructor

    /// <summary>Creates an IAsyncInfo from the specified delegate. The delegate will be called to construct a task that will
    /// represent the future encapsulated by this IAsyncInfo.</summary>
    /// <param name="factory">The task generator to use for creating the task.</param>
    protected TaskToAsyncInfoAdapter(Delegate factory)
    {
        Debug.Assert(factory is
            Func<Task> or
            Func<CancellationToken, Task> or
            Func<IProgress<TProgress>, Task> or
            Func<CancellationToken, IProgress<TProgress>, Task>);

        // Construct task from the specified provider
        Task? task = InvokeTaskFactory(factory);

        if (task is null)
        {
            throw new NullReferenceException(SR.NullReference_TaskProviderReturnedNull);
        }

        if (task.Status == TaskStatus.Created)
        {
            throw new InvalidOperationException(SR.InvalidOperation_TaskProviderReturnedUnstartedTask);
        }

        _dataContainer = task;
        _state = STATEFLAG_COMPLETION_HNDL_NOT_YET_INVOKED | STATE_STARTED;

        // Set the completion routine and let the task run
        _ = task.ContinueWith(
            continuationAction: static (_, @this) => Unsafe.As<TaskToAsyncInfoAdapter<TResult, TProgress, TCompletedHandler, TProgressHandler>>(@this!).TaskCompleted(),
            state: this,
            cancellationToken: CancellationToken.None,
            continuationOptions: TaskContinuationOptions.ExecuteSynchronously,
            scheduler: TaskScheduler.Default);
    }


    /// <summary>
    /// Creates an IAsyncInfo from the Task object. The specified task represents the future encapsulated by this IAsyncInfo.
    /// The specified CancellationTokenSource and Progress are assumed to be the source of the specified Task's cancellation and
    /// the Progress that receives reports from the specified Task.
    /// </summary>
    /// <param name="task">The Task whose operation is represented by this IAsyncInfo</param>
    /// <param name="cancellationTokenSource">The cancellation control for the cancellation token observed
    /// by <code>underlyingTask</code>.</param>
    /// <param name="progress">A progress listener/pugblisher that receives progress notifications
    /// form <code>underlyingTask</code>.</param>
    protected TaskToAsyncInfoAdapter(Task task, CancellationTokenSource? cancellationTokenSource, Progress<TProgress>? progress)
    {
        // Throw InvalidOperation and not Argument for parity with the constructor that takes Delegate taskProvider:
        if (task.Status == TaskStatus.Created)
        {
            throw new InvalidOperationException(SR.InvalidOperation_UnstartedTaskSpecified);
        }

        // We do not need to invoke any delegates to get the task, it is provided for us:
        _dataContainer = task;

        // This must be the cancellation source for the token that the specified underlyingTask observes for cancellation:
        // (it may also be null in cases where the specified underlyingTask does nto support cancellation)
        _cancelTokenSource = cancellationTokenSource;

        // Iff the specified underlyingTask reports progress, chain the reports to this IAsyncInfo's reporting method:
        progress?.ProgressChanged += OnReportChainedProgress;

        _state = STATEFLAG_COMPLETION_HNDL_NOT_YET_INVOKED | STATE_STARTED;

        // Set the completion routine and let the task run
        _ = task.ContinueWith(
            continuationAction: static (_, @this) => Unsafe.As<TaskToAsyncInfoAdapter<TResult, TProgress, TCompletedHandler, TProgressHandler>>(@this!).TaskCompleted(),
            state: this,
            cancellationToken: CancellationToken.None,
            continuationOptions: TaskContinuationOptions.ExecuteSynchronously,
            scheduler: TaskScheduler.Default);
    }


    /// <summary>
    /// Creates an IAsyncInfo from the specified result value. The IAsyncInfo is created in the Completed state and the
    /// specified <code>synchronousResult</code> is used as the result value.
    /// </summary>
    /// <param name="result">The result of this synchronously completed IAsyncInfo.</param>
    protected TaskToAsyncInfoAdapter(TResult result)
    {
        // Set the synchronous result
        _dataContainer = result;

        // Mark the task as completed successfully
        _state =
            STATEFLAG_COMPLETED_SYNCHRONOUSLY |
            STATEFLAG_MUST_RUN_COMPLETION_HNDL_WHEN_SET |
            STATEFLAG_COMPLETION_HNDL_NOT_YET_INVOKED |
            STATE_RUN_TO_COMPLETION;
    }

    /// <summary>
    /// Creates a new <see cref="TaskToAsyncInfoAdapter{TResult, TProgress, TCompletedHandler, TProgressHandler}"/> instance with the specified parameters.
    /// </summary>
    /// <param name="_">The <see cref="CanceledTaskPlaceholder"/> value to select this overload.</param>
    protected TaskToAsyncInfoAdapter(CanceledTaskPlaceholder _)
    {
        _dataContainer = null;
        _error = null;

        // Mark the task as completed and canceled
        _state =
            STATEFLAG_COMPLETED_SYNCHRONOUSLY |
            STATEFLAG_MUST_RUN_COMPLETION_HNDL_WHEN_SET |
            STATEFLAG_COMPLETION_HNDL_NOT_YET_INVOKED |
            STATE_CANCELLATION_COMPLETED;
    }

    /// <summary>
    /// Creates a new <see cref="TaskToAsyncInfoAdapter{TResult, TProgress, TCompletedHandler, TProgressHandler}"/> instance with the specified parameters.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to use to set the error state for the resulting instance.</param>
    protected TaskToAsyncInfoAdapter(Exception exception)
    {
        _dataContainer = null;
        _error = exception;

        // Mark the task as completed and in faulted state
        _state =
            STATEFLAG_COMPLETED_SYNCHRONOUSLY |
            STATEFLAG_MUST_RUN_COMPLETION_HNDL_WHEN_SET |
            STATEFLAG_COMPLETION_HNDL_NOT_YET_INVOKED |
            STATE_ERROR;
    }

    ~TaskToAsyncInfoAdapter()
    {
        TransitionToClosed();
    }

    #endregion Constructors and Destructor


    #region State bit field operations

    [MemberNotNullWhen(false, nameof(_dataContainer))]
    private bool CompletedSynchronously
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (_state & STATEFLAG_COMPLETED_SYNCHRONOUSLY) != 0;
    }

    private bool IsInRunToCompletionState
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (_state & STATE_RUN_TO_COMPLETION) != 0;
    }

    [MemberNotNullWhen(true, nameof(ErrorCode))]
    private bool IsInErrorState
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (_state & STATE_ERROR) != 0;
    }

    private bool IsInClosedState
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (_state & STATE_CLOSED) != 0;
    }

    private bool IsInRunningState
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (_state & (STATE_STARTED | STATE_CANCELLATION_REQUESTED)) != 0;
    }

    private bool IsInTerminalState
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (_state & (STATE_RUN_TO_COMPLETION | STATE_CANCELLATION_COMPLETED | STATE_ERROR)) != 0;
    }

    private static bool CheckUniqueAsyncState(int state)
    {
        return state == 0 || BitOperations.IsPow2(unchecked((uint)state));
    }

    #endregion State bit field operations


    #region Infrastructure methods

    /// <inheritdoc/>
    public sealed override Task? Task
    {
        get
        {
            EnsureNotClosed();

            return CompletedSynchronously ? null : (Task)_dataContainer;
        }
    }

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

    protected abstract void OnCompleted(TCompletedHandler handler, AsyncStatus asyncStatus);


    protected virtual void OnProgress(TProgressHandler handler, TProgress progressInfo)
    {
        Debug.Fail($"This 'IAsyncInfo' adapter type ('{GetType()}') doesn't support progress notifications.");
    }


    private void OnCompletedInvoker(AsyncStatus status)
    {
        // Get the current handler value
        TCompletedHandler? handler = _completedHandler;

        // If we might not run the handler now, we need to remember that if it is set later, it will need to be run then
        if (handler is null)
        {
            // Remember to run the handler when it is set:
            _ = SetState(
                newStateSetMask: STATEFLAG_MUST_RUN_COMPLETION_HNDL_WHEN_SET,
                newStateIgnoreMask: ~STATEFLAG_MUST_RUN_COMPLETION_HNDL_WHEN_SET,
                conditionBitMask: 0,
                useCondition: false,
                conditionFailed: out _);

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
            useCondition: true,
            conditionFailed: out bool conditionFailed);

        // We lost the race, just stop here
        if (conditionFailed)
        {
            return;
        }

        // Invoke the user handler
        OnCompleted(handler, status);
    }

    /// <inheritdoc/>
    void IProgress<TProgress>.Report(TProgress value)
    {
        Report(value);
    }

    private void Report(TProgress value)
    {
        TProgressHandler? handler = _progressHandler;

        // If no progress handler is set, there is nothing to do
        if (handler is null)
        {
            return;
        }

        // Try calling progress handler in the right synchronization context.
        // If the user callback throws an exception, it will bubble up through here and reach the
        // user worker code running as this async future. The user should catch it.
        // If the user does not catch it, it will be treated just as any other exception coming from the async execution code:
        // this AsyncInfo will be faulted.

        // The IAsyncInfo is not expected to trigger the Completed and Progress handlers on the same context as the async operation.
        // Instead the awaiter (co_await in C++/WinRT, await in C#) is expected to make sure the handler runs on the context that the caller
        // wants it to based on how it was configured.  For instance, in C#, by default the await runs on the same context, but a caller
        // can use ConfigureAwait to say it doesn't want to run on the same context and that should be respected which it is
        // by allowing the awaiter to decide the context.

        // The starting context is null, invoke directly:
        OnProgress(handler, value);
    }

    private void OnReportChainedProgress(object? sender, TProgress progressInfo)
    {
        Report(progressInfo);
    }

    /// <summary>
    /// Sets the <code>m_state</code> bit field to reflect the specified async state with the corresponding STATE_XXX bit mask.
    /// </summary>
    /// <param name="newAsyncState">Must be one of the STATE_XXX (not STATEYYY_ZZZ !) constants defined in this class.</param>
    /// <param name="conditionBitMask">If <code>useCondition</code> is FALSE: this field is ignored.
    ///                                If <code>useCondition</code> is TRUE: Unless this value has at least one bit with <code>m_state</code> in
    ///                                                                      common, this method will not perform any action.</param>
    /// <param name="useCondition">If TRUE, use <code>conditionBitMask</code> to determine whether the state should be set;
    ///                            If FALSE, ignore <code>conditionBitMask</code>.</param>
    /// <param name="conditionFailed">If <code>useCondition</code> is FALSE: this field is set to FALSE;
    ///                               If <code>useCondition</code> is TRUE: this field indicated whether the specified <code>conditionBitMask</code>
    ///                                                                     had at least one bit in common with <code>m_state</code> (TRUE)
    ///                                                                     or not (FALSE).
    ///                               (!) Note that the meaning of this parameter to the caller is not quite the same as whether <code>m_state</code>
    ///                               is/was set to the specified value, because <code>m_state</code> may already have had the specified value, or it
    ///                               may be set and then immediately changed by another thread. The true meaning of this parameter is whether or not
    ///                               the specified condition did hold before trying to change the state.</param>
    /// <returns>The value at which the current invocation of this method left <code>m_state</code>.</returns>
    private int SetAsyncState(int newAsyncState, int conditionBitMask, bool useCondition, out bool conditionFailed)
    {
        Debug.Assert(CheckUniqueAsyncState(newAsyncState & STATEMASK_SELECT_ANY_ASYNC_STATE));
        Debug.Assert(CheckUniqueAsyncState(_state & STATEMASK_SELECT_ANY_ASYNC_STATE));

        int resultState = SetState(newAsyncState, STATEMASK_CLEAR_ALL_ASYNC_STATES, conditionBitMask, useCondition, out conditionFailed);

        Debug.Assert(CheckUniqueAsyncState(resultState & STATEMASK_SELECT_ANY_ASYNC_STATE));

        return resultState;
    }


    /// <summary>
    /// Sets the specified bits in the <code>m_state</code> bit field according to the specified bit-mask parameters.
    /// </summary>
    /// <param name="newStateSetMask">The bits to turn ON in the <code>m_state</code> bit field</param>
    /// <param name="newStateIgnoreMask">Any bits that are OFF in this value will get turned OFF,
    ///                                  unless they are explicitly switched on by <code>newStateSetMask</code>.</param>
    /// <param name="conditionBitMask">If <code>useCondition</code> is FALSE: this field is ignored.
    ///                                If <code>useCondition</code> is TRUE: Unless this value has at least one bit with <code>m_state</code> in
    ///                                                                      common, this method will not perform any action.</param>
    /// <param name="useCondition">If TRUE, use <code>conditionBitMask</code> to determine whether the state should be set;
    ///                            If FALSE, ignore <code>conditionBitMask</code>.</param>
    /// <param name="conditionFailed">If <code>useCondition</code> is FALSE: this field is set to FALSE;
    ///                               If <code>useCondition</code> is TRUE: this field indicated whether the specified <code>conditionBitMask</code>
    ///                                                                     had at least one bit in common with <code>m_state</code> (TRUE)
    ///                                                                     or not (FALSE).
    ///                               (!) Note that the meaning of this parameter to the caller is not quite the same as whether <code>m_state</code>
    ///                               is/was set to the specified value, because <code>m_state</code> may already have had the specified value, or it
    ///                               may be set and then immediately changed by another thread. The true meaning of this parameter is whether or not
    ///                               the specified condition did hold before trying to change the state.</param>
    /// <returns>The value at which the current invocation of this method left <code>m_state</code>.</returns>
    private int SetState(int newStateSetMask, int newStateIgnoreMask, int conditionBitMask, bool useCondition, out bool conditionFailed)
    {
        int origState = _state;

        if (useCondition && 0 == (origState & conditionBitMask))
        {
            conditionFailed = true;
            return origState;
        }

        int newState = (origState & newStateIgnoreMask) | newStateSetMask;
        int prevState = Interlocked.CompareExchange(ref _state, newState, origState);

        // If m_state changed concurrently, we want to make sure that the change being made is based on a bitmask that is up to date:
        // (this relies of the fact that all state machines that save their state in m_state have no cycles)
        while (true)
        {
            if (prevState == origState)
            {
                conditionFailed = false;
                return newState;
            }

            origState = _state;

            if (useCondition && 0 == (origState & conditionBitMask))
            {
                conditionFailed = true;
                return origState;
            }

            newState = (origState & newStateIgnoreMask) | newStateSetMask;
            prevState = Interlocked.CompareExchange(ref _state, newState, origState);
        }
    }


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
            useCondition: true,
            conditionFailed: out _);

        Debug.Assert((newState & STATEMASK_SELECT_ANY_ASYNC_STATE) == terminalAsyncState);
        Debug.Assert((_state & STATEMASK_SELECT_ANY_ASYNC_STATE) == terminalAsyncState || IsInClosedState);

        return newState;
    }


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


    private static AsyncStatus GetStatus(int state)
    {
        int asyncState = state & STATEMASK_SELECT_ANY_ASYNC_STATE;

        Debug.Assert(CheckUniqueAsyncState(asyncState));

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

    private void TransitionToClosed()
    {
        // From the finaliser we always call this Close version since finalisation can happen any time, even when STARTED (e.g. process ends)
        // and we do not want to throw in those cases.

        // Always go to closed, even from STATE_NOT_INITIALIZED.
        // Any checking whether it is legal to call CLosed inthe current state, should occur in Close().
        _ = SetAsyncState(
            newAsyncState: STATE_CLOSED,
            conditionBitMask: 0,
            useCondition: false,
            conditionFailed: out _);

        _cancelTokenSource = null;
        _dataContainer = null;
        _error = null;
        _completedHandler = null;
        _progressHandler = null;
    }

    #endregion Infrastructure methods


    #region Implementation of IAsyncInfo

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

    /// <inheritdoc/>
    public void Cancel()
    {
        // 'Cancel' will be ignored in any terminal state, including closed.
        // In other words, it is ignored in any state except the started one.
        _ = SetAsyncState(
            newAsyncState: STATE_CANCELLATION_REQUESTED,
            conditionBitMask: STATE_STARTED,
            useCondition: true,
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
    #endregion Implementation of IAsyncInfo
}