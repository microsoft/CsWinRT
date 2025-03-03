﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;

#pragma warning disable CS0649

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Binding type for the <c>IAgileReference</c> interface vtable.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/objidl/nn-objidl-iagilereference"/>
internal unsafe struct IAgileReferenceVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> Resolve;

    /// <summary>
    /// Gets the interface ID of an agile reference to an object.
    /// </summary>
    /// <param name="thisPtr">The target COM object.</param>
    /// <param name="riid">The IID of the interface to resolve.</param>
    /// <param name="ppvObjectReference">
    /// On successful completion, <paramref name="ppvObjectReference"/> is a pointer to the interface specified by <paramref name="riid"/>.
    /// </param>
    /// <returns>The <c>HRESULT</c> for the operation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HRESULT ResolveUnsafe(
        void* thisPtr,
        Guid* riid,
        void** ppvObjectReference)
    {
        return ((IAgileReferenceVftbl*)thisPtr)->Resolve(thisPtr, riid, ppvObjectReference);
    }
}
