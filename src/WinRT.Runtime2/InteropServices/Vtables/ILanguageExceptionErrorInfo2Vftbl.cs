// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Binding type for the <c>IBindableVectorView</c> interface vtable.
/// </summary>
/// <remarks>
/// This interface is equivalent to <see href="https://learn.microsoft.com/en-us/windows/win32/api/restrictederrorinfo/nn-restrictederrorinfo-ilanguageexceptionerrorinfo2"/>.
/// </remarks>
/// <see href="https://learn.microsoft.com/en-us/windows/win32/api/restrictederrorinfo/nn-restrictederrorinfo-ilanguageexceptionerrorinfo2"/>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct ILanguageExceptionErrorInfo2
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, void**, int> GetPreviousLanguageExceptionErrorInfo;
    public delegate* unmanaged[MemberFunction]<void*, void*, int> CapturePropagationContext;
    public delegate* unmanaged[MemberFunction]<void*, void**, int> GetPropagationContextHead;
}