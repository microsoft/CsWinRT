// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
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
    /// <inheritdoc/>
    RuntimeTypeHandle IDynamicInterfaceCastable.GetInterfaceImplementation(RuntimeTypeHandle interfaceType)
    {
#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
        throw null;
#elif WINDOWS_RUNTIME_IMPLEMENTATION_ASSEMBLY
        return GetInterfaceImplementation(interfaceType);
#endif
    }

    /// <inheritdoc/>
    bool IDynamicInterfaceCastable.IsInterfaceImplemented(RuntimeTypeHandle interfaceType, bool throwIfNotImplemented)
    {
#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
        throw null;
#elif WINDOWS_RUNTIME_IMPLEMENTATION_ASSEMBLY
        return IsInterfaceImplemented(interfaceType, throwIfNotImplemented);
#endif
    }

    /// <inheritdoc/>
    VirtualMethodTableInfo IUnmanagedVirtualMethodTableProvider.GetVirtualMethodTableInfoForKey(Type type)
    {
#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
        throw null;
#elif WINDOWS_RUNTIME_IMPLEMENTATION_ASSEMBLY
        return GetVirtualMethodTableInfoForKey(type);
#endif
    }

    /// <inheritdoc/>
    CustomQueryInterfaceResult ICustomQueryInterface.GetInterface(ref Guid iid, out nint ppv)
    {
#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
        throw null;
#elif WINDOWS_RUNTIME_IMPLEMENTATION_ASSEMBLY
        return GetInterface(ref iid, out ppv);
#endif
    }
}
