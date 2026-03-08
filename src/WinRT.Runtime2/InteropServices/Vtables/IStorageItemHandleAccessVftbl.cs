// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Binding type for the <c>IStorageItemHandleAccess</c> interface vtable.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/windowsstoragecom/nn-windowsstoragecom-istorageitemhandleaccess"/>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct IStorageItemHandleAccessVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, HANDLE_ACCESS_OPTIONS, HANDLE_SHARING_OPTIONS, HANDLE_OPTIONS, void*, HANDLE*, HRESULT> Create;

    /// <summary>
    /// Creates a handle for a random access stream for the storage item.
    /// </summary>
    /// <param name="thisPtr">The target COM object.</param>
    /// <param name="accessOptions">The access options for the handle.</param>
    /// <param name="sharingOptions">The sharing options for the handle.</param>
    /// <param name="options">The handle options.</param>
    /// <param name="oplockBreakingHandler">The oplock breaking handler.</param>
    /// <param name="interopHandle">The resulting file handle.</param>
    /// <returns>If this method succeeds, it returns <c>S_OK</c>. Otherwise, it returns an <c>HRESULT</c> error code.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HRESULT CreateUnsafe(
        void* thisPtr,
        HANDLE_ACCESS_OPTIONS accessOptions,
        HANDLE_SHARING_OPTIONS sharingOptions,
        HANDLE_OPTIONS options,
        void* oplockBreakingHandler,
        HANDLE* interopHandle)
    {
        return ((IStorageItemHandleAccessVftbl*)*(void***)thisPtr)->Create(
            thisPtr,
            accessOptions,
            sharingOptions,
            options,
            oplockBreakingHandler,
            interopHandle);
    }
}
