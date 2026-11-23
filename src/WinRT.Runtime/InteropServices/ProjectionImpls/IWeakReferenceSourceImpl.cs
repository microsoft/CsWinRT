// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;
using static System.Runtime.InteropServices.ComWrappers;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// The <c>IWeakReferenceSource</c> implementation for managed types.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/weakreference/nn-weakreference-iweakreferencesource"/>
[WindowsRuntimeImplementationOnlyMember]
public static unsafe class IWeakReferenceSourceImpl
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
        *(IUnknownVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IUnknownVftbl*)IUnknownImpl.Vtable;

        Vftbl.GetWeakReference = &GetWeakReference;
    }

    /// <summary>
    /// Gets a pointer to the managed <c>IWeakReferenceSource</c> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Vftbl);
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/weakreference/nf-weakreference-iweakreferencesource-getweakreference"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT GetWeakReference(void* thisPtr, void** weakReference)
    {
        *weakReference = null;

        try
        {
            object instance = ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            // The managed weak reference is a thin native object (it doesn't go through 'ComWrappers' at all). That
            // also allows 'WindowsRuntimeComWrappers' to only ever have to handle Windows Runtime types. The returned
            // object has a reference count of 1, so we can just return it directly, as ownership goes to the caller.
            *weakReference = ManagedWeakReference.Create(instance);

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}
