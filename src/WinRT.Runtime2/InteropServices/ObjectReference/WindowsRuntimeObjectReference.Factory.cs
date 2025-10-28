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
    /// This method will perform a <c>QueryInterface</c> call on <paramref name="thisPtr"/> to retrieve the requested interface pointer.
    /// </para>
    /// <para>
    /// The resulting <see cref="WindowsRuntimeObjectReference"/> is <see langword="null"/> if <paramref name="thisPtr"/> is <see langword="null"/>.
    /// </para>
    /// </remarks>
    /// <exception cref="Exception">Thrown if the <c>QueryInterface</c> operation fails.</exception>
    internal static WindowsRuntimeObjectReference? Create(void* thisPtr, in Guid iid)
    {
        if (thisPtr is null)
        {
            return null;
        }

        HRESULT isFreeThreaded = ComObjectHelpers.IsFreeThreadedUnsafe(thisPtr);

        Marshal.ThrowExceptionForHR(isFreeThreaded);

        // Do a 'QueryInterface' to actually get the interface pointer we're looking for
        IUnknownVftbl.QueryInterfaceUnsafe(thisPtr, in iid, out void* interfacePtr).Assert();

        // Now we can safely wrap it (no need to increment its reference count here)
        // Handle 'S_OK' exactly, see notes for this inside 'IsFreeThreadedUnsafe'
        if (isFreeThreaded == WellKnownErrorCodes.S_OK)
        {
            return new FreeThreadedObjectReference(interfacePtr, referenceTrackerPtr: null);
        }

        // Same optimization as above for context aware object references
        return iid == WellKnownWindowsInterfaceIIDs.IID_IInspectable
            ? new ContextAwareInspectableObjectReference(interfacePtr, referenceTrackerPtr: null)
            : new ContextAwareInterfaceObjectReference(interfacePtr, referenceTrackerPtr: null, iid: in iid);
    }

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
    internal static WindowsRuntimeObjectReference? CreateUnsafe(void* thisPtr, in Guid iid)
    {
        if (thisPtr is null)
        {
            return null;
        }

        // If the object is agile, avoid all the context tracking overhead
        HRESULT isFreeThreaded = ComObjectHelpers.IsFreeThreadedUnsafe(thisPtr);

        Marshal.ThrowExceptionForHR(isFreeThreaded);

        // We're not transferring ownership (meaning the input pointer should remain valid, and callers
        // are responsible for eventually releasing it), so we need to increment the reference count here.
        _ = IUnknownVftbl.AddRefUnsafe(thisPtr);

        // Handle 'S_OK' exactly, see notes for this inside 'IsFreeThreadedUnsafe'
        if (isFreeThreaded == WellKnownErrorCodes.S_OK)
        {
            return new FreeThreadedObjectReference(thisPtr, referenceTrackerPtr: null);
        }

        // Otherwise, use a context aware object reference to track it, with the specialized instance
        return iid == WellKnownWindowsInterfaceIIDs.IID_IInspectable
            ? new ContextAwareInspectableObjectReference(thisPtr, referenceTrackerPtr: null)
            : new ContextAwareInterfaceObjectReference(thisPtr, referenceTrackerPtr: null, iid: in iid);
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
    internal static WindowsRuntimeObjectReference? AttachUnsafe(ref void* thisPtr, in Guid iid)
    {
        if (thisPtr is null)
        {
            return null;
        }

        void* acquiredThisPtr = thisPtr;

        thisPtr = null;

        HRESULT isFreeThreaded = ComObjectHelpers.IsFreeThreadedUnsafe(acquiredThisPtr);

        // This method is meant to transfer ownership to the returned object reference. However,
        // we need to handle the scenario where 'IsFreeThreadedUnsafe' might actually fail in a
        // way that we can't recover from (that is, if 'GetUnmarshalClass' fails). In that case,
        // we need to release the input pointer before throwing an exception, to avoid leaking it.
        // So we handle this special case here first, before doing anything else.
        if (!WellKnownErrorCodes.Succeeded(isFreeThreaded))
        {
            _ = IUnknownVftbl.ReleaseUnsafe(acquiredThisPtr);

            Marshal.ThrowExceptionForHR(isFreeThreaded);
        }

        // Special case for free-threaded object references (see notes above).
        // Handle 'S_OK' exactly, see notes for this inside 'IsFreeThreadedUnsafe'
        if (isFreeThreaded == WellKnownErrorCodes.S_OK)
        {
            return new FreeThreadedObjectReference(acquiredThisPtr, referenceTrackerPtr: null);
        }

        // Once again, same optimization as above for context aware object references
        return iid == WellKnownWindowsInterfaceIIDs.IID_IInspectable
            ? new ContextAwareInspectableObjectReference(acquiredThisPtr, referenceTrackerPtr: null)
            : new ContextAwareInterfaceObjectReference(acquiredThisPtr, referenceTrackerPtr: null, iid: in iid);
    }
}
