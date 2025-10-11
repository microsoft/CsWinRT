// Copyright (c) Microsoft Corporation.
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
        private readonly long _vtableIndexForAddHandler;
#if NET
        private readonly delegate* unmanaged[Stdcall]<IntPtr, IntPtr, EventRegistrationToken*, int> _addHandler;
#else
        private readonly delegate* unmanaged[Stdcall]<IntPtr, IntPtr, out EventRegistrationToken, int> _addHandler;
#endif
        private readonly delegate* unmanaged[Stdcall]<IntPtr, EventRegistrationToken, int> _removeHandler;
        private global::System.WeakReference<object>? _state;

        // The add / remove handlers given to us can be for an object which is not agile meaning we may have
        // a proxy which we need to call through.  Due to this, we store the offset of the add handler
        // we are given rather than directly caching it. Then we use that offset, to determine the add and
        // remove handler to call based on the pointer from the current context.
#if NET
        private delegate* unmanaged[Stdcall]<IntPtr, IntPtr, EventRegistrationToken*, int> AddHandler
#else
        private delegate* unmanaged[Stdcall]<IntPtr, IntPtr, out EventRegistrationToken, int> AddHandler
#endif
        {
            get
            {
                if (_addHandler is not null)
                {
                    return _addHandler;
                }

                var thisPtr = _objectReference.ThisPtr;
#if NET
                return (*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr, global::WinRT.EventRegistrationToken*, int>**)thisPtr)[_vtableIndexForAddHandler];
#else
                return (*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr, out EventRegistrationToken, int>**)thisPtr)[_vtableIndexForAddHandler];
#endif
            }
        }

        private delegate* unmanaged[Stdcall]<IntPtr, EventRegistrationToken, int> RemoveHandler
        {
            get
            {
                if (_removeHandler is not null)
                {
                    return _removeHandler;
                }

                var thisPtr = _objectReference.ThisPtr;
                // Add 1 to the offset to get remove handler from add handler offset.
                return (*(delegate* unmanaged[Stdcall]<IntPtr, EventRegistrationToken, int>**)thisPtr)[_vtableIndexForAddHandler  + 1];
            }
        }

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
            _index = index;
            _state = EventSourceCache.GetState(objectReference, index);

            // If this isn't a free threaded object, we can't cache the handlers due to we
            // might be accessing it from a different context which would have its own add handler address
            // for the proxy. So caching it would end up calling the wrong vtable.
            // We instead use it to calculate the vtable offset for the add handler to use later.
            if (objectReference is IObjectReferenceWithContext)
            {
                int vtableIndexForAddHandler = 0;
                while ((*(void***)objectReference.ThisPtr)[vtableIndexForAddHandler] != addHandler)
                {
                    vtableIndexForAddHandler++;
                }
                _vtableIndexForAddHandler = vtableIndexForAddHandler;
            }
            else
            {
                _addHandler = addHandler;
                _removeHandler = removeHandler;
            }
        }

        /// <summary>
        /// Creates a new <see cref="EventSource{TDelegate}"/> instance with the specified parameters.
        /// </summary>
        /// <param name="objectReference">The <see cref="IObjectReference"/> instance holding the event.</param>
        /// <param name="vtableIndexForAddHandler">The vtable index for the add handler of the event being managed.</param>
        protected EventSource(
            IObjectReference objectReference,
            int vtableIndexForAddHandler)
        {
            _objectReference = objectReference;
            _index = vtableIndexForAddHandler;
            _state = EventSourceCache.GetState(objectReference, vtableIndexForAddHandler);
            _vtableIndexForAddHandler = vtableIndexForAddHandler;
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
                        state.InitializeReferenceTracking(nativeDelegate);

                        EventRegistrationToken token;
#if NET
                        ExceptionHelpers.ThrowExceptionForHR(AddHandler(_objectReference.ThisPtr, nativeDelegate, &token));
#else
                        ExceptionHelpers.ThrowExceptionForHR(AddHandler(_objectReference.ThisPtr, nativeDelegate, out token));
#endif
                        global::System.GC.KeepAlive(_objectReference);
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
            ExceptionHelpers.ThrowExceptionForHR(RemoveHandler(_objectReference.ThisPtr, state.token));
            global::System.GC.KeepAlive(_objectReference);
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

// Restore in case this file is merged with others.
#nullable restore