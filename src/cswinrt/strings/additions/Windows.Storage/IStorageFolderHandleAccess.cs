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
#if NET
        public static global::System.Guid IID { get; } = new(new global::System.ReadOnlySpan<byte>(new byte[] { 0x8F, 0x93, 0x19, 0xDF, 0x62, 0x54, 0xA0, 0x48, 0xBE, 0x65, 0xD2, 0xA3, 0x27, 0x1A, 0x08, 0xD6 }));
#else
        public static global::System.Guid IID { get; } = new(0xDF19938F, 0x5462, 0x48A0, 0xBE, 0x65, 0xD2, 0xA3, 0x27, 0x1A, 0x08, 0xD6);
#endif

        public static unsafe SafeFileHandle Create(
            global::Windows.Storage.IStorageFolder storageFolder,
            string fileName,
            global::Windows.Storage.HANDLE_CREATION_OPTIONS creationOptions,
            global::Windows.Storage.HANDLE_ACCESS_OPTIONS accessOptions,
            global::Windows.Storage.HANDLE_SHARING_OPTIONS sharingOptions,
            global::Windows.Storage.HANDLE_OPTIONS options,
            IntPtr oplockBreakingHandler)
        {
            global::WinRT.ObjectReferenceValue obj = default;
            try
            {
                obj = global::WinRT.MarshalInspectable<object>.CreateMarshaler2(storageFolder, IID);
            }
            catch (Exception)
            {
                return null;
            }

            SafeFileHandle interopHandle = default;
            IntPtr _interopHandle = default;
            try
            {
                var ThisPtr = obj.GetAbi();
                fixed (char* fileNamePtr = fileName)
                {
                    ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr, global::Windows.Storage.HANDLE_CREATION_OPTIONS, global::Windows.Storage.HANDLE_ACCESS_OPTIONS, global::Windows.Storage.HANDLE_SHARING_OPTIONS, global::Windows.Storage.HANDLE_OPTIONS, IntPtr, IntPtr*, int>**)ThisPtr)[3]
                        (ThisPtr, (IntPtr)fileNamePtr, creationOptions, accessOptions, sharingOptions, options, oplockBreakingHandler, &_interopHandle));
                }
            }
            finally
            {
                interopHandle = new SafeFileHandle(_interopHandle, true);
                obj.Dispose();
            }
            return interopHandle;
        }
    }
}
