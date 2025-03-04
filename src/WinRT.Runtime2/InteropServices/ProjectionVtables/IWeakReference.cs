// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static System.Runtime.InteropServices.ComWrappers;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// The <c>IWeakReference</c> implementation for managed types.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/weakreference/nn-weakreference-iweakreference"/>
internal static unsafe class IWeakReference
{
    /// <summary>
    /// The vtable for the <c>IWeakReference</c> implementation.
    /// </summary>
    public static nint AbiToProjectionVftablePtr { get; } = GetAbiToProjectionVftablePtr();

    /// <summary>
    /// Computes the <c>IWeakReference</c> implementation vtable.
    /// </summary>
    private static nint GetAbiToProjectionVftablePtr()
    {
        IWeakReferenceVftbl* vftbl = (IWeakReferenceVftbl*)WindowsRuntimeHelpers.AllocateTypeAssociatedUnknownVtable(typeof(IWeakReference), 4);

        vftbl->Resolve = &Resolve;

        return (nint)vftbl;
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/weakreference/nf-weakreference-iweakreference-resolve(refiid_iinspectable)"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT Resolve(void* thisPtr, Guid* riid, void** objectReference)
    {
        *objectReference = null;

        try
        {
            object instance = ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            // TODO
            *objectReference = null;

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return e.HResult;
        }
    }
}
