// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static System.Runtime.InteropServices.ComWrappers;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// The <c>IWeakReferenceSource</c> implementation for managed types.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/weakreference/nn-weakreference-iweakreferencesource"/>
internal static unsafe class IWeakReferenceSource
{
    /// <summary>
    /// The vtable for the <c>IWeakReferenceSource</c> implementation.
    /// </summary>
    public static nint AbiToProjectionVftablePtr { get; } = GetAbiToProjectionVftablePtr();

    /// <summary>
    /// Computes the <c>IWeakReferenceSource</c> implementation vtable.
    /// </summary>
    private static nint GetAbiToProjectionVftablePtr()
    {
        IWeakReferenceSourceVftbl* vftbl = (IWeakReferenceSourceVftbl*)WindowsRuntimeHelpers.AllocateTypeAssociatedUnknownVtable(typeof(IWeakReferenceSource), 4);

        vftbl->GetWeakReference = &GetWeakReference;

        return (nint)vftbl;
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/weakreference/nf-weakreference-iweakreferencesource-getweakreference"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT GetWeakReference(void* thisPtr, void** weakReference)
    {
        *weakReference = null;

        try
        {
            object instance = ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            // TODO
            *weakReference = null;

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return e.HResult;
        }
    }
}
