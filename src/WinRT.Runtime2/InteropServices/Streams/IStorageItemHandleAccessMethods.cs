// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.IO;
using Microsoft.Win32.SafeHandles;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Provides methods for interacting with the <c>IStorageItemHandleAccess</c> COM interface.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/windowsstoragecom/nn-windowsstoragecom-istorageitemhandleaccess"/>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IStorageItemHandleAccessMethods
{
    /// <summary>
    /// Creates a <see cref="SafeFileHandle"/> for the specified storage item.
    /// </summary>
    /// <param name="storageItem">The storage item to create the handle for.</param>
    /// <param name="access">The file access mode for the handle.</param>
    /// <param name="share">The file share mode for the handle.</param>
    /// <param name="options">The file options for the handle.</param>
    /// <returns>A <see cref="SafeFileHandle"/> for the storage item, or <see langword="null"/> if the operation failed.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="access"/> is not a valid value.</exception>
    /// <exception cref="NotSupportedException">Thrown if <paramref name="share"/> or <paramref name="options"/> contain unsupported flags.</exception>
    public static SafeFileHandle? Create(
        object storageItem,
        FileAccess access,
        FileShare share,
        FileOptions options)
    {
        return Create(
            storageItem: storageItem,
            accessOptions: access.ToHandleAccessOptions(),
            sharingOptions: share.ToHandleSharingOptions(),
            options: options.ToHandleOptions(),
            oplockBreakingHandler: null);
    }

    /// <summary>
    /// Creates a <see cref="SafeFileHandle"/> for the specified storage item.
    /// </summary>
    /// <param name="storageItem">The storage item to create the handle for.</param>
    /// <param name="accessOptions">The access options for the handle.</param>
    /// <param name="sharingOptions">The sharing options for the handle.</param>
    /// <param name="options">The handle options.</param>
    /// <param name="oplockBreakingHandler">The oplock breaking handler.</param>
    /// <returns>A <see cref="SafeFileHandle"/> for the storage item, or <see langword="null"/> if the operation failed.</returns>
    private static SafeFileHandle? Create(
        object storageItem,
        HANDLE_ACCESS_OPTIONS accessOptions,
        HANDLE_SHARING_OPTIONS sharingOptions,
        HANDLE_OPTIONS options,
        void* oplockBreakingHandler)
    {
        if (!WindowsRuntimeComWrappersMarshal.TryUnwrapObjectReference(storageItem, out WindowsRuntimeObjectReference? objectReference))
        {
            return null;
        }

        if (!objectReference.TryAsUnsafe(in WellKnownWindowsInterfaceIIDs.IID_IStorageItemHandleAccess, out void* thisPtr))
        {
            return null;
        }

        HANDLE interopHandle;

        try
        {
            HRESULT hresult = IStorageItemHandleAccessVftbl.CreateUnsafe(
                thisPtr,
                accessOptions,
                sharingOptions,
                options,
                oplockBreakingHandler,
                &interopHandle);

            RestrictedErrorInfo.ThrowExceptionForHR(hresult);
        }
        finally
        {
            _ = IUnknownVftbl.ReleaseUnsafe(thisPtr);
        }

        return new(interopHandle, ownsHandle: true);
    }
}
