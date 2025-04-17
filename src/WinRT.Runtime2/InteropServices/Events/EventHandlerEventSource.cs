// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using ABI.System;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// An <see cref="EventSource{T}"/> implementation for <see cref="EventHandler"/>.
/// </summary>
public sealed unsafe class EventHandlerEventSource : EventSource<EventHandler>
{
    /// <inheritdoc cref="EventSource{T}.EventSource"/>
    public EventHandlerEventSource(
        WindowsRuntimeObjectReference nativeObjectReference,
        delegate* unmanaged[MemberFunction]<void*, void*, EventRegistrationToken*, int> addHandler,
        delegate* unmanaged[MemberFunction]<void*, EventRegistrationToken, int> removeHandler,
        int index = 0)
        : base(nativeObjectReference, addHandler, removeHandler, index)
    {
    }

    /// <inheritdoc/>
    protected override WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(EventHandler handler)
    {
        return EventHandlerMarshaller.ConvertToUnmanaged(handler);
    }

    /// <inheritdoc/>
    protected override EventSourceState<EventHandler> CreateEventSourceState()
    {
        return new EventState(GetNativeObjectReferenceThisPtrUnsafe(), Index);
    }

    /// <summary>
    /// The <see cref="EventSourceState{T}"/> implementation for <see cref="EventHandlerEventSource"/>.
    /// </summary>
    private sealed class EventState : EventSourceState<EventHandler>
    {
        /// <inheritdoc cref="EventSourceState{T}.EventSourceState"/>
        public EventState(void* thisPtr, int index)
            : base(thisPtr, index)
        {
        }

        /// <inheritdoc/>
        protected override EventHandler GetEventInvoke()
        {
            return (obj, e) => TargetDelegate?.Invoke(obj, e);
        }
    }
}
