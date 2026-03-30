// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System.ComponentModel;
using WindowsRuntime.InteropServices;

namespace ABI.System.ComponentModel;

/// <summary>
/// An <see cref="EventSource{T}"/> implementation for <see cref="PropertyChangedEventHandler"/>.
/// </summary>
public sealed unsafe class PropertyChangedEventHandlerEventSource : EventSource<PropertyChangedEventHandler>
{
    /// <inheritdoc cref="EventSource{T}.EventSource"/>
    public PropertyChangedEventHandlerEventSource(WindowsRuntimeObjectReference nativeObjectReference, int index)
        : base(nativeObjectReference, index)
    {
    }

    /// <inheritdoc/>
    protected override WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(PropertyChangedEventHandler value)
    {
        return PropertyChangedEventHandlerMarshaller.ConvertToUnmanaged(value);
    }

    /// <inheritdoc/>
    protected override EventSourceState<PropertyChangedEventHandler> CreateEventSourceState()
    {
        return new EventState(GetNativeObjectReferenceThisPtrUnsafe(), Index);
    }

    /// <summary>
    /// The <see cref="EventSourceState{T}"/> implementation for <see cref="PropertyChangedEventHandlerEventSource"/>.
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
#endif
