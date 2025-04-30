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
    value: "Windows.Foundation.IReference<Int32>",
    target: typeof(ABI.System.Int32),
    trimTarget: typeof(int))]

[assembly: TypeMapAssociation<WindowsRuntimeTypeMapGroup>(typeof(int), typeof(ABI.System.Int32))]

namespace ABI.System;

/// <summary>
/// ABI type for <see cref="int"/>.
/// </summary>
[WindowsRuntimeClassName("Windows.Foundation.IReference<Int32>")]
[Int32ComWrappersMarshaller]
file static class Int32;

/// <summary>
/// Marshaller for <see cref="int"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class Int32Marshaller
{
    /// <inheritdoc cref="WindowsRuntimeValueTypeMarshaller.BoxToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue BoxToUnmanaged(int? value)
    {
        return WindowsRuntimeValueTypeMarshaller.BoxToUnmanaged(value, in WellKnownInterfaceIds.IID_IReferenceOfInt);
    }

    /// <inheritdoc cref="WindowsRuntimeValueTypeMarshaller.UnboxToManaged(void*)"/>
    public static int? UnboxToManaged(void* value)
    {
        return WindowsRuntimeValueTypeMarshaller.UnboxToManaged<int>(value);
    }
}

/// <summary>
/// The set of <see cref="ComInterfaceEntry"/> values for <see cref="int"/>.
/// </summary>
file struct Int32InterfaceEntries
{
    public ComInterfaceEntry IReferenceOfInt32;
    public ComInterfaceEntry IPropertyValue;
    public ComInterfaceEntry IStringable;
    public ComInterfaceEntry IWeakReferenceSource;
    public ComInterfaceEntry IMarshal;
    public ComInterfaceEntry IAgileObject;
    public ComInterfaceEntry IInspectable;
    public ComInterfaceEntry IUnknown;
}

/// <summary>
/// The implementation of <see cref="Int32InterfaceEntries"/>.
/// </summary>
file static class Int32InterfaceEntriesImpl
{
    /// <summary>
    /// The <see cref="Int32InterfaceEntries"/> value for <see cref="int"/>.
    /// </summary>
    [FixedAddressValueType]
    public static readonly Int32InterfaceEntries Entries;

    /// <summary>
    /// Initializes <see cref="Entries"/>.
    /// </summary>
    static Int32InterfaceEntriesImpl()
    {
        Entries.IReferenceOfInt32.IID = WellKnownInterfaceIds.IID_IReferenceOfInt;
        Entries.IReferenceOfInt32.Vtable = Int32ReferenceImpl.Vtable;
        Entries.IPropertyValue.IID = WellKnownInterfaceIds.IID_IPropertyValue;
        Entries.IPropertyValue.Vtable = Int32PropertyValueImpl.Vtable;
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
/// A custom <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> implementation for <see cref="int"/>.
/// </summary>
internal sealed unsafe class Int32ComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
{
    /// <inheritdoc/>
    public override void* GetOrCreateComInterfaceForObject(object value)
    {
        return (void*)WindowsRuntimeComWrappers.Default.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.None);
    }

    /// <inheritdoc/>
    public override ComInterfaceEntry* ComputeVtables(out int count)
    {
        count = sizeof(Int32InterfaceEntries) / sizeof(ComInterfaceEntry);

        return (ComInterfaceEntry*)Unsafe.AsPointer(ref Unsafe.AsRef(in Int32InterfaceEntriesImpl.Entries));
    }

    /// <inheritdoc/>
    public override object CreateObject(void* value)
    {
        return WindowsRuntimeValueTypeMarshaller.UnboxToManagedUnsafe<int>(value, in WellKnownInterfaceIds.IID_IReferenceOfInt);
    }
}

/// <summary>
/// Binding type for the <c>IReference`1</c> implementation for <see cref="int"/>.
/// </summary>
file unsafe struct Int32ReferenceVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, int*, HRESULT> Value;
}

