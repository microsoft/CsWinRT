// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using Microsoft.Win32.SafeHandles;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Provides methods for interacting with the <c>IStorageFolderHandleAccess</c> COM interface.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/windowsstoragecom/nn-windowsstoragecom-istoragefolderhandleaccess"/>
[WindowsRuntimeImplementationOnlyMember]
public static unsafe class IStorageFolderHandleAccessMethods
{
    /// <summary>
    /// Creates a <see cref="SafeFileHandle"/> for a file within the specified storage folder.
    /// </summary>
    /// <param name="storageFolder">The storage folder to create the handle in.</param>
    /// <param name="fileName">The name of the file to create the handle for.</param>
    /// <param name="mode">The file mode for the handle.</param>
    /// <param name="access">The file access mode for the handle.</param>
    /// <param name="share">The file share mode for the handle.</param>
    /// <param name="options">The file options for the handle.</param>
    /// <returns>A <see cref="SafeFileHandle"/> for the file, or <see langword="null"/> if the operation failed.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="mode"/> or <paramref name="access"/> are not valid values.</exception>
    /// <exception cref="NotSupportedException">Thrown if <paramref name="share"/> or <paramref name="options"/> contain unsupported flags.</exception>
    public static SafeFileHandle? Create(
        object storageFolder,
        string fileName,
        FileMode mode,
        FileAccess access,
        FileShare share,
        FileOptions options)
    {
        return Create(
            storageFolder: storageFolder,
            fileName: fileName,
            creationOptions: mode.ToHandleCreationOptions(),
            accessOptions: access.ToHandleAccessOptions(),
            sharingOptions: share.ToHandleSharingOptions(),
            options: options.ToHandleOptions(),
            oplockBreakingHandler: null);
    }

    /// <summary>
    /// Creates a <see cref="SafeFileHandle"/> for a file within the specified storage folder.
    /// </summary>
    /// <param name="storageFolder">The storage folder to create the handle in.</param>
    /// <param name="fileName">The name of the file to create the handle for.</param>
    /// <param name="creationOptions">The creation options for the handle.</param>
    /// <param name="accessOptions">The access options for the handle.</param>
    /// <param name="sharingOptions">The sharing options for the handle.</param>
    /// <param name="options">The handle options.</param>
    /// <param name="oplockBreakingHandler">The oplock breaking handler.</param>
    /// <returns>A <see cref="SafeFileHandle"/> for the file, or <see langword="null"/> if the operation failed.</returns>
    private static SafeFileHandle? Create(
        object storageFolder,
        string fileName,
        HANDLE_CREATION_OPTIONS creationOptions,
        HANDLE_ACCESS_OPTIONS accessOptions,
        HANDLE_SHARING_OPTIONS sharingOptions,
        HANDLE_OPTIONS options,
        void* oplockBreakingHandler)
    {
        if (!WindowsRuntimeComWrappersMarshal.TryUnwrapObjectReference(storageFolder, out WindowsRuntimeObjectReference? objectReference))
        {
            return null;
        }

        if (!objectReference.TryAsUnsafe(in WellKnownWindowsInterfaceIIDs.IID_IStorageFolderHandleAccess, out void* thisPtr))
        {
            return null;
        }

        HANDLE interopHandle;

        try
        {
            fixed (char* fileNamePtr = fileName)
            {
                HRESULT hresult = IStorageFolderHandleAccessVftbl.CreateUnsafe(
                    thisPtr,
                    fileNamePtr,
                    creationOptions,
                    accessOptions,
                    sharingOptions,
                    options,
                    oplockBreakingHandler,
                    &interopHandle);

                RestrictedErrorInfo.ThrowExceptionForHR(hresult);
            }
        }
        finally
        {
            _ = IUnknownVftbl.ReleaseUnsafe(thisPtr);
        }

        return new(interopHandle, ownsHandle: true);
    }
}
