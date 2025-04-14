// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;
using static System.Runtime.InteropServices.ComWrappers;

#pragma warning disable CS0649

[assembly: TypeMap<WindowsRuntimeTypeMapUniverse>(
    value: "Windows.Foundation.IReference<Windows.Foundation.Numerics.Matrix4x4>",
    target: typeof(ABI.System.Numerics.Matrix4x4),
    trimTarget: typeof(Matrix4x4))]

[assembly: TypeMapAssociation<WindowsRuntimeTypeMapUniverse>(typeof(Matrix4x4), typeof(ABI.System.Numerics.Matrix4x4))]

namespace ABI.System.Numerics;

/// <summary>
/// ABI type for <see cref="global::System.Numerics.Matrix4x4"/>.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.numerics.matrix4x4"/>
[WindowsRuntimeClassName("Windows.Foundation.IReference<Windows.Foundation.Numerics.Matrix4x4>")]
[Matrix4x4ComWrappersMarshaller]
file static class Matrix4x4;

/// <summary>
/// Marshaller for <see cref="global::System.Numerics.Matrix4x4"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class Matrix4x4Marshaller
{
    /// <inheritdoc cref="WindowsRuntimeValueTypeMarshaller.BoxToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue BoxToUnmanaged(global::System.Numerics.Matrix4x4? value)
    {
        return WindowsRuntimeValueTypeMarshaller.BoxToUnmanaged(value, in WellKnownInterfaceIds.IID_IReferenceMatrix4x4);
    }

    /// <inheritdoc cref="WindowsRuntimeValueTypeMarshaller.UnboxToManaged(void*)"/>
    public static global::System.Numerics.Matrix4x4? UnboxToManaged(void* value)
    {
        return WindowsRuntimeValueTypeMarshaller.UnboxToManaged<global::System.Numerics.Matrix4x4>(value);
    }
}

/// <summary>
/// The set of <see cref="ComInterfaceEntry"/> values for <see cref="global::System.Numerics.Matrix4x4"/>.
/// </summary>
file struct Matrix4x4InterfaceEntries
{
    public ComInterfaceEntry IReferenceOfMatrix4x4;
    public ComInterfaceEntry IStringable;
    public ComInterfaceEntry IWeakReferenceSource;
    public ComInterfaceEntry IMarshal;
    public ComInterfaceEntry IAgileObject;
    public ComInterfaceEntry IInspectable;
    public ComInterfaceEntry IUnknown;
}

/// <summary>
/// The implementation of <see cref="Matrix4x4InterfaceEntries"/>.
/// </summary>
file static class Matrix4x4InterfaceEntriesImpl
{
    /// <summary>
    /// The <see cref="Matrix4x4InterfaceEntries"/> value for <see cref="global::System.Numerics.Matrix4x4"/>.
    /// </summary>
    [FixedAddressValueType]
    public static readonly Matrix4x4InterfaceEntries Entries;

    /// <summary>
    /// Initializes <see cref="Entries"/>.
    /// </summary>
    static Matrix4x4InterfaceEntriesImpl()
    {
        Entries.IReferenceOfMatrix4x4.IID = WellKnownInterfaceIds.IID_IReferenceMatrix4x4;
        Entries.IReferenceOfMatrix4x4.Vtable = Matrix4x4ReferenceImpl.Vtable;
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
/// A custom <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> implementation for <see cref="global::System.Numerics.Matrix4x4"/>.
/// </summary>
internal sealed unsafe class Matrix4x4ComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
{
    /// <inheritdoc/>
    public override void* GetOrCreateComInterfaceForObject(object value)
    {
        return (void*)WindowsRuntimeComWrappers.Default.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.None);
    }

    /// <inheritdoc/>
    public override ComInterfaceEntry* ComputeVtables(out int count)
    {
        count = sizeof(Matrix4x4InterfaceEntries) / sizeof(ComInterfaceEntry);

        return (ComInterfaceEntry*)Unsafe.AsPointer(ref Unsafe.AsRef(in Matrix4x4InterfaceEntriesImpl.Entries));
    }

    /// <inheritdoc/>
    public override object CreateObject(void* value)
    {
        return WindowsRuntimeValueTypeMarshaller.UnboxToManagedUnsafe<global::System.Numerics.Matrix4x4>(value, in WellKnownInterfaceIds.IID_IReferenceMatrix4x4);
    }
}

/// <summary>
/// Binding type for the <c>IReference`1</c> implementation for <see cref="global::System.Numerics.Matrix4x4"/>.
/// </summary>
file unsafe struct Matrix4x4ReferenceVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, global::System.Numerics.Matrix4x4*, HRESULT> Value;
}

/// <summary>
/// The <c>IReference`1</c> implementation for <see cref="global::System.Numerics.Matrix4x4"/>.
/// </summary>
file static unsafe class Matrix4x4ReferenceImpl
{
    /// <summary>
    /// The <see cref="Matrix4x4ReferenceVftbl"/> value for the managed <c>IReference`1</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly Matrix4x4ReferenceVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static Matrix4x4ReferenceImpl()
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
    private static HRESULT Value(void* thisPtr, global::System.Numerics.Matrix4x4* result)
    {
        if (result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            global::System.Numerics.Matrix4x4 unboxedValue = (global::System.Numerics.Matrix4x4)ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            Unsafe.WriteUnaligned(result, unboxedValue);

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            Unsafe.WriteUnaligned(result, default(global::System.Numerics.Matrix4x4));

            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}
