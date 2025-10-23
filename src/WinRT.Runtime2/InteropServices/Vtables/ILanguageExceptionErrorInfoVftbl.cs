// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Enables retrieving the IUnknown pointer stored in the error info with the call to RoOriginateLanguageException.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/restrictederrorinfo/nn-restrictederrorinfo-ilanguageexceptionerrorinfo"/>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct ILanguageExceptionErrorInfoVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, void**, HRESULT> GetLanguageException;

    /// <summary>
    /// Enables retrieving the IUnknown pointer stored in the error info with the call to RoOriginateLanguageException.
    /// </summary>
    /// <param name="thisPtr">The target COM object.</param>
    /// <returns>The <c>HRESULT</c> for the operation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void* GetLanguageExceptionUnsafe(void* thisPtr)
    {
        void* __return_value__ = null;
        Marshal.ThrowExceptionForHR(((ILanguageExceptionErrorInfoVftbl*)*(void***)thisPtr)->GetLanguageException(
            thisPtr,
            &__return_value__));
        return __return_value__;
    }
}