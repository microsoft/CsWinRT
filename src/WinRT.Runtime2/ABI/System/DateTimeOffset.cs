// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Foundation;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;
using static System.Runtime.InteropServices.ComWrappers;

#pragma warning disable CS0649, IDE1006

[assembly: TypeMap<WindowsRuntimeTypeMapGroup>(
    value: "Windows.Foundation.IReference<Windows.Foundation.DateTime>",
    target: typeof(ABI.System.DateTimeOffset),
    trimTarget: typeof(DateTimeOffset))]

[assembly: TypeMapAssociation<WindowsRuntimeTypeMapGroup>(typeof(DateTimeOffset), typeof(ABI.System.DateTimeOffset))]

namespace ABI.System;

/// <summary>
/// ABI type for <see cref="global::System.DateTimeOffset"/>.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.datetime"/>
[EditorBrowsable(EditorBrowsableState.Never)]
[WindowsRuntimeClassName("Windows.Foundation.IReference<Windows.Foundation.DateTime>")]
[DateTimeOffsetComWrappersMarshaller]
public struct DateTimeOffset
{
    /// <summary>
    /// A 64-bit signed integer that represents a point in time as the number of 100-nanosecond intervals prior to or after midnight on January 1, 1601 (according to the Gregorian Calendar).
    /// </summary>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.datetime.universaltime"/>
    public ulong UniversalTime;
}

/// <summary>
/// Marshaller for <see cref="global::System.DateTimeOffset"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class DateTimeOffsetMarshaller
{
    /// <summary>
    /// The number of ticks counted between <c>0001-01-01, 00:00:00</c> and <c>1601-01-01, 00:00:00</c>.
    /// This is equivalent to the result of this expression:
    /// <code lang="csharp">
    /// var ticks = new DateTimeOffset(1601, 1, 1, 0, 0, 1, TimeSpan.Zero).Ticks;
    /// </code>
    /// </summary>
    private const long ManagedUtcTicksAtNativeZero = 504911232000000000;

    /// <summary>
    /// Converts a managed <see cref="global::System.DateTimeOffset"/> to an unmanaged <see cref="DateTimeOffset"/>.
    /// </summary>
    /// <param name="value">The managed <see cref="global::System.DateTimeOffset"/> value.</param>
    /// <returns>The unmanaged <see cref="DateTimeOffset"/> value.</returns>
    public static DateTimeOffset ConvertToUnmanaged(global::System.DateTimeOffset value)
    {
        return new() { UniversalTime = unchecked((ulong)value.UtcTicks - ManagedUtcTicksAtNativeZero) };
    }

    /// <summary>
    /// Converts an unmanaged <see cref="DateTimeOffset"/> to a managed <see cref="global::System.DateTimeOffset"/>.
    /// </summary>
    /// <param name="value">The unmanaged <see cref="DateTimeOffset"/> value.</param>
    /// <returns>The managed <see cref="global::System.DateTimeOffset"/> value</returns>
    public static global::System.DateTimeOffset ConvertToManaged(DateTimeOffset value)
    {
        return new global::System.DateTimeOffset(unchecked((long)value.UniversalTime + ManagedUtcTicksAtNativeZero), global::System.TimeSpan.Zero);
    }

    /// <inheritdoc cref="WindowsRuntimeValueTypeMarshaller.BoxToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue BoxToUnmanaged(global::System.DateTimeOffset? value)
    {
        return WindowsRuntimeValueTypeMarshaller.BoxToUnmanaged(value, in WellKnownInterfaceIds.IID_IReferenceOfDateTimeOffset);
    }

    /// <inheritdoc cref="WindowsRuntimeValueTypeMarshaller.UnboxToManaged(void*)"/>
    public static global::System.DateTimeOffset? UnboxToManaged(void* value)
    {
        DateTimeOffset? abi = WindowsRuntimeValueTypeMarshaller.UnboxToManaged<DateTimeOffset>(value);

        return abi.HasValue ? ConvertToManaged(abi.GetValueOrDefault()) : null;
    }
}

