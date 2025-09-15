// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Windows.Foundation.Collections;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// An <see cref="EventSource{T}"/> implementation for <see cref="VectorChangedEventHandler{T}"/>.
/// </summary>
/// <typeparam name="T">The type of elements in the observable vector.</typeparam>
public abstract unsafe class VectorChangedEventHandlerEventSource<T> : EventSource<VectorChangedEventHandler<T>>
{
    /// <inheritdoc cref="EventSource{T}.EventSource"/>
    protected VectorChangedEventHandlerEventSource(WindowsRuntimeObjectReference nativeObjectReference, int index)
        : base(nativeObjectReference, index)
    {
    }

    /// <inheritdoc/>
    protected sealed override EventSourceState<VectorChangedEventHandler<T>> CreateEventSourceState()
    {
        return new EventState(GetNativeObjectReferenceThisPtrUnsafe(), Index);
    }

    /// <summary>
    /// The <see cref="EventSourceState{T}"/> implementation for <see cref="VectorChangedEventHandlerEventSource{T}"/>.
    /// </summary>
    private sealed class EventState : EventSourceState<VectorChangedEventHandler<T>>
    {
        /// <inheritdoc cref="EventSourceState{T}.EventSourceState"/>
        public EventState(void* thisPtr, int index)
            : base(thisPtr, index)
        {
        }

        /// <inheritdoc/>
        protected override VectorChangedEventHandler<T> GetEventInvoke()
        {
            return (obj, e) => TargetDelegate?.Invoke(obj, e);
        }
    }
}
