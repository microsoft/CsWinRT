// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
    private protected override unsafe void* GetThisPtrWithContextUnsafe()
    {
        // This method is never called for free-threaded objects
        return null;
    }
}
