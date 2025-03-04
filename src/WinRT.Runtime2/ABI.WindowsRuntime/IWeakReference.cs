// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using WindowsRuntime.InteropServices;

namespace ABI.WindowsRuntime;

/// <inheritdoc cref="global::WindowsRuntime.IWeakReference"/>
[DynamicInterfaceCastableImplementation]
public interface IWeakReference : global::WindowsRuntime.IWeakReference
{
    /// <inheritdoc/>
    WindowsRuntimeObjectReference global::WindowsRuntime.IWeakReference.Resolve(in Guid interfaceId)
    {
        // TODO
        throw null!;
    }
}
