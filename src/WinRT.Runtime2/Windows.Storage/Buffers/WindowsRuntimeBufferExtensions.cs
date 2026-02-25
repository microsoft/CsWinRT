// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Windows.Storage.Streams;
using WindowsRuntime;
using WindowsRuntime.InteropServices;

namespace Windows.Storage.Buffers;

/// <summary>
/// Provides extension methods that expose operations on <see cref="IBuffer"/> objects.
/// </summary>
public static class WindowsRuntimeBufferExtensions
{
    /// <summary>
    /// Returns an <see cref="IBuffer"/> instance that represents the specified byte array.
    /// </summary>
    /// <param name="source">The byte array to represent.</param>
    /// <returns>The resulting <see cref="IBuffer"/> instance.</returns>
    /// <remarks>
    /// The returned <see cref="IBuffer"/> instance will have <see cref="IBuffer.Capacity"/> and <see cref="IBuffer.Length"/> equal to the length of <paramref name="source"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
    public static IBuffer AsBuffer(this byte[] source)
    {
        return AsBuffer(source, offset: 0, length: source.Length, capacity: source.Length);
    }

    /// <summary>
    /// Returns an <see cref="IBuffer"/> instance that represents a range of bytes in the specified byte array.
    /// </summary>
    /// <param name="source">The byte array to represent.</param>
    /// <param name="offset">The offset in <paramref name="source"/> where the range begins.</param>
    /// <param name="length">The length of the range that is represented by the <see cref="IBuffer"/> instance.</param>
    /// <returns>The resulting <see cref="IBuffer"/> instance that represents the specified range of bytes in <paramref name="source"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="offset"/> or <paramref name="length"/> are less than <c>0</c>.</exception>
    /// <exception cref="ArgumentException">Thrown if the specified range is not valid.</exception>
    public static IBuffer AsBuffer(this byte[] source, int offset, int length)
    {
        return AsBuffer(source, offset: offset, length: length, capacity: length);
    }

    /// <summary>
    /// Returns an <see cref="IBuffer"/> instance that represents a range of bytes in the specified byte array and has a specified capacity.
    /// </summary>
    /// <param name="source">The byte array to represent.</param>
    /// <param name="offset">The offset in <paramref name="source"/> where the range begins.</param>
    /// <param name="length">The length of the range that is represented by the <see cref="IBuffer"/> instance.</param>
    /// <param name="capacity">The value to use for the <see cref="IBuffer.Capacity"/> property on the returned instance.</param>
    /// <returns>The resulting <see cref="IBuffer"/> instance that represents the specified range of bytes in <paramref name="source"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="offset"/>, <paramref name="length"/>, or <paramref name="capacity"/> are less than <c>0</c>.</exception>
    /// <exception cref="ArgumentException">Thrown if the specified range is not valid, or if <paramref name="capacity"/> is less than the specified range.</exception>
    public static IBuffer AsBuffer(this byte[] source, int offset, int length, int capacity)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        //if (source.Length - offset < length) throw new ArgumentException(global::Windows.Storage.Streams.SR.Argument_InsufficientArrayElementsAfterOffset);
        //if (source.Length - offset < capacity) throw new ArgumentException(global::Windows.Storage.Streams.SR.Argument_InsufficientArrayElementsAfterOffset);
        //if (capacity < length) throw new ArgumentException(global::Windows.Storage.Streams.SR.Argument_InsufficientBufferCapacity);

