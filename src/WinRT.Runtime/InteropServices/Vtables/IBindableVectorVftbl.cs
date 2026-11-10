// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using Windows.Foundation;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Binding type for the <c>IBindableVector</c> interface vtable.
/// </summary>
/// <remarks>
/// This interface is equivalent to <see href="https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.interop.ibindablevector"/>.
/// </remarks>
/// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindablevector"/>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct IBindableVectorVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, uint, void**, HRESULT> GetAt;
    public delegate* unmanaged[MemberFunction]<void*, uint*, HRESULT> get_Size;
    public delegate* unmanaged[MemberFunction]<void*, void**, HRESULT> GetView;
    public delegate* unmanaged[MemberFunction]<void*, void*, uint*, bool*, HRESULT> IndexOf;
    public delegate* unmanaged[MemberFunction]<void*, uint, void*, HRESULT> SetAt;
    public delegate* unmanaged[MemberFunction]<void*, uint, void*, HRESULT> InsertAt;
    public delegate* unmanaged[MemberFunction]<void*, uint, HRESULT> RemoveAt;
    public delegate* unmanaged[MemberFunction]<void*, void*, HRESULT> Append;
    public delegate* unmanaged[MemberFunction]<void*, HRESULT> RemoveAtEnd;
    public delegate* unmanaged[MemberFunction]<void*, HRESULT> Clear;
}