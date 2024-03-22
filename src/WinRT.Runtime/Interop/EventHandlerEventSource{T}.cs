﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using WinRT;
using WinRT.Interop;

namespace ABI.WinRT.Interop
{
    using EventRegistrationToken = global::WinRT.EventRegistrationToken;

    /// <summary>
    /// An <see cref="EventSource{TDelegate}"/> implementation for <see cref="EventHandler{TEventArgs}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the event data generated by the event.</typeparam>
    /// <remarks>This type is only meant to be used by generated projections.</remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
#if EMBED
    internal
#else
    public
#endif
    sealed unsafe class EventHandlerEventSource<T> : EventSource<EventHandler<T>>
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

        /// <inheritdoc/>
        protected override ObjectReferenceValue CreateMarshaler(EventHandler<T> del) =>
            ABI.System.EventHandler<T>.CreateMarshaler2(del);

        /// <inheritdoc/>
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