// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.Runtime.InteropServices.WindowsRuntime
{
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using global::Windows.Foundation;
    using global::Windows.Storage.Streams;
    /// <summary>
    /// Contains extension methods that expose operations on WinRT <code>Windows.Foundation.IBuffer</code>.
    /// </summary>
#if EMBED
    internal
#else
    public 
#endif 
    static class WindowsRuntimeBufferExtensions
    {
#region (Byte []).AsBuffer extensions

        public static IBuffer AsBuffer(this byte[] source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return AsBuffer(source, 0, source.Length, source.Length);
        }


        public static IBuffer AsBuffer(this byte[] source, int offset, int length)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
            if (source.Length - offset < length) throw new ArgumentException(global::Windows.Storage.Streams.SR.Argument_InsufficientArrayElementsAfterOffset);

            return AsBuffer(source, offset, length, length);
        }


        public static IBuffer AsBuffer(this byte[] source, int offset, int length, int capacity)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (source.Length - offset < length) throw new ArgumentException(global::Windows.Storage.Streams.SR.Argument_InsufficientArrayElementsAfterOffset);
            if (source.Length - offset < capacity) throw new ArgumentException(global::Windows.Storage.Streams.SR.Argument_InsufficientArrayElementsAfterOffset);
            if (capacity < length) throw new ArgumentException(global::Windows.Storage.Streams.SR.Argument_InsufficientBufferCapacity);

            return new WindowsRuntimeBuffer(source, offset, length, capacity);
        }

#endregion (Byte []).AsBuffer extensions


#region (Span<Byte>).CopyTo extensions for copying to an (IBuffer)

        /// <summary>
        /// Copies the contents of <code>source</code> to <code>destination</code> starting at offset 0.
        /// This method does <em>NOT</em> update <code>destination.Length</code>.
        /// </summary>
        /// <param name="source">Array to copy data from.</param>
        /// <param name="destination">The buffer to copy to.</param>
        public static void CopyTo(this Span<byte> source, IBuffer destination)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            CopyTo(source, destination, 0);
        }


        /// <summary>
        /// Copies <code>count</code> bytes from <code>source</code> starting at offset <code>sourceIndex</code>
        /// to <code>destination</code> starting at <code>destinationIndex</code>.
        /// This method does <em>NOT</em> update <code>destination.Length</code>.
        /// </summary>
        /// <param name="source">Array to copy data from.</param>
        /// <param name="sourceIndex">Position in the array from where to start copying.</param>
        /// <param name="destination">The buffer to copy to.</param>
        /// <param name="destinationIndex">Position in the buffer to where to start copying.</param>
        /// <param name="count">The number of bytes to copy.</param>
        public static void CopyTo(this Span<byte> source, IBuffer destination, uint destinationIndex)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            if (destination.Capacity < destinationIndex) throw new ArgumentException(global::Windows.Storage.Streams.SR.Argument_BufferIndexExceedsCapacity);
            if (destination.Capacity - destinationIndex < source.Length) throw new ArgumentException(global::Windows.Storage.Streams.SR.Argument_InsufficientSpaceInTargetBuffer);
            if (source.Length == 0) return;

            Debug.Assert(destinationIndex <= int.MaxValue);

            // If destination is backed by a managed memory, use the memory instead of the pointer as it does not require pinning:
            Span<byte> destSpan = destination.TryGetUnderlyingData(out byte[] destDataArr, out int destOffset) ? destDataArr.AsSpan(destOffset + (int)destinationIndex) : destination.GetSpanForCapacity(destinationIndex);
            source.CopyTo(destSpan);

            // Update Length last to make sure the data is valid
            if (destinationIndex + source.Length > destination.Length)
            {
                destination.Length = destinationIndex + (uint)source.Length;
            }
        }

#endregion (Span<Byte>).CopyTo extensions for copying to an (IBuffer)

