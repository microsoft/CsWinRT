﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;

#pragma warning disable CS0649

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Binding type for the <c>IPropertyChangedEventArgs</c> interface vtable.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.data.propertychangedeventargs"/>
internal unsafe struct IPropertyChangedEventArgsVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> PropertyName;

    /// <summary>
    /// Gets the name of the property that changed.
    /// </summary>
    /// <param name="thisPtr">The target COM object.</param>
    /// <param name="propertyName">The name of the property that changed.</param>
    /// <returns>The <c>HRESULT</c> for the operation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HRESULT PropertyNameUnsafe(void* thisPtr, HSTRING* propertyName)
    {
        return ((IPropertyChangedEventArgsVftbl*)thisPtr)->PropertyName(thisPtr, propertyName);
    }
}
