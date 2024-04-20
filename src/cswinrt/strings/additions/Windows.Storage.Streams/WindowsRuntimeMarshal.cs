// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Runtime.InteropServices.WindowsRuntime
{
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using global::Windows.Foundation;
    using global::Windows.Storage.Streams;
    using WinRT;
    /// <summary>
    /// An unsafe class that provides a set of methods to access the underlying data representations of WinRT types.
    /// </summary>
#if EMBED
    internal
#else
    public
#endif
    static partial class WindowsRuntimeMarshal
    {
#if NET
        private static global::System.Guid IID { get; } = new Guid(new global::System.ReadOnlySpan<byte>(new byte[] { 0xEF, 0x0F, 0x5A, 0x90, 0x53, 0xBC, 0xDF, 0x11, 0x8C, 0x49, 0x00, 0x1E, 0x4F, 0xC6, 0x86, 0xDA }));
#else
        private static global::System.Guid IID { get; } = new Guid(new byte[] { 0xEF, 0x0F, 0x5A, 0x90, 0x53, 0xBC, 0xDF, 0x11, 0x8C, 0x49, 0x00, 0x1E, 0x4F, 0xC6, 0x86, 0xDA });
#endif
        /// <summary>
        /// Returns a pointer to the underlying data representation of the <see cref="IBuffer"/>.
        /// Callers are responsible for ensuring that the buffer is kept alive while the pointer is in use.
        /// </summary>
        /// <param name="buffer">The buffer to get the data pointer for.</param>
        /// <param name="dataPtr">The pointer to the underlying data representation of the buffer.</param>
        public static unsafe bool TryGetDataUnsafe(IBuffer buffer, out IntPtr dataPtr)
        {
            if (buffer == null)
            {
                dataPtr = IntPtr.Zero;
                return false;
            }

            if (ComWrappersSupport.TryUnwrapObject(buffer, out var unwrapped) &&
                unwrapped.TryAs(global::ABI.Windows.Storage.Streams.IBufferByteAccessMethods.IID, out IntPtr ThisPtr) >= 0)
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
    }  // class WindowsRuntimeMarshal
}  // namespace

// WindowsRuntimeMarshal.cs
