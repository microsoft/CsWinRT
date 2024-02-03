// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;

namespace WinRT.Interop
{
    public unsafe abstract class EventSource<TDelegate>
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
        private System.WeakReference<object> _state;

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

        protected IObjectReference ObjectReference => _objectReference;

        protected int Index => _index;

        protected abstract ObjectReferenceValue CreateMarshaler(TDelegate del);

        protected abstract EventSourceState<TDelegate> CreateEventSourceState();

        public void Subscribe(TDelegate del)
        {
            lock (this)
            {
                EventSourceState<TDelegate> state = null;
                bool registerHandler =
                    _state is null ||
                    !TryGetStateUnsafe(out state) ||
                    // We have a wrapper delegate, but no longer has any references from any event source.
                    !state.HasComReferences();
                if (registerHandler)
                {
                    state = CreateEventSourceState();
                    _state = state.GetWeakReferenceForCache();
                    EventSourceCache.Create(_objectReference, _index, _state);
                }

                state.targetDelegate = (TDelegate)Delegate.Combine(state.targetDelegate, del);
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

        public void Unsubscribe(TDelegate del)
        {
            if (_state is null || !TryGetStateUnsafe(out var state))
            {
                return;
            }

            lock (this)
            {
                var oldEvent = state.targetDelegate;
                state.targetDelegate = (TDelegate)Delegate.Remove(state.targetDelegate, del);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGetStateUnsafe(out EventSourceState<TDelegate> state)
        {
            if (_state.TryGetTarget(out object stateObj))
            {
                state = Unsafe.As<EventSourceState<TDelegate>>(stateObj);

                return true;
            }

            state = null;

            return false;
        }
    }
}
