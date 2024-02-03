// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WinRT.Interop
{
    public unsafe sealed class EventHandlerEventSource<T> : EventSource<EventHandler<T>>
    {
        public EventHandlerEventSource(
            IObjectReference objectReference,
#if NET
            delegate* unmanaged[Stdcall]<IntPtr, IntPtr, EventRegistrationToken*, int> addHandler,
#else
            delegate* unmanaged[Stdcall]<IntPtr, IntPtr, out EventRegistrationToken, int> addHandler,
#endif
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
