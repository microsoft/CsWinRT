// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A free-threaded <see cref="WindowsRuntimeObjectReference"/> implementation.
/// </summary>
internal sealed unsafe class FreeThreadedObjectReference : WindowsRuntimeObjectReference
{
    /// <inheritdoc/>
    public FreeThreadedObjectReference(
        void* thisPtr,
        void* referenceTrackerPtr,
        CreateObjectReferenceFlags flags = CreateObjectReferenceFlags.None)
        : base(thisPtr, referenceTrackerPtr, flags)
    {
    }

    /// <inheritdoc/>
    private protected override bool DerivedIsInCurrentContext()
    {
        return true;
    }

    /// <inheritdoc/>
    private protected override HRESULT DerivedTryAsNative(in Guid iid, out WindowsRuntimeObjectReference? objectReference)
    {
        objectReference = null;

        AddRefUnsafe();

        try
        {
            HRESULT hresult = IUnknownVftbl.QueryInterfaceUnsafe(GetThisPtrUnsafe(), in iid, out void* targetObject);

            if (WellKnownErrorCodes.Succeeded(hresult))
            {
                if (IsAggregated)
                {
                    _ = IUnknownVftbl.ReleaseUnsafe(targetObject);
                }

                NativeAddRefFromTrackerSourceUnsafe();

                objectReference = new FreeThreadedObjectReference(
                    thisPtr: targetObject,
                    referenceTrackerPtr: GetReferenceTrackerPtrUnsafe(),
                    flags: CopyFlags(CreateObjectReferenceFlags.IsAggregated | CreateObjectReferenceFlags.PreventReleaseOnDispose));
            }

            return hresult;
        }
        finally
        {
            ReleaseUnsafe();
        }
    }

    /// <inheritdoc/>
    private protected override unsafe void* GetThisPtrWithContextUnsafe()
    {
        // This method is never called for free-threaded objects
        return null;
    }

    /// <inheritdoc/>
    private protected override void NativeReleaseWithContextUnsafe()
    {
        // This method is also never called
    }
}
