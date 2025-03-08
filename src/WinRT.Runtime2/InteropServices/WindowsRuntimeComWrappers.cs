// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// The <see cref="ComWrappers"/> implementation for Windows Runtime interop.
/// </summary>
public sealed unsafe class WindowsRuntimeComWrappers : ComWrappers
{
    /// <summary>
    /// The statically-visible type that should be used by <see cref="CreateObject"/>, if available.
    /// </summary>
    /// <remarks>
    /// This can be set by a thread right before calling <see cref="CreateObject"/>, to pass additional
    /// information to the <see cref="ComWrappers"/> instance. It should be set to <see langword="null"/>
    /// immediately afterwards, to ensure following calls won't accidentally see the wrong type.
    /// </remarks>
    [ThreadStatic]
    internal static Type? CreateObjectTargetType;

    /// <summary>
    /// Gets the shared default instance of <see cref="WindowsRuntimeComWrappers"/>.
    /// </summary>
    /// <remarks>
    /// This instance is the one that CsWinRT will use to marshall all Windows Runtime objects.
    /// </remarks>
    public static WindowsRuntimeComWrappers Default { get; } = new();

    /// <inheritdoc/>
    protected override ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
    {
        WindowsRuntimeVtableInfo vtableInfo = WindowsRuntimeMarshallingInfo.GetInfo(obj.GetType()).GetVtableInfo();

        count = vtableInfo.Count;

        // The computed vtable will unconditionally include 'IUnknown' as the last vtable entry.
        // However, this entry should only be included if the 'CallerDefinedIUnknown' flag is set.
        // To achieve this, we can just decrement the coutn by 1 in case the flag is not set.
        if (count != 0 && !flags.HasFlag(CreateComInterfaceFlags.CallerDefinedIUnknown))
        {
            count--;
        }

        return vtableInfo.VtableEntries;
    }

    /// <inheritdoc/>
    protected override object? CreateObject(nint externalComObject, CreateObjectFlags flags)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    protected override void ReleaseObjects(IEnumerable objects)
    {
        throw new NotImplementedException();
    }
}