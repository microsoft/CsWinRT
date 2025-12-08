// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using Windows.Foundation;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Binding type for the <c>ICustomPropertyVftbl</c> interface vtable.
/// </summary>
/// <remarks>
/// This interface is equivalent to <see href="https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.data.icustomproperty"/>.
/// </remarks>
/// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.data.icustomproperty"/>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct ICustomPropertyVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, ABI.System.Type*, HRESULT> get_Type;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> get_Name;
    public delegate* unmanaged[MemberFunction]<void*, void*, void**, HRESULT> GetValue;
    public delegate* unmanaged[MemberFunction]<void*, void*, void*, HRESULT> SetValue;
    public delegate* unmanaged[MemberFunction]<void*, void*, void*, void**, HRESULT> GetIndexedValue;
    public delegate* unmanaged[MemberFunction]<void*, void*, void*, void*, HRESULT> SetIndexedValue;
    public delegate* unmanaged[MemberFunction]<void*, bool*, HRESULT> get_CanWrite;
    public delegate* unmanaged[MemberFunction]<void*, bool*, HRESULT> get_CanRead;
}