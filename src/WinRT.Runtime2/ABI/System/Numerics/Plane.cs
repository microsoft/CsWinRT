// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Foundation;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;
using static System.Runtime.InteropServices.ComWrappers;

#pragma warning disable CS0649, IDE1006

[assembly: TypeMap<WindowsRuntimeTypeMapGroup>(
    value: "Windows.Foundation.IReference<Windows.Foundation.Numerics.Plane>",
    target: typeof(ABI.System.Numerics.Plane),
    trimTarget: typeof(Plane))]

[assembly: TypeMapAssociation<WindowsRuntimeTypeMapGroup>(typeof(Plane), typeof(ABI.System.Numerics.Plane))]

namespace ABI.System.Numerics;

/// <summary>
/// ABI type for <see cref="global::System.Numerics.Plane"/>.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.numerics.plane"/>
[WindowsRuntimeClassName("Windows.Foundation.IReference<Windows.Foundation.Numerics.Plane>")]
[PlaneComWrappersMarshaller]
file static class Plane;

/// <summary>
/// Marshaller for <see cref="global::System.Numerics.Plane"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class PlaneMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeValueTypeMarshaller.BoxToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue BoxToUnmanaged(global::System.Numerics.Plane? value)
    {
        return WindowsRuntimeValueTypeMarshaller.BoxToUnmanaged(value, in WellKnownInterfaceIds.IID_IReferenceOfPlane);
    }

    /// <inheritdoc cref="WindowsRuntimeValueTypeMarshaller.UnboxToManaged(void*)"/>
    public static global::System.Numerics.Plane? UnboxToManaged(void* value)
    {
        return WindowsRuntimeValueTypeMarshaller.UnboxToManaged<global::System.Numerics.Plane>(value);
    }
}

/// <summary>
/// The set of <see cref="ComInterfaceEntry"/> values for <see cref="global::System.Numerics.Plane"/>.
/// </summary>
file struct PlaneInterfaceEntries
{
    public ComInterfaceEntry IReferenceOfPlane;
    public ComInterfaceEntry IPropertyValue;
    public ComInterfaceEntry IStringable;
    public ComInterfaceEntry IWeakReferenceSource;
    public ComInterfaceEntry IMarshal;
    public ComInterfaceEntry IAgileObject;
    public ComInterfaceEntry IInspectable;
    public ComInterfaceEntry IUnknown;
}

/// <summary>
/// The implementation of <see cref="PlaneInterfaceEntries"/>.
/// </summary>
file static class PlaneInterfaceEntriesImpl
{
    /// <summary>
    /// The <see cref="PlaneInterfaceEntries"/> value for <see cref="global::System.Numerics.Plane"/>.
    /// </summary>
    [FixedAddressValueType]
    public static readonly PlaneInterfaceEntries Entries;

    /// <summary>
    /// Initializes <see cref="Entries"/>.
    /// </summary>
    static PlaneInterfaceEntriesImpl()
    {
        Entries.IReferenceOfPlane.IID = WellKnownInterfaceIds.IID_IReferenceOfPlane;
        Entries.IReferenceOfPlane.Vtable = PlaneReferenceImpl.Vtable;
        Entries.IPropertyValue.IID = WellKnownInterfaceIds.IID_IPropertyValue;
        Entries.IPropertyValue.Vtable = IPropertyValueImpl.OtherTypeVtable;
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
/// A custom <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> implementation for <see cref="global::System.Numerics.Plane"/>.
/// </summary>
internal sealed unsafe class PlaneComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
{
    /// <inheritdoc/>
    public override void* GetOrCreateComInterfaceForObject(object value)
    {
        return (void*)WindowsRuntimeComWrappers.Default.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.None);
    }

    /// <inheritdoc/>
    public override ComInterfaceEntry* ComputeVtables(out int count)
    {
        count = sizeof(PlaneInterfaceEntries) / sizeof(ComInterfaceEntry);

        return (ComInterfaceEntry*)Unsafe.AsPointer(ref Unsafe.AsRef(in PlaneInterfaceEntriesImpl.Entries));
    }

    /// <inheritdoc/>
    public override object CreateObject(void* value)
    {
        return WindowsRuntimeValueTypeMarshaller.UnboxToManagedUnsafe<global::System.Numerics.Plane>(value, in WellKnownInterfaceIds.IID_IReferenceOfPlane);
    }
}

/// <summary>
/// Binding type for the <c>IReference`1</c> implementation for <see cref="global::System.Numerics.Plane"/>.
/// </summary>
file unsafe struct PlaneReferenceVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, global::System.Numerics.Plane*, HRESULT> get_Value;
}

/// <summary>
/// The <c>IReference`1</c> implementation for <see cref="global::System.Numerics.Plane"/>.
/// </summary>
file static unsafe class PlaneReferenceImpl
{
    /// <summary>
    /// The <see cref="PlaneReferenceVftbl"/> value for the managed <c>IReference`1</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly PlaneReferenceVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static PlaneReferenceImpl()
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
    private static HRESULT get_Value(void* thisPtr, global::System.Numerics.Plane* result)
    {
        if (result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            *result = (global::System.Numerics.Plane)ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}
