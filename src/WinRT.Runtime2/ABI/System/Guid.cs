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

#pragma warning disable IDE0008, IDE1006, CA1416

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
[assembly: TypeMap<WindowsRuntimeComWrappersTypeMapGroup>(
    value: "Windows.Foundation.IReference<Guid>",
    target: typeof(ABI.System.Guid),
    trimTarget: typeof(Guid))]
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code

[assembly: TypeMapAssociation<WindowsRuntimeComWrappersTypeMapGroup>(typeof(Guid), typeof(ABI.System.Guid))]

namespace ABI.System;

/// <summary>
/// ABI type for <see cref="global::System.Guid"/>.
/// </summary>
[WindowsRuntimeClassName("Windows.Foundation.IReference<Guid>")]
[GuidComWrappersMarshaller]
file static class Guid;

/// <summary>
/// Marshaller for <see cref="global::System.Guid"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class GuidMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeValueTypeMarshaller.BoxToUnmanaged{T}(T?, CreateComInterfaceFlags, in global::System.Guid)"/>
    public static WindowsRuntimeObjectReferenceValue BoxToUnmanaged(global::System.Guid? value)
    {
        return WindowsRuntimeValueTypeMarshaller.BoxToUnmanaged(value, CreateComInterfaceFlags.None, in WellKnownWindowsInterfaceIIDs.IID_IReferenceOfGuid);
    }

    /// <inheritdoc cref="WindowsRuntimeValueTypeMarshaller.UnboxToManaged(void*)"/>
    public static global::System.Guid? UnboxToManaged(void* value)
    {
        return WindowsRuntimeValueTypeMarshaller.UnboxToManaged<global::System.Guid>(value);
    }
}

/// <summary>
/// The set of <see cref="ComInterfaceEntry"/> values for <see cref="global::System.Guid"/>.
/// </summary>
file struct GuidInterfaceEntries
{
    public ComInterfaceEntry IReferenceOfGuid;
    public ComInterfaceEntry IPropertyValue;
    public ComInterfaceEntry IStringable;
    public ComInterfaceEntry IWeakReferenceSource;
    public ComInterfaceEntry IMarshal;
    public ComInterfaceEntry IAgileObject;
    public ComInterfaceEntry IInspectable;
    public ComInterfaceEntry IUnknown;
}

/// <summary>
/// The implementation of <see cref="GuidInterfaceEntries"/>.
/// </summary>
file static class GuidInterfaceEntriesImpl
{
    /// <summary>
    /// The <see cref="GuidInterfaceEntries"/> value for <see cref="global::System.Guid"/>.
    /// </summary>
    [FixedAddressValueType]
    public static readonly GuidInterfaceEntries Entries;

    /// <summary>
    /// Initializes <see cref="Entries"/>.
    /// </summary>
    static GuidInterfaceEntriesImpl()
    {
        Entries.IReferenceOfGuid.IID = WellKnownWindowsInterfaceIIDs.IID_IReferenceOfGuid;
        Entries.IReferenceOfGuid.Vtable = GuidReferenceImpl.Vtable;
        Entries.IPropertyValue.IID = WellKnownWindowsInterfaceIIDs.IID_IPropertyValue;
        Entries.IPropertyValue.Vtable = GuidPropertyValueImpl.Vtable;
        Entries.IStringable.IID = WellKnownWindowsInterfaceIIDs.IID_IStringable;
        Entries.IStringable.Vtable = IStringableImpl.Vtable;
        Entries.IWeakReferenceSource.IID = WellKnownWindowsInterfaceIIDs.IID_IWeakReferenceSource;
        Entries.IWeakReferenceSource.Vtable = IWeakReferenceSourceImpl.Vtable;
        Entries.IMarshal.IID = WellKnownWindowsInterfaceIIDs.IID_IMarshal;
        Entries.IMarshal.Vtable = IMarshalImpl.Vtable;
        Entries.IAgileObject.IID = WellKnownWindowsInterfaceIIDs.IID_IAgileObject;
        Entries.IAgileObject.Vtable = IAgileObjectImpl.Vtable;
        Entries.IInspectable.IID = WellKnownWindowsInterfaceIIDs.IID_IInspectable;
        Entries.IInspectable.Vtable = IInspectableImpl.Vtable;
        Entries.IUnknown.IID = WellKnownWindowsInterfaceIIDs.IID_IUnknown;
        Entries.IUnknown.Vtable = IUnknownImpl.Vtable;
    }
}

/// <summary>
/// A custom <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> implementation for <see cref="global::System.Guid"/>.
/// </summary>
internal sealed unsafe class GuidComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
{
    /// <inheritdoc/>
    public override void* GetOrCreateComInterfaceForObject(object value)
    {
        return WindowsRuntimeComWrappersMarshal.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.None);
    }

    /// <inheritdoc/>
    public override ComInterfaceEntry* ComputeVtables(out int count)
    {
        count = sizeof(GuidInterfaceEntries) / sizeof(ComInterfaceEntry);

        return (ComInterfaceEntry*)Unsafe.AsPointer(in GuidInterfaceEntriesImpl.Entries);
    }

    /// <inheritdoc/>
    public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
    {
        wrapperFlags = CreatedWrapperFlags.NonWrapping;

        return WindowsRuntimeValueTypeMarshaller.UnboxToManagedUnsafe<global::System.Guid>(value, in WellKnownWindowsInterfaceIIDs.IID_IReferenceOfGuid);
    }
}

/// <summary>
/// Binding type for the <c>IReference`1</c> implementation for <see cref="global::System.Guid"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
file unsafe struct GuidReferenceVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, global::System.Guid*, HRESULT> get_Value;
}

/// <summary>
/// The <c>IReference`1</c> implementation for <see cref="global::System.Guid"/>.
/// </summary>
file static unsafe class GuidReferenceImpl
{
    /// <summary>
    /// The <see cref="GuidReferenceVftbl"/> value for the managed <c>IReference`1</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly GuidReferenceVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static GuidReferenceImpl()
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
    public static HRESULT get_Value(void* thisPtr, global::System.Guid* result)
    {
        if (result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            *result = (global::System.Guid)ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}

/// <summary>
/// The <c>IPropertyValue</c> implementation for <see cref="global::System.Guid"/>.
/// </summary>
file static unsafe class GuidPropertyValueImpl
{
    /// <summary>
    /// The <see cref="IPropertyValueVftbl"/> value for the managed <c>IPropertyValue</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly IPropertyValueVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static GuidPropertyValueImpl()
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
        Vftbl.GetString = &GetString;
        Vftbl.GetGuid = &GuidReferenceImpl.get_Value;
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
        Vftbl.GetGuidArray = (delegate* unmanaged[MemberFunction]<void*, int*, global::System.Guid**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, int*, void**, HRESULT>)&IPropertyValueImpl.ThrowStubForGetArrayOverloads;
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
        if (value is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        *value = PropertyType.Guid;

        return WellKnownErrorCodes.S_OK;
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue.getstring"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT GetString(void* thisPtr, HSTRING* value)
    {
        if (value is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            var unboxedValue = (global::System.Guid)ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            // A 'Guid' formatted as 'D' is always exactly 36 characters
            Span<char> buffer = stackalloc char[36];

            // This formatting call will always succeed, and it will always write 36 characters
            _ = unboxedValue.TryFormat(buffer, out _, format: "D");

            *value = HStringMarshaller.ConvertToUnmanaged(buffer);

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}
