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
    public override unsafe void* ConvertToUnmanagedUnsafe(object value)
    {
        return (void*)WindowsRuntimeComWrappers.Default.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.TrackerSupport);
    }
}
