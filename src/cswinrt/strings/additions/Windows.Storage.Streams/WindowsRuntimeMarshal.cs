// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Runtime.InteropServices.WindowsRuntime
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using global::Windows.Foundation;
    using global::Windows.Storage.Streams;
    using WinRT;

#nullable enable
    /// <summary>
    /// An unsafe class that provides a set of methods to access the underlying data representations of WinRT types.
    /// </summary>
#if EMBED || !NET // for netstandard this type conflicts with the type in the BCL so make it internal
    internal
#else
    public
#endif
    static partial class WindowsRuntimeMarshal
    {
        /// <summary>
        /// Returns a pointer to the underlying data representation of the <see cref="IBuffer"/>.
        /// Callers are responsible for ensuring that the buffer is kept alive while the pointer is in use.
        /// </summary>
        /// <param name="buffer">The buffer to get the data pointer for.</param>
        /// <param name="dataPtr">The pointer to the underlying data representation of the buffer.</param>
        public static unsafe bool TryGetDataUnsafe(
#if NET
            [NotNullWhen(true)]
#endif
            IBuffer? buffer, out IntPtr dataPtr)
        {
            if (buffer == null)
            {
                dataPtr = IntPtr.Zero;
                return false;
            }

            if (ComWrappersSupport.TryUnwrapObject(buffer, out var unwrapped) &&
                unwrapped.TryAs(global::WinRT.Interop.IID.IID_IBufferByteAccess, out IntPtr ThisPtr) >= 0)
            {
                try
                {
                    IntPtr __retval = default;
                    global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>**)ThisPtr)[3](ThisPtr, &__retval));
                    dataPtr = __retval;
                    return true;
                }
                finally
                {
                    Marshal.Release(ThisPtr);
                }
            }

            if (buffer is IBufferByteAccess managedBuffer)
            {
                dataPtr = managedBuffer.Buffer;
                return true;
            }

            dataPtr = IntPtr.Zero;
            return false;
        }

        /// <summary>
        /// Tries to get an array segment from the underlying buffer. The return value indicates the success of the operation.
        /// </summary>
        /// <param name="buffer">The buffer to get the array segment for.</param>
        /// <param name="dataPtr">When this method returns, contains the array segment retrieved from the underlying  buffer. If the method fails, the method returns a default array segment.</param>
        public static bool TryGetArray(
#if NET
            [NotNullWhen(true)]
# endif
            IBuffer source,
#if NET
            [NotNullWhen(true)]
# endif
            out ArraySegment<byte> array)
        {
            // If source is backed by a managed array, return it
            byte[] srcDataArr;
            int srcDataOffs;
            if (source.TryGetUnderlyingData(out srcDataArr, out srcDataOffs))
            {
                array = new ArraySegment<byte>(srcDataArr, offset: srcDataOffs, count: (int)source.Length);
                return true;
            }

            array = default;
            return false;
        }
    }  // class WindowsRuntimeMarshal
#nullable restore
}  // namespace

// WindowsRuntimeMarshal.cs
