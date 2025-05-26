// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Windows.Foundation;

#pragma warning disable CS0649

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Binding type for the <c>IVectorView&lt;T&gt;</c> interface vtable.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivectorview-1"/>
internal unsafe struct IVectorViewVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, uint, void*, HRESULT> GetAt;
    public delegate* unmanaged[MemberFunction]<void*, uint*, HRESULT> get_Size;

    // The 'value' parameter (index '1') does not have the correct type here, because it's a by-value
    // parameter of a generic type (meaning it could be either 'void*' or some exact value type). This
    // does not matter, since this vtable slot is never actually used within this assembly. It is only
    // used from 'WinRT.Interop.dll', which will emit specialized vtable types when necessary.
    public delegate* unmanaged[MemberFunction]<void*, void*, uint*, HRESULT> IndexOf;
    public delegate* unmanaged[MemberFunction]<void*, uint, void*, uint*, HRESULT> GetMany;
}
