// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Windows.Foundation.Collections;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// An <see cref="EventSource{T}"/> implementation for <see cref="MapChangedEventHandler{K, V}"/>.
/// </summary>
/// <typeparam name="K">The type of keys in the observable map.</typeparam>
/// <typeparam name="V">The type of values in the observable map.</typeparam>
public abstract unsafe class MapChangedEventHandlerEventSource<K, V> : EventSource<MapChangedEventHandler<K, V>>
{
    /// <inheritdoc cref="EventSource{T}.EventSource"/>
    protected MapChangedEventHandlerEventSource(WindowsRuntimeObjectReference nativeObjectReference, int index)
        : base(nativeObjectReference, index)
    {
    }

    /// <inheritdoc/>
    protected sealed override EventSourceState<MapChangedEventHandler<K, V>> CreateEventSourceState()
    {
        return new EventState(GetNativeObjectReferenceThisPtrUnsafe(), Index);
    }

    /// <summary>
    /// The <see cref="EventSourceState{T}"/> implementation for <see cref="MapChangedEventHandlerEventSource{K, V}"/>.
    /// </summary>
    private sealed class EventState : EventSourceState<MapChangedEventHandler<K, V>>
    {
        /// <inheritdoc cref="EventSourceState{T}.EventSourceState"/>
        public EventState(void* thisPtr, int index)
            : base(thisPtr, index)
        {
        }

        /// <inheritdoc/>
        protected override MapChangedEventHandler<K, V> GetEventInvoke()
        {
            return (obj, e) => TargetDelegate?.Invoke(obj, e);
        }
    }
}
