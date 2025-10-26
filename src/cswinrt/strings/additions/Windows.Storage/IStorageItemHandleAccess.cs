// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace ABI.Windows.Storage
{
    using global::Microsoft.Win32.SafeHandles;

    // Available in 14393 (RS1) and later
    internal static class IStorageItemHandleAccessMethods
    {
        private static ref readonly Guid IID_IStorageItemHandleAccess
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ReadOnlySpan<byte> data =
                [
                    0xB2, 0x96, 0xA2, 0x5C, 0x25, 0x2C, 0x22, 0x4D, 0xB7, 0x85, 0xB8, 0x85, 0xC8, 0x20, 0x1E, 0x6A
                ];
                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        public static unsafe SafeFileHandle Create(
            global::Windows.Storage.IStorageFile storageFile,
            global::Windows.Storage.HANDLE_ACCESS_OPTIONS accessOptions,
            global::Windows.Storage.HANDLE_SHARING_OPTIONS sharingOptions,
            global::Windows.Storage.HANDLE_OPTIONS options,
            IntPtr oplockBreakingHandler)
        {
            if (global::WindowsRuntime.InteropServices.WindowsRuntimeMarshal.TryUnwrapObjectReference(storageFile, out WindowsRuntimeObjectReference unwrapped) &&
                unwrapped.TryAsUnsafe(IID_IStorageItemHandleAccess, out void* thisPtr))
            {
                SafeFileHandle interopHandle = default;
                IntPtr _interopHandle = default;
                try
                {
                    RestrictedErrorInfo.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<void*, global::Windows.Storage.HANDLE_ACCESS_OPTIONS, global::Windows.Storage.HANDLE_SHARING_OPTIONS, global::Windows.Storage.HANDLE_OPTIONS, IntPtr, IntPtr*, int>**)thisPtr)[3]
                        (thisPtr, accessOptions, sharingOptions, options, oplockBreakingHandler, &_interopHandle));
                }
                finally
                {
                    interopHandle = new SafeFileHandle(_interopHandle, true);
                    global::WindowsRuntime.InteropServices.Marshalling.WindowsRuntimeObjectMarshaller.Free(thisPtr);
                }
                return interopHandle;
            }
            return null;
        }
    }
}