#region (Byte []).CopyTo extensions for copying to an (IBuffer)

        /// <summary>
        /// Copies the contents of <code>source</code> to <code>destination</code> starting at offset 0.
        /// This method does <em>NOT</em> update <code>destination.Length</code>.
        /// </summary>
        /// <param name="source">Array to copy data from.</param>
        /// <param name="destination">The buffer to copy to.</param>
        public static void CopyTo(this byte[] source, IBuffer destination)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            CopyTo(source.AsSpan(), destination, 0);
        }


        /// <summary>
        /// Copies <code>count</code> bytes from <code>source</code> starting at offset <code>sourceIndex</code>
        /// to <code>destination</code> starting at <code>destinationIndex</code>.
        /// This method does <em>NOT</em> update <code>destination.Length</code>.
        /// </summary>
        /// <param name="source">Array to copy data from.</param>
        /// <param name="sourceIndex">Position in the array from where to start copying.</param>
        /// <param name="destination">The buffer to copy to.</param>
        /// <param name="destinationIndex">Position in the buffer to where to start copying.</param>
        /// <param name="count">The number of bytes to copy.</param>
        public static void CopyTo(this byte[] source, int sourceIndex, IBuffer destination, uint destinationIndex, int count)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            CopyTo(source.AsSpan(sourceIndex, count), destination, destinationIndex);
        }

#endregion (Byte []).CopyTo extensions for copying to an (IBuffer)


#region (IBuffer).ToArray extensions for copying to a new (Byte [])

        public static byte[] ToArray(this IBuffer source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return ToArray(source, 0, checked((int)source.Length));
        }


        public static byte[] ToArray(this IBuffer source, uint sourceIndex, int count)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (source.Length < sourceIndex) throw new ArgumentException("The specified buffer index is not within the buffer length.");
            if (source.Length - sourceIndex < count) throw new ArgumentException(global::Windows.Storage.Streams.SR.Argument_InsufficientSpaceInSourceBuffer);

            if (count == 0)
                return Array.Empty<byte>();

            byte[] destination = new byte[count];
            source.CopyTo(sourceIndex, destination, 0, count);
            return destination;
        }

#endregion (IBuffer).ToArray extensions for copying to a new (Byte [])


#region (IBuffer).CopyTo extensions for copying to a (Span<Byte>)

        public static void CopyTo(this IBuffer source, Span<byte> destination)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            CopyTo(source, 0, destination, checked((int)source.Length));
        }

        public static void CopyTo(this IBuffer source, uint sourceIndex, Span<byte> destination, int count)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (source.Length < sourceIndex) throw new ArgumentException("The specified buffer index is not within the buffer length.");
            if (source.Length - sourceIndex < count) throw new ArgumentException(global::Windows.Storage.Streams.SR.Argument_InsufficientSpaceInSourceBuffer);
            if (destination.Length < count) throw new ArgumentException(global::Windows.Storage.Streams.SR.Argument_InsufficientArrayElementsAfterOffset);
            if (count == 0) return;

            Debug.Assert(sourceIndex <= int.MaxValue);

            Span<byte> srcSpan = source.TryGetUnderlyingData(out byte[] srcDataArr, out int srcOffset) ? srcDataArr.AsSpan(srcOffset + (int)sourceIndex, count) : source.GetSpanForCapacity(sourceIndex);
            srcSpan.CopyTo(destination);

            GC.KeepAlive(source);
        }

#endregion (IBuffer).CopyTo extensions for copying to a (Span<Byte>)

#region (IBuffer).CopyTo extensions for copying to a (Byte [])

        public static void CopyTo(this IBuffer source, byte[] destination)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            CopyTo(source, destination.AsSpan());
        }

        public static void CopyTo(this IBuffer source, uint sourceIndex, byte[] destination, int destinationIndex, int count)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            CopyTo(source, sourceIndex, destination.AsSpan(destinationIndex, count), count);
        }

#endregion (IBuffer).CopyTo extensions for copying to a (Byte [])


