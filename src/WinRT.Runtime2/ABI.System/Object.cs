// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Buffers;
using System.ComponentModel;
using System.Runtime.InteropServices;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using static System.Runtime.InteropServices.ComWrappers;

[assembly: TypeMapAssociation<WindowsRuntimeTypeMapUniverse>(typeof(object), typeof(ABI.System.Object))]

namespace ABI.System;

/// <summary>
/// ABI type for <see cref="object"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[WindowsRuntimeClassName("Object")]
[ObjectVtableProvider]
file static class Object;

/// <summary>
/// A custom <see cref="WindowsRuntimeVtableProviderAttribute"/> implementation for <see cref="object"/>.
/// </summary>
file sealed class ObjectVtableProviderAttribute : WindowsRuntimeVtableProviderAttribute
{
    /// <inheritdoc/>
    public override void ComputeVtables(IBufferWriter<ComInterfaceEntry> bufferWriter)
    {
        // This is intentionally empty: we have no vtable slots to add for 'object'.
        // All 'object' instances will be marshalled with the default vtable entries.
    }
}
