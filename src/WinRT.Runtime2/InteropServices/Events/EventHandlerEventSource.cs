// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using ABI.System;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// An <see cref="EventSource{T}"/> implementation for <see cref="EventHandler"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed unsafe class EventHandlerEventSource : EventSource<EventHandler>
{
    /// <inheritdoc cref="EventSource{T}.EventSource"/>
    public EventHandlerEventSource(WindowsRuntimeObjectReference nativeObjectReference, int index)
        : base(nativeObjectReference, index)
    {
    }

    /// <inheritdoc/>
    protected override WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(EventHandler value)
    {
        return EventHandlerMarshaller.ConvertToUnmanaged(value);
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
