﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using WinRT;

#nullable enable

namespace ABI.WinRT.Interop
{
    using EventRegistrationToken = global::WinRT.EventRegistrationToken;

    /// <summary>
    /// A managed wrapper for an event to expose to a native WinRT consumer.
    /// </summary>
    /// <typeparam name="TDelegate">The type of delegate being managed.</typeparam>
    /// <remarks>This type is only meant to be used by generated projections.</remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    #if EMBED
    internal
#else
    public
#endif
    abstract unsafe class EventSource<TDelegate>
        where TDelegate : class, MulticastDelegate
    {
        private readonly IObjectReference _objectReference;
        private readonly int _index;
#if NET
        private readonly delegate* unmanaged[Stdcall]<IntPtr, IntPtr, EventRegistrationToken*, int> _addHandler;
#else
        private readonly delegate* unmanaged[Stdcall]<IntPtr, IntPtr, out EventRegistrationToken, int> _addHandler;
#endif
        private readonly delegate* unmanaged[Stdcall]<IntPtr, EventRegistrationToken, int> _removeHandler;
        private global::System.WeakReference<object>? _state;

        /// <summary>
        /// Creates a new <see cref="EventSource{TDelegate}"/> instance with the specified parameters.
        /// </summary>
        /// <param name="objectReference">The <see cref="IObjectReference"/> instance holding the event.</param>
        /// <param name="addHandler">The native function pointer for the <c>AddHandler</c> method on the target object.</param>
        /// <param name="removeHandler">The native function pointer for the <c>RemoveHandler</c> method on the target object.</param>
        /// <param name="index">The index of the event being managed.</param>
        protected EventSource(
            IObjectReference objectReference,
#if NET
            delegate* unmanaged[Stdcall]<IntPtr, IntPtr, EventRegistrationToken*, int> addHandler,
#else
            delegate* unmanaged[Stdcall]<IntPtr, IntPtr, out EventRegistrationToken, int> addHandler,
#endif
            delegate* unmanaged[Stdcall]<IntPtr, EventRegistrationToken, int> removeHandler,
            int index = 0)
        {
            _objectReference = objectReference;
            _addHandler = addHandler;
            _removeHandler = removeHandler;
            _index = index;
            _state = EventSourceCache.GetState(objectReference, index);
        }

        /// <summary>
        /// Gets the <see cref="IObjectReference"/> instance holding the event.
        /// </summary>
        protected IObjectReference ObjectReference => _objectReference;

        /// <summary>
        /// Gets the index of the event being managed.
        /// </summary>
        protected int Index => _index;

        /// <summary>
        /// Gets an <see cref="ObjectReferenceValue"/> instance to marshal a <typeparamref name="TDelegate"/> instance.
        /// </summary>
        /// <param name="handler">The input <typeparamref name="TDelegate"/> handler to create the marshaller for.</param>
        /// <returns>An <see cref="ObjectReferenceValue"/> instance to marshal a <typeparamref name="TDelegate"/> instance.</returns>
        protected abstract ObjectReferenceValue CreateMarshaler(TDelegate handler);

        /// <summary>
        /// Creates the <see cref="EventSourceState{TDelegate}"/> instance for the current event source.
        /// </summary>
        /// <returns>The <see cref="EventSourceState{TDelegate}"/> instance for the current event source.</returns>
        protected abstract EventSourceState<TDelegate> CreateEventSourceState();

        /// <summary>
        /// Subscribes a given handler to the target event.
        /// </summary>
        /// <param name="handler">The handler to subscribe to the target event.</param>
        public void Subscribe(TDelegate handler)
        {
            lock (this)
            {
                EventSourceState<TDelegate>? state = null;
                bool registerHandler =
                    !TryGetStateUnsafe(out state) ||
                    // We have a wrapper delegate, but no longer has any references from any event source.
                    !state!.HasComReferences();
                if (registerHandler)
                {
                    state = CreateEventSourceState();
                    _state = state.GetWeakReferenceForCache();
                    EventSourceCache.Create(_objectReference, _index, _state);
                }

                state!.targetDelegate = (TDelegate)Delegate.Combine(state.targetDelegate, handler);
                if (registerHandler)
                {
                    var eventInvoke = state.eventInvoke;
                    var marshaler = CreateMarshaler(eventInvoke);
                    try
                    {
                        var nativeDelegate = marshaler.GetAbi();
                        state.InitalizeReferenceTracking(nativeDelegate);

                        EventRegistrationToken token;
#if NET
                        ExceptionHelpers.ThrowExceptionForHR(_addHandler(_objectReference.ThisPtr, nativeDelegate, &token));
#else
                        ExceptionHelpers.ThrowExceptionForHR(_addHandler(_objectReference.ThisPtr, nativeDelegate, out token));
#endif
                        state.token = token;
                    }
                    finally
                    {
                        // Dispose our managed reference to the delegate's CCW.
                        // Either the native event holds a reference now or the _addHandler call failed.
                        marshaler.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// Removes a given handler from the target event.
        /// </summary>
        /// <param name="handler">The handler to remove from the target event.</param>
        public void Unsubscribe(TDelegate handler)
        {
            if (_state is null || !TryGetStateUnsafe(out var state))
            {
                return;
            }

            lock (this)
            {
                var oldEvent = state!.targetDelegate;
                state.targetDelegate = (TDelegate?)Delegate.Remove(state.targetDelegate, handler);
                if (oldEvent is object && state.targetDelegate is null)
                {
                    UnsubscribeFromNative(state);
                }
            }
        }

        private void UnsubscribeFromNative(EventSourceState<TDelegate> state)
        {
            ExceptionHelpers.ThrowExceptionForHR(_removeHandler(_objectReference.ThisPtr, state.token));
            state.Dispose();
            _state = null;
        }

#if NET
        [MemberNotNullWhen(true, nameof(_state))]
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGetStateUnsafe(
#if NET
            [NotNullWhen(true)]
#endif
            out EventSourceState<TDelegate>? state)
        {
            if (_state is not null && _state.TryGetTarget(out object? stateObj))
            {
                state = Unsafe.As<EventSourceState<TDelegate>>(stateObj);

                return true;
            }

            state = null;

            return false;
        }
    }
}