/// <summary>
/// The set of <see cref="ComInterfaceEntry"/> values for <see cref="global::System.DateTimeOffset"/>.
/// </summary>
file struct DateTimeOffsetInterfaceEntries
{
    public ComInterfaceEntry IReferenceOfDateTimeOffset;
    public ComInterfaceEntry IPropertyValue;
    public ComInterfaceEntry IStringable;
    public ComInterfaceEntry IWeakReferenceSource;
    public ComInterfaceEntry IMarshal;
    public ComInterfaceEntry IAgileObject;
    public ComInterfaceEntry IInspectable;
    public ComInterfaceEntry IUnknown;
}

/// <summary>
/// The implementation of <see cref="DateTimeOffsetInterfaceEntries"/>.
/// </summary>
file static class DateTimeOffsetInterfaceEntriesImpl
{
    /// <summary>
    /// The <see cref="DateTimeOffsetInterfaceEntries"/> value for <see cref="global::System.DateTimeOffset"/>.
    /// </summary>
    [FixedAddressValueType]
    public static readonly DateTimeOffsetInterfaceEntries Entries;

    /// <summary>
    /// Initializes <see cref="Entries"/>.
    /// </summary>
    static DateTimeOffsetInterfaceEntriesImpl()
    {
        Entries.IReferenceOfDateTimeOffset.IID = WellKnownInterfaceIds.IID_IReferenceOfDateTimeOffset;
        Entries.IReferenceOfDateTimeOffset.Vtable = DateTimeOffsetReferenceImpl.Vtable;
        Entries.IPropertyValue.IID = WellKnownInterfaceIds.IID_IPropertyValue;
        Entries.IPropertyValue.Vtable = DateTimeOffsetPropertyValueImpl.Vtable;
        Entries.IStringable.IID = WellKnownInterfaceIds.IID_IStringable;
        Entries.IStringable.Vtable = IStringableImpl.Vtable;
        Entries.IWeakReferenceSource.IID = WellKnownInterfaceIds.IID_IWeakReferenceSource;
        Entries.IWeakReferenceSource.Vtable = IWeakReferenceSourceImpl.Vtable;
        Entries.IMarshal.IID = WellKnownInterfaceIds.IID_IMarshal;
        Entries.IMarshal.Vtable = IMarshalImpl.Vtable;
        Entries.IAgileObject.IID = WellKnownInterfaceIds.IID_IAgileObject;
        Entries.IAgileObject.Vtable = IUnknownImpl.Vtable;
        Entries.IInspectable.IID = WellKnownInterfaceIds.IID_IInspectable;
        Entries.IInspectable.Vtable = IInspectableImpl.Vtable;
        Entries.IUnknown.IID = WellKnownInterfaceIds.IID_IUnknown;
        Entries.IUnknown.Vtable = IUnknownImpl.Vtable;
    }
}

/// <summary>
/// A custom <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> implementation for <see cref="global::System.DateTimeOffset"/>.
/// </summary>
file sealed unsafe class DateTimeOffsetComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
{
    /// <inheritdoc/>
    public override void* GetOrCreateComInterfaceForObject(object value)
    {
        return (void*)WindowsRuntimeComWrappers.Default.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.None);
    }

    /// <inheritdoc/>
    public override ComInterfaceEntry* ComputeVtables(out int count)
    {
        count = sizeof(DateTimeOffsetInterfaceEntries) / sizeof(ComInterfaceEntry);

        return (ComInterfaceEntry*)Unsafe.AsPointer(ref Unsafe.AsRef(in DateTimeOffsetInterfaceEntriesImpl.Entries));
    }

    /// <inheritdoc/>
    public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
    {
        wrapperFlags = CreatedWrapperFlags.NonWrapping;

        DateTimeOffset abi = WindowsRuntimeValueTypeMarshaller.UnboxToManagedUnsafe<DateTimeOffset>(value, in WellKnownInterfaceIds.IID_IReferenceOfDateTimeOffset);

        return DateTimeOffsetMarshaller.ConvertToManaged(abi);
    }
}

