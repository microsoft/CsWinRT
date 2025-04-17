// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using ABI.System.ComponentModel;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// An <see cref="EventSource{T}"/> implementation for <see cref="PropertyChangedEventHandler"/>.
/// </summary>
internal sealed unsafe class PropertyChangedEventSource : EventSource<PropertyChangedEventHandler>
{
    /// <inheritdoc cref="EventSource{T}.EventSource"/>
    public PropertyChangedEventSource(WindowsRuntimeObjectReference nativeObjectReference, int index)
        : base(nativeObjectReference, index)
    {
    }

    /// <inheritdoc/>
    protected override WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(PropertyChangedEventHandler handler)
    {
        return PropertyChangedEventHandlerMarshaller.ConvertToUnmanaged(handler);
    }

    /// <inheritdoc/>
    protected override EventSourceState<PropertyChangedEventHandler> CreateEventSourceState()
    {
        return new EventState(GetNativeObjectReferenceThisPtrUnsafe(), Index);
    }

    /// <summary>
    /// The <see cref="EventSourceState{T}"/> implementation for <see cref="PropertyChangedEventSource"/>.
    /// </summary>
    private sealed class EventState : EventSourceState<PropertyChangedEventHandler>
    {
        /// <inheritdoc cref="EventSourceState{T}.EventSourceState"/>
        public EventState(void* thisPtr, int index)
            : base(thisPtr, index)
        {
        }

        /// <inheritdoc/>
        protected override PropertyChangedEventHandler GetEventInvoke()
        {
            return (obj, e) => TargetDelegate?.Invoke(obj, e);
        }
    }
}
