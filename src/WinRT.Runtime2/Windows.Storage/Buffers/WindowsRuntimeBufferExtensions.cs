// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Storage.Streams;
using WindowsRuntime;
using WindowsRuntime.InteropServices;

#pragma warning disable IDE0057

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
    /// Copies the contents of a given <see cref="ReadOnlySpan{T}"/> value to a target <see cref="IBuffer"/> instance.
    /// </summary>
    /// <param name="source">The <see cref="ReadOnlySpan{T}"/> value to copy from.</param>
    /// <param name="destination">The destination <see cref="IBuffer"/> instance to copy data to.</param>
    /// <remarks>
    /// This method will update the <see cref="IBuffer.Length"/> property of <paramref name="destination"/> if copying the data
    /// exceeds the current value of that property (but still falls within the bounds of <see cref="IBuffer.Capacity"/>). If the
    /// data being copied fits within the current value of <see cref="IBuffer.Length"/>, its value is not modified.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="destination"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="destination"/> does not have enough capacity for the copy operation.</exception>
    /// <exception cref="Exception">Thrown if invoking <see href="https://learn.microsoft.com/windows/win32/api/robuffer/nf-robuffer-ibufferbyteaccess-buffer"><c>IBufferByteAccess.Buffer</c></see> on the input buffer fails.</exception>
    public static void CopyTo(this ReadOnlySpan<byte> source, IBuffer destination)
    {
        CopyTo(source, destination, destinationIndex: 0);
    }

    /// <summary>
    /// Copies the contents of a given <see cref="ReadOnlySpan{T}"/> value to a target <see cref="IBuffer"/> instance.
    /// </summary>
    /// <param name="source">The <see cref="ReadOnlySpan{T}"/> value to copy from.</param>
    /// <param name="destination">The destination <see cref="IBuffer"/> instance to copy data to.</param>
    /// <param name="destinationIndex">The index within <paramref name="destination"/> from which to start copying data to.</param>
    /// <remarks>
    /// This method will update the <see cref="IBuffer.Length"/> property of <paramref name="destination"/> if copying the data
    /// exceeds the current value of that property (but still falls within the bounds of <see cref="IBuffer.Capacity"/>). If the
    /// data being copied fits within the current value of <see cref="IBuffer.Length"/>, its value is not modified.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="destination"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="destinationIndex"/> exceeds the value of the <see cref="IBuffer.Capacity"/> property for <paramref name="destination"/>,
    /// or if the remaining space starting at the specified index is not enough for the copy operation.
    /// </exception>
    /// <exception cref="Exception">Thrown if invoking <see href="https://learn.microsoft.com/windows/win32/api/robuffer/nf-robuffer-ibufferbyteaccess-buffer"><c>IBufferByteAccess.Buffer</c></see> on the input buffer fails.</exception>
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

        Debug.Assert(destinationIndex <= int.MaxValue);

        Span<byte> destinationSpan = GetSpanForCapacity(destination).Slice(start: (int)destinationIndex);

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
    /// Copies the contents of a given byte array to a target <see cref="IBuffer"/> instance.
    /// </summary>
    /// <param name="source">The byte array to copy from.</param>
    /// <param name="destination">The destination <see cref="IBuffer"/> instance to copy data to.</param>
    /// <remarks>
    /// This method will update the <see cref="IBuffer.Length"/> property of <paramref name="destination"/> if copying the data
    /// exceeds the current value of that property (but still falls within the bounds of <see cref="IBuffer.Capacity"/>). If the
    /// data being copied fits within the current value of <see cref="IBuffer.Length"/>, its value is not modified.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> or <paramref name="destination"/> are <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="destination"/> does not have enough capacity for the copy operation.</exception>
    /// <exception cref="Exception">Thrown if invoking <see href="https://learn.microsoft.com/windows/win32/api/robuffer/nf-robuffer-ibufferbyteaccess-buffer"><c>IBufferByteAccess.Buffer</c></see> on the input buffer fails.</exception>
    public static void CopyTo(this byte[] source, IBuffer destination)
    {
        ArgumentNullException.ThrowIfNull(source);

        CopyTo(source.AsSpan(), destination, destinationIndex: 0);
    }

    /// <summary>
    /// Copies a range of bytes in the specified byte array to a target <see cref="IBuffer"/> instance.
    /// </summary>
    /// <param name="source">The byte array to copy from.</param>
    /// <param name="sourceIndex">The index in <paramref name="source"/> to begin copying data from.</param>
    /// <param name="destination">The destination <see cref="IBuffer"/> instance to copy data to.</param>
    /// <param name="destinationIndex">The index within <paramref name="destination"/> from which to start copying data to.</param>
    /// <param name="count">The number of bytes to copy.</param>
    /// <remarks>
    /// This method will update the <see cref="IBuffer.Length"/> property of <paramref name="destination"/> if copying the data
    /// exceeds the current value of that property (but still falls within the bounds of <see cref="IBuffer.Capacity"/>). If the
    /// data being copied fits within the current value of <see cref="IBuffer.Length"/>, its value is not modified.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> or <paramref name="destination"/> are <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="sourceIndex"/> is less than <c>0</c>, if it exceeds the length of <paramref name="source"/>,
    /// or if <paramref name="count"/> exceeds the capacity of <paramref name="destination"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="destinationIndex"/> exceeds the value of the <see cref="IBuffer.Capacity"/> property for <paramref name="destination"/>,
    /// or if the remaining space starting at the specified index is not enough for the copy operation.
    /// </exception>
    /// <exception cref="Exception">Thrown if invoking <see href="https://learn.microsoft.com/windows/win32/api/robuffer/nf-robuffer-ibufferbyteaccess-buffer"><c>IBufferByteAccess.Buffer</c></see> on the input buffer fails.</exception>
    public static void CopyTo(this byte[] source, int sourceIndex, IBuffer destination, uint destinationIndex, int count)
    {
        ArgumentNullException.ThrowIfNull(source);

        CopyTo(source.AsSpan(start: sourceIndex, length: count), destination, destinationIndex: destinationIndex);
    }

    /// <summary>
    /// Copies the contents of a given <see cref="IBuffer"/> instance to a target <see cref="Span{T}"/> value.
    /// </summary>
    /// <param name="source">The <see cref="IBuffer"/> instance to copy from.</param>
    /// <param name="destination">The destination <see cref="Span{T}"/> value to copy data to.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="destination"/> does not have enough capacity for the copy operation.</exception>
    /// <exception cref="Exception">Thrown if invoking <see href="https://learn.microsoft.com/windows/win32/api/robuffer/nf-robuffer-ibufferbyteaccess-buffer"><c>IBufferByteAccess.Buffer</c></see> on the input buffer fails.</exception>
    public static void CopyTo(this IBuffer source, Span<byte> destination)
    {
        CopyTo(source, sourceIndex: 0, destination, count: checked((int)source.Length));
    }

    /// <summary>
    /// Copies a range of bytes of a given <see cref="IBuffer"/> instance to a target <see cref="Span{T}"/> value.
    /// </summary>
    /// <param name="source">The <see cref="IBuffer"/> instance to copy from.</param>
    /// <param name="sourceIndex">The index in <paramref name="source"/> to begin copying data from.</param>
    /// <param name="destination">The destination <see cref="Span{T}"/> value to copy data to.</param>
    /// <param name="count">The number of bytes to copy.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="count"/> is less than <c>0</c>, if it exceeds the length of <paramref name="source"/>,
    /// or if <paramref name="count"/> exceeds the length of <paramref name="destination"/>.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown if the remaining space starting at the specified index is not enough for the copy operation.</exception>
    /// <exception cref="Exception">Thrown if invoking <see href="https://learn.microsoft.com/windows/win32/api/robuffer/nf-robuffer-ibufferbyteaccess-buffer"><c>IBufferByteAccess.Buffer</c></see> on the input buffer fails.</exception>
    public static void CopyTo(this IBuffer source, uint sourceIndex, Span<byte> destination, int count)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        //if (source.Length < sourceIndex) throw new ArgumentException("The specified buffer index is not within the buffer length.");
        //if (source.Length - sourceIndex < count) throw new ArgumentException(global::Windows.Storage.Streams.SR.Argument_InsufficientSpaceInSourceBuffer);
        //if (destination.Length < count) throw new ArgumentException(global::Windows.Storage.Streams.SR.Argument_InsufficientArrayElementsAfterOffset);

        // If there are no values to copy, just stop here immediately and skip all overhead of preparing the target range
        if (count == 0)
        {
            return;
        }

        Debug.Assert(sourceIndex <= int.MaxValue);

        Span<byte> sourceSpan = GetSpanForCapacity(source).Slice(start: (int)sourceIndex, length: count);

        sourceSpan.CopyTo(destination);

        GC.KeepAlive(source);
    }

    /// <summary>
    /// Copies the contents of a given <see cref="IBuffer"/> instance to a target byte array.
    /// </summary>
    /// <param name="source">The <see cref="IBuffer"/> instance to copy from.</param>
    /// <param name="destination">The destination byte array to copy data to.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> or <paramref name="destination"/> are <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="destination"/> does not have enough capacity for the copy operation.</exception>
    /// <exception cref="Exception">Thrown if invoking <see href="https://learn.microsoft.com/windows/win32/api/robuffer/nf-robuffer-ibufferbyteaccess-buffer"><c>IBufferByteAccess.Buffer</c></see> on the input buffer fails.</exception>
    public static void CopyTo(this IBuffer source, byte[] destination)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);

        CopyTo(source, destination.AsSpan());
    }

    /// <summary>
    /// Copies a range of bytes of a given <see cref="IBuffer"/> instance to a target byte array.
    /// </summary>
    /// <param name="source">The <see cref="IBuffer"/> instance to copy from.</param>
    /// <param name="sourceIndex">The index in <paramref name="source"/> to begin copying data from.</param>
    /// <param name="destination">The destination byte array to copy data to.</param>
    /// <param name="destinationIndex">The index within <paramref name="destination"/> from which to start copying data to.</param>
    /// <param name="count">The number of bytes to copy.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> or <paramref name="destination"/> are <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="count"/> is less than <c>0</c>, if it exceeds the length of <paramref name="source"/>,
    /// or if <paramref name="count"/> exceeds the length of <paramref name="destination"/>.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown if the remaining space starting at the specified index is not enough for the copy operation.</exception>
    /// <exception cref="Exception">Thrown if invoking <see href="https://learn.microsoft.com/windows/win32/api/robuffer/nf-robuffer-ibufferbyteaccess-buffer"><c>IBufferByteAccess.Buffer</c></see> on the input buffer fails.</exception>
    public static void CopyTo(this IBuffer source, uint sourceIndex, byte[] destination, int destinationIndex, int count)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);

        CopyTo(source, sourceIndex: sourceIndex, destination.AsSpan(destinationIndex, count), count: count);
    }

    /// <summary>
    /// Copies all bytes from the source <see cref="IBuffer"/> instance to the destination <see cref="IBuffer"/> instance, starting at offset <c>0</c> in both.
    /// </summary>
    /// <param name="source">The <see cref="IBuffer"/> instance to copy from.</param>
    /// <param name="destination">The destination <see cref="IBuffer"/> instance to copy data to.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> or <paramref name="destination"/> are <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="destination"/> does not have enough capacity for the copy operation.</exception>
    /// <exception cref="Exception">Thrown if invoking <see href="https://learn.microsoft.com/windows/win32/api/robuffer/nf-robuffer-ibufferbyteaccess-buffer"><c>IBufferByteAccess.Buffer</c></see> on either input buffer fails.</exception>
    public static void CopyTo(this IBuffer source, IBuffer destination)
    {
        ArgumentNullException.ThrowIfNull(source);

        CopyTo(source, 0, destination, 0, source.Length);
    }

    /// <summary>
    /// Copies a range of bytes from the source <see cref="IBuffer"/> instance to the destination <see cref="IBuffer"/> instance.
    /// </summary>
    /// <param name="source">The <see cref="IBuffer"/> instance to copy from.</param>
    /// <param name="sourceIndex">The index in <paramref name="source"/> to begin copying data from.</param>
    /// <param name="destination">The destination <see cref="IBuffer"/> instance to copy data to.</param>
    /// <param name="destinationIndex">The index within <paramref name="destination"/> from which to start copying data to.</param>
    /// <param name="count">The number of bytes to copy.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> or <paramref name="destination"/> are <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="sourceIndex"/> exceeds the value of the <see cref="IBuffer.Capacity"/> property for <paramref name="source"/>,
    /// if <paramref name="destinationIndex"/> exceeds the value of the <see cref="IBuffer.Capacity"/> property for <paramref name="destination"/>,
    /// or if the remaining space starting at the specified index is not enough for the copy operation.
    /// </exception>
    /// <exception cref="Exception">Thrown if invoking <see href="https://learn.microsoft.com/windows/win32/api/robuffer/nf-robuffer-ibufferbyteaccess-buffer"><c>IBufferByteAccess.Buffer</c></see> on either input buffer fails.</exception>
    public static void CopyTo(this IBuffer source, uint sourceIndex, IBuffer destination, uint destinationIndex, uint count)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        //if (source.Length < sourceIndex) throw new ArgumentException("The specified buffer index is not within the buffer length.");
        //if (source.Length - sourceIndex < count) throw new ArgumentException(global::Windows.Storage.Streams.SR.Argument_InsufficientSpaceInSourceBuffer);
        //if (destination.Capacity < destinationIndex) throw new ArgumentException(global::Windows.Storage.Streams.SR.Argument_BufferIndexExceedsCapacity);
        //if (destination.Capacity - destinationIndex < count) throw new ArgumentException(global::Windows.Storage.Streams.SR.Argument_InsufficientSpaceInTargetBuffer);

        // If there are no values to copy, just stop here immediately and skip all overhead of preparing the target range
        if (count == 0)
        {
            return;
        }

        Debug.Assert(count <= int.MaxValue);
        Debug.Assert(sourceIndex <= int.MaxValue);
        Debug.Assert(destinationIndex <= int.MaxValue);

        Span<byte> sourceSpan = GetSpanForCapacity(source).Slice(start: (int)sourceIndex, length: (int)count);
        Span<byte> destinationSpan = GetSpanForCapacity(destination).Slice(start: (int)destinationIndex);

        sourceSpan.CopyTo(destinationSpan);

        GC.KeepAlive(source);
        GC.KeepAlive(destination);

        // Update the 'Length' property last to make sure the data is valid
        if (destinationIndex + count > destination.Length)
        {
            destination.Length = destinationIndex + count;
        }
    }

    /// <summary>
    /// Returns a new array that is created from the contents of the specified <see cref="IBuffer"/> instance.
    /// The size of the array is the value of the <see cref="IBuffer.Length"/> property of the input buffer.
    /// </summary>
    /// <param name="source">The <see cref="IBuffer"/> instance whose contents will be used to populate the new array.</param>
    /// <returns>A byte array that contains the bytes in <paramref name="source"/>, beginning at offset <c>0</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the length of <paramref name="source"/> exceeds <see cref="Array.MaxLength"/>.</exception>
    /// <exception cref="Exception">Thrown if invoking <see href="https://learn.microsoft.com/windows/win32/api/robuffer/nf-robuffer-ibufferbyteaccess-buffer"><c>IBufferByteAccess.Buffer</c></see> on the input buffer fails.</exception>
    public static byte[] ToArray(this IBuffer source)
    {
        ArgumentNullException.ThrowIfNull(source);
        // if (source.Length > Array.MaxLength) throw new ArgumentOutOfRangeException("The specified buffer has a length that exceeds the maximum array length.");

        return ToArray(source, sourceIndex: 0, count: (int)source.Length);
    }

    /// <summary>
    /// Returns a new array that is created from the contents of the specified <see cref="IBuffer"/>
    /// instance, starting at a specified offset and including a specified number of bytes.
    /// </summary>
    /// <param name="source">The <see cref="IBuffer"/> instance whose contents will be used to populate the new array.</param>
    /// <param name="sourceIndex">The index in <paramref name="source"/> to begin copying data from.</param>
    /// <param name="count">The number of bytes to copy.</param>
    /// <returns>A byte array that contains the specified range of bytes.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="count"/> is less than <c>0</c>.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="sourceIndex"/> exceeds the value of the <see cref="IBuffer.Capacity"/> property for <paramref name="source"/>,
    /// or if the remaining space starting at the specified index is not enough for the copy operation.
    /// </exception>
    /// <exception cref="Exception">Thrown if invoking <see href="https://learn.microsoft.com/windows/win32/api/robuffer/nf-robuffer-ibufferbyteaccess-buffer"><c>IBufferByteAccess.Buffer</c></see> on the input buffer fails.</exception>
    public static byte[] ToArray(this IBuffer source, uint sourceIndex, int count)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        //if (source.Length < sourceIndex) throw new ArgumentException("The specified buffer index is not within the buffer length.");
        //if (source.Length - sourceIndex < count) throw new ArgumentException(global::Windows.Storage.Streams.SR.Argument_InsufficientSpaceInSourceBuffer);

        // If the specified length is just '0', we can return a cached empty array
        if (count == 0)
        {
            return [];
        }

        byte[] destination = GC.AllocateUninitializedArray<byte>(count);

        source.CopyTo(sourceIndex: sourceIndex, destination, destinationIndex: 0, count: count);

        return destination;
    }

    /// <summary>
    /// Checks if the underlying memory backing two <see cref="IBuffer"/> instances is actually the same memory.
    /// </summary>
    /// <param name="buffer">The first <see cref="IBuffer"/> instance.</param>
    /// <param name="otherBuffer">The second <see cref="IBuffer"/> instance.</param>
    /// <returns>Whether the underlying memory pointer is the same for both specified <see cref="IBuffer"/> instances (i.e. if they're backed by the same memory).</returns>
    /// <remarks>
    /// <para>
    /// When applied to <see cref="IBuffer"/> instances backed by managed arrays, this method is preferable to a naive comparison
    /// (such as via <see cref="WindowsRuntimeBufferMarshal.TryGetDataUnsafe(IBuffer?, out byte*)"/>), because it avoids pinning
    /// the backing array which would be necessary if trying to retrieve a native memory pointer.
    /// </para>
    /// <para>
    /// This method will not take the <see cref="IBuffer.Length"/> and <see cref="IBuffer.Capacity"/> properties into account.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="buffer"/> is <see langword="null"/>.</exception>
    /// <exception cref="Exception">Thrown if invoking <see href="https://learn.microsoft.com/windows/win32/api/robuffer/nf-robuffer-ibufferbyteaccess-buffer"><c>IBufferByteAccess.Buffer</c></see> on either input buffer fails.</exception>
    public static unsafe bool IsSameData(this IBuffer buffer, [NotNullWhen(true)] IBuffer? otherBuffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        if (otherBuffer is null)
        {
            return false;
        }

        if (buffer == otherBuffer)
        {
            return true;
        }

        bool bufferIsManaged = TryGetManagedSpanForCapacity(buffer, out Span<byte> span);
        bool otherBufferIsManaged = TryGetManagedSpanForCapacity(otherBuffer, out Span<byte> otherSpan);

        // If only one of the two input buffers is backed by managed memory, they can't possibly be equal
        if (bufferIsManaged != otherBufferIsManaged)
        {
            return false;
        }

        // If both buffers are backed by managed memory, check whether they're pointing to the same area. Note that this could return 'true'
        // even if the actual managed buffer types are different. For instance, one could be a 'WindowsRuntimePinnedArrayBuffer', which the
        // user then unwrapped via 'WindowsRuntimeBufferMarshal.TryGetArray', and then used to create a separate managed buffer instance,
        // by calling one of the 'AsBuffer' extensions defined above. So the only thing we can do is to compare the actual memory address.
        if (bufferIsManaged)
        {
            return Unsafe.AreSame(
                left: in MemoryMarshal.GetReference(span),
                right: in MemoryMarshal.GetReference(otherSpan));
        }

        // Lastly, check whether they're both native buffer objects that point to the same memory. Here we're intentionally not reusing
        // the 'TryGetNativeSpanForCapacity', because that method also does a range check for the 'Capacity' property. For the purposes
        // of this method, we actually don't want that. Two buffers should just compare as equal even if their capacity exceeds the limit
        // for managed spans. That is fine here, given we're not actually passing that span anywhere (and this method shouldn't throw).
        if (buffer is WindowsRuntimeObject { HasUnwrappableNativeObjectReference: true } bufferObject &&
            otherBuffer is WindowsRuntimeObject { HasUnwrappableNativeObjectReference: true } otherBufferObject)
        {
            using WindowsRuntimeObjectReferenceValue bufferByteAccessValue = bufferObject.NativeObjectReference.AsValue(WellKnownInterfaceIIDs.IID_IBufferByteAccess);
            using WindowsRuntimeObjectReferenceValue otherBufferByteAccessValue = otherBufferObject.NativeObjectReference.AsValue(WellKnownInterfaceIIDs.IID_IBufferByteAccess);

            byte* bufferPtr;
            byte* otherBufferPtr;

            HRESULT hresult = IBufferByteAccessVftbl.BufferUnsafe(bufferByteAccessValue.GetThisPtrUnsafe(), &bufferPtr);

            RestrictedErrorInfo.ThrowExceptionForHR(hresult);

            hresult = IBufferByteAccessVftbl.BufferUnsafe(bufferByteAccessValue.GetThisPtrUnsafe(), &otherBufferPtr);

            RestrictedErrorInfo.ThrowExceptionForHR(hresult);

            return bufferPtr == otherBufferPtr;
        }

        return false;
    }

    /// <summary>
    /// Creates a new <see cref="IBuffer"/> instance backed by the same memory as the specified <see cref="MemoryStream"/> instance.
    /// </summary>
    /// <param name="stream">The <see cref="MemoryStream"/> to use to share the data memory with the buffer being created.</param>
    /// <returns>A new <see cref="IBuffer"/> instance backed by the same memory as <paramref name="stream"/>.</returns>
    /// <remarks>
    /// The <see cref="MemoryStream"/> instance may re-sized in future, which would cause that stream to be backed by a different memory region.
    /// In that scenario, the buffer created by this method will remain backed by the memory behind the stream at the time the buffer was created.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="stream"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if <paramref name="stream"/> has been disposed.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown if the underlying array that <paramref name="stream"/> is used can't be accessed.</exception>
    public static IBuffer GetWindowsRuntimeBuffer(this MemoryStream stream)
    {
        // Note: the naming inconsistency with 'byte[].AsBuffer' is intentional. This extension method will appear on
        // 'MemoryStream', so consistency with method names on 'MemoryStream' is more important. There we already have
        // an API called 'GetBuffer,' which returns the underlying array.

        ArgumentNullException.ThrowIfNull(stream);

        // Try to extract the underlying buffer from the provided stream. We can only construct a Windows Runtime
        // buffer instance if this succeeds. Otherwise, there's no way to actually get the memory area we need.
        if (!stream.TryGetBuffer(out ArraySegment<byte> arraySegment))
        {
            //throw new UnauthorizedAccessException(global::Windows.Storage.Streams.SR.UnauthorizedAccess_InternalBuffer);
        }

        Debug.Assert(stream.Length <= int.MaxValue);
        Debug.Assert(stream.Capacity <= int.MaxValue);

        return new WindowsRuntimeExternalArrayBuffer(arraySegment.Array!, arraySegment.Offset, (int)stream.Length, stream.Capacity);
    }

    /// <summary>
    /// Creates a new <see cref="IBuffer"/> instance backed by the same memory as the specified <see cref="MemoryStream"/> instance.
    /// </summary>
    /// <param name="stream">The <see cref="MemoryStream"/> to use to share the data memory with the buffer being created.</param>
    /// <param name="position">The position of the shared memory region.</param>
    /// <param name="length">The maximum size of the shared memory region.</param>
    /// <returns>A new <see cref="IBuffer"/> instance backed by the same memory as <paramref name="stream"/>.</returns>
    /// <remarks>
    /// <para>
    /// The <see cref="MemoryStream"/> instance may re-sized in future, which would cause that stream to be backed by a different memory region.
    /// In that scenario, the buffer created by this method will remain backed by the memory behind the stream at the time the buffer was created.
    /// </para>
    /// <para>
    /// The created buffer begins at the specified position in the stream, and extends over up to <paramref name="length"/> bytes.
    /// If the stream has less than <paramref name="length"/> bytes after the specified starting position, the created buffer covers
    /// only as many bytes as available in the stream. In either case, the <see cref="Stream.Length"/> and the <see cref="MemoryStream.Capacity"/>
    /// properties of the created buffer are set accordingly:
    /// <list type="bullet">
    ///   <item>
    ///     <see cref="MemoryStream.Capacity"/>: number of bytes between <paramref name="position"/>
    ///     and the stream capacity end, but not more than <paramref name="length"/>.
    ///   </item>
    ///   <item>
    ///     <see cref="Stream.Length"/>: number of bytes between <paramref name="position"/> and the stream length end,
    ///     or zero if <paramref name="position"/> is beyond stream length end, but not more than <paramref name="length"/>.
    ///   </item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="stream"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="position"/> or <paramref name="length"/> are less than <c>0</c>.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if <paramref name="stream"/> has been disposed.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown if the underlying array that <paramref name="stream"/> is used can't be accessed.</exception>
    public static IBuffer GetWindowsRuntimeBuffer(this MemoryStream stream, int position, int length)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentOutOfRangeException.ThrowIfNegative(position);
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        //if (stream.Length < position) throw new ArgumentException(global::Windows.Storage.Streams.SR.Argument_StreamPositionBeyondEOS);

        // Extract the underlying buffer from the stream (same as above)
        if (!stream.TryGetBuffer(out ArraySegment<byte> arraySegment))
        {
            //throw new UnauthorizedAccessException(global::Windows.Storage.Streams.SR.UnauthorizedAccess_InternalBuffer);
        }

        int bufferOffset = arraySegment.Offset + position;
        int bufferCapacity = Math.Min(length, stream.Capacity - position);
        int bufferLength = Math.Max(0, Math.Min(length, (int)stream.Length - position));

        return new WindowsRuntimeExternalArrayBuffer(arraySegment.Array!, bufferOffset, bufferLength, bufferCapacity);
    }

    /// <summary>
    /// Returns a <see cref="Stream"/> object that represents the same memory that the specified <see cref="IBuffer"/> instance represents.
    /// </summary>
    /// <param name="source">The <see cref="IBuffer"/> instance to wrap as a stream.</param>
    /// <returns>A stream that represents the same memory that the specified <see cref="IBuffer"/> instance represents.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="source"/> is not a valid <see cref="IBuffer"/> implementation.</exception>
    /// <exception cref="Exception">Thrown if invoking <see href="https://learn.microsoft.com/windows/win32/api/robuffer/nf-robuffer-ibufferbyteaccess-buffer"><c>IBufferByteAccess.Buffer</c></see> on either input buffer fails.</exception>
    public static unsafe Stream AsStream(this IBuffer source)
    {
        ArgumentNullException.ThrowIfNull(source);

        // If buffer is backed by a managed array, unwrap it and use it for the stream
        if (source is WindowsRuntimeExternalArrayBuffer externalArrayBuffer)
        {
            byte[] array = externalArrayBuffer.GetArray(out int offset);

            return new WindowsRuntimeBufferMemoryStream(source, array, offset);
        }

        // Same as above for pinned arrays as well
        if (source is WindowsRuntimePinnedArrayBuffer pinnedArrayBuffer)
        {
            byte[] array = pinnedArrayBuffer.GetArray(out int offset);

            return new WindowsRuntimeBufferMemoryStream(source, array, offset);
        }

        // At this point the buffer must be a native object wrapper, so validate that it is the case
        if (source is not WindowsRuntimeObject { HasUnwrappableNativeObjectReference: true } bufferObject)
        {
            throw new ArgumentException(WindowsRuntimeExceptionMessages.Argument_InvalidIBufferInstance);
        }

        // Equivalent logic as 'WindowsRuntimeBufferMarshal.TryGetDataUnsafe', just tweaked for this method
        using WindowsRuntimeObjectReferenceValue bufferByteAccessValue = bufferObject.NativeObjectReference.AsValue(WellKnownInterfaceIIDs.IID_IBufferByteAccess);

        byte* bufferPtr;

        HRESULT hresult = IBufferByteAccessVftbl.BufferUnsafe(bufferByteAccessValue.GetThisPtrUnsafe(), &bufferPtr);

        RestrictedErrorInfo.ThrowExceptionForHR(hresult);

        return new WindowsRuntimeBufferUnmanagedMemoryStream(source, bufferPtr);
    }

    /// <summary>
    /// Returns the byte at the specified offset in the specified <see cref="IBuffer"/> instance.
    /// </summary>
    /// <param name="source">The <see cref="IBuffer"/> instance to get the byte from.</param>
    /// <param name="byteOffset">The offset of the byte.</param>
    /// <returns>The byte at the specified offset.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="byteOffset"/> is not in a valid range for <paramref name="source"/>.</exception>
    /// <exception cref="Exception">Thrown if invoking <see href="https://learn.microsoft.com/windows/win32/api/robuffer/nf-robuffer-ibufferbyteaccess-buffer"><c>IBufferByteAccess.Buffer</c></see> on either input buffer fails.</exception>
    public static byte GetByte(this IBuffer source, uint byteOffset)
    {
        ArgumentNullException.ThrowIfNull(source);
        //if (source.Length <= byteOffset) throw new ArgumentException("The specified buffer offset is not within the buffer length.");

        Span<byte> span = GetSpanForCapacity(source);

        byte value = span[(int)byteOffset];

        GC.KeepAlive(source);

        return value;
    }

    /// <summary>
    /// Gets a <see cref="Span{T}"/> value for the underlying data in the specified buffer.
    /// </summary>
    /// <param name="buffer">The input <see cref="IBuffer"/> instance.</param>
    /// <returns>The resulting <see cref="Span{T}"/> value.</returns>
    /// <remarks>
    /// The returned <see cref="Span{T}"/> value has a length equal to <see cref="IBuffer.Capacity"/>, not <see cref="IBuffer.Length"/>.
    /// </remarks>
    private static Span<byte> GetSpanForCapacity(IBuffer buffer)
    {
        if (!TryGetNativeSpanForCapacity(buffer, out Span<byte> span) && !TryGetManagedSpanForCapacity(buffer, out span))
        {
            throw new ArgumentException(WindowsRuntimeExceptionMessages.Argument_InvalidIBufferInstance);
        }

        return span;
    }

    /// <summary>
    /// Tries to get a <see cref="Span{T}"/> value for the underlying data in the specified buffer, only if backed by native memory.
    /// </summary>
    /// <param name="buffer">The input <see cref="IBuffer"/> instance.</param>
    /// <param name="span">The resulting <see cref="Span{T}"/> value, if retrieved.</param>
    /// <returns>Whether <paramref name="span"/> could be retrieved.</returns>
    /// <remarks>
    /// The returned <see cref="Span{T}"/> value has a length equal to <see cref="IBuffer.Capacity"/>, not <see cref="IBuffer.Length"/>.
    /// </remarks>
    private static unsafe bool TryGetNativeSpanForCapacity(IBuffer buffer, out Span<byte> span)
    {
        // Equivalent logic as 'WindowsRuntimeBufferMarshal.TryGetDataUnsafe', but returning a 'Span<byte>' instead
        if (buffer is WindowsRuntimeObject { HasUnwrappableNativeObjectReference: true } bufferObject)
        {
            using WindowsRuntimeObjectReferenceValue bufferByteAccessValue = bufferObject.NativeObjectReference.AsValue(WellKnownInterfaceIIDs.IID_IBufferByteAccess);

            byte* bufferPtr;

            HRESULT hresult = IBufferByteAccessVftbl.BufferUnsafe(bufferByteAccessValue.GetThisPtrUnsafe(), &bufferPtr);

            RestrictedErrorInfo.ThrowExceptionForHR(hresult);

            span = new(bufferPtr, checked((int)buffer.Capacity));

            return true;
        }

        span = default;

        return false;
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
    private static bool TryGetManagedSpanForCapacity(IBuffer buffer, out Span<byte> span)
    {
        // If buffer is backed by a managed array, return it
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

        span = default;

        return false;
    }
}
