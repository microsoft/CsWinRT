// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Windows.Foundation;

#pragma warning disable CS0649

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Binding type for the <c>IVector&lt;T&gt;</c> interface vtable.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivector-1"/>
internal unsafe struct IVectorVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, uint, void*, HRESULT> GetAt;
    public delegate* unmanaged[MemberFunction]<void*, uint*, HRESULT> get_Size;

    // See notes in 'IVectorViewVftbl' regarding ABI mismatches for the by-value parameters below
    public delegate* unmanaged[MemberFunction]<void*, void**, HRESULT> GetView;
    public delegate* unmanaged[MemberFunction]<void*, void*, uint*, bool*, HRESULT> IndexOf;
    public delegate* unmanaged[MemberFunction]<void*, uint, void*, HRESULT> SetAt;
    public delegate* unmanaged[MemberFunction]<void*, uint, void*, HRESULT> InsertAt;
    public delegate* unmanaged[MemberFunction]<void*, uint, HRESULT> RemoveAt;
    public delegate* unmanaged[MemberFunction]<void*, void*, HRESULT> Append;
    public delegate* unmanaged[MemberFunction]<void*, HRESULT> RemoveAtEnd;
    public delegate* unmanaged[MemberFunction]<void*, HRESULT> Clear;
    public delegate* unmanaged[MemberFunction]<void*, uint, uint, void*, uint*, HRESULT> GetMany;
    public delegate* unmanaged[MemberFunction]<void*, uint, void*, HRESULT> ReplaceAll;
}
