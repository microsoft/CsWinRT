// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
using System;
#endif
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace WindowsRuntime;

/// <summary>
/// The base class for all projected Windows Runtime types.
/// </summary>
/// <remarks>
/// This type should only be used as a base type by generated projected types.
/// </remarks>
public abstract partial class WindowsRuntimeObject :
    IDynamicInterfaceCastable,
    IUnmanagedVirtualMethodTableProvider,
    ICustomQueryInterface
{
    // This file only contains the public API surface for 'WindowsRuntimeObject' that
    // is included in reference assemblies. This allows the entire implementation-only
    // side to be stripped, without needing to have multiple '#ifdef'-s in the same file.

#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
    /// <inheritdoc/>
    bool IDynamicInterfaceCastable.IsInterfaceImplemented(RuntimeTypeHandle interfaceType, bool throwIfNotImplemented)
    {
        throw null;
    }

    /// <inheritdoc/>
    RuntimeTypeHandle IDynamicInterfaceCastable.GetInterfaceImplementation(RuntimeTypeHandle interfaceType)
    {
        throw null;
    }

    /// <inheritdoc/>
    VirtualMethodTableInfo IUnmanagedVirtualMethodTableProvider.GetVirtualMethodTableInfoForKey(Type type)
    {
        throw null;
    }

    /// <inheritdoc/>
    CustomQueryInterfaceResult ICustomQueryInterface.GetInterface(ref Guid iid, out nint ppv)
    {
        throw null;
    }
#endif
}
