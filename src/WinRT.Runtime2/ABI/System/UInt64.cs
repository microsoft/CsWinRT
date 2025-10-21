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

#pragma warning disable IDE1006, CA1416

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
[assembly: TypeMap<WindowsRuntimeComWrappersTypeMapGroup>(
    value: "Windows.Foundation.IReference<UInt64>",
    target: typeof(ABI.System.UInt64),
    trimTarget: typeof(ulong))]
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code

[assembly: TypeMapAssociation<WindowsRuntimeComWrappersTypeMapGroup>(typeof(ulong), typeof(ABI.System.UInt64))]

namespace ABI.System;

/// <summary>
/// ABI type for <see cref="ulong"/>.
/// </summary>
[WindowsRuntimeClassName("Windows.Foundation.IReference<UInt64>")]
[UInt64ComWrappersMarshaller]
file static class UInt64;

/// <summary>
/// Marshaller for <see cref="ulong"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class UInt64Marshaller
{
    /// <inheritdoc cref="WindowsRuntimeValueTypeMarshaller.BoxToUnmanaged{T}(T?, CreateComInterfaceFlags, in Guid)"/>
    public static WindowsRuntimeObjectReferenceValue BoxToUnmanaged(ulong? value)
    {
        return WindowsRuntimeValueTypeMarshaller.BoxToUnmanaged(value, CreateComInterfaceFlags.None, in WellKnownInterfaceIIDs.IID_IReferenceOfULong);
    }

    /// <inheritdoc cref="WindowsRuntimeValueTypeMarshaller.UnboxToManaged(void*)"/>
    public static ulong? UnboxToManaged(void* value)
    {
        return WindowsRuntimeValueTypeMarshaller.UnboxToManaged<ulong>(value);
    }
}

/// <summary>
/// The set of <see cref="ComInterfaceEntry"/> values for <see cref="ulong"/>.
/// </summary>
file struct UInt64InterfaceEntries
{
    public ComInterfaceEntry IReferenceOfUInt64;
    public ComInterfaceEntry IPropertyValue;
    public ComInterfaceEntry IStringable;
    public ComInterfaceEntry IWeakReferenceSource;
    public ComInterfaceEntry IMarshal;
    public ComInterfaceEntry IAgileObject;
    public ComInterfaceEntry IInspectable;
    public ComInterfaceEntry IUnknown;
}

/// <summary>
/// The implementation of <see cref="UInt64InterfaceEntries"/>.
/// </summary>
file static class UInt64InterfaceEntriesImpl
{
    /// <summary>
    /// The <see cref="UInt64InterfaceEntries"/> value for <see cref="ulong"/>.
    /// </summary>
    [FixedAddressValueType]
    public static readonly UInt64InterfaceEntries Entries;

    /// <summary>
    /// Initializes <see cref="Entries"/>.
    /// </summary>
    static UInt64InterfaceEntriesImpl()
    {
        Entries.IReferenceOfUInt64.IID = WellKnownInterfaceIIDs.IID_IReferenceOfULong;
        Entries.IReferenceOfUInt64.Vtable = UInt64ReferenceImpl.Vtable;
        Entries.IPropertyValue.IID = WellKnownInterfaceIIDs.IID_IPropertyValue;
        Entries.IPropertyValue.Vtable = UInt64PropertyValueImpl.Vtable;
        Entries.IStringable.IID = WellKnownInterfaceIIDs.IID_IStringable;
        Entries.IStringable.Vtable = IStringableImpl.Vtable;
        Entries.IWeakReferenceSource.IID = WellKnownInterfaceIIDs.IID_IWeakReferenceSource;
        Entries.IWeakReferenceSource.Vtable = IWeakReferenceSourceImpl.Vtable;
        Entries.IMarshal.IID = WellKnownInterfaceIIDs.IID_IMarshal;
        Entries.IMarshal.Vtable = IMarshalImpl.Vtable;
        Entries.IAgileObject.IID = WellKnownInterfaceIIDs.IID_IAgileObject;
        Entries.IAgileObject.Vtable = IUnknownImpl.Vtable;
        Entries.IInspectable.IID = WellKnownInterfaceIIDs.IID_IInspectable;
        Entries.IInspectable.Vtable = IInspectableImpl.Vtable;
        Entries.IUnknown.IID = WellKnownInterfaceIIDs.IID_IUnknown;
        Entries.IUnknown.Vtable = IUnknownImpl.Vtable;
    }
}

