// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;

#pragma warning disable CS0649, IDE1006

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Binding type for the <c>IUri</c> runtime class interface vtable.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.uri"/>
internal unsafe struct IUriRuntimeClassVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> get_AbsoluteUri;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> get_DisplayUri;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> get_Domain;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> get_Extension;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> get_Fragment;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> get_Host;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> get_Password;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> get_Path;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> get_Query;
    public delegate* unmanaged[MemberFunction]<void*, void**, HRESULT> get_QueryParsed;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> _get_RawUri;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> get_SchemeName;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> get_UserName;
    public delegate* unmanaged[MemberFunction]<void*, int*, HRESULT> get_Port;
    public delegate* unmanaged[MemberFunction]<void*, bool*, HRESULT> get_Suspicious;
    public new delegate* unmanaged[MemberFunction]<void*, void*, bool*, HRESULT> Equals;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING, void**, HRESULT> CombineUri;

    /// <summary>
    /// Gets the entire original Uniform Resource Identifier (URI) string as used to construct
    /// this <see cref="Uri"/> object, before parsing, and without any encoding applied.
    /// </summary>
    /// <param name="thisPtr">The target COM object.</param>
    /// <param name="result">The raw Uniform Resource Identifier (URI) string.</param>
    /// <returns>The <c>HRESULT</c> for the operation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int get_RawUriUnsafe(void* thisPtr, HSTRING* result)
    {
        return ((IUriRuntimeClassVftbl*)thisPtr)->_get_RawUri(thisPtr, result);
    }
}
