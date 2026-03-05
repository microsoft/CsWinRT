// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.IO;
using Microsoft.Win32.SafeHandles;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Provides methods for interacting with the <c>IStorageFolderHandleAccess</c> COM interface.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/windowsstoragecom/nn-windowsstoragecom-istoragefolderhandleaccess"/>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
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
        WindowsRuntimeObject storageFolder,
        string fileName,
        FileMode mode,
        FileAccess access,
        FileShare share,
        FileOptions options)
    {
        return Create(
            storageFolder,
            fileName,
            WindowsRuntimeStorageHelpers.FileModeToHandleCreationOptions(mode),
            WindowsRuntimeStorageHelpers.FileAccessToHandleAccessOptions(access),
            WindowsRuntimeStorageHelpers.FileShareToHandleSharingOptions(share),
            WindowsRuntimeStorageHelpers.FileOptionsToHandleOptions(options),
            0);
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
        WindowsRuntimeObject storageFolder,
        string fileName,
        uint creationOptions,
        uint accessOptions,
        uint sharingOptions,
        uint options,
        nint oplockBreakingHandler)
    {
        if (!storageFolder.HasUnwrappableNativeObjectReference)
        {
            return null;
        }

        if (!storageFolder.NativeObjectReference.TryAsUnsafe(in WellKnownWindowsInterfaceIIDs.IID_IStorageFolderHandleAccess, out void* thisPtr))
        {
            return null;
        }

        nint interopHandle = 0;

        try
        {
            fixed (char* fileNamePtr = fileName)
            {
                RestrictedErrorInfo.ThrowExceptionForHR(IStorageFolderHandleAccessVftbl.CreateUnsafe(
                    thisPtr,
                    (nint)fileNamePtr,
                    creationOptions,
                    accessOptions,
                    sharingOptions,
                    options,
                    oplockBreakingHandler,
                    &interopHandle));
            }
        }
        finally
        {
            _ = IUnknownVftbl.ReleaseUnsafe(thisPtr);
        }

        return new SafeFileHandle(interopHandle, ownsHandle: true);
    }
}
