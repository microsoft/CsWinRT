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

#pragma warning disable IDE1006

[assembly: TypeMap<WindowsRuntimeTypeMapGroup>(
    value: "Windows.Foundation.IReference<Windows.Foundation.Numerics.Vector2>",
    target: typeof(ABI.System.Numerics.Vector2),
    trimTarget: typeof(Vector2))]

[assembly: TypeMapAssociation<WindowsRuntimeTypeMapGroup>(typeof(Vector2), typeof(ABI.System.Numerics.Vector2))]

namespace ABI.System.Numerics;

/// <summary>
/// ABI type for <see cref="global::System.Numerics.Vector2"/>.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.numerics.vector2"/>
[WindowsRuntimeClassName("Windows.Foundation.IReference<Windows.Foundation.Numerics.Vector2>")]
[Vector2ComWrappersMarshaller]
file static class Vector2;

/// <summary>
/// Marshaller for <see cref="global::System.Numerics.Vector2"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class Vector2Marshaller
{
    /// <inheritdoc cref="WindowsRuntimeValueTypeMarshaller.BoxToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue BoxToUnmanaged(global::System.Numerics.Vector2? value)
    {
        return WindowsRuntimeValueTypeMarshaller.BoxToUnmanaged(value, in WellKnownInterfaceIds.IID_IReferenceOfVector2);
    }

    /// <inheritdoc cref="WindowsRuntimeValueTypeMarshaller.UnboxToManaged(void*)"/>
    public static global::System.Numerics.Vector2? UnboxToManaged(void* value)
    {
        return WindowsRuntimeValueTypeMarshaller.UnboxToManaged<global::System.Numerics.Vector2>(value);
    }
}

/// <summary>
/// The set of <see cref="ComInterfaceEntry"/> values for <see cref="global::System.Numerics.Vector2"/>.
/// </summary>
file struct Vector2InterfaceEntries
{
    public ComInterfaceEntry IReferenceOfVector2;
    public ComInterfaceEntry IPropertyValue;
    public ComInterfaceEntry IStringable;
    public ComInterfaceEntry IWeakReferenceSource;
    public ComInterfaceEntry IMarshal;
    public ComInterfaceEntry IAgileObject;
    public ComInterfaceEntry IInspectable;
    public ComInterfaceEntry IUnknown;
}

/// <summary>
/// The implementation of <see cref="Vector2InterfaceEntries"/>.
/// </summary>
file static class Vector2InterfaceEntriesImpl
{
    /// <summary>
    /// The <see cref="Vector2InterfaceEntries"/> value for <see cref="global::System.Numerics.Vector2"/>.
    /// </summary>
    [FixedAddressValueType]
    public static readonly Vector2InterfaceEntries Entries;

    /// <summary>
    /// Initializes <see cref="Entries"/>.
    /// </summary>
    static Vector2InterfaceEntriesImpl()
    {
        Entries.IReferenceOfVector2.IID = WellKnownInterfaceIds.IID_IReferenceOfVector2;
        Entries.IReferenceOfVector2.Vtable = Vector2ReferenceImpl.Vtable;
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
/// A custom <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> implementation for <see cref="global::System.Numerics.Vector2"/>.
/// </summary>
internal sealed unsafe class Vector2ComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
{
    /// <inheritdoc/>
    public override void* GetOrCreateComInterfaceForObject(object value)
    {
        return (void*)WindowsRuntimeComWrappers.Default.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.None);
    }

    /// <inheritdoc/>
    public override ComInterfaceEntry* ComputeVtables(out int count)
    {
        count = sizeof(Vector2InterfaceEntries) / sizeof(ComInterfaceEntry);

        return (ComInterfaceEntry*)Unsafe.AsPointer(ref Unsafe.AsRef(in Vector2InterfaceEntriesImpl.Entries));
    }

    /// <inheritdoc/>
    public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
    {
        wrapperFlags = CreatedWrapperFlags.NonWrapping;

        return WindowsRuntimeValueTypeMarshaller.UnboxToManagedUnsafe<global::System.Numerics.Vector2>(value, in WellKnownInterfaceIds.IID_IReferenceOfVector2);
    }
}

/// <summary>
/// Binding type for the <c>IReference`1</c> implementation for <see cref="global::System.Numerics.Vector2"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
file unsafe struct Vector2ReferenceVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, global::System.Numerics.Vector2*, HRESULT> get_Value;
}

/// <summary>
/// The <c>IReference`1</c> implementation for <see cref="global::System.Numerics.Vector2"/>.
/// </summary>
file static unsafe class Vector2ReferenceImpl
{
    /// <summary>
    /// The <see cref="Vector2ReferenceVftbl"/> value for the managed <c>IReference`1</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly Vector2ReferenceVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static Vector2ReferenceImpl()
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
    private static HRESULT get_Value(void* thisPtr, global::System.Numerics.Vector2* result)
    {
        if (result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            *result = (global::System.Numerics.Vector2)ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}
