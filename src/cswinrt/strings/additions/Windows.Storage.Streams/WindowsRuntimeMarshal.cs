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
    /// Contains extension methods that expose operations on WinRT <code>Windows.Foundation.IBuffer</code>.
    /// </summary>
#if EMBED
    internal
#else
    public
#endif
    static partial class WindowsRuntimeMarshal
    {
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
