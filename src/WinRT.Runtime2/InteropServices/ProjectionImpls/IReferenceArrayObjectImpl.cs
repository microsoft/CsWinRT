// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;
using static System.Runtime.InteropServices.ComWrappers;

#pragma warning disable IDE1006

namespace WindowsRuntime.InteropServices;

/// <summary>
/// The <c>IReferenceArray&lt;Object&gt;</c> implementation shared by covariant reference-type arrays.
/// </summary>
[WindowsRuntimeImplementationOnlyMember]
public static unsafe class IReferenceArrayObjectImpl
{
    /// <summary>
    /// The vtable for the managed inspectable array implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly IReferenceArrayVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static IReferenceArrayObjectImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.get_Value = (delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void***, HRESULT>)&get_Value;
    }

    /// <summary>
    /// Gets the IID of <c>IReferenceArray&lt;Object&gt;</c>.
    /// </summary>
    public static ref readonly Guid IID
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref WellKnownWindowsInterfaceIIDs.IID_IReferenceArrayOfObject;
    }

    /// <summary>
    /// Gets a pointer to the managed inspectable array implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Vftbl);
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ireferencearray-1.value"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static HRESULT get_Value(void* thisPtr, uint* size, void*** value)
    {
        if (size is null || value is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            object[] thisObject = ComInterfaceDispatch.GetInstance<object[]>((ComInterfaceDispatch*)thisPtr);

            WindowsRuntimeObjectArrayMarshaller.ConvertToUnmanaged(thisObject, out *size, out *value);

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}