/// <summary>
/// A custom <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> implementation for <see cref="ulong"/>.
/// </summary>
internal sealed unsafe class UInt64ComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
{
    /// <inheritdoc/>
    public override void* GetOrCreateComInterfaceForObject(object value)
    {
        return WindowsRuntimeComWrappersMarshal.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.None);
    }

    /// <inheritdoc/>
    public override ComInterfaceEntry* ComputeVtables(out int count)
    {
        count = sizeof(UInt64InterfaceEntries) / sizeof(ComInterfaceEntry);

        return (ComInterfaceEntry*)Unsafe.AsPointer(in UInt64InterfaceEntriesImpl.Entries);
    }

    /// <inheritdoc/>
    public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
    {
        wrapperFlags = CreatedWrapperFlags.NonWrapping;

        return WindowsRuntimeValueTypeMarshaller.UnboxToManagedUnsafe<ulong>(value, in WellKnownInterfaceIIDs.IID_IReferenceOfULong);
    }
}

/// <summary>
/// Binding type for the <c>IReference`1</c> implementation for <see cref="ulong"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
file unsafe struct UInt64ReferenceVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, ulong*, HRESULT> get_Value;
}

/// <summary>
/// The <c>IReference`1</c> implementation for <see cref="ulong"/>.
/// </summary>
file static unsafe class UInt64ReferenceImpl
{
    /// <summary>
    /// The <see cref="UInt64ReferenceVftbl"/> value for the managed <c>IReference`1</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly UInt64ReferenceVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static UInt64ReferenceImpl()
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
        get => (nint)Unsafe.AsPointer(in Vftbl);
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ireference-1.value"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    public static HRESULT get_Value(void* thisPtr, ulong* result)
    {
        if (result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            *result = (ulong)ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}

/// <summary>
/// The <c>IPropertyValue</c> implementation for <see cref="ulong"/>.
/// </summary>
file static unsafe class UInt64PropertyValueImpl
{
    /// <summary>
    /// The <see cref="IPropertyValueVftbl"/> value for the managed <c>IPropertyValue</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly IPropertyValueVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static UInt64PropertyValueImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.get_Type = &get_Type;
        Vftbl.get_IsNumericScalar = &IPropertyValueImpl.get_IsNumericScalarTrue;
        Vftbl.GetUInt8 = &GetUInt8;
        Vftbl.GetInt16 = &GetInt16;
        Vftbl.GetUInt16 = &GetUInt16;
        Vftbl.GetInt32 = &GetInt32;
        Vftbl.GetUInt32 = &GetUInt32;
        Vftbl.GetInt64 = &GetInt64;
        Vftbl.GetUInt64 = &UInt64ReferenceImpl.get_Value;
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
        get => (nint)Unsafe.AsPointer(in Vftbl);
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.type"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT get_Type(void* thisPtr, PropertyType* value)
    {
        if (value == null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        *value = PropertyType.UInt64;

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
            ulong unboxedValue = (ulong)ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            if (unboxedValue is < byte.MinValue or > byte.MaxValue)
            {
                throw new InvalidCastException("", WellKnownErrorCodes.DISP_E_OVERFLOW);
            }

            *value = (byte)unboxedValue;

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
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
            ulong unboxedValue = (ulong)ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            if (unboxedValue > (long)short.MaxValue)
            {
                throw new InvalidCastException("", WellKnownErrorCodes.DISP_E_OVERFLOW);
            }

            *value = (short)unboxedValue;

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
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
            ulong unboxedValue = (ulong)ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            if (unboxedValue is < ushort.MinValue or > ushort.MaxValue)
            {
                throw new InvalidCastException("", WellKnownErrorCodes.DISP_E_OVERFLOW);
            }

            *value = (ushort)unboxedValue;

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.getuint32"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT GetInt32(void* thisPtr, int* value)
    {
        if (value == null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            ulong unboxedValue = (ulong)ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            if (unboxedValue > int.MaxValue)
            {
                throw new InvalidCastException("", WellKnownErrorCodes.DISP_E_OVERFLOW);
            }

            *value = (int)unboxedValue;

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
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
            ulong unboxedValue = (ulong)ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            if (unboxedValue > int.MaxValue)
            {
                throw new InvalidCastException("", WellKnownErrorCodes.DISP_E_OVERFLOW);
            }

            *value = (uint)unboxedValue;

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
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
            ulong unboxedValue = (ulong)ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            if (unboxedValue > long.MaxValue)
            {
                throw new InvalidCastException("", WellKnownErrorCodes.DISP_E_OVERFLOW);
            }

            *value = (long)unboxedValue;

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
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
            *value = (ulong)ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
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
            *value = (ulong)ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}
