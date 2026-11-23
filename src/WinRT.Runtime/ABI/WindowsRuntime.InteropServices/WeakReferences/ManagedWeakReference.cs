// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WindowsRuntime.InteropServices;

namespace ABI.WindowsRuntime.InteropServices;

/// <summary>
/// The native implementation of <c>IWeakReference</c> for <see cref="ManagedWeakReference"/>.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/weakreference/nn-weakreference-iweakreference"/>
internal static unsafe class ManagedWeakReferenceImpl
{
    /// <summary>
    /// The <see cref="IWeakReferenceVftbl"/> value for the <see cref="ManagedWeakReference"/> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly IWeakReferenceVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static ManagedWeakReferenceImpl()
    {
        Vftbl.QueryInterface = &QueryInterface;
        Vftbl.AddRef = &AddRef;
        Vftbl.Release = &Release;
        Vftbl.Resolve = &Resolve;
    }

    /// <summary>
    /// Gets a pointer to the <see cref="ManagedWeakReference"/> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Vftbl);
    }

    /// <inheritdoc href="https://learn.microsoft.com/windows/win32/api/unknwn/nf-unknwn-iunknown-queryinterface(refiid_void)"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    public static HRESULT QueryInterface(void* @this, Guid* riid, void** ppvObject)
    {
        return ((ManagedWeakReference*)@this)->QueryInterface(riid, ppvObject);
    }

    /// <inheritdoc href="https://learn.microsoft.com/windows/win32/api/unknwn/nf-unknwn-iunknown-addref"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    public static uint AddRef(void* @this)
    {
        return ((ManagedWeakReference*)@this)->AddRef();
    }

    /// <inheritdoc href="https://learn.microsoft.com/windows/win32/api/unknwn/nf-unknwn-iunknown-release"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    public static uint Release(void* @this)
    {
        return ((ManagedWeakReference*)@this)->Release();
    }

    /// <inheritdoc href="https://learn.microsoft.com/windows/win32/api/weakreference/nf-weakreference-iweakreference-resolve(refiid_iinspectable)"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    public static HRESULT Resolve(void* @this, Guid* riid, void** objectReference)
    {
        try
        {
            return ((ManagedWeakReference*)@this)->Resolve(riid, objectReference);
        }
        catch (Exception e)
        {
            return e.HResult;
        }
    }
}