        return new WindowsRuntimeExternalArrayBuffer(source, offset, length, capacity);
    }

    /// <summary>
    /// Copies the contents of a given <see cref="ReadOnlySpan{T}"/> of values to a target <see cref="IBuffer"/> instance.
    /// </summary>
    /// <param name="source">The <see cref="ReadOnlySpan{T}"/> value to copy from.</param>
    /// <param name="destination">The destination <see cref="IBuffer"/> instance to copy data to.</param>
    /// <remarks>
    /// This method will update the <see cref="IBuffer.Length"/> property of <paramref name="destination"/> if copying the data
    /// exceeds the current value of that property (but still falls within the bounds of <see cref="IBuffer.Capacity"/>). If the
    /// data being copied fits within the current value of <see cref="IBuffer.Length"/>, its value is not modified.
    /// </remarks>
    public static void CopyTo(this ReadOnlySpan<byte> source, IBuffer destination)
    {
        CopyTo(source, destination, destinationIndex: 0);
    }

    /// <summary>
    /// Copies the contents of a given <see cref="ReadOnlySpan{T}"/> of values to a target <see cref="IBuffer"/> instance.
    /// </summary>
    /// <param name="source">The <see cref="ReadOnlySpan{T}"/> value to copy from.</param>
    /// <param name="destination">The destination <see cref="IBuffer"/> instance to copy data to.</param>
    /// <param name="destinationIndex">The index within <paramref name="destination"/> from which to start copying data to.</param>
    /// <remarks>
    /// This method will update the <see cref="IBuffer.Length"/> property of <paramref name="destination"/> if copying the data
    /// exceeds the current value of that property (but still falls within the bounds of <see cref="IBuffer.Capacity"/>). If the
    /// data being copied fits within the current value of <see cref="IBuffer.Length"/>, its value is not modified.
    /// </remarks>
    public static void CopyTo(this ReadOnlySpan<byte> source, IBuffer destination, uint destinationIndex)
    {
        ArgumentNullException.ThrowIfNull(destination);
        //if (destination.Capacity < destinationIndex) throw new ArgumentException(global::Windows.Storage.Streams.SR.Argument_BufferIndexExceedsCapacity);
        //if (destination.Capacity - destinationIndex < source.Length) throw new ArgumentException(global::Windows.Storage.Streams.SR.Argument_InsufficientSpaceInTargetBuffer);

        // If the source span is empty, just stop here immediately and skip all overhead of preparing the target range
        if (source.IsEmpty)
        {
            return;
        }

        Span<byte> destinationSpan = GetSpanForCapacity(destination)[(int)destinationIndex..];

        source.CopyTo(destinationSpan);

        // Ensure the destination buffer stays alive for the copy operation. This is required because
        // 'IBuffer' implementations might release their memory immediately when collected. The span
        // we have retrieved will not be enough to keep the actual owning buffer instances alive.
        GC.KeepAlive(destination);

        // Update the 'Length' property last to make sure the data is valid
        if (destinationIndex + source.Length > destination.Length)
        {
            destination.Length = destinationIndex + (uint)source.Length;
        }
    }

    /// <summary>
    /// Gets a <see cref="Span{T}"/> value for the underlying data in the specified buffer.
    /// </summary>
    /// <param name="buffer">The input <see cref="IBuffer"/> instance.</param>
    /// <returns>The resulting <see cref="Span{T}"/> value.</returns>
    /// <remarks>
    /// The returned <see cref="Span{T}"/> value has a length equal to <see cref="IBuffer.Capacity"/>, not <see cref="IBuffer.Length"/>.
    /// </remarks>
    private static unsafe Span<byte> GetSpanForCapacity(IBuffer buffer)
    {
        // Equivalent logic as 'WindowsRuntimeBufferMarshal.TryGetDataUnsafe', but returning a 'Span<byte>' instead
        if (buffer is WindowsRuntimeObject { HasUnwrappableNativeObjectReference: true } bufferObject)
        {
            using WindowsRuntimeObjectReferenceValue bufferByteAccessValue = bufferObject.NativeObjectReference.AsValue(WellKnownInterfaceIIDs.IID_IBufferByteAccess);

            byte* bufferPtr;

            HRESULT hresult = IBufferByteAccessVftbl.BufferUnsafe(bufferByteAccessValue.GetThisPtrUnsafe(), &bufferPtr);

            RestrictedErrorInfo.ThrowExceptionForHR(hresult);

            return new(bufferPtr, checked((int)buffer.Capacity));
        }

        // If buffer is backed by a managed array, return it
        if (buffer is WindowsRuntimeExternalArrayBuffer externalArrayBuffer)
        {
            return externalArrayBuffer.GetSpanForCapacity();
        }

        // Same as above for pinned arrays as well
        if (buffer is WindowsRuntimePinnedArrayBuffer pinnedArrayBuffer)
        {
            return pinnedArrayBuffer.GetSpanForCapacity();
        }

        throw new ArgumentException(WindowsRuntimeExceptionMessages.Argument_InvalidIBufferInstance);
    }
}
