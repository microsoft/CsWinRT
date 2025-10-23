// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
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
    public delegate* unmanaged[MemberFunction]<void*, void**, HRESULT> GetPreviousLanguageExceptionErrorInfo;
    public delegate* unmanaged[MemberFunction]<void*, void*, HRESULT> CapturePropagationContext;
    public delegate* unmanaged[MemberFunction]<void*, void**, HRESULT> GetPropagationContextHead;

    public static void* GetPreviousLanguageExceptionErrorInfoUnsafe(void* thisPtr)
    {
        void* __retval = default;
        Marshal.ThrowExceptionForHR(((ILanguageExceptionErrorInfo2Vftbl*)*(void***)thisPtr)->GetPreviousLanguageExceptionErrorInfo(
            thisPtr,
            &__retval));
        return __retval;
    }

    public static unsafe void* GetPropagationContextHeadUnsafe(void* thisPtr)
    {
        void* __retval = default;
        Marshal.ThrowExceptionForHR(((ILanguageExceptionErrorInfo2Vftbl*)*(void***)thisPtr)->GetPropagationContextHead(
            thisPtr,
            &__retval));
        return __retval;
    }
}