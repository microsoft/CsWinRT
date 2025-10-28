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
    /// <summary>
    /// Tries to create an agile reference from an input <see cref="WindowsRuntimeObjectReference"/> instance.
    /// </summary>
    /// <param name="objectReference">The input <see cref="WindowsRuntimeObjectReference"/> instance.</param>
    /// <returns>An agile reference for <paramref name="objectReference"/>.</returns>
    /// <remarks>
    /// This method will always use <c>IUnknown</c> as the IID to create the returned agile reference.
    /// </remarks>
    public static WindowsRuntimeObjectReference? AsAgileUnsafe(this WindowsRuntimeObjectReference objectReference)
    {
        void* pAgileReference;
        HRESULT hresult;

        objectReference.AddRefUnsafe();

        try
        {
            // Get the agile reference on the current object reference
            fixed (Guid* riid = &WellKnownWindowsInterfaceIIDs.IID_IUnknown)
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

        // We know the object is agile, so we can construct 'FreeThreadedObjectReference' directly.
        // The interface is also 'IUnknown', as we asked for it, so no 'QueryInterface' is needed.
        return new FreeThreadedObjectReference(
            thisPtr: pAgileReference,
            referenceTrackerPtr: null,
            flags: CreateObjectReferenceFlags.None);
    }

    /// <summary>
    /// Creates a non-agile reference with a specified IID from an agile reference.
    /// </summary>
    /// <param name="objectReference">The source agile reference (this is expected to be from <see cref="AsAgileUnsafe"/>).</param>
    /// <param name="iid">The IID to use to create the resulting non-agile reference.</param>
    /// <returns>The resulting non-agile reference resolved from <paramref name="objectReference"/>.</returns>
    public static WindowsRuntimeObjectReference FromAgileUnsafe(this WindowsRuntimeObjectReference objectReference, in Guid iid)
    {
        void* pObjectReference;
        HRESULT hresult;

        objectReference.AddRefUnsafe();

        try
        {
            // Resolve the original object from the agile reference
            fixed (Guid* riid = &iid)
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
        // We don't need to do another 'QueryInterface', as 'Resolve' guarantees things.
        return WindowsRuntimeObjectReference.CreateUnsafe(pObjectReference, in iid)!;
    }
}
