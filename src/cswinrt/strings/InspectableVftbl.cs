// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Foundation;

namespace WindowsRuntime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct IInspectableVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, int> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, void**, int> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, int*, int> GetTrustLevel;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct ReferenceVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, int> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, void**, int> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, int> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, void*, int> get_Value;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct DelegateVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, void*, void*, int> Invoke;
}