#region (IBuffer).CopyTo extensions for copying to an (IBuffer)

        public static void CopyTo(this IBuffer source, IBuffer destination)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            CopyTo(source, 0, destination, 0, source.Length);
        }


        public static void CopyTo(this IBuffer source, uint sourceIndex, IBuffer destination, uint destinationIndex, uint count)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            if (source.Length < sourceIndex) throw new ArgumentException("The specified buffer index is not within the buffer length.");
            if (source.Length - sourceIndex < count) throw new ArgumentException(global::Windows.Storage.Streams.SR.Argument_InsufficientSpaceInSourceBuffer);
            if (destination.Capacity < destinationIndex) throw new ArgumentException(global::Windows.Storage.Streams.SR.Argument_BufferIndexExceedsCapacity);
            if (destination.Capacity - destinationIndex < count) throw new ArgumentException(global::Windows.Storage.Streams.SR.Argument_InsufficientSpaceInTargetBuffer);
            if (count == 0) return;

            Debug.Assert(count <= int.MaxValue);
            Debug.Assert(sourceIndex <= int.MaxValue);
            Debug.Assert(destinationIndex <= int.MaxValue);

            // If source are destination are backed by managed arrays, use the arrays instead of the pointers as it does not require pinning:
            Span<byte> srcSpan = source.TryGetUnderlyingData(out byte[] srcDataArr, out int srcOffset) ? srcDataArr.AsSpan(srcOffset + (int)sourceIndex, (int)count) : source.GetSpanForCapacity(sourceIndex);
            Span<byte> destSpan = destination.TryGetUnderlyingData(out byte[] destDataArr, out int destOffset) ? destDataArr.AsSpan(destOffset + (int)destinationIndex) : destination.GetSpanForCapacity(destinationIndex);

            srcSpan.CopyTo(destSpan);

            GC.KeepAlive(source);

            // Update Length last to make sure the data is valid
            if (destinationIndex + count > destination.Length)
            {
                destination.Length = destinationIndex + count;
            }
        }

#endregion (IBuffer).CopyTo extensions for copying to an (IBuffer)


#region Access to underlying array optimised for IBuffers backed by managed arrays (to avoid pinning)

        /// <summary>
        /// If the specified <code>IBuffer</code> is backed by a managed array, this method will return <code>true</code> and
        /// set <code>underlyingDataArray</code> to refer to that array
        /// and <code>underlyingDataArrayStartOffset</code> to the value at which the buffer data begins in that array.
        /// If the specified <code>IBuffer</code> is <em>not</em> backed by a managed array, this method will return <code>false</code>.
        /// This method is required by managed APIs that wish to use the buffer's data with other managed APIs that use
        /// arrays without a need for a memory copy.
        /// </summary>
        /// <param name="buffer">An <code>IBuffer</code>.</param>
        /// <param name="underlyingDataArray">Will be set to the data array backing <code>buffer</code> or to <code>null</code>.</param>
        /// <param name="underlyingDataArrayStartOffset">Will be set to the start offset of the buffer data in the backing array
        /// or to <code>-1</code>.</param>
        /// <returns>Whether the <code>IBuffer</code> is backed by a managed byte array.</returns>
        internal static bool TryGetUnderlyingData(this IBuffer buffer, out byte[] underlyingDataArray, out int underlyingDataArrayStartOffset)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            WindowsRuntimeBuffer winRtBuffer = buffer as WindowsRuntimeBuffer;
            if (winRtBuffer == null)
            {
                underlyingDataArray = null;
                underlyingDataArrayStartOffset = -1;
                return false;
            }

            winRtBuffer.GetUnderlyingData(out underlyingDataArray, out underlyingDataArrayStartOffset);
            return true;
        }


        /// <summary>
        /// Checks if the underlying memory backing two <code>IBuffer</code> instances is actually the same memory.
        /// When applied to <code>IBuffer</code> instances backed by managed arrays this method is preferable to a naive comparison
        /// (such as <code>((IBufferByteAccess) buffer).Buffer == ((IBufferByteAccess) otherBuffer).Buffer</code>) because it avoids
        /// pinning the backing array which would be necessary if a direct memory pointer was obtained.
        /// </summary>
        /// <param name="buffer">An <code>IBuffer</code> instance.</param>
        /// <param name="otherBuffer">An <code>IBuffer</code> instance or <code>null</code>.</param>
        /// <returns><code>true</code> if the underlying <code>Buffer</code> memory pointer is the same for both specified
        /// <code>IBuffer</code> instances (i.e. if they are backed by the same memory); <code>false</code> otherwise.</returns>
        public static bool IsSameData(this IBuffer buffer, IBuffer otherBuffer)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (otherBuffer == null)
                return false;

            if (buffer == otherBuffer)
                return true;

            byte[] thisDataArr, otherDataArr;
            int thisDataOffs, otherDataOffs;

            bool thisIsManaged = buffer.TryGetUnderlyingData(out thisDataArr, out thisDataOffs);
            bool otherIsManaged = otherBuffer.TryGetUnderlyingData(out otherDataArr, out otherDataOffs);

            if (thisIsManaged != otherIsManaged)
                return false;

            if (thisIsManaged)
                return (thisDataArr == otherDataArr) && (thisDataOffs == otherDataOffs);

            IBufferByteAccess thisBuff = buffer.As<IBufferByteAccess>();
            IBufferByteAccess otherBuff = otherBuffer.As<IBufferByteAccess>();

            unsafe
            {
                return (thisBuff.Buffer == otherBuff.Buffer);
            }
        }

