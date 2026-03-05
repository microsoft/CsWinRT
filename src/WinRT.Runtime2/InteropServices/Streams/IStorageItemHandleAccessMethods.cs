// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
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
    /// <param name="accessOptions">The access options for the handle.</param>
    /// <param name="sharingOptions">The sharing options for the handle.</param>
    /// <param name="options">The handle options.</param>
    /// <param name="oplockBreakingHandler">The oplock breaking handler.</param>
    /// <returns>A <see cref="SafeFileHandle"/> for the storage item, or <see langword="null"/> if the operation failed.</returns>
    public static SafeFileHandle? Create(
        WindowsRuntimeObject storageItem,
        uint accessOptions,
        uint sharingOptions,
        uint options,
        nint oplockBreakingHandler)
    {
        if (!storageItem.HasUnwrappableNativeObjectReference)
        {
            return null;
        }

        if (!storageItem.NativeObjectReference.TryAsUnsafe(in WellKnownWindowsInterfaceIIDs.IID_IStorageItemHandleAccess, out void* thisPtr))
        {
            return null;
        }

        nint interopHandle = 0;

        try
        {
            RestrictedErrorInfo.ThrowExceptionForHR(IStorageItemHandleAccessVftbl.CreateUnsafe(
                thisPtr,
                accessOptions,
                sharingOptions,
                options,
                oplockBreakingHandler,
                &interopHandle));
        }
        finally
        {
            _ = IUnknownVftbl.ReleaseUnsafe(thisPtr);
        }

        return new SafeFileHandle(interopHandle, ownsHandle: true);
    }
}
