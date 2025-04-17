// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Specialized;

#pragma warning disable CS0649

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Binding type for the <c>INotifyCollectionChangedEventArgs</c> interface vtable.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.notifycollectionchangedeventargs"/>
internal unsafe struct INotifyCollectionChangedEventArgsVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, NotifyCollectionChangedAction*, HRESULT> get_Action;
    public delegate* unmanaged[MemberFunction]<void*, void*, HRESULT> get_NewItems;
    public delegate* unmanaged[MemberFunction]<void*, void*, HRESULT> get_OldItems;
    public delegate* unmanaged[MemberFunction]<void*, int*, HRESULT> get_NewStartingIndex;
    public delegate* unmanaged[MemberFunction]<void*, int*, HRESULT> get_OldStartingIndex;
}
