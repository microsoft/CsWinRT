// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using Windows.Foundation.Collections;
using WindowsRuntime;
using WindowsRuntime.InteropServices;

namespace ABI.Windows.Foundation.Collections;

/// <summary>
/// An <see cref="EventSource{T}"/> implementation for <see cref="MapChangedEventHandler{K, V}"/>.
/// </summary>
/// <typeparam name="K">The type of keys in the observable map.</typeparam>
/// <typeparam name="V">The type of values in the observable map.</typeparam>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
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