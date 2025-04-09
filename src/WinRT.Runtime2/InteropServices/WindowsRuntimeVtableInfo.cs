// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A type holding infor on a computed vtable for a given managed type.
/// </summary>
internal sealed unsafe class WindowsRuntimeVtableInfo
{
    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeVtableInfo"/> instance with the specified parameters.
    /// </summary>
    /// <param name="vtableEntries">The computed vtable entries, allocated in native memory.</param>
    /// <param name="count">The number of elements in the vtable entries.</param>
    public WindowsRuntimeVtableInfo(ComWrappers.ComInterfaceEntry* vtableEntries, int count)
    {
        VtableEntries = vtableEntries;
        Count = count;
    }

    /// <summary>
    /// Gets the computed vtable entries, allocated in native memory.
    /// </summary>
    public ComWrappers.ComInterfaceEntry* VtableEntries { get; }

    /// <summary>
    /// Gets tt\he number of elements in <see cref="VtableEntries"/>.
    /// </summary>
    public int Count { get; }
}