#endregion Access to underlying array optimised for IBuffers backed by managed arrays (to avoid pinning)


#region Extensions for co-operation with memory streams (share mem stream data; expose data as managed/unmanaged mem stream)
        /// <summary>
        /// Creates a new <code>IBuffer</code> instance backed by the same memory as is backing the specified <code>MemoryStream</code>.
        /// The <code>MemoryStream</code> may re-sized in future, as a result the stream will be backed by a different memory region.
        /// In such case, the buffer created by this method will remain backed by the memory behind the stream at the time the buffer was created.<br />
        /// This method can throw an <code>ObjectDisposedException</code> if the specified stream is closed.<br />
        /// This method can throw an <code>UnauthorizedAccessException</code> if the specified stream cannot expose its underlying memory buffer.
        /// </summary>
        /// <param name="underlyingStream">A memory stream to share the data memory with the buffer being created.</param>
        /// <returns>A new <code>IBuffer</code> backed by the same memory as this specified stream.</returns>
        // The naming inconsistency with (Byte []).AsBuffer is intentional: as this extension method will appear on
        // MemoryStream, consistency with method names on MemoryStream is more important. There we already have an API
        // called GetBuffer which returns the underlying array.
        public static IBuffer GetWindowsRuntimeBuffer(this MemoryStream underlyingStream)
        {
            if (underlyingStream == null)
                throw new ArgumentNullException(nameof(underlyingStream));

            ArraySegment<byte> streamData;
            if (!underlyingStream.TryGetBuffer(out streamData))
            {
                throw new UnauthorizedAccessException(global::Windows.Storage.Streams.SR.UnauthorizedAccess_InternalBuffer);
            }
            return new WindowsRuntimeBuffer(streamData.Array!, (int)streamData.Offset, (int)underlyingStream.Length, underlyingStream.Capacity);
        }


        /// <summary>
        /// Creates a new <code>IBuffer</code> instance backed by the same memory as is backing the specified <code>MemoryStream</code>.
        /// The <code>MemoryStream</code> may re-sized in future, as a result the stream will be backed by a different memory region.
        /// In such case buffer created by this method will remain backed by the memory behind the stream at the time the buffer was created.<br />
        /// This method can throw an <code>ObjectDisposedException</code> if the specified stream is closed.<br />
        /// This method can throw an <code>UnauthorizedAccessException</code> if the specified stream cannot expose its underlying memory buffer.
        /// The created buffer begins at position <code>positionInStream</code> in the stream and extends over up to <code>length</code> bytes.
        /// If the stream has less than <code>length</code> bytes after the specified starting position, the created buffer covers only as many
        /// bytes as available in the stream. In either case, the <code>Length</code> and the <code>Capacity</code> properties of the created
        /// buffer are set accordingly: <code>Capacity</code> - number of bytes between <code>positionInStream</code> and the stream capacity end,
        /// but not more than <code>length</code>; <code>Length</code> - number of bytes between <code>positionInStream</code> and the stream
        /// length end, or zero if <code>positionInStream</code> is beyond stream length end, but not more than <code>length</code>.
        /// </summary>
        /// <param name="underlyingStream">A memory stream to share the data memory with the buffer being created.</param>
        /// <param name="positionInStream">The position of the shared memory region.</param>
        /// <param name="length">The maximum size of the shared memory region.</param>
        /// <returns>A new <code>IBuffer</code> backed by the same memory as this specified stream.</returns>
        public static IBuffer GetWindowsRuntimeBuffer(this MemoryStream underlyingStream, int positionInStream, int length)
        {
            // The naming inconsistency with (Byte []).AsBuffer is intentional: as this extension method will appear on
            // MemoryStream, consistency with method names on MemoryStream is more important. There we already have an API
            // called GetBuffer which returns the underlying array.

            if (underlyingStream == null)
                throw new ArgumentNullException(nameof(underlyingStream));

            if (positionInStream < 0)
                throw new ArgumentOutOfRangeException(nameof(positionInStream));

            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));

            if (underlyingStream.Length < positionInStream)
                throw new ArgumentException(global::Windows.Storage.Streams.SR.Argument_StreamPositionBeyondEOS);

            ArraySegment<byte> streamData;

            if (!underlyingStream.TryGetBuffer(out streamData))
            {
                throw new UnauthorizedAccessException(global::Windows.Storage.Streams.SR.UnauthorizedAccess_InternalBuffer);
            }

            int originInStream = streamData.Offset;
            int buffCapacity = Math.Min(length, underlyingStream.Capacity - positionInStream);
            int buffLength = Math.Max(0, Math.Min(length, ((int)underlyingStream.Length) - positionInStream));
            return new WindowsRuntimeBuffer(streamData.Array!, originInStream + positionInStream, buffLength, buffCapacity);
        }


        public static Stream AsStream(this IBuffer source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            byte[] dataArr;
            int dataOffs;
            if (source.TryGetUnderlyingData(out dataArr, out dataOffs))
            {
                Debug.Assert(source.Capacity < int.MaxValue);
                return new WindowsRuntimeBufferMemoryStream(source, dataArr, dataOffs);
            }

            unsafe
            {
                IBufferByteAccess bufferByteAccess = source.As<IBufferByteAccess>();
                return new WindowsRuntimeBufferUnmanagedMemoryStream(source, (byte*)bufferByteAccess.Buffer);
            }
        }