/// <summary>
/// Binding type for the <c>IReference`1</c> implementation for <see cref="global::System.DateTimeOffset"/>.
/// </summary>
file unsafe struct DateTimeOffsetReferenceVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, DateTimeOffset*, HRESULT> get_Value;
}

/// <summary>
/// The <c>IReference`1</c> implementation for <see cref="global::System.DateTimeOffset"/>.
/// </summary>
file static unsafe class DateTimeOffsetReferenceImpl
{
    /// <summary>
    /// The <see cref="DateTimeOffsetReferenceVftbl"/> value for the managed <c>IReference`1</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly DateTimeOffsetReferenceVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static DateTimeOffsetReferenceImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.get_Value = &get_Value;
    }

    /// <summary>
    /// Gets a pointer to the managed <c>IReference`1</c> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(ref Unsafe.AsRef(in Vftbl));
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ireference-1.value"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    public static HRESULT get_Value(void* thisPtr, DateTimeOffset* result)
    {
        if (result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            global::System.DateTimeOffset unboxedValue = (global::System.DateTimeOffset)ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            *result = DateTimeOffsetMarshaller.ConvertToUnmanaged(unboxedValue);

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}

/// <summary>
/// The <c>IPropertyValue</c> implementation for <see cref="global::System.DateTimeOffset"/>.
/// </summary>
file static unsafe class DateTimeOffsetPropertyValueImpl
{
    /// <summary>
    /// The <see cref="IPropertyValueVftbl"/> value for the managed <c>IPropertyValue</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly IPropertyValueVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static DateTimeOffsetPropertyValueImpl()
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
        Vftbl.GetDateTime = &DateTimeOffsetReferenceImpl.get_Value;
        Vftbl.GetTimeSpan = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetPoint = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetSize = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetRect = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetUInt8Array = (delegate* unmanaged[MemberFunction]<void*, int*, byte**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, int*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt16Array = (delegate* unmanaged[MemberFunction]<void*, int*, short**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, int*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt16Array = (delegate* unmanaged[MemberFunction]<void*, int*, ushort**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, int*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt32Array = (delegate* unmanaged[MemberFunction]<void*, int*, int**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, int*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt32Array = (delegate* unmanaged[MemberFunction]<void*, int*, uint**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, int*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInt64Array = (delegate* unmanaged[MemberFunction]<void*, int*, long**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, int*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetUInt64Array = (delegate* unmanaged[MemberFunction]<void*, int*, ulong**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, int*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetSingleArray = (delegate* unmanaged[MemberFunction]<void*, int*, float**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, int*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetDoubleArray = (delegate* unmanaged[MemberFunction]<void*, int*, double**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, int*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetChar16Array = (delegate* unmanaged[MemberFunction]<void*, int*, char**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, int*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetBooleanArray = (delegate* unmanaged[MemberFunction]<void*, int*, bool**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, int*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetStringArray = (delegate* unmanaged[MemberFunction]<void*, int*, HSTRING**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, int*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetInspectableArray = (delegate* unmanaged[MemberFunction]<void*, int*, void***, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, int*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetGuidArray = (delegate* unmanaged[MemberFunction]<void*, int*, Guid**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, int*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetDateTimeArray = (delegate* unmanaged[MemberFunction]<void*, int*, DateTimeOffset**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, int*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetTimeSpanArray = (delegate* unmanaged[MemberFunction]<void*, int*, TimeSpan**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, int*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetPointArray = (delegate* unmanaged[MemberFunction]<void*, int*, Point**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, int*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetSizeArray = (delegate* unmanaged[MemberFunction]<void*, int*, Size**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, int*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
        Vftbl.GetRectArray = (delegate* unmanaged[MemberFunction]<void*, int*, Rect**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, int*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
    }

    /// <summary>
    /// Gets a pointer to the managed <c>IPropertyValue</c> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(ref Unsafe.AsRef(in Vftbl));
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.type"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT get_Type(void* thisPtr, PropertyType* value)
    {
        if (value == null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        *value = PropertyType.DateTime;

        return WellKnownErrorCodes.S_OK;
    }
}
