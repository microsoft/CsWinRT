// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using WinRT;
using WinRT.Interop;

namespace ABI.WinRT.Interop
{
    using EventRegistrationToken = global::WinRT.EventRegistrationToken;

    /// <summary>
    /// An <see cref="EventSource{TDelegate}"/> implementation for <see cref="EventHandler"/>.
    /// </summary>
    /// <remarks>This type is only meant to be used by generated projections.</remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
#if EMBED
    internal
#else
    public
#endif  
    sealed unsafe class EventHandlerEventSource : EventSource<EventHandler>
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

        public EventHandlerEventSource(
            IObjectReference objectReference,
            int vtableIndexForAddHandler)
            : base(objectReference, vtableIndexForAddHandler)
        {
        }

        /// <inheritdoc/>
        protected override ObjectReferenceValue CreateMarshaler(EventHandler del) =>
            ABI.System.EventHandler.CreateMarshaler2(del);

        /// <inheritdoc/>
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