#endregion Extensions for co-operation with memory streams (share mem stream data; expose data as managed/unmanaged mem stream)


#region Extensions for direct by-offset access to buffer data elements

        public static byte GetByte(this IBuffer source, uint byteOffset)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (source.Length <= byteOffset) throw new ArgumentException("The specified buffer offset is not within the buffer length.");

            byte[] srcDataArr;
            int srcDataOffs;
            if (source.TryGetUnderlyingData(out srcDataArr, out srcDataOffs))
            {
                return srcDataArr[srcDataOffs + byteOffset];
            }

            IntPtr srcPtr = source.GetPointerAtOffset(byteOffset);
            unsafe
            {
                // Let's avoid an unnesecary call to Marshal.ReadByte():
                byte* ptr = (byte*)srcPtr;
                return *ptr;
            }
        }

        #endregion Extensions for direct by-offset access to buffer data elements


        #region Private plumbing

        private sealed class WindowsRuntimeBufferMemoryStream : MemoryStream
        {
            private readonly IBuffer _sourceBuffer;

            internal WindowsRuntimeBufferMemoryStream(IBuffer sourceBuffer, byte[] dataArr, int dataOffs)
                : base(dataArr, dataOffs, (int)sourceBuffer.Capacity, true)
            {
                _sourceBuffer = sourceBuffer;

                SetLength((long)sourceBuffer.Length);
            }

            public override void SetLength(long value)
            {
                base.SetLength(value);

                // Length is limited by Capacity which should be a valid value.
                // Therefore this cast is safe.
                _sourceBuffer.Length = (uint)Length;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                base.Write(buffer, offset, count);

                // Length is limited by Capacity which should be a valid value.
                // Therefore this cast is safe.
                _sourceBuffer.Length = (uint)Length;
            }

#if NET
            public override void Write(ReadOnlySpan<byte> buffer)
            {
                base.Write(buffer);

                // Length is limited by Capacity which should be a valid value.
                // Therefore this cast is safe.
                _sourceBuffer.Length = (uint)Length;
            }
#endif

            public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                await base.WriteAsync(buffer, offset, count, cancellationToken);
                // Length is limited by Capacity which should be a valid value.
                // Therefore this cast is safe.
                _sourceBuffer.Length = (uint)Length;
            }

#if NET
            public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                await base.WriteAsync(buffer, cancellationToken);

                // Length is limited by Capacity which should be a valid value.
                // Therefore this cast is safe.
                _sourceBuffer.Length = (uint)Length;
            }
