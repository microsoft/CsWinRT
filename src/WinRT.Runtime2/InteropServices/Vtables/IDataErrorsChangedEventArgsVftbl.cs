// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Foundation;

#pragma warning disable IDE1006

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Binding type for the <c>IDataErrorsChangedEventArgs</c> interface vtable.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.data.dataerrorschangedeventargs"/>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct IDataErrorsChangedEventArgsVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> get_PropertyName;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING, HRESULT> put_PropertyName;

    /// <summary>
    /// Gets the name of the property whose errors have changed.
    /// </summary>
    /// <param name="thisPtr">The target COM object.</param>
    /// <param name="propertyName">The name of the property whose errors have changed.</param>
    /// <returns>The <c>HRESULT</c> for the operation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HRESULT get_PropertyNameUnsafe(void* thisPtr, HSTRING* propertyName)
    {
        return ((IDataErrorsChangedEventArgsVftbl*)*(void***)thisPtr)->get_PropertyName(thisPtr, propertyName);
    }
}