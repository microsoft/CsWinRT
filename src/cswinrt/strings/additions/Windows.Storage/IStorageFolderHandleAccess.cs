// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace ABI.Windows.Storage
{
    using global::Microsoft.Win32.SafeHandles;
    using global::System;

    // Available in 14393 (RS1) and later
    internal static class IStorageFolderHandleAccessMethods
    {
        private static ref readonly Guid IID_IStorageFolderHandleAccess
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ReadOnlySpan<byte> data =
                [
                    0x8F, 0x93, 0x19, 0xDF, 0x62, 0x54, 0xA0, 0x48, 0xBE, 0x65, 0xD2, 0xA3, 0x27, 0x1A, 0x08, 0xD6
                ];
                return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
            }
        }

        public static unsafe SafeFileHandle Create(
            global::Windows.Storage.IStorageFolder storageFolder,
            string fileName,
            global::Windows.Storage.HANDLE_CREATION_OPTIONS creationOptions,
            global::Windows.Storage.HANDLE_ACCESS_OPTIONS accessOptions,
            global::Windows.Storage.HANDLE_SHARING_OPTIONS sharingOptions,
            global::Windows.Storage.HANDLE_OPTIONS options,
            IntPtr oplockBreakingHandler)
        {
            if (global::WindowsRuntime.InteropServices.WindowsRuntimeMarshal.TryUnwrapObjectReference(storageFolder, out WindowsRuntimeObjectReference unwrapped) &&
                unwrapped.TryAsUnsafe(IID_IStorageFolderHandleAccess, out void* thisPtr))
            {
                SafeFileHandle interopHandle = default;
                IntPtr _interopHandle = default;
                try
                {
                    fixed (char* fileNamePtr = fileName)
                    {
                        RestrictedErrorInfo.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<void*, IntPtr, global::Windows.Storage.HANDLE_CREATION_OPTIONS, global::Windows.Storage.HANDLE_ACCESS_OPTIONS, global::Windows.Storage.HANDLE_SHARING_OPTIONS, global::Windows.Storage.HANDLE_OPTIONS, IntPtr, IntPtr*, int>**)thisPtr)[3]
                            (thisPtr, (IntPtr)fileNamePtr, creationOptions, accessOptions, sharingOptions, options, oplockBreakingHandler, &_interopHandle));
                    }
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