#endif

            public override void WriteByte(byte value)
            {
                base.WriteByte(value);

                // Length is limited by Capacity which should be a valid value.
                // Therefore this cast is safe.
                _sourceBuffer.Length = (uint)Length;
            }
        }  // class WindowsRuntimeBufferMemoryStream

        private sealed class WindowsRuntimeBufferUnmanagedMemoryStream : UnmanagedMemoryStream
        {
            // We need this class because if we construct an UnmanagedMemoryStream on an IBuffer backed by native memory,
            // we must keep around a reference to the IBuffer from which we got the memory pointer. Otherwise the ref count
            // of the underlying COM object may drop to zero and the memory may get freed.

            private readonly IBuffer _sourceBuffer;

            internal unsafe WindowsRuntimeBufferUnmanagedMemoryStream(IBuffer sourceBuffer, byte* dataPtr)

                : base(dataPtr, (long)sourceBuffer.Length, (long)sourceBuffer.Capacity, FileAccess.ReadWrite)
            {
                _sourceBuffer = sourceBuffer;
            }

            public override void SetLength(long value)
            {
                base.SetLength(value);

                // Length is limited by Capacity which should be a valid value.
                // Therefore this cast is safe.
                _sourceBuffer.Length = (uint)Length;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                base.Write(buffer, offset, count);

                // Length is limited by Capacity which should be a valid value.
                // Therefore this cast is safe.
                _sourceBuffer.Length = (uint)Length;
            }

#if NET
            public override void Write(ReadOnlySpan<byte> buffer)
            {
                base.Write(buffer);

                // Length is limited by Capacity which should be a valid value.
                // Therefore this cast is safe.
                _sourceBuffer.Length = (uint)Length;
            }
#endif

            public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                await base.WriteAsync(buffer, offset, count, cancellationToken);
                // Length is limited by Capacity which should be a valid value.
                // Therefore this cast is safe.
                _sourceBuffer.Length = (uint)Length;
            }

#if NET
            public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                await base.WriteAsync(buffer, cancellationToken);

                // Length is limited by Capacity which should be a valid value.
                // Therefore this cast is safe.
                _sourceBuffer.Length = (uint)Length;
            }
#endif

            public override void WriteByte(byte value)
            {
                base.WriteByte(value);

                // Length is limited by Capacity which should be a valid value.
                // Therefore this cast is safe.
                _sourceBuffer.Length = (uint)Length;
            }
        }  // class WindowsRuntimeBufferUnmanagedMemoryStream

        private static IntPtr GetPointerAtOffset(this IBuffer buffer, uint offset)
        {
            Debug.Assert(0 <= offset);
            Debug.Assert(offset < buffer.Capacity);

            unsafe
            {
                IntPtr buffPtr = buffer.As<IBufferByteAccess>().Buffer;
                return new IntPtr((byte*)buffPtr + offset);
            }
        }

        private static Span<byte> GetSpanForCapacity(this IBuffer buffer, uint offset)
        {
            Debug.Assert(0 <= offset);
            Debug.Assert(offset < buffer.Capacity);

            unsafe
            {
                IntPtr buffPtr = buffer.As<IBufferByteAccess>().Buffer;
                return new Span<byte>((byte*)buffPtr + offset, (int)(buffer.Capacity - offset));
            }
        }

        private static unsafe void MemCopy(IntPtr src, IntPtr dst, uint count)
        {
            if (count > int.MaxValue)
            {
                MemCopy(src, dst, int.MaxValue);
                MemCopy(src + int.MaxValue, dst + int.MaxValue, count - int.MaxValue);
                return;
            }

            Debug.Assert(count <= int.MaxValue);
            int bCount = (int)count;


            // Copy via buffer.
            // Note: if becomes perf critical, we will port the routine that
            // copies the data without using Marshal (and byte[])
            byte[] tmp = new byte[bCount];
            Marshal.Copy(src, tmp, 0, bCount);
            Marshal.Copy(tmp, 0, dst, bCount);
            return;
        }
#endregion Private plumbing
    }  // class WindowsRuntimeBufferExtensions
}  // namespace

// WindowsRuntimeBufferExtensions.cs
