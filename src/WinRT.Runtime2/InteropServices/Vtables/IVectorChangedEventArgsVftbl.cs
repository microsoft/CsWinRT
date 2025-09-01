// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Binding type for the <c>IVectorChangedEventArgs&lt;K&gt;</c> interface vtable.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivectorchangedeventargs"/>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct IVectorChangedEventArgsVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, CollectionChange*, HRESULT> get_CollectionChange;
    public delegate* unmanaged[MemberFunction]<void*, uint*, HRESULT> get_Index;
}
