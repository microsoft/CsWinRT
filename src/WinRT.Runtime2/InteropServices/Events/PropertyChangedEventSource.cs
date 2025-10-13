// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using ABI.System.ComponentModel;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// An <see cref="EventSource{T}"/> implementation for <see cref="PropertyChangedEventHandler"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage, DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId)]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed unsafe class PropertyChangedEventSource : EventSource<PropertyChangedEventHandler>
{
    /// <inheritdoc cref="EventSource{T}.EventSource"/>
    public PropertyChangedEventSource(WindowsRuntimeObjectReference nativeObjectReference, int index)
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
