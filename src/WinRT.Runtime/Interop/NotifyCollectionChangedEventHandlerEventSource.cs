// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using ABI.System.Collections.Specialized;

namespace WinRT.Interop
{
    internal sealed unsafe class NotifyCollectionChangedEventHandlerEventSource : EventSource<System.Collections.Specialized.NotifyCollectionChangedEventHandler>
    {
        internal NotifyCollectionChangedEventHandlerEventSource(
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

        protected override ObjectReferenceValue CreateMarshaler(System.Collections.Specialized.NotifyCollectionChangedEventHandler del) =>
            NotifyCollectionChangedEventHandler.CreateMarshaler2(del);

        protected override EventSourceState<System.Collections.Specialized.NotifyCollectionChangedEventHandler> CreateEventSourceState() =>
            new EventState(ObjectReference.ThisPtr, Index);

        private sealed class EventState : EventSourceState<System.Collections.Specialized.NotifyCollectionChangedEventHandler>
        {
            public EventState(IntPtr obj, int index)
                : base(obj, index)
            {
            }

            protected override System.Collections.Specialized.NotifyCollectionChangedEventHandler GetEventInvoke()
            {
                return (obj, e) => targetDelegate?.Invoke(obj, e);
            }
        }
    }
}
