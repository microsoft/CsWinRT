﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// An <see cref="EventSource{T}"/> implementation for <see cref="EventHandler{T}"/>.
/// </summary>
/// <typeparam name="T">The type of the event data generated by the event.</typeparam>
public abstract unsafe class EventHandlerEventSource<T> : EventSource<EventHandler<T>>
{
    /// <inheritdoc cref="EventSource{T}.EventSource"/>
    protected EventHandlerEventSource(WindowsRuntimeObjectReference nativeObjectReference, int index)
        : base(nativeObjectReference, index)
    {
    }

    /// <inheritdoc/>
    protected sealed override EventSourceState<EventHandler<T>> CreateEventSourceState()
    {
        return new EventState(GetNativeObjectReferenceThisPtrUnsafe(), Index);
    }

    /// <summary>
    /// The <see cref="EventSourceState{T}"/> implementation for <see cref="EventHandlerEventSource{T}"/>.
    /// </summary>
    private sealed class EventState : EventSourceState<EventHandler<T>>
    {
        /// <inheritdoc cref="EventSourceState{T}.EventSourceState"/>
        public EventState(void* thisPtr, int index)
            : base(thisPtr, index)
        {
        }

        /// <inheritdoc/>
        protected override EventHandler<T> GetEventInvoke()
        {
            return (obj, e) => TargetDelegate?.Invoke(obj, e);
        }
    }
}
