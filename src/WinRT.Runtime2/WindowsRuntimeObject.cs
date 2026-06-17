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
        return GetInterfaceImplementation(interfaceType);
    }

    /// <inheritdoc/>
    bool IDynamicInterfaceCastable.IsInterfaceImplemented(RuntimeTypeHandle interfaceType, bool throwIfNotImplemented)
    {
        return IsInterfaceImplemented(interfaceType, throwIfNotImplemented);
    }

    /// <inheritdoc/>
    VirtualMethodTableInfo IUnmanagedVirtualMethodTableProvider.GetVirtualMethodTableInfoForKey(Type type)
    {
        return GetVirtualMethodTableInfoForKey(type);
    }

    /// <inheritdoc/>
    CustomQueryInterfaceResult ICustomQueryInterface.GetInterface(ref Guid iid, out nint ppv)
    {
        return GetInterface(ref iid, out ppv);
    }
}
