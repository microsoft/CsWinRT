// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A managed wrapper for an event to expose to a native Windows Runtime consumer.
/// </summary>
/// <typeparam name="T">The type of delegate being managed.</typeparam>
public abstract unsafe class EventSource<T>
    where T : MulticastDelegate
{
    /// <summary>
    /// The <see cref="WindowsRuntimeObjectReference"/> instance holding the event.
    /// </summary>
    private readonly WindowsRuntimeObjectReference _nativeObjectReference;

    /// <summary>
    /// The function pointer to add a new event handler to the target native object (ie. the <c>add_EventName</c> method in the interface vtable).
    /// </summary>
    private readonly delegate* unmanaged[MemberFunction]<void*, void*, EventRegistrationToken*, HRESULT> _addHandler;

    /// <summary>
    /// The function pointer to remove a new event handler to the target native object (ie. the <c>remove_EventName</c> method in the interface vtable).
    /// </summary>
    private readonly delegate* unmanaged[MemberFunction]<void*, EventRegistrationToken, HRESULT> _removeHandler;

    /// <summary>
    /// The weak reference to the event source state, for the current event source.
    /// </summary>
    private WeakReference<object>? _weakReferenceToEventSourceState;

    /// <summary>
    /// Creates a new <see cref="EventSource{T}"/> instance with the specified parameters.
    /// </summary>
    /// <param name="nativeObjectReference">The <see cref="WindowsRuntimeObjectReference"/> instance holding the event.</param>
    /// <param name="addHandler">The native function pointer for the <c>AddHandler</c> method on the target object.</param>
    /// <param name="removeHandler">The native function pointer for the <c>RemoveHandler</c> method on the target object.</param>
    /// <param name="index">The index of the event being managed.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="nativeObjectReference"/>, <paramref name="addHandler"/>, or <paramref name="removeHandler"/> are <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is less than zero.</exception>
    protected EventSource(
        WindowsRuntimeObjectReference nativeObjectReference,
        delegate* unmanaged[MemberFunction]<void*, void*, EventRegistrationToken*, HRESULT> addHandler,
        delegate* unmanaged[MemberFunction]<void*, EventRegistrationToken, HRESULT> removeHandler,
        int index = 0)
    {
        ArgumentNullException.ThrowIfNull(nativeObjectReference);
        ArgumentNullException.ThrowIfNull(addHandler);
        ArgumentNullException.ThrowIfNull(removeHandler);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        _nativeObjectReference = nativeObjectReference;
        _addHandler = addHandler;
        _removeHandler = removeHandler;
        Index = index;
        _weakReferenceToEventSourceState = EventSourceCache.GetState(nativeObjectReference, index);
    }

    /// <summary>
    /// Gets the index of the event being managed.
    /// </summary>
    protected int Index { get; }

    /// <summary>
    /// Subscribes a given handler to the target event.
    /// </summary>
    /// <param name="handler">The handler to subscribe to the target event.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="handler"/> is <see langword="null"/>.</exception>
    public void Subscribe(T handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        lock (this)
        {
            // We should register the event if we don't have a state object yet,
            // or if we have one, but there's no COM references left to it. This
            // means we have a wrapper delegate, but no event source references.
            bool registerHandler =
                !TryGetStateUnsafe(out EventSourceState<T>? state) ||
                !state.HasComReferences();

            // If we should register the handler, create a new state and insert it into the cache
            if (registerHandler)
            {
                state = CreateEventSourceState();

                _weakReferenceToEventSourceState = state.GetWeakReferenceToSelf();

                // This will either create a new cache entry for the native object, or update the cache entry for
                // this event. This will also cover the scenario where we still had a state with no COM references.
                EventSourceCache.Create(_nativeObjectReference, Index, _weakReferenceToEventSourceState);
            }

            // Add the new handler to the target delegate, which is invoked by the marshalled CCW.
            // That CCW will point the event invoker, ie. a stub on the event source state object.
            // This stub captures the event source state, and just invokes the target delegate.
            // If we don't need to register the handler, we still just add the new handler here.
            // THe existing CCW will just pick it up the next time the native event is invoked.
            state!.AddHandler(handler);

            if (registerHandler)
            {
                using WindowsRuntimeObjectReferenceValue nativeObjectReferenceValue = _nativeObjectReference.AsValue();
                using WindowsRuntimeObjectReferenceValue eventInvokeValue = ConvertToUnmanaged(state.EventInvoke);

                // Ensure the reference tracking is initialized on this new CCW
                state.InitalizeReferenceTracking(eventInvokeValue.GetThisPtrUnsafe());

                EventRegistrationToken token;

                // Actually register the marshalled event invoke on the native object
                HRESULT hresult = _addHandler(nativeObjectReferenceValue.GetThisPtrUnsafe(), eventInvokeValue.GetThisPtrUnsafe(), &token);

                RestrictedErrorInfo.ThrowExceptionForHR(hresult);

                state.Token = token;
            }
        }
    }

    /// <summary>
    /// Removes a given handler from the target event.
    /// </summary>
    /// <param name="handler">The handler to remove from the target event.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="handler"/> is <see langword="null"/>.</exception>
    public void Unsubscribe(T handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        // If the event source state has been collected, there's nothing to do
        if (_weakReferenceToEventSourceState is null || !TryGetStateUnsafe(out EventSourceState<T>? state))
        {
            return;
        }

        lock (this)
        {
            bool hasAnyTargetDelegatesBeforeRemoval = state.TargetDelegate is not null;

            state.RemoveHandler(handler);

            bool hasAnyTargetDelegatesAfterRemoval = state.TargetDelegate is not null;

            // If this was the last remaining target delegate, we can unsubscribe from the native event
            if (hasAnyTargetDelegatesBeforeRemoval && !hasAnyTargetDelegatesAfterRemoval)
            {
                using WindowsRuntimeObjectReferenceValue nativeObjectReferenceValue = _nativeObjectReference.AsValue();

                // Pass the token we got from 'Subscribe' to remove the native event subscription
                HRESULT hresult = _removeHandler(nativeObjectReferenceValue.GetThisPtrUnsafe(), state.Token);

                RestrictedErrorInfo.ThrowExceptionForHR(hresult);

                // We've unsubscribed from the native object, so we can also manually dispose the
                // event source state we were using. This will help reduce GC finalizer pressure.
                state.Dispose();

                // Clear the weak reference, since the target is also gone anyway
                _weakReferenceToEventSourceState = null;
            }
        }
    }

    /// <summary>
    /// Marshals a given <typeparamref name="T"/> delegate instance to a <see cref="WindowsRuntimeObjectReferenceValue"/> value.
    /// </summary>
    /// <param name="handler">The input <typeparamref name="T"/> handler to marshal.</param>
    /// <returns>The resulting marshalled object for <paramref name="handler"/>.</returns>
    /// <seealso cref="Marshalling.WindowsRuntimeDelegateMarshaller.ConvertToUnmanaged"/>
    protected abstract WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(T handler);

    /// <summary>
    /// Creates the <see cref="EventSourceState{T}"/> instance for the current event source.
    /// </summary>
    /// <returns>The <see cref="EventSourceState{T}"/> instance for the current event source.</returns>
    protected abstract EventSourceState<T> CreateEventSourceState();

    /// <summary>
    /// Gets the native pointer for the underlying native object, without adding a reference count.
    /// </summary>
    /// <returns>The native pointer for the underlying native object.</returns>
    /// <remarks>
    /// This method should only be used to produce a key to use in <see cref="EventSourceState{T}"/> objects.
    /// The returned pointer should never be dereferenced, and it is not safe to use, as its reference count
    /// has not been incremented. The only valid use for it is as argument for <see cref="EventSourceState{T}"/>.
    /// </remarks>
    protected void* GetNativeObjectReferenceThisPtrUnsafe()
    {
        _nativeObjectReference.AddRefUnsafe();

        void* thisPtr = _nativeObjectReference.GetThisPtrUnsafe();

        _nativeObjectReference.ReleaseUnsafe();

        return thisPtr;
    }

    /// <summary>
    /// Tries to get the <see cref="EventSourceState{T}"/> object currently in use, if present.
    /// </summary>
    /// <param name="state">The resulting <see cref="EventSourceState{T}"/> object in use, if any.</param>
    /// <returns>Whether <paramref name="state"/> was successfully retrieved.</returns>
    [MemberNotNullWhen(true, nameof(_weakReferenceToEventSourceState))]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryGetStateUnsafe([NotNullWhen(true)] out EventSourceState<T>? state)
    {
        if (_weakReferenceToEventSourceState is not null && _weakReferenceToEventSourceState.TryGetTarget(out object? stateObj))
        {
            state = Unsafe.As<EventSourceState<T>>(stateObj);

            return true;
        }

        state = null;

        return false;
    }
}
