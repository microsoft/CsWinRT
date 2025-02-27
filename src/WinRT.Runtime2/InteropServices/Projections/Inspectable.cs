// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// The <c>IInspectable</c> implementation for managed types.
/// </summary>
internal static unsafe class Inspectable
{
    /// <summary>
    /// The vtable for the <c>IInspectable</c> implementation.
    /// </summary>
    public static nint AbiToProjectionVftablePtr { get; } = GetAbiToProjectionVftablePtr();

    /// <summary>
    /// Computes the <c>IInspectable</c> implementation vtable.
    /// </summary>
    private static nint GetAbiToProjectionVftablePtr()
    {
        IInspectableVftbl* vftbl = (IInspectableVftbl*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(Inspectable), sizeof(IInspectableVftbl));

        // Get the 'IUnknown' implementation from the runtime. This is implemented in native code,
        // so that it can work correctly even when used from native code during a GC (eg. from XAML).
        ComWrappers.GetIUnknownImpl(
            fpQueryInterface: out *(nint*)&vftbl->QueryInterface,
            fpAddRef: out *(nint*)&vftbl->AddRef,
            fpRelease: out *(nint*)&vftbl->Release);

        // Populate the rest of the 'IInspectable' methods
        vftbl->GetIids = &GetIids;
        vftbl->GetRuntimeClassName = &GetRuntimeClassName;
        vftbl->GetTrustLevel = &GetTrustLevel;

        return (nint)vftbl;
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/inspectable/nf-inspectable-iinspectable-getiids"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT GetIids(void* thisPtr, uint* iidCount, Guid** iids)
    {
        *iidCount = 0;
        *iids = null;

        try
        {
            // TODO
        }
        catch (Exception ex)
        {
            return ex.HResult;
        }

        return 0;
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/inspectable/nf-inspectable-iinspectable-getruntimeclassname"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT GetRuntimeClassName(void* thisPtr, HSTRING* className)
    {
        *className = (HSTRING)null;

        try
        {
            // TODO
        }
        catch (Exception ex)
        {
            return ex.HResult;
        }

        return 0;
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/inspectable/nf-inspectable-iinspectable-gettrustlevel"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT GetTrustLevel(void* thisPtr, TrustLevel* trustLevel)
    {
        *trustLevel = TrustLevel.BaseTrust;

        return 0;
    }
}
