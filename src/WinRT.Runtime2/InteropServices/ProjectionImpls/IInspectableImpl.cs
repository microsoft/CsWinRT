// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;
using static System.Runtime.InteropServices.ComWrappers;

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
            object instance = ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            // Get the managed CCW vtable entries to gather the IIDs. This method should only be used with
            // managed objects that were produced by 'WindowsRuntimeComWrappers', so this should never fail.
            WindowsRuntimeVtableInfo vtableInfo = WindowsRuntimeMarshallingInfo.GetInfo(instance.GetType()).GetVtableInfo();

            Guid* pIids = (Guid*)Marshal.AllocCoTaskMem(sizeof(Guid) * vtableInfo.Count);

            // Copy the IIDs manually, as we need to select from 'ComInterfaceEntry' to just 'Guid'
            for (int i = 0; i < vtableInfo.Count; i++)
            {
                pIids[i] = vtableInfo.VtableEntries[i].IID;
            }

            *iidCount = (uint)vtableInfo.Count;
            *iids = pIids;

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
            object instance = ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            // Like for 'GetIids', this method should only be called on managed types, and it should never fail
            string runtimeClassName = WindowsRuntimeMarshallingInfo.GetInfo(instance.GetType()).GetRuntimeClassName();

            *className = HStringMarshaller.ConvertToUnmanaged(runtimeClassName);

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
