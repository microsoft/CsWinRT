// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Binding type for the <c>IUnknown</c> interface vtable.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/unknwn/nn-unknwn-iunknown"/>
internal unsafe struct IUnknownVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;

    /// <summary>
    /// Queries a COM object for a pointer to one of its interface.
    /// </summary>
    /// <param name="thisPtr">The target COM object.</param>
    /// <param name="riid">A reference to the interface identifier (IID) of the interface being queried for.</param>
    /// <param name="ppvObject">The address of a pointer to an interface with the IID specified in the <paramref name="riid"/> parameter.</param>
    /// <returns>This method returns <c>S_OK</c> if the interface is supported, and <c>E_NOINTERFACE</c> otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HRESULT QueryInterfaceUnsafe(void* thisPtr, Guid* riid, void** ppvObject)
    {
        return ((IUnknownVftbl*)thisPtr)->QueryInterface(thisPtr, riid, ppvObject);
    }

    /// <param name="thisPtr">The target COM object.</param>
    /// <param name="iid">The interface identifier (IID) of the interface being queried for.</param>
    /// <param name="pvObject">The pointer to an interface with the IID specified in the <paramref name="iid"/> parameter.</param>
    /// <inheritdoc cref="QueryInterfaceUnsafe(void*, Guid*, void**)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HRESULT QueryInterfaceUnsafe(void* thisPtr, in Guid iid, out void* pvObject)
    {
        fixed (Guid* riid = &iid)
        fixed (void** ppvObject = &pvObject)
        {
            return QueryInterfaceUnsafe(thisPtr, riid, ppvObject);
        }
    }

    /// <summary>
    /// Increments the reference count for an interface pointer to a COM object.
    /// </summary>
    /// <param name="thisPtr">The target COM object.</param>
    /// <returns>The method returns the new reference count.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint AddRefUnsafe(void* thisPtr)
    {
        return ((IUnknownVftbl*)thisPtr)->AddRef(thisPtr);
    }

    /// <summary>
    /// Decrements the reference count for an interface on a COM object.
    /// </summary>
    /// <param name="thisPtr">The target COM object.</param>
    /// <returns>The method returns the new reference count.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ReleaseUnsafe(void* thisPtr)
    {
        return ((IUnknownVftbl*)thisPtr)->Release(thisPtr);
    }
}
