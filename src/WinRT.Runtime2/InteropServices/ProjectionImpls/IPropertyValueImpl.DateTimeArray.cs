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
    /// Gets a pointer to the managed <c>IPropertyValue</c> implementation, specifically for arrays of <see cref="PropertyType.DateTimeArray"/> objects.
    /// </summary>
    public static nint DateTimeArrayVtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in DateTimeArrayPropertyValueImpl.Vftbl);
    }
}

/// <summary>
/// The <c>IPropertyValue</c> implementation returning <see cref="PropertyType.DateTimeArray"/>.
/// </summary>
file static unsafe class DateTimeArrayPropertyValueImpl
{
    /// <summary>
    /// The <see cref="IPropertyValueVftbl"/> value for the managed <c>IPropertyValue</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    public static readonly IPropertyValueVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static DateTimeArrayPropertyValueImpl()
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
        Vftbl.GetDateTimeArray = &GetDateTimeArray;
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

        *value = PropertyType.DateTimeArray;

        return WellKnownErrorCodes.S_OK;
    }

    /// <seealso href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.getdatetimearray"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    internal static HRESULT GetDateTimeArray(void* thisPtr, uint* size, ABI.System.DateTimeOffset** value)
    {
        if (size is null || value is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            DateTimeOffset[] thisObject = ComInterfaceDispatch.GetInstance<DateTimeOffset[]>((ComInterfaceDispatch*)thisPtr);

            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = nameof(ConvertToUnmanaged))]
            static extern void ConvertToUnmanaged(
                [UnsafeAccessorType("ABI.System.<#corlib>DateTimeOffsetArrayMarshaller, WinRT.Interop")] object? _,
                DateTimeOffset[] source,
                out uint size,
                out ABI.System.DateTimeOffset* array);

            ConvertToUnmanaged(null, thisObject, out *size, out *value);

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}
#endif