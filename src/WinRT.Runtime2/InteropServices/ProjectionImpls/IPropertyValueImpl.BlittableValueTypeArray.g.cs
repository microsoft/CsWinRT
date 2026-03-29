// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Foundation;
using WindowsRuntime.InteropServices.Marshalling;
using static System.Runtime.InteropServices.ComWrappers;

#pragma warning disable IDE1006, CA1416

namespace WindowsRuntime.InteropServices;

/// <inheritdoc cref="IPropertyValueImpl"/>
public unsafe partial class IPropertyValueImpl
{
    /// <summary>
    /// Gets a pointer to the managed <c>IPropertyValue</c> implementation, specifically for arrays of <see cref="PropertyType.UInt8Array"/> objects.
    /// </summary>
    public static nint UInt8ArrayVtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in UInt8ArrayPropertyValueImpl.Vftbl);
    }
}

/// <summary>
/// The <c>IPropertyValue</c> implementation returning <see cref="PropertyType.UInt8Array"/>.
/// </summary>
file static unsafe class UInt8ArrayPropertyValueImpl
{
    /// <summary>
    /// The <see cref="IPropertyValueVftbl"/> value for the managed <c>IPropertyValue</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    public static readonly IPropertyValueVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static UInt8ArrayPropertyValueImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.get_Type = &get_Type;
        Vftbl.get_IsNumericScalar = &IPropertyValueImpl.get_IsNumericScalarFalse;
        Vftbl.GetUInt8 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt32 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt32 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt64 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt64 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetSingle = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetDouble = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetChar16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetBoolean = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetString = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetGuid = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetDateTime = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetTimeSpan = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetPoint = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetSize = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetRect = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt8Array = &GetUInt8Array;
        Vftbl.GetInt16Array = (delegate* unmanaged[MemberFunction]<void*, uint*, short**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt16Array = (delegate* unmanaged[MemberFunction]<void*, uint*, ushort**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt32Array = (delegate* unmanaged[MemberFunction]<void*, uint*, int**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt32Array = (delegate* unmanaged[MemberFunction]<void*, uint*, uint**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt64Array = (delegate* unmanaged[MemberFunction]<void*, uint*, long**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt64Array = (delegate* unmanaged[MemberFunction]<void*, uint*, ulong**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetSingleArray = (delegate* unmanaged[MemberFunction]<void*, uint*, float**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetDoubleArray = (delegate* unmanaged[MemberFunction]<void*, uint*, double**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetChar16Array = (delegate* unmanaged[MemberFunction]<void*, uint*, char**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetBooleanArray = (delegate* unmanaged[MemberFunction]<void*, uint*, bool**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetStringArray = (delegate* unmanaged[MemberFunction]<void*, uint*, HSTRING**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInspectableArray = (delegate* unmanaged[MemberFunction]<void*, uint*, void***, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetGuidArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetDateTimeArray = (delegate* unmanaged[MemberFunction]<void*, uint*, ABI.System.DateTimeOffset**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetTimeSpanArray = (delegate* unmanaged[MemberFunction]<void*, uint*, ABI.System.TimeSpan**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetPointArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Point**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetSizeArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Size**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetRectArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Rect**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.type"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT get_Type(void* thisPtr, PropertyType* value)
    {
        if (value is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        *value = PropertyType.UInt8Array;

        return WellKnownErrorCodes.S_OK;
    }

    /// <seealso href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.getuint8array"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static HRESULT GetUInt8Array(void* thisPtr, uint* size, byte** value)
    {
        if (size is null || value is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            byte[] thisObject = ComInterfaceDispatch.GetInstance<byte[]>((ComInterfaceDispatch*)thisPtr);

            WindowsRuntimeBlittableValueTypeArrayMarshaller<byte>.ConvertToUnmanaged(thisObject, out *size, out *value);

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}

/// <inheritdoc cref="IPropertyValueImpl"/>
public unsafe partial class IPropertyValueImpl
{
    /// <summary>
    /// Gets a pointer to the managed <c>IPropertyValue</c> implementation, specifically for arrays of <see cref="PropertyType.Int16Array"/> objects.
    /// </summary>
    public static nint Int16ArrayVtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Int16ArrayPropertyValueImpl.Vftbl);
    }
}

/// <summary>
/// The <c>IPropertyValue</c> implementation returning <see cref="PropertyType.Int16Array"/>.
/// </summary>
file static unsafe class Int16ArrayPropertyValueImpl
{
    /// <summary>
    /// The <see cref="IPropertyValueVftbl"/> value for the managed <c>IPropertyValue</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    public static readonly IPropertyValueVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static Int16ArrayPropertyValueImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.get_Type = &get_Type;
        Vftbl.get_IsNumericScalar = &IPropertyValueImpl.get_IsNumericScalarFalse;
        Vftbl.GetUInt8 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt32 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt32 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt64 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt64 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetSingle = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetDouble = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetChar16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetBoolean = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetString = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetGuid = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetDateTime = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetTimeSpan = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetPoint = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetSize = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetRect = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt8Array = (delegate* unmanaged[MemberFunction]<void*, uint*, byte**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt16Array = &GetInt16Array;
        Vftbl.GetUInt16Array = (delegate* unmanaged[MemberFunction]<void*, uint*, ushort**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt32Array = (delegate* unmanaged[MemberFunction]<void*, uint*, int**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt32Array = (delegate* unmanaged[MemberFunction]<void*, uint*, uint**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt64Array = (delegate* unmanaged[MemberFunction]<void*, uint*, long**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt64Array = (delegate* unmanaged[MemberFunction]<void*, uint*, ulong**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetSingleArray = (delegate* unmanaged[MemberFunction]<void*, uint*, float**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetDoubleArray = (delegate* unmanaged[MemberFunction]<void*, uint*, double**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetChar16Array = (delegate* unmanaged[MemberFunction]<void*, uint*, char**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetBooleanArray = (delegate* unmanaged[MemberFunction]<void*, uint*, bool**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetStringArray = (delegate* unmanaged[MemberFunction]<void*, uint*, HSTRING**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInspectableArray = (delegate* unmanaged[MemberFunction]<void*, uint*, void***, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetGuidArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetDateTimeArray = (delegate* unmanaged[MemberFunction]<void*, uint*, ABI.System.DateTimeOffset**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetTimeSpanArray = (delegate* unmanaged[MemberFunction]<void*, uint*, ABI.System.TimeSpan**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetPointArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Point**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetSizeArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Size**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetRectArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Rect**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.type"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT get_Type(void* thisPtr, PropertyType* value)
    {
        if (value is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        *value = PropertyType.Int16Array;

        return WellKnownErrorCodes.S_OK;
    }

    /// <seealso href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.getint16array"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static HRESULT GetInt16Array(void* thisPtr, uint* size, short** value)
    {
        if (size is null || value is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            short[] thisObject = ComInterfaceDispatch.GetInstance<short[]>((ComInterfaceDispatch*)thisPtr);

            WindowsRuntimeBlittableValueTypeArrayMarshaller<short>.ConvertToUnmanaged(thisObject, out *size, out *value);

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}

/// <inheritdoc cref="IPropertyValueImpl"/>
public unsafe partial class IPropertyValueImpl
{
    /// <summary>
    /// Gets a pointer to the managed <c>IPropertyValue</c> implementation, specifically for arrays of <see cref="PropertyType.UInt16Array"/> objects.
    /// </summary>
    public static nint UInt16ArrayVtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in UInt16ArrayPropertyValueImpl.Vftbl);
    }
}

/// <summary>
/// The <c>IPropertyValue</c> implementation returning <see cref="PropertyType.UInt16Array"/>.
/// </summary>
file static unsafe class UInt16ArrayPropertyValueImpl
{
    /// <summary>
    /// The <see cref="IPropertyValueVftbl"/> value for the managed <c>IPropertyValue</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    public static readonly IPropertyValueVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static UInt16ArrayPropertyValueImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.get_Type = &get_Type;
        Vftbl.get_IsNumericScalar = &IPropertyValueImpl.get_IsNumericScalarFalse;
        Vftbl.GetUInt8 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt32 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt32 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt64 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt64 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetSingle = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetDouble = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetChar16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetBoolean = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetString = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetGuid = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetDateTime = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetTimeSpan = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetPoint = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetSize = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetRect = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt8Array = (delegate* unmanaged[MemberFunction]<void*, uint*, byte**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt16Array = (delegate* unmanaged[MemberFunction]<void*, uint*, short**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt16Array = &GetUInt16Array;
        Vftbl.GetInt32Array = (delegate* unmanaged[MemberFunction]<void*, uint*, int**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt32Array = (delegate* unmanaged[MemberFunction]<void*, uint*, uint**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt64Array = (delegate* unmanaged[MemberFunction]<void*, uint*, long**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt64Array = (delegate* unmanaged[MemberFunction]<void*, uint*, ulong**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetSingleArray = (delegate* unmanaged[MemberFunction]<void*, uint*, float**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetDoubleArray = (delegate* unmanaged[MemberFunction]<void*, uint*, double**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetChar16Array = (delegate* unmanaged[MemberFunction]<void*, uint*, char**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetBooleanArray = (delegate* unmanaged[MemberFunction]<void*, uint*, bool**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetStringArray = (delegate* unmanaged[MemberFunction]<void*, uint*, HSTRING**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInspectableArray = (delegate* unmanaged[MemberFunction]<void*, uint*, void***, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetGuidArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetDateTimeArray = (delegate* unmanaged[MemberFunction]<void*, uint*, ABI.System.DateTimeOffset**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetTimeSpanArray = (delegate* unmanaged[MemberFunction]<void*, uint*, ABI.System.TimeSpan**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetPointArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Point**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetSizeArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Size**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetRectArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Rect**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.type"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT get_Type(void* thisPtr, PropertyType* value)
    {
        if (value is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        *value = PropertyType.UInt16Array;

        return WellKnownErrorCodes.S_OK;
    }

    /// <seealso href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.getuint16array"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static HRESULT GetUInt16Array(void* thisPtr, uint* size, ushort** value)
    {
        if (size is null || value is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            ushort[] thisObject = ComInterfaceDispatch.GetInstance<ushort[]>((ComInterfaceDispatch*)thisPtr);

            WindowsRuntimeBlittableValueTypeArrayMarshaller<ushort>.ConvertToUnmanaged(thisObject, out *size, out *value);

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}

/// <inheritdoc cref="IPropertyValueImpl"/>
public unsafe partial class IPropertyValueImpl
{
    /// <summary>
    /// Gets a pointer to the managed <c>IPropertyValue</c> implementation, specifically for arrays of <see cref="PropertyType.Int32Array"/> objects.
    /// </summary>
    public static nint Int32ArrayVtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Int32ArrayPropertyValueImpl.Vftbl);
    }
}

/// <summary>
/// The <c>IPropertyValue</c> implementation returning <see cref="PropertyType.Int32Array"/>.
/// </summary>
file static unsafe class Int32ArrayPropertyValueImpl
{
    /// <summary>
    /// The <see cref="IPropertyValueVftbl"/> value for the managed <c>IPropertyValue</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    public static readonly IPropertyValueVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static Int32ArrayPropertyValueImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.get_Type = &get_Type;
        Vftbl.get_IsNumericScalar = &IPropertyValueImpl.get_IsNumericScalarFalse;
        Vftbl.GetUInt8 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt32 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt32 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt64 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt64 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetSingle = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetDouble = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetChar16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetBoolean = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetString = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetGuid = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetDateTime = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetTimeSpan = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetPoint = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetSize = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetRect = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt8Array = (delegate* unmanaged[MemberFunction]<void*, uint*, byte**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt16Array = (delegate* unmanaged[MemberFunction]<void*, uint*, short**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt16Array = (delegate* unmanaged[MemberFunction]<void*, uint*, ushort**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt32Array = &GetInt32Array;
        Vftbl.GetUInt32Array = (delegate* unmanaged[MemberFunction]<void*, uint*, uint**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt64Array = (delegate* unmanaged[MemberFunction]<void*, uint*, long**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt64Array = (delegate* unmanaged[MemberFunction]<void*, uint*, ulong**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetSingleArray = (delegate* unmanaged[MemberFunction]<void*, uint*, float**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetDoubleArray = (delegate* unmanaged[MemberFunction]<void*, uint*, double**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetChar16Array = (delegate* unmanaged[MemberFunction]<void*, uint*, char**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetBooleanArray = (delegate* unmanaged[MemberFunction]<void*, uint*, bool**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetStringArray = (delegate* unmanaged[MemberFunction]<void*, uint*, HSTRING**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInspectableArray = (delegate* unmanaged[MemberFunction]<void*, uint*, void***, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetGuidArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetDateTimeArray = (delegate* unmanaged[MemberFunction]<void*, uint*, ABI.System.DateTimeOffset**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetTimeSpanArray = (delegate* unmanaged[MemberFunction]<void*, uint*, ABI.System.TimeSpan**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetPointArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Point**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetSizeArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Size**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetRectArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Rect**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.type"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT get_Type(void* thisPtr, PropertyType* value)
    {
        if (value is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        *value = PropertyType.Int32Array;

        return WellKnownErrorCodes.S_OK;
    }

    /// <seealso href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.getint32array"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static HRESULT GetInt32Array(void* thisPtr, uint* size, int** value)
    {
        if (size is null || value is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            int[] thisObject = ComInterfaceDispatch.GetInstance<int[]>((ComInterfaceDispatch*)thisPtr);

            WindowsRuntimeBlittableValueTypeArrayMarshaller<int>.ConvertToUnmanaged(thisObject, out *size, out *value);

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}

/// <inheritdoc cref="IPropertyValueImpl"/>
public unsafe partial class IPropertyValueImpl
{
    /// <summary>
    /// Gets a pointer to the managed <c>IPropertyValue</c> implementation, specifically for arrays of <see cref="PropertyType.UInt32Array"/> objects.
    /// </summary>
    public static nint UInt32ArrayVtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in UInt32ArrayPropertyValueImpl.Vftbl);
    }
}

/// <summary>
/// The <c>IPropertyValue</c> implementation returning <see cref="PropertyType.UInt32Array"/>.
/// </summary>
file static unsafe class UInt32ArrayPropertyValueImpl
{
    /// <summary>
    /// The <see cref="IPropertyValueVftbl"/> value for the managed <c>IPropertyValue</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    public static readonly IPropertyValueVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static UInt32ArrayPropertyValueImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.get_Type = &get_Type;
        Vftbl.get_IsNumericScalar = &IPropertyValueImpl.get_IsNumericScalarFalse;
        Vftbl.GetUInt8 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt32 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt32 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt64 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt64 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetSingle = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetDouble = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetChar16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetBoolean = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetString = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetGuid = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetDateTime = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetTimeSpan = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetPoint = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetSize = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetRect = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt8Array = (delegate* unmanaged[MemberFunction]<void*, uint*, byte**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt16Array = (delegate* unmanaged[MemberFunction]<void*, uint*, short**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt16Array = (delegate* unmanaged[MemberFunction]<void*, uint*, ushort**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt32Array = (delegate* unmanaged[MemberFunction]<void*, uint*, int**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt32Array = &GetUInt32Array;
        Vftbl.GetInt64Array = (delegate* unmanaged[MemberFunction]<void*, uint*, long**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt64Array = (delegate* unmanaged[MemberFunction]<void*, uint*, ulong**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetSingleArray = (delegate* unmanaged[MemberFunction]<void*, uint*, float**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetDoubleArray = (delegate* unmanaged[MemberFunction]<void*, uint*, double**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetChar16Array = (delegate* unmanaged[MemberFunction]<void*, uint*, char**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetBooleanArray = (delegate* unmanaged[MemberFunction]<void*, uint*, bool**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetStringArray = (delegate* unmanaged[MemberFunction]<void*, uint*, HSTRING**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInspectableArray = (delegate* unmanaged[MemberFunction]<void*, uint*, void***, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetGuidArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetDateTimeArray = (delegate* unmanaged[MemberFunction]<void*, uint*, ABI.System.DateTimeOffset**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetTimeSpanArray = (delegate* unmanaged[MemberFunction]<void*, uint*, ABI.System.TimeSpan**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetPointArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Point**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetSizeArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Size**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetRectArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Rect**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.type"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT get_Type(void* thisPtr, PropertyType* value)
    {
        if (value is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        *value = PropertyType.UInt32Array;

        return WellKnownErrorCodes.S_OK;
    }

    /// <seealso href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.getuint32array"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static HRESULT GetUInt32Array(void* thisPtr, uint* size, uint** value)
    {
        if (size is null || value is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            uint[] thisObject = ComInterfaceDispatch.GetInstance<uint[]>((ComInterfaceDispatch*)thisPtr);

            WindowsRuntimeBlittableValueTypeArrayMarshaller<uint>.ConvertToUnmanaged(thisObject, out *size, out *value);

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}

/// <inheritdoc cref="IPropertyValueImpl"/>
public unsafe partial class IPropertyValueImpl
{
    /// <summary>
    /// Gets a pointer to the managed <c>IPropertyValue</c> implementation, specifically for arrays of <see cref="PropertyType.Int64Array"/> objects.
    /// </summary>
    public static nint Int64ArrayVtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Int64ArrayPropertyValueImpl.Vftbl);
    }
}

/// <summary>
/// The <c>IPropertyValue</c> implementation returning <see cref="PropertyType.Int64Array"/>.
/// </summary>
file static unsafe class Int64ArrayPropertyValueImpl
{
    /// <summary>
    /// The <see cref="IPropertyValueVftbl"/> value for the managed <c>IPropertyValue</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    public static readonly IPropertyValueVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static Int64ArrayPropertyValueImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.get_Type = &get_Type;
        Vftbl.get_IsNumericScalar = &IPropertyValueImpl.get_IsNumericScalarFalse;
        Vftbl.GetUInt8 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt32 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt32 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt64 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt64 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetSingle = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetDouble = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetChar16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetBoolean = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetString = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetGuid = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetDateTime = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetTimeSpan = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetPoint = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetSize = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetRect = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt8Array = (delegate* unmanaged[MemberFunction]<void*, uint*, byte**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt16Array = (delegate* unmanaged[MemberFunction]<void*, uint*, short**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt16Array = (delegate* unmanaged[MemberFunction]<void*, uint*, ushort**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt32Array = (delegate* unmanaged[MemberFunction]<void*, uint*, int**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt32Array = (delegate* unmanaged[MemberFunction]<void*, uint*, uint**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt64Array = &GetInt64Array;
        Vftbl.GetUInt64Array = (delegate* unmanaged[MemberFunction]<void*, uint*, ulong**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetSingleArray = (delegate* unmanaged[MemberFunction]<void*, uint*, float**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetDoubleArray = (delegate* unmanaged[MemberFunction]<void*, uint*, double**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetChar16Array = (delegate* unmanaged[MemberFunction]<void*, uint*, char**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetBooleanArray = (delegate* unmanaged[MemberFunction]<void*, uint*, bool**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetStringArray = (delegate* unmanaged[MemberFunction]<void*, uint*, HSTRING**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInspectableArray = (delegate* unmanaged[MemberFunction]<void*, uint*, void***, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetGuidArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetDateTimeArray = (delegate* unmanaged[MemberFunction]<void*, uint*, ABI.System.DateTimeOffset**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetTimeSpanArray = (delegate* unmanaged[MemberFunction]<void*, uint*, ABI.System.TimeSpan**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetPointArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Point**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetSizeArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Size**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetRectArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Rect**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.type"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT get_Type(void* thisPtr, PropertyType* value)
    {
        if (value is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        *value = PropertyType.Int64Array;

        return WellKnownErrorCodes.S_OK;
    }

    /// <seealso href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.getint64array"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static HRESULT GetInt64Array(void* thisPtr, uint* size, long** value)
    {
        if (size is null || value is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            long[] thisObject = ComInterfaceDispatch.GetInstance<long[]>((ComInterfaceDispatch*)thisPtr);

            WindowsRuntimeBlittableValueTypeArrayMarshaller<long>.ConvertToUnmanaged(thisObject, out *size, out *value);

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}

/// <inheritdoc cref="IPropertyValueImpl"/>
public unsafe partial class IPropertyValueImpl
{
    /// <summary>
    /// Gets a pointer to the managed <c>IPropertyValue</c> implementation, specifically for arrays of <see cref="PropertyType.UInt64Array"/> objects.
    /// </summary>
    public static nint UInt64ArrayVtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in UInt64ArrayPropertyValueImpl.Vftbl);
    }
}

/// <summary>
/// The <c>IPropertyValue</c> implementation returning <see cref="PropertyType.UInt64Array"/>.
/// </summary>
file static unsafe class UInt64ArrayPropertyValueImpl
{
    /// <summary>
    /// The <see cref="IPropertyValueVftbl"/> value for the managed <c>IPropertyValue</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    public static readonly IPropertyValueVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static UInt64ArrayPropertyValueImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.get_Type = &get_Type;
        Vftbl.get_IsNumericScalar = &IPropertyValueImpl.get_IsNumericScalarFalse;
        Vftbl.GetUInt8 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt32 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt32 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt64 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt64 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetSingle = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetDouble = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetChar16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetBoolean = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetString = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetGuid = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetDateTime = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetTimeSpan = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetPoint = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetSize = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetRect = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt8Array = (delegate* unmanaged[MemberFunction]<void*, uint*, byte**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt16Array = (delegate* unmanaged[MemberFunction]<void*, uint*, short**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt16Array = (delegate* unmanaged[MemberFunction]<void*, uint*, ushort**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt32Array = (delegate* unmanaged[MemberFunction]<void*, uint*, int**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt32Array = (delegate* unmanaged[MemberFunction]<void*, uint*, uint**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt64Array = (delegate* unmanaged[MemberFunction]<void*, uint*, long**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt64Array = &GetUInt64Array;
        Vftbl.GetSingleArray = (delegate* unmanaged[MemberFunction]<void*, uint*, float**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetDoubleArray = (delegate* unmanaged[MemberFunction]<void*, uint*, double**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetChar16Array = (delegate* unmanaged[MemberFunction]<void*, uint*, char**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetBooleanArray = (delegate* unmanaged[MemberFunction]<void*, uint*, bool**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetStringArray = (delegate* unmanaged[MemberFunction]<void*, uint*, HSTRING**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInspectableArray = (delegate* unmanaged[MemberFunction]<void*, uint*, void***, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetGuidArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetDateTimeArray = (delegate* unmanaged[MemberFunction]<void*, uint*, ABI.System.DateTimeOffset**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetTimeSpanArray = (delegate* unmanaged[MemberFunction]<void*, uint*, ABI.System.TimeSpan**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetPointArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Point**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetSizeArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Size**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetRectArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Rect**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.type"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT get_Type(void* thisPtr, PropertyType* value)
    {
        if (value is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        *value = PropertyType.UInt64Array;

        return WellKnownErrorCodes.S_OK;
    }

    /// <seealso href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.getuint64array"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static HRESULT GetUInt64Array(void* thisPtr, uint* size, ulong** value)
    {
        if (size is null || value is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            ulong[] thisObject = ComInterfaceDispatch.GetInstance<ulong[]>((ComInterfaceDispatch*)thisPtr);

            WindowsRuntimeBlittableValueTypeArrayMarshaller<ulong>.ConvertToUnmanaged(thisObject, out *size, out *value);

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}

/// <inheritdoc cref="IPropertyValueImpl"/>
public unsafe partial class IPropertyValueImpl
{
    /// <summary>
    /// Gets a pointer to the managed <c>IPropertyValue</c> implementation, specifically for arrays of <see cref="PropertyType.SingleArray"/> objects.
    /// </summary>
    public static nint SingleArrayVtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in SingleArrayPropertyValueImpl.Vftbl);
    }
}

/// <summary>
/// The <c>IPropertyValue</c> implementation returning <see cref="PropertyType.SingleArray"/>.
/// </summary>
file static unsafe class SingleArrayPropertyValueImpl
{
    /// <summary>
    /// The <see cref="IPropertyValueVftbl"/> value for the managed <c>IPropertyValue</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    public static readonly IPropertyValueVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static SingleArrayPropertyValueImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.get_Type = &get_Type;
        Vftbl.get_IsNumericScalar = &IPropertyValueImpl.get_IsNumericScalarFalse;
        Vftbl.GetUInt8 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt32 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt32 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt64 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt64 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetSingle = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetDouble = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetChar16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetBoolean = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetString = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetGuid = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetDateTime = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetTimeSpan = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetPoint = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetSize = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetRect = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt8Array = (delegate* unmanaged[MemberFunction]<void*, uint*, byte**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt16Array = (delegate* unmanaged[MemberFunction]<void*, uint*, short**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt16Array = (delegate* unmanaged[MemberFunction]<void*, uint*, ushort**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt32Array = (delegate* unmanaged[MemberFunction]<void*, uint*, int**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt32Array = (delegate* unmanaged[MemberFunction]<void*, uint*, uint**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt64Array = (delegate* unmanaged[MemberFunction]<void*, uint*, long**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt64Array = (delegate* unmanaged[MemberFunction]<void*, uint*, ulong**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetSingleArray = &GetSingleArray;
        Vftbl.GetDoubleArray = (delegate* unmanaged[MemberFunction]<void*, uint*, double**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetChar16Array = (delegate* unmanaged[MemberFunction]<void*, uint*, char**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetBooleanArray = (delegate* unmanaged[MemberFunction]<void*, uint*, bool**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetStringArray = (delegate* unmanaged[MemberFunction]<void*, uint*, HSTRING**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInspectableArray = (delegate* unmanaged[MemberFunction]<void*, uint*, void***, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetGuidArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetDateTimeArray = (delegate* unmanaged[MemberFunction]<void*, uint*, ABI.System.DateTimeOffset**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetTimeSpanArray = (delegate* unmanaged[MemberFunction]<void*, uint*, ABI.System.TimeSpan**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetPointArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Point**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetSizeArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Size**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetRectArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Rect**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.type"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT get_Type(void* thisPtr, PropertyType* value)
    {
        if (value is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        *value = PropertyType.SingleArray;

        return WellKnownErrorCodes.S_OK;
    }

    /// <seealso href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.getsinglearray"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static HRESULT GetSingleArray(void* thisPtr, uint* size, float** value)
    {
        if (size is null || value is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            float[] thisObject = ComInterfaceDispatch.GetInstance<float[]>((ComInterfaceDispatch*)thisPtr);

            WindowsRuntimeBlittableValueTypeArrayMarshaller<float>.ConvertToUnmanaged(thisObject, out *size, out *value);

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}

/// <inheritdoc cref="IPropertyValueImpl"/>
public unsafe partial class IPropertyValueImpl
{
    /// <summary>
    /// Gets a pointer to the managed <c>IPropertyValue</c> implementation, specifically for arrays of <see cref="PropertyType.DoubleArray"/> objects.
    /// </summary>
    public static nint DoubleArrayVtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in DoubleArrayPropertyValueImpl.Vftbl);
    }
}

/// <summary>
/// The <c>IPropertyValue</c> implementation returning <see cref="PropertyType.DoubleArray"/>.
/// </summary>
file static unsafe class DoubleArrayPropertyValueImpl
{
    /// <summary>
    /// The <see cref="IPropertyValueVftbl"/> value for the managed <c>IPropertyValue</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    public static readonly IPropertyValueVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static DoubleArrayPropertyValueImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.get_Type = &get_Type;
        Vftbl.get_IsNumericScalar = &IPropertyValueImpl.get_IsNumericScalarFalse;
        Vftbl.GetUInt8 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt32 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt32 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt64 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt64 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetSingle = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetDouble = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetChar16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetBoolean = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetString = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetGuid = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetDateTime = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetTimeSpan = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetPoint = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetSize = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetRect = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt8Array = (delegate* unmanaged[MemberFunction]<void*, uint*, byte**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt16Array = (delegate* unmanaged[MemberFunction]<void*, uint*, short**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt16Array = (delegate* unmanaged[MemberFunction]<void*, uint*, ushort**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt32Array = (delegate* unmanaged[MemberFunction]<void*, uint*, int**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt32Array = (delegate* unmanaged[MemberFunction]<void*, uint*, uint**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt64Array = (delegate* unmanaged[MemberFunction]<void*, uint*, long**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt64Array = (delegate* unmanaged[MemberFunction]<void*, uint*, ulong**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetSingleArray = (delegate* unmanaged[MemberFunction]<void*, uint*, float**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetDoubleArray = &GetDoubleArray;
        Vftbl.GetChar16Array = (delegate* unmanaged[MemberFunction]<void*, uint*, char**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetBooleanArray = (delegate* unmanaged[MemberFunction]<void*, uint*, bool**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetStringArray = (delegate* unmanaged[MemberFunction]<void*, uint*, HSTRING**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInspectableArray = (delegate* unmanaged[MemberFunction]<void*, uint*, void***, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetGuidArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetDateTimeArray = (delegate* unmanaged[MemberFunction]<void*, uint*, ABI.System.DateTimeOffset**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetTimeSpanArray = (delegate* unmanaged[MemberFunction]<void*, uint*, ABI.System.TimeSpan**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetPointArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Point**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetSizeArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Size**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetRectArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Rect**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.type"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT get_Type(void* thisPtr, PropertyType* value)
    {
        if (value is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        *value = PropertyType.DoubleArray;

        return WellKnownErrorCodes.S_OK;
    }

    /// <seealso href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.getdoublearray"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static HRESULT GetDoubleArray(void* thisPtr, uint* size, double** value)
    {
        if (size is null || value is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            double[] thisObject = ComInterfaceDispatch.GetInstance<double[]>((ComInterfaceDispatch*)thisPtr);

            WindowsRuntimeBlittableValueTypeArrayMarshaller<double>.ConvertToUnmanaged(thisObject, out *size, out *value);

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}

/// <inheritdoc cref="IPropertyValueImpl"/>
public unsafe partial class IPropertyValueImpl
{
    /// <summary>
    /// Gets a pointer to the managed <c>IPropertyValue</c> implementation, specifically for arrays of <see cref="PropertyType.Char16Array"/> objects.
    /// </summary>
    public static nint Char16ArrayVtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Char16ArrayPropertyValueImpl.Vftbl);
    }
}

/// <summary>
/// The <c>IPropertyValue</c> implementation returning <see cref="PropertyType.Char16Array"/>.
/// </summary>
file static unsafe class Char16ArrayPropertyValueImpl
{
    /// <summary>
    /// The <see cref="IPropertyValueVftbl"/> value for the managed <c>IPropertyValue</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    public static readonly IPropertyValueVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static Char16ArrayPropertyValueImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.get_Type = &get_Type;
        Vftbl.get_IsNumericScalar = &IPropertyValueImpl.get_IsNumericScalarFalse;
        Vftbl.GetUInt8 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt32 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt32 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt64 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt64 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetSingle = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetDouble = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetChar16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetBoolean = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetString = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetGuid = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetDateTime = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetTimeSpan = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetPoint = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetSize = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetRect = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt8Array = (delegate* unmanaged[MemberFunction]<void*, uint*, byte**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt16Array = (delegate* unmanaged[MemberFunction]<void*, uint*, short**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt16Array = (delegate* unmanaged[MemberFunction]<void*, uint*, ushort**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt32Array = (delegate* unmanaged[MemberFunction]<void*, uint*, int**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt32Array = (delegate* unmanaged[MemberFunction]<void*, uint*, uint**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt64Array = (delegate* unmanaged[MemberFunction]<void*, uint*, long**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt64Array = (delegate* unmanaged[MemberFunction]<void*, uint*, ulong**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetSingleArray = (delegate* unmanaged[MemberFunction]<void*, uint*, float**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetDoubleArray = (delegate* unmanaged[MemberFunction]<void*, uint*, double**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetChar16Array = &GetChar16Array;
        Vftbl.GetBooleanArray = (delegate* unmanaged[MemberFunction]<void*, uint*, bool**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetStringArray = (delegate* unmanaged[MemberFunction]<void*, uint*, HSTRING**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInspectableArray = (delegate* unmanaged[MemberFunction]<void*, uint*, void***, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetGuidArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetDateTimeArray = (delegate* unmanaged[MemberFunction]<void*, uint*, ABI.System.DateTimeOffset**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetTimeSpanArray = (delegate* unmanaged[MemberFunction]<void*, uint*, ABI.System.TimeSpan**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetPointArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Point**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetSizeArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Size**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetRectArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Rect**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.type"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT get_Type(void* thisPtr, PropertyType* value)
    {
        if (value is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        *value = PropertyType.Char16Array;

        return WellKnownErrorCodes.S_OK;
    }

    /// <seealso href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.getchar16array"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static HRESULT GetChar16Array(void* thisPtr, uint* size, char** value)
    {
        if (size is null || value is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            char[] thisObject = ComInterfaceDispatch.GetInstance<char[]>((ComInterfaceDispatch*)thisPtr);

            WindowsRuntimeBlittableValueTypeArrayMarshaller<char>.ConvertToUnmanaged(thisObject, out *size, out *value);

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}

/// <inheritdoc cref="IPropertyValueImpl"/>
public unsafe partial class IPropertyValueImpl
{
    /// <summary>
    /// Gets a pointer to the managed <c>IPropertyValue</c> implementation, specifically for arrays of <see cref="PropertyType.BooleanArray"/> objects.
    /// </summary>
    public static nint BooleanArrayVtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in BooleanArrayPropertyValueImpl.Vftbl);
    }
}

/// <summary>
/// The <c>IPropertyValue</c> implementation returning <see cref="PropertyType.BooleanArray"/>.
/// </summary>
file static unsafe class BooleanArrayPropertyValueImpl
{
    /// <summary>
    /// The <see cref="IPropertyValueVftbl"/> value for the managed <c>IPropertyValue</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    public static readonly IPropertyValueVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static BooleanArrayPropertyValueImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.get_Type = &get_Type;
        Vftbl.get_IsNumericScalar = &IPropertyValueImpl.get_IsNumericScalarFalse;
        Vftbl.GetUInt8 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt32 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt32 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt64 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt64 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetSingle = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetDouble = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetChar16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetBoolean = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetString = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetGuid = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetDateTime = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetTimeSpan = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetPoint = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetSize = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetRect = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt8Array = (delegate* unmanaged[MemberFunction]<void*, uint*, byte**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt16Array = (delegate* unmanaged[MemberFunction]<void*, uint*, short**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt16Array = (delegate* unmanaged[MemberFunction]<void*, uint*, ushort**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt32Array = (delegate* unmanaged[MemberFunction]<void*, uint*, int**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt32Array = (delegate* unmanaged[MemberFunction]<void*, uint*, uint**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt64Array = (delegate* unmanaged[MemberFunction]<void*, uint*, long**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt64Array = (delegate* unmanaged[MemberFunction]<void*, uint*, ulong**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetSingleArray = (delegate* unmanaged[MemberFunction]<void*, uint*, float**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetDoubleArray = (delegate* unmanaged[MemberFunction]<void*, uint*, double**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetChar16Array = (delegate* unmanaged[MemberFunction]<void*, uint*, char**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetBooleanArray = &GetBooleanArray;
        Vftbl.GetStringArray = (delegate* unmanaged[MemberFunction]<void*, uint*, HSTRING**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInspectableArray = (delegate* unmanaged[MemberFunction]<void*, uint*, void***, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetGuidArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetDateTimeArray = (delegate* unmanaged[MemberFunction]<void*, uint*, ABI.System.DateTimeOffset**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetTimeSpanArray = (delegate* unmanaged[MemberFunction]<void*, uint*, ABI.System.TimeSpan**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetPointArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Point**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetSizeArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Size**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetRectArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Rect**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.type"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT get_Type(void* thisPtr, PropertyType* value)
    {
        if (value is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        *value = PropertyType.BooleanArray;

        return WellKnownErrorCodes.S_OK;
    }

    /// <seealso href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.getbooleanarray"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static HRESULT GetBooleanArray(void* thisPtr, uint* size, bool** value)
    {
        if (size is null || value is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            bool[] thisObject = ComInterfaceDispatch.GetInstance<bool[]>((ComInterfaceDispatch*)thisPtr);

            WindowsRuntimeBlittableValueTypeArrayMarshaller<bool>.ConvertToUnmanaged(thisObject, out *size, out *value);

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}

/// <inheritdoc cref="IPropertyValueImpl"/>
public unsafe partial class IPropertyValueImpl
{
    /// <summary>
    /// Gets a pointer to the managed <c>IPropertyValue</c> implementation, specifically for arrays of <see cref="PropertyType.GuidArray"/> objects.
    /// </summary>
    public static nint GuidArrayVtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in GuidArrayPropertyValueImpl.Vftbl);
    }
}

/// <summary>
/// The <c>IPropertyValue</c> implementation returning <see cref="PropertyType.GuidArray"/>.
/// </summary>
file static unsafe class GuidArrayPropertyValueImpl
{
    /// <summary>
    /// The <see cref="IPropertyValueVftbl"/> value for the managed <c>IPropertyValue</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    public static readonly IPropertyValueVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static GuidArrayPropertyValueImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.get_Type = &get_Type;
        Vftbl.get_IsNumericScalar = &IPropertyValueImpl.get_IsNumericScalarFalse;
        Vftbl.GetUInt8 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt32 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt32 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt64 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt64 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetSingle = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetDouble = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetChar16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetBoolean = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetString = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetGuid = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetDateTime = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetTimeSpan = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetPoint = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetSize = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetRect = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt8Array = (delegate* unmanaged[MemberFunction]<void*, uint*, byte**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt16Array = (delegate* unmanaged[MemberFunction]<void*, uint*, short**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt16Array = (delegate* unmanaged[MemberFunction]<void*, uint*, ushort**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt32Array = (delegate* unmanaged[MemberFunction]<void*, uint*, int**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt32Array = (delegate* unmanaged[MemberFunction]<void*, uint*, uint**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt64Array = (delegate* unmanaged[MemberFunction]<void*, uint*, long**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt64Array = (delegate* unmanaged[MemberFunction]<void*, uint*, ulong**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetSingleArray = (delegate* unmanaged[MemberFunction]<void*, uint*, float**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetDoubleArray = (delegate* unmanaged[MemberFunction]<void*, uint*, double**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetChar16Array = (delegate* unmanaged[MemberFunction]<void*, uint*, char**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetBooleanArray = (delegate* unmanaged[MemberFunction]<void*, uint*, bool**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetStringArray = (delegate* unmanaged[MemberFunction]<void*, uint*, HSTRING**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInspectableArray = (delegate* unmanaged[MemberFunction]<void*, uint*, void***, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetGuidArray = &GetGuidArray;
        Vftbl.GetDateTimeArray = (delegate* unmanaged[MemberFunction]<void*, uint*, ABI.System.DateTimeOffset**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetTimeSpanArray = (delegate* unmanaged[MemberFunction]<void*, uint*, ABI.System.TimeSpan**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetPointArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Point**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetSizeArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Size**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetRectArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Rect**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.type"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT get_Type(void* thisPtr, PropertyType* value)
    {
        if (value is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        *value = PropertyType.GuidArray;

        return WellKnownErrorCodes.S_OK;
    }

    /// <seealso href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.getguidarray"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static HRESULT GetGuidArray(void* thisPtr, uint* size, Guid** value)
    {
        if (size is null || value is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            Guid[] thisObject = ComInterfaceDispatch.GetInstance<Guid[]>((ComInterfaceDispatch*)thisPtr);

            WindowsRuntimeBlittableValueTypeArrayMarshaller<Guid>.ConvertToUnmanaged(thisObject, out *size, out *value);

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}

/// <inheritdoc cref="IPropertyValueImpl"/>
public unsafe partial class IPropertyValueImpl
{
    /// <summary>
    /// Gets a pointer to the managed <c>IPropertyValue</c> implementation, specifically for arrays of <see cref="PropertyType.PointArray"/> objects.
    /// </summary>
    public static nint PointArrayVtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in PointArrayPropertyValueImpl.Vftbl);
    }
}

/// <summary>
/// The <c>IPropertyValue</c> implementation returning <see cref="PropertyType.PointArray"/>.
/// </summary>
file static unsafe class PointArrayPropertyValueImpl
{
    /// <summary>
    /// The <see cref="IPropertyValueVftbl"/> value for the managed <c>IPropertyValue</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    public static readonly IPropertyValueVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static PointArrayPropertyValueImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.get_Type = &get_Type;
        Vftbl.get_IsNumericScalar = &IPropertyValueImpl.get_IsNumericScalarFalse;
        Vftbl.GetUInt8 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt32 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt32 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt64 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt64 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetSingle = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetDouble = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetChar16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetBoolean = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetString = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetGuid = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetDateTime = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetTimeSpan = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetPoint = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetSize = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetRect = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt8Array = (delegate* unmanaged[MemberFunction]<void*, uint*, byte**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt16Array = (delegate* unmanaged[MemberFunction]<void*, uint*, short**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt16Array = (delegate* unmanaged[MemberFunction]<void*, uint*, ushort**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt32Array = (delegate* unmanaged[MemberFunction]<void*, uint*, int**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt32Array = (delegate* unmanaged[MemberFunction]<void*, uint*, uint**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt64Array = (delegate* unmanaged[MemberFunction]<void*, uint*, long**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt64Array = (delegate* unmanaged[MemberFunction]<void*, uint*, ulong**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetSingleArray = (delegate* unmanaged[MemberFunction]<void*, uint*, float**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetDoubleArray = (delegate* unmanaged[MemberFunction]<void*, uint*, double**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetChar16Array = (delegate* unmanaged[MemberFunction]<void*, uint*, char**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetBooleanArray = (delegate* unmanaged[MemberFunction]<void*, uint*, bool**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetStringArray = (delegate* unmanaged[MemberFunction]<void*, uint*, HSTRING**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInspectableArray = (delegate* unmanaged[MemberFunction]<void*, uint*, void***, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetGuidArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetDateTimeArray = (delegate* unmanaged[MemberFunction]<void*, uint*, ABI.System.DateTimeOffset**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetTimeSpanArray = (delegate* unmanaged[MemberFunction]<void*, uint*, ABI.System.TimeSpan**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetPointArray = &GetPointArray;
        Vftbl.GetSizeArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Size**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetRectArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Rect**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.type"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT get_Type(void* thisPtr, PropertyType* value)
    {
        if (value is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        *value = PropertyType.PointArray;

        return WellKnownErrorCodes.S_OK;
    }

    /// <seealso href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.getpointarray"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static HRESULT GetPointArray(void* thisPtr, uint* size, Point** value)
    {
        if (size is null || value is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            Point[] thisObject = ComInterfaceDispatch.GetInstance<Point[]>((ComInterfaceDispatch*)thisPtr);

            WindowsRuntimeBlittableValueTypeArrayMarshaller<Point>.ConvertToUnmanaged(thisObject, out *size, out *value);

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}

/// <inheritdoc cref="IPropertyValueImpl"/>
public unsafe partial class IPropertyValueImpl
{
    /// <summary>
    /// Gets a pointer to the managed <c>IPropertyValue</c> implementation, specifically for arrays of <see cref="PropertyType.SizeArray"/> objects.
    /// </summary>
    public static nint SizeArrayVtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in SizeArrayPropertyValueImpl.Vftbl);
    }
}

/// <summary>
/// The <c>IPropertyValue</c> implementation returning <see cref="PropertyType.SizeArray"/>.
/// </summary>
file static unsafe class SizeArrayPropertyValueImpl
{
    /// <summary>
    /// The <see cref="IPropertyValueVftbl"/> value for the managed <c>IPropertyValue</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    public static readonly IPropertyValueVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static SizeArrayPropertyValueImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.get_Type = &get_Type;
        Vftbl.get_IsNumericScalar = &IPropertyValueImpl.get_IsNumericScalarFalse;
        Vftbl.GetUInt8 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt32 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt32 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt64 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt64 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetSingle = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetDouble = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetChar16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetBoolean = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetString = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetGuid = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetDateTime = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetTimeSpan = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetPoint = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetSize = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetRect = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt8Array = (delegate* unmanaged[MemberFunction]<void*, uint*, byte**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt16Array = (delegate* unmanaged[MemberFunction]<void*, uint*, short**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt16Array = (delegate* unmanaged[MemberFunction]<void*, uint*, ushort**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt32Array = (delegate* unmanaged[MemberFunction]<void*, uint*, int**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt32Array = (delegate* unmanaged[MemberFunction]<void*, uint*, uint**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt64Array = (delegate* unmanaged[MemberFunction]<void*, uint*, long**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt64Array = (delegate* unmanaged[MemberFunction]<void*, uint*, ulong**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetSingleArray = (delegate* unmanaged[MemberFunction]<void*, uint*, float**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetDoubleArray = (delegate* unmanaged[MemberFunction]<void*, uint*, double**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetChar16Array = (delegate* unmanaged[MemberFunction]<void*, uint*, char**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetBooleanArray = (delegate* unmanaged[MemberFunction]<void*, uint*, bool**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetStringArray = (delegate* unmanaged[MemberFunction]<void*, uint*, HSTRING**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInspectableArray = (delegate* unmanaged[MemberFunction]<void*, uint*, void***, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetGuidArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetDateTimeArray = (delegate* unmanaged[MemberFunction]<void*, uint*, ABI.System.DateTimeOffset**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetTimeSpanArray = (delegate* unmanaged[MemberFunction]<void*, uint*, ABI.System.TimeSpan**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetPointArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Point**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetSizeArray = &GetSizeArray;
        Vftbl.GetRectArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Rect**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.type"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT get_Type(void* thisPtr, PropertyType* value)
    {
        if (value is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        *value = PropertyType.SizeArray;

        return WellKnownErrorCodes.S_OK;
    }

    /// <seealso href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.getsizearray"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static HRESULT GetSizeArray(void* thisPtr, uint* size, Size** value)
    {
        if (size is null || value is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            Size[] thisObject = ComInterfaceDispatch.GetInstance<Size[]>((ComInterfaceDispatch*)thisPtr);

            WindowsRuntimeBlittableValueTypeArrayMarshaller<Size>.ConvertToUnmanaged(thisObject, out *size, out *value);

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}

/// <inheritdoc cref="IPropertyValueImpl"/>
public unsafe partial class IPropertyValueImpl
{
    /// <summary>
    /// Gets a pointer to the managed <c>IPropertyValue</c> implementation, specifically for arrays of <see cref="PropertyType.RectArray"/> objects.
    /// </summary>
    public static nint RectArrayVtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in RectArrayPropertyValueImpl.Vftbl);
    }
}

/// <summary>
/// The <c>IPropertyValue</c> implementation returning <see cref="PropertyType.RectArray"/>.
/// </summary>
file static unsafe class RectArrayPropertyValueImpl
{
    /// <summary>
    /// The <see cref="IPropertyValueVftbl"/> value for the managed <c>IPropertyValue</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    public static readonly IPropertyValueVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static RectArrayPropertyValueImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.get_Type = &get_Type;
        Vftbl.get_IsNumericScalar = &IPropertyValueImpl.get_IsNumericScalarFalse;
        Vftbl.GetUInt8 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt32 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt32 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetInt64 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt64 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetSingle = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetDouble = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetChar16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetBoolean = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetString = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetGuid = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetDateTime = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetTimeSpan = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetPoint = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetSize = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetRect = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt8Array = (delegate* unmanaged[MemberFunction]<void*, uint*, byte**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt16Array = (delegate* unmanaged[MemberFunction]<void*, uint*, short**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt16Array = (delegate* unmanaged[MemberFunction]<void*, uint*, ushort**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt32Array = (delegate* unmanaged[MemberFunction]<void*, uint*, int**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt32Array = (delegate* unmanaged[MemberFunction]<void*, uint*, uint**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt64Array = (delegate* unmanaged[MemberFunction]<void*, uint*, long**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt64Array = (delegate* unmanaged[MemberFunction]<void*, uint*, ulong**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetSingleArray = (delegate* unmanaged[MemberFunction]<void*, uint*, float**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetDoubleArray = (delegate* unmanaged[MemberFunction]<void*, uint*, double**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetChar16Array = (delegate* unmanaged[MemberFunction]<void*, uint*, char**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetBooleanArray = (delegate* unmanaged[MemberFunction]<void*, uint*, bool**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetStringArray = (delegate* unmanaged[MemberFunction]<void*, uint*, HSTRING**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInspectableArray = (delegate* unmanaged[MemberFunction]<void*, uint*, void***, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetGuidArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetDateTimeArray = (delegate* unmanaged[MemberFunction]<void*, uint*, ABI.System.DateTimeOffset**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetTimeSpanArray = (delegate* unmanaged[MemberFunction]<void*, uint*, ABI.System.TimeSpan**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetPointArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Point**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetSizeArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Size**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetRectArray = &GetRectArray;
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.type"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT get_Type(void* thisPtr, PropertyType* value)
    {
        if (value is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        *value = PropertyType.RectArray;

        return WellKnownErrorCodes.S_OK;
    }

    /// <seealso href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.getrectarray"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static HRESULT GetRectArray(void* thisPtr, uint* size, Rect** value)
    {
        if (size is null || value is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            Rect[] thisObject = ComInterfaceDispatch.GetInstance<Rect[]>((ComInterfaceDispatch*)thisPtr);

            WindowsRuntimeBlittableValueTypeArrayMarshaller<Rect>.ConvertToUnmanaged(thisObject, out *size, out *value);

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}
#endif
