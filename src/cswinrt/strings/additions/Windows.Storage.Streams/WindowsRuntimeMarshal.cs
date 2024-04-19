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
        public static bool TryGetDataUnsafe(IBuffer buffer, out IntPtr dataPtr)
        {
            if (buffer == null)
            {
                dataPtr = IntPtr.Zero;
                return false;
            }

            if (ComWrappersSupport.TryUnwrapObject(buffer, out var unwrapped) &&
                unwrapped.TryAs<IUnknownVftbl>(global::ABI.Windows.Storage.Streams.IBufferByteAccessMethods.IID, out var objRef) >= 0)
            {
                using (objRef)
                {
                    dataPtr = global::ABI.Windows.Storage.Streams.IBufferByteAccessMethods.get_Buffer(objRef);
                    return true;
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
