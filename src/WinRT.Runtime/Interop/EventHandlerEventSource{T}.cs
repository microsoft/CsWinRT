﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WinRT.Interop
{   
    internal unsafe sealed class EventHandlerEventSource<T> : EventSource<EventHandler<T>>
    {
        internal EventHandlerEventSource(
            IObjectReference objectReference,
            delegate* unmanaged[Stdcall]<IntPtr, IntPtr, EventRegistrationToken*, int> addHandler,
            delegate* unmanaged[Stdcall]<IntPtr, EventRegistrationToken, int> removeHandler,
            int index) : base(objectReference, addHandler, removeHandler, index)
        {
        }

        protected override ObjectReferenceValue CreateMarshaler(EventHandler<T> del) =>
            ABI.System.EventHandler<T>.CreateMarshaler2(del);

        protected override EventSourceState<EventHandler<T>> CreateEventSourceState() =>
            new EventState(ObjectReference.ThisPtr, Index);

        private sealed class EventState : EventSourceState<EventHandler<T>>
        {
            public EventState(IntPtr obj, int index)
                : base(obj, index)
            {
            }

            protected override EventHandler<T> GetEventInvoke()
            {
                return (obj, e) => targetDelegate?.Invoke(obj, e);
            }
        }
    }
}
