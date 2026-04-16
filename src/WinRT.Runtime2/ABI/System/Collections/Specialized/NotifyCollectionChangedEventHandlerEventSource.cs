// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Specialized;
using WindowsRuntime.InteropServices;

namespace ABI.System.Collections.Specialized;

/// <summary>
/// An <see cref="EventSource{T}"/> implementation for <see cref="NotifyCollectionChangedEventHandler"/>.
/// </summary>
public sealed unsafe class NotifyCollectionChangedEventHandlerEventSource : EventSource<NotifyCollectionChangedEventHandler>
{
    /// <inheritdoc cref="EventSource{T}.EventSource"/>
    public NotifyCollectionChangedEventHandlerEventSource(WindowsRuntimeObjectReference nativeObjectReference, int index)
        : base(nativeObjectReference, index)
    {
    }

    /// <inheritdoc/>
    protected override WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(NotifyCollectionChangedEventHandler value)
    {
        return NotifyCollectionChangedEventHandlerMarshaller.ConvertToUnmanaged(value);
    }

    /// <inheritdoc/>
    protected override EventSourceState<NotifyCollectionChangedEventHandler> CreateEventSourceState()
    {
        return new EventState(GetNativeObjectReferenceThisPtrUnsafe(), Index);
    }

    /// <summary>
    /// The <see cref="EventSourceState{T}"/> implementation for <see cref="NotifyCollectionChangedEventHandler"/>.
    /// </summary>
    private sealed class EventState : EventSourceState<NotifyCollectionChangedEventHandler>
    {
        /// <inheritdoc cref="EventSourceState{T}.EventSourceState"/>
        public EventState(void* thisPtr, int index)
            : base(thisPtr, index)
        {
        }

        /// <inheritdoc/>
        protected override NotifyCollectionChangedEventHandler GetEventInvoke()
        {
            return (obj, e) => TargetDelegate?.Invoke(obj, e);
        }
    }
}