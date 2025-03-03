// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

/// <inheritdoc cref="WindowsRuntimeObjectReference"/>
public unsafe partial class WindowsRuntimeObjectReference
{
    /// <summary>
    /// Creates a <see cref="WindowsRuntimeObjectReference"/> object for a given COM pointer (to the specified interface).
    /// </summary>
    /// <param name="thisPtr">The native COM object for which to construct the <see cref="WindowsRuntimeObjectReference"/> object.</param>
    /// <param name="iid">The IID that represents the interface implemented by <paramref name="thisPtr"/>.</param>
    /// <returns>The <see cref="WindowsRuntimeObjectReference"/> holding onto the <paramref name="thisPtr"/> pointer.</returns>
    /// <remarks>
    /// <para>
    /// This method will increment the reference count for <paramref name="thisPtr"/>. Additionally, it assumes that the input COM object
    /// already points to the interface represented by <paramref name="iid"/>. It is responsibility of the caller to respect this invariant.
    /// </para>
    /// <para>
    /// The resulting <see cref="WindowsRuntimeObjectReference"/> is <see langword="null"/> if <paramref name="thisPtr"/> is <see langword="null"/>.
    /// </para>
    /// </remarks>
    public static WindowsRuntimeObjectReference? CreateUnsafe(void* thisPtr, in Guid iid)
    {
        if (thisPtr is null)
        {
            return null;
        }

        // We're not transferring ownership, so we need to increment the reference count
        _ = IUnknownVftbl.AddRefUnsafe(thisPtr);

        // If the object is agile, avoid all the context tracking overhead.
        // Otherwise, use a context aware object reference to track it.
        return ComObjectHelpers.IsFreeThreadedUnsafe(thisPtr)
            ? new FreeThreadedObjectReference(thisPtr, referenceTrackerPtr: null)
            : new ContextAwareObjectReference(thisPtr, referenceTrackerPtr: null, iid: in iid);
    }

    /// <summary>
    /// Creates a <see cref="WindowsRuntimeObjectReference"/> object for a given COM pointer (to the specified interface).
    /// </summary>
    /// <param name="thisPtr">The native COM object for which to construct the <see cref="WindowsRuntimeObjectReference"/> object.</param>
    /// <param name="iid">The IID that represents the interface implemented by <paramref name="thisPtr"/>.</param>
    /// <returns>The <see cref="WindowsRuntimeObjectReference"/> holding onto the <paramref name="thisPtr"/> pointer.</returns>
    /// <remarks>
    /// <para>
    /// This method will perform a <c>QueryInterface</c> call on <paramref name="thisPtr"/> to retrieve the requested interface pointer.
    /// </para>
    /// <para>
    /// The resulting <see cref="WindowsRuntimeObjectReference"/> is <see langword="null"/> if <paramref name="thisPtr"/> is <see langword="null"/>.
    /// </para>
    /// </remarks>
    /// <exception cref="Exception">Thrown if the <c>QueryInterface</c> operation fails.</exception>
    public static WindowsRuntimeObjectReference? As(void* thisPtr, in Guid iid)
    {
        if (thisPtr is null)
        {
            return null;
        }

        // Do a 'QueryInterface' to actually get the interface pointer we're looking for
        HRESULT hresult = IUnknownVftbl.QueryInterfaceUnsafe(thisPtr, in iid, out void* qiObject);

        Marshal.ThrowExceptionForHR(hresult);

        // Now we can safely wrap it (no need to increment its reference count here)
        return ComObjectHelpers.IsFreeThreadedUnsafe(thisPtr)
            ? new FreeThreadedObjectReference(qiObject, referenceTrackerPtr: null)
            : new ContextAwareObjectReference(qiObject, referenceTrackerPtr: null, iid: in iid);
    }

    /// <summary>
    /// Creates a <see cref="WindowsRuntimeObjectReference"/> object for a given COM pointer (to the specified interface), taking ownership of it.
    /// </summary>
    /// <param name="thisPtr">The native COM object for which to construct the <see cref="WindowsRuntimeObjectReference"/> object.</param>
    /// <param name="iid">The IID that represents the interface implemented by <paramref name="thisPtr"/>.</param>
    /// <returns>The <see cref="WindowsRuntimeObjectReference"/> holding onto the <paramref name="thisPtr"/> pointer.</returns>
    /// <remarks>
    /// <para>
    /// This method will not increment the reference count for <paramref name="thisPtr"/>. Additionally, it assumes that the input COM object
    /// already points to the interface represented by <paramref name="iid"/>. It is responsibility of the caller to respect this invariant.
    /// </para>
    /// <para>
    /// The resulting <see cref="WindowsRuntimeObjectReference"/> is <see langword="null"/> if <paramref name="thisPtr"/> is <see langword="null"/>.
    /// </para>
    /// </remarks>
    public static WindowsRuntimeObjectReference? AttachUnsafe(ref void* thisPtr, in Guid iid)
    {
        if (thisPtr is null)
        {
            return null;
        }

        // Create the appropriate wrapping object reference (see notes above)
        WindowsRuntimeObjectReference objectReference = ComObjectHelpers.IsFreeThreadedUnsafe(thisPtr)
            ? new FreeThreadedObjectReference(thisPtr, referenceTrackerPtr: null)
            : new ContextAwareObjectReference(thisPtr, referenceTrackerPtr: null, iid: in iid);

        // We transferred ownership of the input pointer, so clear it to avoid double-free issues
        thisPtr = null;

        return objectReference;
    }
}
