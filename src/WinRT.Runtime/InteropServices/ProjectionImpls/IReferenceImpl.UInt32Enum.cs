// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;
using static System.Runtime.InteropServices.ComWrappers;

#pragma warning disable IDE1006

namespace WindowsRuntime.InteropServices;

/// <inheritdoc cref="IReferenceImpl"/>
public unsafe partial class IReferenceImpl
{
    /// <summary>
    /// Gets a pointer to the managed <c>IReference`1</c> implementation, specifically for <see cref="uint"/>-backed enum types.
    /// </summary>
    public static nint UInt32Enum
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in UInt32EnumImpl.Vftbl);
    }
}

/// <summary>
/// The <c>IReference`1</c> implementation for <see cref="uint"/>-backed enum types.
/// </summary>
file static unsafe class UInt32EnumImpl
{
    /// <summary>
    /// The <see cref="IReferenceVftbl"/> value for the managed <c>IReference`1</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    public static readonly IReferenceVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static UInt32EnumImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.get_Value = (delegate* unmanaged[MemberFunction]<void*, void*, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, HRESULT>)&get_Value;
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ireference-1.value"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    public static HRESULT get_Value(void* thisPtr, uint* result)
    {
        if (result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            *result = (uint)ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}