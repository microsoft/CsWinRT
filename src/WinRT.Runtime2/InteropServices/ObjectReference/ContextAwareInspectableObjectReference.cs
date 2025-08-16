// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A <see cref="WindowsRuntimeObjectReference"/> implementation tied to a specific context, specific for <c>IInspectable</c> interface pointers.
/// </summary>
/// <remarks>
/// <para>
/// This specialized type allows saving a <see cref="Guid"/> field for all object references for <c>IInspectable</c>.
/// </para>
/// <para>
/// This is especially useful because every projected class has <see cref="WindowsRuntimeObject"/> as a base type,
/// which has an explicit <see cref="WindowsRuntimeObjectReference"/> field for the <c>IInspectable</c> interface.
/// </para>
/// </remarks>
internal sealed unsafe class ContextAwareInspectableObjectReference : ContextAwareObjectReference
{
    /// <summary>
    /// Creates a new <see cref="ContextAwareObjectReference"/> instance with the specified parameters.
    /// </summary>
    /// <param name="thisPtr"><inheritdoc cref="WindowsRuntimeObjectReference(void*, void*, CreateObjectReferenceFlags)" path="/param[@name='thisPtr']/node()"/></param>
    /// <param name="referenceTrackerPtr"><inheritdoc cref="WindowsRuntimeObjectReference(void*, void*, CreateObjectReferenceFlags)" path="/param[@name='referenceTrackerPtr']/node()"/></param>
    /// <param name="flags"><inheritdoc cref="WindowsRuntimeObjectReference(void*, void*, CreateObjectReferenceFlags)" path="/param[@name='flags']/node()"/></param>
    public ContextAwareInspectableObjectReference(
        void* thisPtr,
        void* referenceTrackerPtr,
        CreateObjectReferenceFlags flags = CreateObjectReferenceFlags.None)
        : base(thisPtr, referenceTrackerPtr, flags)
    {
    }

    /// <summary>
    /// Creates a new <see cref="ContextAwareObjectReference"/> instance with the specified parameters.
    /// </summary>
    /// <param name="thisPtr"><inheritdoc cref="WindowsRuntimeObjectReference(void*, void*, CreateObjectReferenceFlags)" path="/param[@name='thisPtr']/node()"/></param>
    /// <param name="referenceTrackerPtr"><inheritdoc cref="WindowsRuntimeObjectReference(void*, void*, CreateObjectReferenceFlags)" path="/param[@name='referenceTrackerPtr']/node()"/></param>
    /// <param name="contextCallbackPtr"><inheritdoc cref="WindowsRuntimeObjectReference(void*, void*, CreateObjectReferenceFlags)" path="/param[@name='contextCallbackPtr']/node()"/></param>
    /// <param name="contextToken"><inheritdoc cref="WindowsRuntimeObjectReference(void*, void*, CreateObjectReferenceFlags)" path="/param[@name='contextToken']/node()"/></param>
    /// <param name="flags"><inheritdoc cref="WindowsRuntimeObjectReference(void*, void*, CreateObjectReferenceFlags)" path="/param[@name='flags']/node()"/></param>
    public ContextAwareInspectableObjectReference(
        void* thisPtr,
        void* referenceTrackerPtr,
        void* contextCallbackPtr,
        nuint contextToken,
        CreateObjectReferenceFlags flags = CreateObjectReferenceFlags.None)
        : base(thisPtr, referenceTrackerPtr, contextCallbackPtr, contextToken, flags)
    {
    }
}
