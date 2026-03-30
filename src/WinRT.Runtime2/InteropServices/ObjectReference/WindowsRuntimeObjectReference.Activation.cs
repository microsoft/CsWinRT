// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable CS1573

namespace WindowsRuntime.InteropServices;

/// <inheritdoc cref="WindowsRuntimeObjectReference"/>
public unsafe partial class WindowsRuntimeObjectReference
{
    /// <returns>A <see cref="WindowsRuntimeObjectReference"/> instance wrapping an instance of the activation factory for the specified Windows Runtime type.</returns>
    /// <inheritdoc cref="WindowsRuntimeActivationFactory.GetActivationFactoryUnsafe(string)"/>
    public static WindowsRuntimeObjectReference GetActivationFactory(string runtimeClassName)
    {
        void* activationFactory = WindowsRuntimeActivationFactory.GetActivationFactoryUnsafe(runtimeClassName);

        return AttachUnsafe(ref activationFactory, in WellKnownWindowsInterfaceIIDs.IID_IActivationFactory)!;
    }

    /// <param name="iid">The IID of the interface pointer (from the resolved activation factory) to wrap in the returned object reference.</param>
    /// <returns>A <see cref="WindowsRuntimeObjectReference"/> instance wrapping an instance of the activation factory for the specified Windows Runtime type.</returns>
    /// <inheritdoc cref="WindowsRuntimeActivationFactory.GetActivationFactoryUnsafe(string)"/>
    public static WindowsRuntimeObjectReference GetActivationFactory(string runtimeClassName, in Guid iid)
    {
        void* activationFactory = WindowsRuntimeActivationFactory.GetActivationFactoryUnsafe(runtimeClassName, in iid);

        return AttachUnsafe(ref activationFactory, in iid)!;
    }

    /// <param name="activationFactory">A <see cref="WindowsRuntimeObjectReference"/> instance wrapping an instance of the activation factory for the specified Windows Runtime type, if successfully retrieved.</param>
    /// <inheritdoc cref="WindowsRuntimeActivationFactory.TryGetActivationFactoryUnsafe(string, out void*)"/>
    public static bool TryGetActivationFactory(string runtimeClassName, [NotNullWhen(true)] out WindowsRuntimeObjectReference? activationFactory)
    {
        if (!WindowsRuntimeActivationFactory.TryGetActivationFactoryUnsafe(runtimeClassName, out void* activationFactoryPtr))
        {
            activationFactory = null;

            return false;
        }

        activationFactory = AttachUnsafe(ref activationFactoryPtr, in WellKnownWindowsInterfaceIIDs.IID_IActivationFactory)!;

        return true;
    }

    /// <param name="activationFactory">A <see cref="WindowsRuntimeObjectReference"/> instance wrapping an instance of the activation factory for the specified Windows Runtime type, if successfully retrieved.</param>
    /// <inheritdoc cref="WindowsRuntimeActivationFactory.TryGetActivationFactoryUnsafe(string, in Guid, out void*)"/>
    public static bool TryGetActivationFactory(string runtimeClassName, in Guid iid, [NotNullWhen(true)] out WindowsRuntimeObjectReference? activationFactory)
    {
        if (!WindowsRuntimeActivationFactory.TryGetActivationFactoryUnsafe(runtimeClassName, in iid, out void* activationFactoryPtr))
        {
            activationFactory = null;

            return false;
        }

        activationFactory = AttachUnsafe(ref activationFactoryPtr, in iid)!;

        return true;
    }
}
#endif