// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Foundation;

#pragma warning disable IDE1006

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Binding type for the <c>IReferenceArray`1</c> interface vtable.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ireferencearray-1"/>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct IReferenceArrayVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT> get_Value;

    /// <summary>
    /// Gets the type that is represented as an <c>IPropertyValue</c> array.
    /// </summary>
    /// <param name="thisPtr">The target COM object.</param>
    /// <param name="count">The resulting count for the array.</param>
    /// <param name="value">The resulting value.</param>
    /// <returns>The <c>HRESULT</c> for the operation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HRESULT get_ValueUnsafe(void* thisPtr, uint* count, void** value)
    {
        return ((IReferenceArrayVftbl*)*(void***)thisPtr)->get_Value(thisPtr, count, value);
    }
}