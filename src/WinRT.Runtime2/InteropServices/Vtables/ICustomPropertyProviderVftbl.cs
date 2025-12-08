// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using Windows.Foundation;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Binding type for the <c>ICustomPropertyProvider</c> interface vtable.
/// </summary>
/// <remarks>
/// This interface is equivalent to <see href="https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.data.icustompropertyprovider"/>.
/// </remarks>
/// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.data.icustompropertyprovider"/>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct ICustomPropertyProviderVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING, void**, HRESULT> GetCustomProperty;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING, ABI.System.Type, void**, HRESULT> GetIndexedProperty;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetStringRepresentation;
    public delegate* unmanaged[MemberFunction]<void*, ABI.System.Type*, HRESULT> get_Type;
}