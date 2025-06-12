// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Provides low-level marshalling functionality for Windows Runtime objects.
/// </summary>
public static unsafe class WindowsRuntimeMarshal
{
    /// <summary>
    /// Creates a <see cref="WindowsRuntimeObjectReference"/> object for a given COM pointer, using <c>QueryInterface</c>.
    /// </summary>
    /// <param name="externalComObject">The external COM object to wrap in a managed object reference.</param>
    /// <param name="iid">The IID that represents the interface implemented by <paramref name="externalComObject"/>.</param>
    /// <param name="wrapperFlags">The resulting <see cref="CreatedWrapperFlags"/> for <paramref name="externalComObject"/>.</param>
    /// <returns>A <see cref="WindowsRuntimeObjectReference"/> wrapping <paramref name="externalComObject"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="externalComObject"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// This method is only meant to be used when creating a managed object reference around native objects. It should not
    /// be used when dealing with Windows Runtime types instantiated from C# (which includes COM aggregation scenarios too).
    /// </para>
    /// <para>
    /// This method should only be used to create <see cref="WindowsRuntimeObjectReference"/> in projection scenarios.
    /// </para>
    /// </remarks>
    public static WindowsRuntimeObjectReference CreateObjectReference(void* externalComObject, in Guid iid, out CreatedWrapperFlags wrapperFlags)
    {
        ArgumentNullException.ThrowIfNull(externalComObject);

        WindowsRuntimeObjectReference objectReference = WindowsRuntimeObjectReference.InitializeObjectReference(externalComObject, in iid);

        wrapperFlags = objectReference.GetReferenceTrackerPtrUnsafe() is null ? CreatedWrapperFlags.None : CreatedWrapperFlags.TrackerObject;

        return objectReference;
    }

    /// <summary>
    /// Initializes a <see cref="WindowsRuntimeObjectReference"/> object for a given COM pointer.
    /// </summary>
    /// <param name="externalComObject">The external COM object to wrap in a managed object reference.</param>
    /// <param name="iid">The IID that represents the interface implemented by <paramref name="externalComObject"/>.</param>
    /// <param name="wrapperFlags">The resulting <see cref="CreatedWrapperFlags"/> for <paramref name="externalComObject"/>.</param>
    /// <returns>A <see cref="WindowsRuntimeObjectReference"/> wrapping <paramref name="externalComObject"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="externalComObject"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// This method is only meant to be used when creating a managed object reference around native objects. It should not
    /// be used when dealing with Windows Runtime types instantiated from C# (which includes COM aggregation scenarios too).
    /// </para>
    /// <para>
    /// Unlike <see cref="CreateObjectReference"/>, this method assumes <paramref name="externalComObject"/> is exactly the
    /// right interface pointer for <paramref name="iid"/>, and will therefore skip doing a <c>QueryInterface</c> call on it.
    /// </para>
    /// <para>
    /// This method should only be used to create <see cref="WindowsRuntimeObjectReference"/> in projection scenarios.
    /// </para>
    /// </remarks>
    public static WindowsRuntimeObjectReference CreateObjectReferenceUnsafe(void* externalComObject, in Guid iid, out CreatedWrapperFlags wrapperFlags)
    {
        ArgumentNullException.ThrowIfNull(externalComObject);

        WindowsRuntimeObjectReference objectReference = WindowsRuntimeObjectReference.InitializeObjectReferenceUnsafe(externalComObject, in iid);

        wrapperFlags = objectReference.GetReferenceTrackerPtrUnsafe() is null ? CreatedWrapperFlags.None : CreatedWrapperFlags.TrackerObject;

        return objectReference;
    }
}
