// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace ABI.Windows.Storage
{
    using global::Microsoft.Win32.SafeHandles;

    // Available in 14393 (RS1) and later
    internal static class IStorageItemHandleAccessMethods
    {
#if NET
        public static global::System.Guid IID { get; } = new(new global::System.ReadOnlySpan<byte>(new byte[] { 0xB2, 0x96, 0xA2, 0x5C, 0x25, 0x2C, 0x22, 0x4D, 0xB7, 0x85, 0xB8, 0x85, 0xC8, 0x20, 0x1E, 0x6A }));
#else
        public static global::System.Guid IID { get; } = new(0x5CA296B2, 0x2C25, 0x4D22, 0xB7, 0x85, 0xB8, 0x85, 0xC8, 0x20, 0x1E, 0x6A);
#endif

        public static unsafe SafeFileHandle Create(
            global::Windows.Storage.IStorageFile storageFile,
            global::Windows.Storage.HANDLE_ACCESS_OPTIONS accessOptions,
            global::Windows.Storage.HANDLE_SHARING_OPTIONS sharingOptions,
            global::Windows.Storage.HANDLE_OPTIONS options,
            IntPtr oplockBreakingHandler)
        {
            global::WinRT.ObjectReferenceValue obj = default;
            try
            {
                obj = global::WinRT.MarshalInspectable<object>.CreateMarshaler2(storageFile, IID);
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
                ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, global::Windows.Storage.HANDLE_ACCESS_OPTIONS, global::Windows.Storage.HANDLE_SHARING_OPTIONS, global::Windows.Storage.HANDLE_OPTIONS, IntPtr, IntPtr*, int>**)ThisPtr)[3]
                    (ThisPtr, accessOptions, sharingOptions, options, oplockBreakingHandler, &_interopHandle));
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
