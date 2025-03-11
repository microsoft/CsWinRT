// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// A marshaller for managed objects going through the normal <see cref="WindowsRuntimeComWrappers"/> CCW creation path.
/// </summary>
public sealed unsafe class WindowsRuntimeComWrappersMarshallerAttribute : WindowsRuntimeObjectMarshallerAttribute
{
    /// <inheritdoc/>
    public override WindowsRuntimeObjectReferenceValue ConvertToUnmanagedUnsafe(object value)
    {
        void* thisPtr = (void*)WindowsRuntimeComWrappers.Default.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.TrackerSupport);

        return new(thisPtr);
    }

    /// <inheritdoc/>
    public override object ConvertToManaged(in WindowsRuntimeObjectReferenceValue value)
    {
        return WindowsRuntimeComWrappers.Default.GetOrCreateObjectForComInstance((nint)value.GetThisPtrUnsafe(), CreateObjectFlags.TrackerObject);
    }
}
