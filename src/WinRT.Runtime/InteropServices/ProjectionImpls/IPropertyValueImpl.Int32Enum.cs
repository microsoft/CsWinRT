// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using Windows.Foundation;

#pragma warning disable CA1416

namespace WindowsRuntime.InteropServices;

/// <inheritdoc cref="IPropertyValueImpl"/>
public unsafe partial class IPropertyValueImpl
{
    /// <summary>
    /// Gets the <c>IPropertyValue</c> vtable for signed 32-bit enums.
    /// </summary>
    public static nint Int32EnumVtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Int32EnumPropertyValueImpl.Vftbl);
    }
}

/// <summary>
/// Numeric coercion for signed enums, preserving their <c>OtherType</c> identity.
/// </summary>
file static unsafe class Int32EnumPropertyValueImpl
{
    /// <summary>
    /// The enum vtable, sharing numeric getters with the underlying primitive.
    /// </summary>
    [FixedAddressValueType]
    public static readonly IPropertyValueVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static Int32EnumPropertyValueImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.get_Type = &IPropertyValueImpl.get_TypeOtherType;
        Vftbl.get_IsNumericScalar = &IPropertyValueImpl.get_IsNumericScalarFalse;

        // Boxed enums can be unboxed as their underlying primitive, so reuse its coercion stubs
        Vftbl.GetUInt8 = &ABI.System.Int32PropertyValueImpl.GetUInt8;
        Vftbl.GetInt16 = &ABI.System.Int32PropertyValueImpl.GetInt16;
        Vftbl.GetUInt16 = &ABI.System.Int32PropertyValueImpl.GetUInt16;
        Vftbl.GetInt32 = &ABI.System.Int32ReferenceImpl.get_Value;
        Vftbl.GetUInt32 = &ABI.System.Int32PropertyValueImpl.GetUInt32;
        Vftbl.GetInt64 = &ABI.System.Int32PropertyValueImpl.GetInt64;
        Vftbl.GetUInt64 = &ABI.System.Int32PropertyValueImpl.GetUInt64;
        Vftbl.GetSingle = &ABI.System.Int32PropertyValueImpl.GetSingle;
        Vftbl.GetDouble = &ABI.System.Int32PropertyValueImpl.GetDouble;
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
        Vftbl.GetRectArray = (delegate* unmanaged[MemberFunction]<void*, uint*, Rect**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
    }
}
