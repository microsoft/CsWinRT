// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
using Windows.Foundation;

namespace WindowsRuntime.InteropServices;

/// <inheritdoc cref="TaskToAsyncInfoAdapter{TResult, TProgress, TCompletedHandler, TProgressHandler}"/>
internal partial class TaskToAsyncInfoAdapter<
    TResult,
    TProgress,
    TCompletedHandler,
    TProgressHandler>
{
    // THIS DIAGRAM ILLUSTRATES THE CONSTANTS BELOW. UPDATE THIS IF UPDATING THE CONSTANTS BELOW:
    //
    //     3         2         1         0
    //    10987654321098765432109876543210
    //    X...............................   Reserved such that we can use 'int' and not worry about negative-valued state constants
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

    // These 'STATE_XXXX' constants describe the async state of this object.
    // Objects of this type are in exactly in one of these states at any given time.

    /// <summary>The object is not initialized (usually because a constructor threw an exception).</summary>
    private const int STATE_NOT_INITIALIZED = 0;

    /// <summary>The async action has started.</summary>
    private const int STATE_STARTED = 1;

    /// <summary>The async action has run to completion (successfully).</summary>
    private const int STATE_RUN_TO_COMPLETION = 2;

    /// <summary>Cancellation has been requested for the async action.</summary>
    private const int STATE_CANCELLATION_REQUESTED = 4;

    /// <summary>Cancellation has been performed for the async action.</summary>
    private const int STATE_CANCELLATION_COMPLETED = 8;

    /// <summary>The async action is in an error (faulted) state.</summary>
    private const int STATE_ERROR = 16;

    /// <summary>The async action has been disposed.</summary>
    private const int STATE_CLOSED = 32;

    // The 'STATEFLAG_XXXX' constants can be bitmasked with the states to describe
    // additional state info that cannot be easily inferred from the async state.

    /// <summary>The async action has completed synchronously.</summary>
    private const int STATEFLAG_COMPLETED_SYNCHRONOUSLY = 0x20000000;

    /// <summary>The completion handler must be run immediately (synchronously) when set.</summary>
    private const int STATEFLAG_MUST_RUN_COMPLETION_HNDL_WHEN_SET = 0x10000000;

    /// <summary>The completion handler has been set but not run yet.</summary>
    private const int STATEFLAG_COMPLETION_HNDL_NOT_YET_INVOKED = 0x8000000;

    // This mask is used to select any 'STATE_XXXX' bits and clear all other
    // (i.e. 'STATEFLAG_XXXX') bits. It is set to:
    //
    //     '(next power of 2 after the largest 'STATE_XXXX' value) - 1'.
    //
    // Make sure to update this if a new 'STATE_XXXX' value is added above.

    /// <summary>Mask to select any async states.</summary>
    private const int STATEMASK_SELECT_ANY_ASYNC_STATE = 64 - 1;

    // This mask is used to clear all 'STATE_XXXX' bits and leave any 'STATEFLAG_XXXX' bits.

    /// <summary>Mask to clear all async states and only leave combined flags (e.g. <see cref="STATEFLAG_COMPLETED_SYNCHRONOUSLY"/>).</summary>
    private const int STATEMASK_CLEAR_ALL_ASYNC_STATES = ~STATEMASK_SELECT_ANY_ASYNC_STATE;

    /// <summary>
    /// Gets whether the async action completed synchronously.
    /// </summary>
    [MemberNotNullWhen(false, nameof(_dataContainer))]
    private bool CompletedSynchronously
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (_state & STATEFLAG_COMPLETED_SYNCHRONOUSLY) != 0;
    }

    /// <summary>
    /// Gets whether the async action has run to completion (successfully).
    /// </summary>
    private bool IsInRunToCompletionState
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (_state & STATE_RUN_TO_COMPLETION) != 0;
    }

    /// <summary>
    /// Gets whether the async action is in an error (faulted) state.
    /// </summary>
    [MemberNotNullWhen(true, nameof(ErrorCode))]
    private bool IsInErrorState
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (_state & STATE_ERROR) != 0;
    }

    /// <summary>
    /// Gets whether the async action has been disposed.
    /// </summary>
    private bool IsInClosedState
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (_state & STATE_CLOSED) != 0;
    }

    /// <summary>
    /// Gets whether the async action is in a running state (started or cancellation requested).
    /// </summary>
    private bool IsInRunningState
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (_state & (STATE_STARTED | STATE_CANCELLATION_REQUESTED)) != 0;
    }

    /// <summary>
    /// Gets whether the async action is in a terminal state (run to completion, cancellation completed, or error).
    /// </summary>
    private bool IsInTerminalState
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (_state & (STATE_RUN_TO_COMPLETION | STATE_CANCELLATION_COMPLETED | STATE_ERROR)) != 0;
    }

    /// <summary>
    /// Checks whether the current state value has at most one async state bit set.
    /// </summary>
    /// <param name="state">The state value to check.</param>
    /// <returns>Whether <paramref name="state"/> is valid.</returns>
    [Conditional("DEBUG")]
    private static void CheckUniqueAsyncState(int state)
    {
        Debug.Assert(state == 0 || BitOperations.IsPow2(unchecked((uint)state)));
    }

    /// <summary>
    /// Sets the <see cref="_state"/> field to reflect the specified async state with the corresponding <c>STATE_XXX</c> bit mask.
    /// </summary>
    /// <param name="newAsyncState">Must be one of the <c>STATE_XXX</c> (not <c>STATEFLAG_XXX</c>) constants defined in this class.</param>
    /// <returns>The value at which the current invocation of this method left <see cref="_state"/>.</returns>
    private int SetAsyncState(int newAsyncState)
    {
        CheckUniqueAsyncState(newAsyncState & STATEMASK_SELECT_ANY_ASYNC_STATE);
        CheckUniqueAsyncState(_state & STATEMASK_SELECT_ANY_ASYNC_STATE);

        int resultState = SetState(newAsyncState, STATEMASK_CLEAR_ALL_ASYNC_STATES);

        CheckUniqueAsyncState(resultState & STATEMASK_SELECT_ANY_ASYNC_STATE);

        return resultState;
    }

    /// <summary>
    /// Sets the <see cref="_state"/> field to reflect the specified async state with the corresponding <c>STATE_XXX</c> bit mask.
    /// </summary>
    /// <param name="newAsyncState">Must be one of the <c>STATE_XXX</c> (not <c>STATEFLAG_XXX</c>) constants defined in this class.</param>
    /// <param name="conditionBitMask">Unless this value has at least one bit with <see cref="_state"/> in common, this method will not perform any action.</param>
    /// <param name="conditionFailed">Indicates whether the specified <paramref name="conditionBitMask"/> had at least one bit in common with <see cref="_state"/>.</param>
    /// <returns>The value at which the current invocation of this method left <see cref="_state"/>.</returns>
    /// <remarks>
    /// Note that the meaning of the <paramref name="conditionFailed"/> parameter to the caller is not quite the same as whether <see cref="_state"/>
    /// is/was set to the specified value, because <see cref="_state"/> may already have had the specified value, or it may be set and then immediately
    /// changed by another thread. The true meaning of this parameter is whether or not the specified condition did hold before trying to change the state.
    /// </remarks>
    private int SetAsyncState(int newAsyncState, int conditionBitMask, out bool conditionFailed)
    {
        CheckUniqueAsyncState(newAsyncState & STATEMASK_SELECT_ANY_ASYNC_STATE);
        CheckUniqueAsyncState(_state & STATEMASK_SELECT_ANY_ASYNC_STATE);

        int resultState = SetState(newAsyncState, STATEMASK_CLEAR_ALL_ASYNC_STATES, conditionBitMask, out conditionFailed);

        CheckUniqueAsyncState(resultState & STATEMASK_SELECT_ANY_ASYNC_STATE);

        return resultState;
    }

    /// <summary>
    /// Sets the specified bits in the <see cref="_state"/> bit field according to the specified bit-mask parameters.
    /// </summary>
    /// <param name="newStateSetMask">The bits to set in the <see cref="_state"/> bit field</param>
    /// <param name="newStateIgnoreMask">Any bits that are unset in this value will get unset, unless explicitly set by <paramref name="newStateSetMask"/>.</param>
    /// <returns>The value at which the current invocation of this method left <see cref="_state"/>.</returns>
    private int SetState(int newStateSetMask, int newStateIgnoreMask)
    {
        int originalState = _state;
        int newState = (originalState & newStateIgnoreMask) | newStateSetMask;
        int previousState = Interlocked.CompareExchange(ref _state, newState, originalState);

        // If '_state' changed concurrently, we want to make sure that the change being made is
        // based on a bitmask that is up to date. This relies of the fact that all state machines
        // that save their state in '_state' have no cycles.
        while (true)
        {
            if (previousState == originalState)
            {
                return newState;
            }

            originalState = _state;
            newState = (originalState & newStateIgnoreMask) | newStateSetMask;
            previousState = Interlocked.CompareExchange(ref _state, newState, originalState);
        }
    }

    /// <summary>
    /// Sets the specified bits in the <see cref="_state"/> bit field according to the specified bit-mask parameters.
    /// </summary>
    /// <param name="newStateSetMask">The bits to set in the <see cref="_state"/> bit field</param>
    /// <param name="newStateIgnoreMask">Any bits that are unset in this value will get unset, unless explicitly set by <paramref name="newStateSetMask"/>.</param>
    /// <param name="conditionBitMask">Unless this value has at least one bit with <see cref="_state"/> in common, this method will not perform any action.</param>
    /// <param name="conditionFailed">Indicates whether the specified <paramref name="conditionBitMask"/> had at least one bit in common with <see cref="_state"/>.</param>
    /// <returns>The value at which the current invocation of this method left <see cref="_state"/>.</returns>
    /// <remarks>
    /// Note that the meaning of the <paramref name="conditionFailed"/> parameter to the caller is not quite the same as whether <see cref="_state"/>
    /// is/was set to the specified value, because <see cref="_state"/> may already have had the specified value, or it may be set and then immediately
    /// changed by another thread. The true meaning of this parameter is whether or not the specified condition did hold before trying to change the state.
    /// </remarks>
    private int SetState(int newStateSetMask, int newStateIgnoreMask, int conditionBitMask, out bool conditionFailed)
    {
        int origState = _state;

        if ((origState & conditionBitMask) == 0)
        {
            conditionFailed = true;

            return origState;
        }

        int newState = (origState & newStateIgnoreMask) | newStateSetMask;
        int prevState = Interlocked.CompareExchange(ref _state, newState, origState);

        // Same loop as above, plus the additional check for the condition
        while (true)
        {
            if (prevState == origState)
            {
                conditionFailed = false;

                return newState;
            }

            origState = _state;

            if ((origState & conditionBitMask) == 0)
            {
                conditionFailed = true;

                return origState;
            }

            newState = (origState & newStateIgnoreMask) | newStateSetMask;
            prevState = Interlocked.CompareExchange(ref _state, newState, origState);
        }
    }

    /// <summary>
    /// Creates an <see cref="InvalidOperationException"/> to indicate that results cannot be obtained.
    /// </summary>
    /// <param name="innerException">The inner exception, if available.</param>
    /// <returns>The resulting <see cref="InvalidOperationException"/> instance.</returns>
    private static InvalidOperationException CreateCannotGetResultsFromIncompleteOperationException(Exception? innerException)
    {
        InvalidOperationException exception = innerException is null
            ? new InvalidOperationException(SR.InvalidOperation_CannotGetResultsFromIncompleteOperation)
            : new InvalidOperationException(SR.InvalidOperation_CannotGetResultsFromIncompleteOperation, innerException);

        exception.HResult = WellKnownErrorCodes.E_ILLEGAL_METHOD_CALL;

        return exception;
    }
}