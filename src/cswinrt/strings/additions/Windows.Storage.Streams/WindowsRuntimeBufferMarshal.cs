// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Runtime.InteropServices.WindowsRuntime
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using global::Windows.Foundation;
    using global::Windows.Storage.Streams;

#nullable enable
    /// <summary>
    /// An unsafe class that provides a set of methods to access the underlying data representations of WinRT types.
    /// </summary>
    public static partial class WindowsRuntimeBufferMarshal
    {
        /// <summary>
        /// Returns a pointer to the underlying data representation of the <see cref="IBuffer"/>.
        /// Callers are responsible for ensuring that the buffer is kept alive while the pointer is in use.
        /// </summary>
        /// <param name="buffer">The buffer to get the data pointer for.</param>
        /// <param name="dataPtr">The pointer to the underlying data representation of the buffer.</param>
        /// <returns>Whether the data was successfully retrieved.</returns>
        /// <exception cref="Exception">Thrown if invoking <c>IBufferByteAccess::Buffer</c> on the input buffer fails.</exception>
        public static unsafe bool TryGetDataUnsafe(
            [NotNullWhen(true)]
            IBuffer? buffer,
            out byte* dataPtr)
        {
            if (buffer == null)
            {
                dataPtr = null;
                return false;
            }

            if (global::WindowsRuntime.InteropServices.WindowsRuntimeMarshal.TryUnwrapObjectReference(buffer, out WindowsRuntimeObjectReference? unwrapped) &&
                unwrapped.TryAsUnsafe(global::ABI.Windows.Storage.Streams.WellKnownStreamInterfaceIIDs.IID_IBufferByteAccess, out nint thisPtr))
            {
                try
                {
                    byte* __retval = null;
                    RestrictedErrorInfo.ThrowExceptionForHR(global::ABI.Windows.Storage.Streams.IBufferByteAccessVftbl.GetBufferUnsafe((void*)thisPtr, &__retval));
                    dataPtr = __retval;
                    return true;
                }
                finally
                {
                    Marshal.Release(thisPtr);
                }
            }

            if (buffer is IBufferByteAccess managedBuffer)
            {
                dataPtr = managedBuffer.Buffer;
                return true;
            }

            dataPtr = null;
            return false;
        }

        /// <summary>
        /// Returns a pointer to the underlying data representation of the <see cref="IMemoryBufferReference"/>.
        /// Callers are responsible for ensuring that the buffer is kept alive while the pointer is in use.
        /// </summary>
        /// <param name="buffer">The buffer to get the data pointer for.</param>
        /// <param name="dataPtr">The pointer to the underlying data representation of the buffer.</param>
        /// <param name="capacity">The capacity of the buffer.</param>
        /// <returns>Whether the data was successfully retrieved.</returns>
        /// <exception cref="Exception">Thrown if invoking <c>IMemoryBufferByteAccess::Buffer</c> on the input buffer fails.</exception>
        public static unsafe bool TryGetDataUnsafe(
            [NotNullWhen(true)]
            IMemoryBufferReference? buffer,
            out byte* dataPtr,
            out uint capacity)
        {
            if (buffer == null)
            {
                dataPtr = null;
                capacity = 0;
                return false;
            }

            if (global::WindowsRuntime.InteropServices.WindowsRuntimeMarshal.TryUnwrapObjectReference(buffer, out WindowsRuntimeObjectReference? unwrapped) &&
                unwrapped.TryAsUnsafe(global::ABI.Windows.Storage.Streams.WellKnownStreamInterfaceIIDs.IID_IMemoryBufferByteAccess, out nint thisPtr))
            {
                try
                {
                    byte* __retval = null;
                    uint __capacity = 0;
                    RestrictedErrorInfo.ThrowExceptionForHR(global::ABI.Windows.Storage.Streams.IMemoryBufferByteAccessVftbl.GetBufferUnsafe((void*)thisPtr, &__retval, &__capacity));
                    dataPtr = __retval;
                    capacity = __capacity;
                    return true;
                }
                finally
                {
                    Marshal.Release(thisPtr);
                }
            }

            dataPtr = null;
            capacity = 0;
            return false;
        }

        /// <summary>
        /// Tries to get an array segment from the underlying buffer. The return value indicates the success of the operation.
        /// </summary>
        /// <param name="buffer">The buffer to get the array segment for.</param>
        /// <param name="array">When this method returns, contains the array segment retrieved from the underlying  buffer. If the method fails, the method returns a default array segment.</param>
        public static bool TryGetArray(
            [NotNullWhen(true)]
            IBuffer? buffer,
            out ArraySegment<byte> array)
        {
            if (buffer == null)
            {
                array = default;
                return false;
            }

            // If buffer is backed by a managed array, return it
            if (buffer.TryGetUnderlyingData(out byte[]? srcDataArr, out int srcDataOffs))
            {
                array = new ArraySegment<byte>(srcDataArr, offset: srcDataOffs, count: (int)buffer.Length);
                return true;
            }

            array = default;
            return false;
        }
    }  // class WindowsRuntimeMarshal
#nullable restore
}  // namespace

// WindowsRuntimeMarshal.cs
