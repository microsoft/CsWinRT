// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using Windows.Storage.Streams;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Provides internal helpers for working with <see cref="IBuffer"/> objects.
/// </summary>
internal static class WindowsRuntimeBufferHelpers
{
    /// <summary>
    /// Gets a <see cref="Span{T}"/> value for the underlying data in the specified buffer.
    /// </summary>
    /// <param name="buffer">The input <see cref="IBuffer"/> instance.</param>
    /// <returns>The resulting <see cref="Span{T}"/> value.</returns>
    /// <remarks>
    /// The returned <see cref="Span{T}"/> value has a length equal to <see cref="IBuffer.Capacity"/>, not <see cref="IBuffer.Length"/>.
    /// </remarks>
    public static unsafe Span<byte> GetSpanForCapacity(IBuffer buffer)
    {
        // Check for managed buffers first
        if (TryGetManagedSpanForCapacity(buffer, out Span<byte> span))
        {
            return span;
        }

        // Check for native buffers next
        if (TryGetNativeData(buffer, out byte* data))
        {
            return new(data, checked((int)buffer.Capacity));
        }

        // If we got here, it means we don't recognize the input buffer instance.
        // There is nothing we can do, but also this shouldn't really happen.
        throw ArgumentException.GetInvalidIBufferInstanceException();
    }

    /// <summary>
    /// Tries to get a <see cref="Span{T}"/> value for the underlying data in the specified buffer, only if backed by a managed array.
    /// </summary>
    /// <param name="buffer">The input <see cref="IBuffer"/> instance.</param>
    /// <param name="span">The resulting <see cref="Span{T}"/> value, if retrieved.</param>
    /// <returns>Whether <paramref name="span"/> could be retrieved.</returns>
    /// <remarks>
    /// The returned <see cref="Span{T}"/> value has a length equal to <see cref="IBuffer.Capacity"/>, not <see cref="IBuffer.Length"/>.
    /// </remarks>
    public static bool TryGetManagedSpanForCapacity(IBuffer buffer, out Span<byte> span)
    {
        // If the buffer is backed by a managed array, return it
        if (buffer is WindowsRuntimeExternalArrayBuffer externalArrayBuffer)
        {
            span = externalArrayBuffer.GetSpanForCapacity();

            return true;
        }

        // Same as above for pinned arrays as well
        if (buffer is WindowsRuntimePinnedArrayBuffer pinnedArrayBuffer)
        {
            span = pinnedArrayBuffer.GetSpanForCapacity();

            return true;
        }

        // Also handle pinned memory buffers (pointer-based, not array-backed)
        if (buffer is WindowsRuntimePinnedMemoryBuffer pinnedMemoryBuffer)
        {
            span = pinnedMemoryBuffer.GetSpanForCapacity();

            return true;
        }

        span = default;

        return false;
    }

    /// <summary>
    /// Tries to get the underlying array for the specified buffer, only if backed by a managed array.
    /// </summary>
    /// <param name="buffer">The input <see cref="IBuffer"/> instance.</param>
    /// <param name="array">The underlying array, if retrieved.</param>
    /// <param name="offset">The offset in the returned array.</param>
    /// <returns>Whether <paramref name="array"/> could be retrieved.</returns>
    public static bool TryGetManagedArray(IBuffer buffer, [NotNullWhen(true)] out byte[]? array, out int offset)
    {
        // If the buffer is backed by a managed array, unwrap it
        if (buffer is WindowsRuntimeExternalArrayBuffer externalArrayBuffer)
        {
            array = externalArrayBuffer.GetArray(out offset);

            return true;
        }

        // Same as above for pinned arrays as well
        if (buffer is WindowsRuntimePinnedArrayBuffer pinnedArrayBuffer)
        {
            array = pinnedArrayBuffer.GetArray(out offset);

            return true;
        }

        array = null;
        offset = 0;

        return false;
    }

    /// <summary>
    /// Tries to get a pointer to the underlying data for the specified buffer, only if it is a known managed buffer implementation.
    /// </summary>
    /// <param name="buffer">The input <see cref="IBuffer"/> instance.</param>
    /// <param name="data">The underlying data, if retrieved.</param>
    /// <returns>Whether <paramref name="data"/> could be retrieved.</returns>
    public static unsafe bool TryGetManagedData(IBuffer buffer, out byte* data)
    {
        // If the buffer is backed by a managed array, get the data pointer
        if (buffer is WindowsRuntimeExternalArrayBuffer externalArrayBuffer)
        {
            data = externalArrayBuffer.Buffer();

            return true;
        }

        // Same as above for pinned arrays as well
        if (buffer is WindowsRuntimePinnedArrayBuffer pinnedArrayBuffer)
        {
            data = pinnedArrayBuffer.Buffer();

            return true;
        }

        // Also handle pinned memory buffers (pointer-based, not array-backed)
        if (buffer is WindowsRuntimePinnedMemoryBuffer pinnedMemoryBuffer)
        {
            data = pinnedMemoryBuffer.Buffer();

            return true;
        }

        data = null;

        return false;
    }

    /// <summary>
    /// Tries to get the underlying data for the specified buffer, only if backed by native memory.
    /// </summary>
    /// <param name="buffer">The input <see cref="IBuffer"/> instance.</param>
    /// <param name="data">The underlying data, if retrieved.</param>
    /// <returns>Whether <paramref name="data"/> could be retrieved.</returns>
    public static unsafe bool TryGetNativeData(IBuffer buffer, out byte* data)
    {
        // Equivalent logic as 'WindowsRuntimeBufferMarshal.TryGetDataUnsafe', just optimized to only check for this
        if (buffer is WindowsRuntimeObject { HasUnwrappableNativeObjectReference: true } bufferObject)
        {
            using WindowsRuntimeObjectReferenceValue bufferByteAccessValue = bufferObject.NativeObjectReference.AsValue(WellKnownInterfaceIIDs.IID_IBufferByteAccess);

            fixed (byte** dataPtr = &data)
            {
                HRESULT hresult = IBufferByteAccessVftbl.BufferUnsafe(bufferByteAccessValue.GetThisPtrUnsafe(), dataPtr);

                RestrictedErrorInfo.ThrowExceptionForHR(hresult);
            }

            return true;
        }

        data = null;

        return false;
    }
}
