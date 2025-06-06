// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using Windows.Foundation;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Binding type for the <c>IMapView&lt;K, V&gt;</c> interface vtable.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.imapview-2"/>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct IMapViewVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;

    // See notes in 'IVectorViewVftbl' regarding ABI mismatches for the by-value parameters below
    public delegate* unmanaged[MemberFunction]<void*, void*, void*, HRESULT> Lookup;
    public delegate* unmanaged[MemberFunction]<void*, uint*, HRESULT> get_Size;
    public delegate* unmanaged[MemberFunction]<void*, void*, bool*, HRESULT> HasKey;
    public delegate* unmanaged[MemberFunction]<void*, void**, void**, HRESULT> Split;
}
