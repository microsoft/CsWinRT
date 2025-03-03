// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Extensions for working with agile objects through <see cref="WindowsRuntimeObjectReference"/> instances.
/// </summary>
internal static unsafe class AgileObjectReferenceExtensions
{
    public static WindowsRuntimeObjectReference? AsAgileUnsafe(this WindowsRuntimeObjectReference objectReference)
    {
        void* pAgileReference;
        HRESULT hresult;

        objectReference.AddRefUnsafe();

        try
        {
            // Get the agile reference on the current object reference
            fixed (Guid* riid = &WellKnownInterfaceIds.IID_IUnknown)
            {
                hresult = WindowsRuntimeImports.RoGetAgileReference(
                    options: AgileReferenceOptions.AGILEREFERENCE_DEFAULT,
                    riid: riid,
                    pUnk: objectReference.GetThisPtrUnsafe(),
                    ppAgileReference: &pAgileReference);
            }
        }
        finally
        {
            objectReference.ReleaseUnsafe();
        }

        Marshal.ThrowExceptionForHR(hresult);

        // The agile reference can be 'null' in some cases
        if (pAgileReference is null)
        {
            return null;
        }

        // We know the object is agile, so we can construct 'FreeThreadedObjectReference' directly
        return new FreeThreadedObjectReference(
            thisPtr: pAgileReference,
            referenceTrackerPtr: null,
            flags: CreateObjectReferenceFlags.None);
    }

    public static WindowsRuntimeObjectReference FromAgileUnsafe(this WindowsRuntimeObjectReference objectReference)
    {
        void* pObjectReference;
        HRESULT hresult;

        objectReference.AddRefUnsafe();

        try
        {
            // Resolve the original object from the agile reference
            fixed (Guid* riid = &WellKnownInterfaceIds.IID_IUnknown)
            {
                hresult = IAgileReferenceVftbl.ResolveUnsafe(
                    thisPtr: objectReference.GetThisPtrUnsafe(),
                    riid: riid,
                    ppvObjectReference: &pObjectReference);
            }
        }
        finally
        {
            objectReference.ReleaseUnsafe();
        }

        Marshal.ThrowExceptionForHR(hresult);

        // If 'Resolve' succeeded, the resulting object is guaranteed to be not 'null'.
        // Increment its reference count before wrapping it in a managed object reference.
        _ = IUnknownVftbl.AddRefUnsafe(pObjectReference);

        // TODO
        return null!;
    }
}
