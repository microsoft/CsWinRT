// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Foundation;
using WindowsRuntime.InteropServices.Marshalling;
using static System.Runtime.InteropServices.ComWrappers;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// The <c>IInspectable</c> implementation for managed types.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/inspectable/nn-inspectable-iinspectable"/>
public static unsafe class IInspectableImpl
{
    /// <summary>
    /// The <see cref="IInspectableVftbl"/> value for the managed <c>IInspectable</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly IInspectableVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static IInspectableImpl()
    {
        *(IUnknownVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IUnknownVftbl*)IUnknownImpl.Vtable;

        Vftbl.GetIids = &GetIids;
        Vftbl.GetRuntimeClassName = &GetRuntimeClassName;
        Vftbl.GetTrustLevel = &GetTrustLevel;
    }

    /// <summary>
    /// Gets the IID for the <c>IInspectable</c> interface.
    /// </summary>
    public static ref readonly Guid IID
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref WellKnownInterfaceIds.IID_IInspectable;
    }

    /// <summary>
    /// Gets a pointer to the managed <c>IInspectable</c> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(ref Unsafe.AsRef(in Vftbl));
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/inspectable/nf-inspectable-iinspectable-getiids"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT GetIids(void* thisPtr, uint* iidCount, Guid** iids)
    {
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
