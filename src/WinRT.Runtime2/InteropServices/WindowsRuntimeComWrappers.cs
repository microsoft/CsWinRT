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
    /// <inheritdoc/>
    protected override ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
    {
        throw new NotImplementedException();
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