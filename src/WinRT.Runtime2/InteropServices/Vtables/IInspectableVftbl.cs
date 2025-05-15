// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using Windows.Foundation;

#pragma warning disable CS0649

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Binding type for the <c>IInspectable</c> interface vtable.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/inspectable/nn-inspectable-iinspectable"/>
internal unsafe struct IInspectableVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HRESULT GetRuntimeClassNameUnsafe(void* thisPtr, HSTRING* value)
    {
        return ((IInspectableVftbl*)thisPtr)->GetRuntimeClassName(thisPtr, value);
    }
}
