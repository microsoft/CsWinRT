// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma warning disable CSWINRT3001 // "Type or member '...' is a private implementation detail"

#if CSWINRT_REFERENCE_PROJECTION
[assembly: WindowsRuntime.InteropServices.WindowsRuntimeReferenceAssembly]
#else
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Foundation;

[assembly: DisableRuntimeMarshallingAttribute]
[assembly: AssemblyMetadata("IsTrimmable", "True")]
[assembly: AssemblyMetadata("IsAotCompatible", "True")]

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
internal unsafe struct IUnknownVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
}
#endif