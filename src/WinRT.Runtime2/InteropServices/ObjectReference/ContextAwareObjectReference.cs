// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A <see cref="WindowsRuntimeObjectReference"/> implementation tied to a specific context.
/// </summary>
internal sealed unsafe class ContextAwareObjectReference : WindowsRuntimeObjectReference
{
    /// <inheritdoc/>
    public ContextAwareObjectReference(
        void* thisPtr,
        void* referenceTrackerPtr,
        CreateObjectReferenceFlags flags)
        : base(thisPtr, referenceTrackerPtr, flags)
    {
    }

    /// <inheritdoc/>
    private protected override bool DerivedIsInCurrentContext()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    private protected override unsafe void* GetThisPtrWithContextUnsafe()
    {
        // TODO
        throw new NotImplementedException();
    }
}
