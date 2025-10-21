// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using Windows.Foundation;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Binding type for the <c>IBindableIterator</c> interface vtable.
/// </summary>
/// <remarks>
/// This interface is equivalent to <see href="https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.interop.ibindableiterator"/>.
/// </remarks>
/// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindableiterator"/>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct IBindableIteratorVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, void**, HRESULT> get_Current;
    public delegate* unmanaged[MemberFunction]<void*, bool*, HRESULT> get_HasCurrent;
    public delegate* unmanaged[MemberFunction]<void*, bool*, HRESULT> MoveNext;

    // 'GetMany' is not implemented by 'IBindableIterator', but it is here for compat purposes
    // with WinUI, as there are scenarios where it does 'reinterpret_cast' from 'IBindableIterator'
    // to 'IIterator<IInspectable>'. It is the last function in the vftable and shouldn't be called
    // by anyone. If called, it will return 'E_NOTIMPL'.
    public delegate* unmanaged[MemberFunction]<void*, uint, void**, uint*, HRESULT> GetMany;
}
