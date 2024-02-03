// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WinRT.Interop
{
    public sealed unsafe class EventHandlerEventSource : EventSource<EventHandler>
    {
        public EventHandlerEventSource(
            IObjectReference objectReference,
#if NET
            delegate* unmanaged[Stdcall]<IntPtr, IntPtr, EventRegistrationToken*, int> addHandler,
#else
            delegate* unmanaged[Stdcall]<IntPtr, IntPtr, out EventRegistrationToken, int> addHandler,
#endif
            delegate* unmanaged[Stdcall]<IntPtr, EventRegistrationToken, int> removeHandler)
            : base(objectReference, addHandler, removeHandler)
        {
        }

        protected override ObjectReferenceValue CreateMarshaler(EventHandler del) =>
            ABI.System.EventHandler.CreateMarshaler2(del);

        protected override EventSourceState<EventHandler> CreateEventSourceState() =>
            new EventState(ObjectReference.ThisPtr, Index);

        private sealed class EventState : EventSourceState<EventHandler>
        {
            public EventState(IntPtr obj, int index)
                : base(obj, index)
            {
            }

            protected override EventHandler GetEventInvoke()
            {
                return (obj, e) => targetDelegate?.Invoke(obj, e);
            }
        }
    }
}
