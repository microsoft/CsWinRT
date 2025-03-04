// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// The <c>IInspectable</c> implementation for managed types.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/inspectable/nn-inspectable-iinspectable"/>
internal static unsafe class IInspectableImpl
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
        IInspectableVftbl* vftbl = (IInspectableVftbl*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(IInspectableImpl), sizeof(IInspectableVftbl));

        // The 'IUnknown' implementation is the same one we already retrieved from the runtime
        *(IUnknownVftbl*)vftbl = *(IUnknownVftbl*)IUnknownImpl.AbiToProjectionVftablePtr;

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

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/inspectable/nf-inspectable-iinspectable-getruntimeclassname"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT GetRuntimeClassName(void* thisPtr, HSTRING* className)
    {
        *className = (HSTRING)null;

        try
        {
            // TODO

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/inspectable/nf-inspectable-iinspectable-gettrustlevel"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT GetTrustLevel(void* thisPtr, TrustLevel* trustLevel)
    {
        *trustLevel = TrustLevel.BaseTrust;

        return WellKnownErrorCodes.S_OK;
    }
}