/// <summary>
/// The <c>IReference`1</c> implementation for <see cref="int"/>.
/// </summary>
file static unsafe class Int32ReferenceImpl
{
    /// <summary>
    /// The <see cref="Int32ReferenceVftbl"/> value for the managed <c>IReference`1</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly Int32ReferenceVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static Int32ReferenceImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.Value = &Value;
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
    private static HRESULT Value(void* thisPtr, int* result)
    {
        if (result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            int unboxedValue = (int)ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            Unsafe.WriteUnaligned(result, unboxedValue);

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            Unsafe.WriteUnaligned(result, default(int));

            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}

/// <summary>
/// The <c>IPropertyValue</c> implementation for <see cref="int"/>.
/// </summary>
file static unsafe class Int32PropertyValueImpl
{
    /// <summary>
    /// The <see cref="IPropertyValueVftbl"/> value for the managed <c>IPropertyValue</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly IPropertyValueVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static Int32PropertyValueImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.get_Type = &get_Type;
        Vftbl.get_IsNumericScalar = &get_IsNumericScalar;
        Vftbl.GetUInt8 = &GetUInt8;
        Vftbl.GetInt16 = &GetInt16;
        Vftbl.GetUInt16 = &GetUInt16;
        Vftbl.GetInt32 = &GetInt32;
        Vftbl.GetUInt32 = &GetUInt32;
        Vftbl.GetInt64 = &GetInt64;
        Vftbl.GetUInt64 = &GetUInt64;
        Vftbl.GetSingle = &GetSingle;
        Vftbl.GetDouble = &GetDouble;
        Vftbl.GetChar16 = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetBoolean = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetString = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetGuid = &IPropertyValueImpl.ThrowStubForGetOverloads;
        Vftbl.GetDateTime = &IPropertyValueImpl.ThrowStubForGetOverloads;
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
        Vftbl.GetInspectableArray = &IPropertyValueImpl.ThrowStubForGetArrayOverloads;
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

        *value = PropertyType.Int32;

        return WellKnownErrorCodes.S_OK;
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.isnumericscalar"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]

    private static HRESULT get_IsNumericScalar(void* thisPtr, bool* value)
    {
        if (value == null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        *value = true;

        return WellKnownErrorCodes.S_OK;
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.getuint8"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]

    private static HRESULT GetUInt8(void* thisPtr, byte* value)
    {
        if (value == null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            int unboxedValue = (int)ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            try
            {
                *value = checked((byte)unboxedValue);
            }
            catch (OverflowException)
            {
                throw new InvalidCastException("", WellKnownErrorCodes.DISP_E_OVERFLOW);
            }

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            *value = 0;

            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.getint16"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]

    private static HRESULT GetInt16(void* thisPtr, short* value)
    {
        if (value == null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            int unboxedValue = (int)ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            try
            {
                *value = checked((short)unboxedValue);
            }
            catch (OverflowException)
            {
                throw new InvalidCastException("", WellKnownErrorCodes.DISP_E_OVERFLOW);
            }

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            *value = 0;

            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.getuint16"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]

    private static HRESULT GetUInt16(void* thisPtr, ushort* value)
    {
        if (value == null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            int unboxedValue = (int)ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            try
            {
                *value = checked((ushort)unboxedValue);
            }
            catch (OverflowException)
            {
                throw new InvalidCastException("", WellKnownErrorCodes.DISP_E_OVERFLOW);
            }

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            *value = 0;

            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.getint32"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]

    private static HRESULT GetInt32(void* thisPtr, int* value)
    {
        if (value == null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            *value = (int)ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            *value = 0;

            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.getuint32"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]

    private static HRESULT GetUInt32(void* thisPtr, uint* value)
    {
        if (value == null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            int unboxedValue = (int)ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            try
            {
                *value = checked((uint)unboxedValue);
            }
            catch (OverflowException)
            {
                throw new InvalidCastException("", WellKnownErrorCodes.DISP_E_OVERFLOW);
            }

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            *value = 0;

            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.getint64"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]

    private static HRESULT GetInt64(void* thisPtr, long* value)
    {
        if (value == null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            *value = (int)ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            *value = 0;

            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.getuint64"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]

    private static HRESULT GetUInt64(void* thisPtr, ulong* value)
    {
        if (value == null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            int unboxedValue = (int)ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            try
            {
                *value = checked((ulong)unboxedValue);
            }
            catch (OverflowException)
            {
                throw new InvalidCastException("", WellKnownErrorCodes.DISP_E_OVERFLOW);
            }

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            *value = 0;

            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.getsingle"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]

    private static HRESULT GetSingle(void* thisPtr, float* value)
    {
        if (value == null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            *value = (int)ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            *value = 0;

            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.getdouble"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]

    private static HRESULT GetDouble(void* thisPtr, double* value)
    {
        if (value == null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            *value = (int)ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            *value = 0;

            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}
