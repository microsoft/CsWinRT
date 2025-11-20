// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Specialized;
using System.ComponentModel;
using WindowsRuntime;
using WindowsRuntime.InteropServices;

namespace ABI.System.Collections.Specialized;

/// <summary>
/// An <see cref="EventSource{T}"/> implementation for <see cref="NotifyCollectionChangedEventHandler"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
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
        return ABI.System.Collections.Specialized.NotifyCollectionChangedEventHandlerMarshaller.ConvertToUnmanaged(value);
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