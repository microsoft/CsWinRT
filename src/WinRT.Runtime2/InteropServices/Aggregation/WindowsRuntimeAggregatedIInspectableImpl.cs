// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#define WINDOWS_RUNTIME_IMPLEMENTATION_ONLY_FILE

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Foundation;

#pragma warning disable CS0649, CS8909

namespace WindowsRuntime.InteropServices;

/// <summary>
/// The delegating <c>IInspectable</c> implementation used by the per-aggregate CCW vtables that CsWinRT
/// builds for an authored Windows Runtime object taking part in COM aggregation.
/// </summary>
/// <remarks>
/// <para>
/// The COM aggregation contract requires every interface an aggregate hands out to share the identity and
/// the lifetime of the controlling outer object. To achieve that without ever disturbing the CCW-s of
/// ordinary (non aggregated) objects, <see cref="WindowsRuntimeAggregationInner"/> allocates a private copy
/// of the CCW vtable of each interface an aggregated object can expose, and replaces its first six entries
/// (i.e. all of <c>IUnknown</c> and <c>IInspectable</c>) with the ones defined here. Every other entry is
/// left untouched, so all interface methods still run the exact same CCW stubs as usual, and still receive a
/// real <see cref="System.Runtime.InteropServices.ComWrappers.ComInterfaceDispatch"/> pointer.
/// </para>
/// <para>
/// Each vtable copy is preceded by one pointer sized slot holding the controlling outer object, so resolving
/// it from an interface pointer is just two pointer loads (see <see cref="GetControllingOuter"/>). There is no
/// lookaside table involved, and no managed object has to be resolved to delegate an <c>IUnknown</c> call.
/// </para>
/// <para>
/// This mirrors <c>winrt::implements</c> in C++/WinRT, where <c>QueryInterface</c>, <c>AddRef</c>,
/// <c>Release</c>, <c>GetIids</c>, <c>GetRuntimeClassName</c>, and <c>GetTrustLevel</c> all forward to
/// <c>m_outer</c> when the object is composed (only the non delegating inner answers them itself).
/// </para>
/// <para>
/// Note that CCW-s for objects that are not aggregated never use these entries: they keep the <c>IUnknown</c>
/// implementation provided by the runtime (see <see cref="IUnknownImpl"/>), which is implemented in native
/// code, and can therefore be safely called from native code even during a garbage collection.
/// </para>
/// </remarks>
internal static unsafe class WindowsRuntimeAggregatedIInspectableImpl
{
    /// <summary>
    /// The <see cref="IInspectableVftbl"/> value for the delegating <c>IInspectable</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly IInspectableVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static WindowsRuntimeAggregatedIInspectableImpl()
    {
        Vftbl.QueryInterface = &QueryInterface;
        Vftbl.AddRef = &AddRef;
        Vftbl.Release = &Release;
        Vftbl.GetIids = &GetIids;
        Vftbl.GetRuntimeClassName = &GetRuntimeClassName;
        Vftbl.GetTrustLevel = &GetTrustLevel;
    }

    /// <summary>
    /// Gets a pointer to the delegating <c>IInspectable</c> implementation.
    /// </summary>
    public static IInspectableVftbl* Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (IInspectableVftbl*)Unsafe.AsPointer(in Vftbl);
    }

    /// <summary>
    /// Checks whether a given COM interface pointer is one of the per-aggregate delegating interface
    /// pointers produced by <see cref="WindowsRuntimeAggregationInner"/>.
    /// </summary>
    /// <param name="externalComObject">The COM interface pointer to inspect.</param>
    /// <returns>Whether <paramref name="externalComObject"/> delegates its <c>IUnknown</c> methods to a controlling outer object.</returns>
    /// <remarks>
    /// All delegating vtables share these entries, so a single function pointer comparison is enough. The
    /// pointer is still a valid <see cref="System.Runtime.InteropServices.ComWrappers.ComInterfaceDispatch"/>
    /// pointer for the CCW of the aggregated object, so callers can resolve the managed object from it as usual.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsDelegatingInterfacePointer(void* externalComObject)
    {
        return ((IUnknownVftbl*)*(void***)externalComObject)->QueryInterface == Vftbl.QueryInterface;
    }

    /// <summary>
    /// Gets the controlling outer object for a given per-aggregate delegating interface pointer.
    /// </summary>
    /// <param name="thisPtr">The delegating interface pointer.</param>
    /// <returns>The controlling outer object (not reference counted).</returns>
    /// <remarks>
    /// The controlling outer is stored in the slot immediately preceding the per-aggregate vtable copy the
    /// interface pointer refers to, which makes this just a pair of dependent pointer loads.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void* GetControllingOuter(void* thisPtr)
    {
        void** vtable = *(void***)thisPtr;

        return vtable[-1];
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/unknwn/nf-unknwn-iunknown-queryinterface(refiid_void)"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT QueryInterface(void* thisPtr, Guid* riid, void** ppvObject)
    {
        return IUnknownVftbl.QueryInterfaceUnsafe(GetControllingOuter(thisPtr), riid, ppvObject);
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/unknwn/nf-unknwn-iunknown-addref"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static uint AddRef(void* thisPtr)
    {
        return IUnknownVftbl.AddRefUnsafe(GetControllingOuter(thisPtr));
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/unknwn/nf-unknwn-iunknown-release"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static uint Release(void* thisPtr)
    {
        return IUnknownVftbl.ReleaseUnsafe(GetControllingOuter(thisPtr));
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/inspectable/nf-inspectable-iinspectable-getiids"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT GetIids(void* thisPtr, uint* iidCount, Guid** iids)
    {
        return IInspectableVftbl.GetIidsUnsafe(GetControllingOuter(thisPtr), iidCount, iids);
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/inspectable/nf-inspectable-iinspectable-getruntimeclassname"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT GetRuntimeClassName(void* thisPtr, HSTRING* className)
    {
        return IInspectableVftbl.GetRuntimeClassNameUnsafe(GetControllingOuter(thisPtr), className);
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/inspectable/nf-inspectable-iinspectable-gettrustlevel"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT GetTrustLevel(void* thisPtr, TrustLevel* trustLevel)
    {
        return IInspectableVftbl.GetTrustLevelUnsafe(GetControllingOuter(thisPtr), trustLevel);
    }
}
