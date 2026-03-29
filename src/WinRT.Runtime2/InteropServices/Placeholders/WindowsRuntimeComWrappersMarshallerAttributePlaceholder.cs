// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A placeholder <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> type.
/// </summary>
internal sealed unsafe class WindowsRuntimeComWrappersMarshallerAttributePlaceholder : WindowsRuntimeComWrappersMarshallerAttribute
{
    /// <summary>
    /// The shared placeholder instance.
    /// </summary>
    public static readonly WindowsRuntimeComWrappersMarshallerAttributePlaceholder Instance = new();

    /// <inheritdoc/>
    public override void* GetOrCreateComInterfaceForObject(object value)
    {
        return null;
    }

    /// <inheritdoc/>
    public override ComWrappers.ComInterfaceEntry* ComputeVtables(out int count)
    {
        count = 0;

        return null;
    }

    /// <inheritdoc/>
    public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
    {
        wrapperFlags = CreatedWrapperFlags.None;

        return null!;
    }
}
#endif
