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
internal static unsafe class IWeakReferenceSourceImpl
{
    /// <summary>
    /// The <see cref="IWeakReferenceSourceVftbl"/> value for the managed <c>IWeakReferenceSource</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly IWeakReferenceSourceVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static IWeakReferenceSourceImpl()
    {
        *(IUnknownVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IUnknownVftbl*)IUnknownImpl.AbiToProjectionVftablePtr;

        Vftbl.GetWeakReference = &GetWeakReference;
    }

    /// <summary>
    /// Gets a pointer to the managed <c>IWeakReferenceSource</c> implementation.
    /// </summary>
    public static nint AbiToProjectionVftablePtr
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(ref Unsafe.AsRef(in Vftbl));
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
