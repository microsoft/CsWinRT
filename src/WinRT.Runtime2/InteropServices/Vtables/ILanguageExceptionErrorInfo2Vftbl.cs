// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Enables language projections to provide and retrieve error information as with ILanguageExceptionErrorInfo, with the additional benefit of working across language boundaries.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/restrictederrorinfo/nn-restrictederrorinfo-ilanguageexceptionerrorinfo2"/>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct ILanguageExceptionErrorInfo2Vftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, void**, HRESULT> GetLanguageException;
    public delegate* unmanaged[MemberFunction]<void*, void**, HRESULT> GetPreviousLanguageExceptionErrorInfo;
    public delegate* unmanaged[MemberFunction]<void*, void*, HRESULT> CapturePropagationContext;
    public delegate* unmanaged[MemberFunction]<void*, void**, HRESULT> GetPropagationContextHead;

    /// <summary>
    /// Retrieves the previous language exception error information object
    /// </summary>
    /// <param name="thisPtr">The target COM object.</param>
    /// <param name="previousLanguageExceptionErrorInfo">
    /// Pointer to an ILanguageExceptionErrorInfo2 object that contains the previous error information object.
    /// </param>
    /// <returns>The <c>HRESULT</c> for the operation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]

    public static HRESULT GetPreviousLanguageExceptionErrorInfoUnsafe(void* thisPtr, void** previousLanguageExceptionErrorInfo)
    {
        return ((ILanguageExceptionErrorInfo2Vftbl*)*(void***)thisPtr)->GetPreviousLanguageExceptionErrorInfo(
            thisPtr,
            previousLanguageExceptionErrorInfo);
    }

    /// <summary>
    /// Retrieves the propagation context head.
    /// </summary>
    /// <param name="thisPtr">The target COM object.</param>
    /// <param name="propagatedLanguageExceptionErrorInfoHead">
    /// On success, returns an ILanguageExceptionErrorInfo2 object that represents the head of the propagation context.
    /// </param>
    /// <returns>The <c>HRESULT</c> for the operation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]

    public static HRESULT GetPropagationContextHeadUnsafe(void* thisPtr, void** propagatedLanguageExceptionErrorInfoHead)
    {
        return ((ILanguageExceptionErrorInfo2Vftbl*)*(void***)thisPtr)->GetPropagationContextHead(
            thisPtr,
            propagatedLanguageExceptionErrorInfoHead);
    }
}