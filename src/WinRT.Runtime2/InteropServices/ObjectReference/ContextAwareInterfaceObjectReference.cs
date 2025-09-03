// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

#pragma warning disable IDE0032

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A <see cref="WindowsRuntimeObjectReference"/> implementation tied to a specific context, for a target interface.
/// </summary>
internal sealed unsafe class ContextAwareInterfaceObjectReference : ContextAwareObjectReference
{
    /// <summary>
    /// The interface id for the wrapped native object (also used to resolve agile references).
    /// </summary>
    private readonly Guid _iid;

    /// <summary>
    /// Creates a new <see cref="ContextAwareInterfaceObjectReference"/> instance with the specified parameters.
    /// </summary>
    /// <param name="thisPtr"><inheritdoc cref="WindowsRuntimeObjectReference(void*, void*, CreateObjectReferenceFlags)" path="/param[@name='thisPtr']/node()"/></param>
    /// <param name="referenceTrackerPtr"><inheritdoc cref="WindowsRuntimeObjectReference(void*, void*, CreateObjectReferenceFlags)" path="/param[@name='referenceTrackerPtr']/node()"/></param>
    /// <param name="iid"><inheritdoc cref="_iid" path="/summary/node()"/></param>
    /// <param name="flags"><inheritdoc cref="WindowsRuntimeObjectReference(void*, void*, CreateObjectReferenceFlags)" path="/param[@name='flags']/node()"/></param>
    public ContextAwareInterfaceObjectReference(
        void* thisPtr,
        void* referenceTrackerPtr,
        in Guid iid,
        CreateObjectReferenceFlags flags = CreateObjectReferenceFlags.None)
        : base(thisPtr, referenceTrackerPtr, flags)
    {
        _iid = iid;
    }

    /// <summary>
    /// Creates a new <see cref="ContextAwareInterfaceObjectReference"/> instance with the specified parameters.
    /// </summary>
    /// <param name="thisPtr"><inheritdoc cref="WindowsRuntimeObjectReference(void*, void*, CreateObjectReferenceFlags)" path="/param[@name='thisPtr']/node()"/></param>
    /// <param name="referenceTrackerPtr"><inheritdoc cref="WindowsRuntimeObjectReference(void*, void*, CreateObjectReferenceFlags)" path="/param[@name='referenceTrackerPtr']/node()"/></param>
    /// <param name="contextCallbackPtr"><inheritdoc cref="WindowsRuntimeObjectReference(void*, void*, CreateObjectReferenceFlags)" path="/param[@name='contextCallbackPtr']/node()"/></param>
    /// <param name="contextToken"><inheritdoc cref="WindowsRuntimeObjectReference(void*, void*, CreateObjectReferenceFlags)" path="/param[@name='contextToken']/node()"/></param>
    /// <param name="iid"><inheritdoc cref="_iid" path="/summary/node()"/></param>
    /// <param name="flags"><inheritdoc cref="WindowsRuntimeObjectReference(void*, void*, CreateObjectReferenceFlags)" path="/param[@name='flags']/node()"/></param>
    public ContextAwareInterfaceObjectReference(
        void* thisPtr,
        void* referenceTrackerPtr,
        void* contextCallbackPtr,
        nuint contextToken,
        in Guid iid,
        CreateObjectReferenceFlags flags = CreateObjectReferenceFlags.None)
        : base(thisPtr, referenceTrackerPtr, contextCallbackPtr, contextToken, flags)
    {
        _iid = iid;
    }

    /// <summary>
    /// Gets a reference to the interface id for the wrapped native object.
    /// </summary>
    public ref readonly Guid Iid => ref _iid;
}
