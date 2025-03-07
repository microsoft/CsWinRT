﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;

#pragma warning disable CS0649

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Binding type for the <c>IAgileReference</c> interface vtable.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/activation/nn-activation-iactivationfactory"/>
internal unsafe struct IActivationFactoryVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, void**, HRESULT> ActivateInstance;

    /// <summary>
    /// Creates a new instance of the Windows Runtime class that is associated with the current activation factory.
    /// </summary>
    /// <param name="thisPtr">The target COM object.</param>
    /// <param name="instance">A pointer to a new instance of the class that is associated with the current activation factory.</param>
    /// <returns>The <c>HRESULT</c> for the operation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HRESULT ActivateInstanceUnsafe(void* thisPtr, void** instance)
    {
        return ((IActivationFactoryVftbl*)thisPtr)->ActivateInstance(thisPtr, instance);
    }
}
