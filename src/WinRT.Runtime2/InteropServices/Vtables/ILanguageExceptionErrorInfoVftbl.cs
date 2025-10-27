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
    /// Gets the stored IUnknown object from the error object.
    /// </summary>
    /// <param name="thisPtr">The target COM object.</param>
    /// <param name="languageException">The stored IUnknown object from the error object.</param>
    /// <returns>The <c>HRESULT</c> for the operation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HRESULT GetLanguageExceptionUnsafe(void* thisPtr, void** languageException)
    {
        return ((ILanguageExceptionErrorInfoVftbl*)*(void***)thisPtr)->GetLanguageException(
            thisPtr,
            languageException);
    }
}