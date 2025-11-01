// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using Windows.Foundation;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Binding type for the <see cref="IAsyncActionWithProgress{TProgress}"/> interface vtable.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.iasyncactionwithprogress-1"/>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct IAsyncActionWithProgressVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, void**, HRESULT> get_Progress;
    public delegate* unmanaged[MemberFunction]<void*, void*, HRESULT> set_Progress;
    public delegate* unmanaged[MemberFunction]<void*, void**, HRESULT> get_Completed;
    public delegate* unmanaged[MemberFunction]<void*, void*, HRESULT> set_Completed;
    public delegate* unmanaged[MemberFunction]<void*, HRESULT> GetResults;
}
