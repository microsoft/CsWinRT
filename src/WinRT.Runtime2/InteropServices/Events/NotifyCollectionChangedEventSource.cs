// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Specialized;
using ABI.System.Collections.Specialized;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// An <see cref="EventSource{T}"/> implementation for <see cref="NotifyCollectionChangedEventHandler"/>.
/// </summary>
public sealed unsafe class NotifyCollectionChangedEventSource : EventSource<NotifyCollectionChangedEventHandler>
{
    /// <inheritdoc cref="EventSource{T}.EventSource"/>
    public NotifyCollectionChangedEventSource(WindowsRuntimeObjectReference nativeObjectReference, int index)
        : base(nativeObjectReference, index)
    {
    }

    /// <inheritdoc/>
    protected override WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(NotifyCollectionChangedEventHandler handler)
    {
        return NotifyCollectionChangedEventHandlerMarshaller.ConvertToUnmanaged(handler);
    }

    /// <inheritdoc/>
    protected override EventSourceState<NotifyCollectionChangedEventHandler> CreateEventSourceState()
    {
        return new EventState(GetNativeObjectReferenceThisPtrUnsafe(), Index);
    }

    /// <summary>
    /// The <see cref="EventSourceState{T}"/> implementation for <see cref="PropertyChangedEventSource"/>.
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
