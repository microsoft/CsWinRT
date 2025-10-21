// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// The built-in default implementation of <see cref="IWeakReference"/>, wrapping managed objects.
/// </summary>
[GeneratedComClass]
internal sealed unsafe partial class ManagedWeakReference : IWeakReference
{
    /// <summary>
    /// The weak reference for the target object.
    /// </summary>
    private readonly WeakReference<object> _weakReference;

    /// <summary>
    /// Creates a new <see cref="ManagedWeakReference"/> instance with the specified parameters.
    /// </summary>
    /// <param name="targetObject">The target object to create a weak reference for.</param>
    public ManagedWeakReference(object targetObject)
    {
        _weakReference = new WeakReference<object>(targetObject);
    }

    /// <inheritdoc/>
    public HRESULT Resolve(in Guid interfaceId, out void* weakReference)
    {
        if (_weakReference.TryGetTarget(out object? targetObject))
        {
            // We should already have a CCW for this object, as this path can only be reached from the vtable of one of our CCWs
            void* thisPtr = (void*)WindowsRuntimeComWrappers.Default.GetOrCreateComInterfaceForObject(targetObject, CreateComInterfaceFlags.TrackerSupport);

            // If the caller is requesting 'IUnknown', we can avoid a 'QueryInterface' call
            if (interfaceId == WellKnownInterfaceIIDs.IID_IUnknown)
            {
                weakReference = thisPtr;

                return WellKnownErrorCodes.S_OK;
            }

            // Try to resolve the specific interface requested from the caller (this path should never really be taken).
            // Managed weak references are basically only used internally, and we always ask for 'IUnknown' pointers.
            HRESULT hresult = IUnknownVftbl.QueryInterfaceUnsafe(thisPtr, in interfaceId, out weakReference);

            _ = IUnknownVftbl.ReleaseUnsafe(thisPtr);

            return hresult;
        }

        weakReference = null;

        return WellKnownErrorCodes.S_OK;
    }
}
