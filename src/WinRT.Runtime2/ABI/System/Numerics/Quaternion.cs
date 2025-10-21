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

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
[assembly: TypeMap<WindowsRuntimeComWrappersTypeMapGroup>(
    value: "Windows.Foundation.IReference<Windows.Foundation.Numerics.Quaternion>",
    target: typeof(ABI.System.Numerics.Quaternion),
    trimTarget: typeof(Quaternion))]
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code

[assembly: TypeMapAssociation<WindowsRuntimeComWrappersTypeMapGroup>(typeof(Quaternion), typeof(ABI.System.Numerics.Quaternion))]

namespace ABI.System.Numerics;

/// <summary>
/// ABI type for <see cref="global::System.Numerics.Quaternion"/>.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.numerics.quaternion"/>
[WindowsRuntimeClassName("Windows.Foundation.IReference<Windows.Foundation.Numerics.Quaternion>")]
[QuaternionComWrappersMarshaller]
file static class Quaternion;

/// <summary>
/// Marshaller for <see cref="global::System.Numerics.Quaternion"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class QuaternionMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeValueTypeMarshaller.BoxToUnmanaged{T}(T?, CreateComInterfaceFlags, in Guid)"/>
    public static WindowsRuntimeObjectReferenceValue BoxToUnmanaged(global::System.Numerics.Quaternion? value)
    {
        return WindowsRuntimeValueTypeMarshaller.BoxToUnmanaged(value, CreateComInterfaceFlags.None, in WellKnownInterfaceIIDs.IID_IReferenceOfQuaternion);
    }

    /// <inheritdoc cref="WindowsRuntimeValueTypeMarshaller.UnboxToManaged(void*)"/>
    public static global::System.Numerics.Quaternion? UnboxToManaged(void* value)
    {
        return WindowsRuntimeValueTypeMarshaller.UnboxToManaged<global::System.Numerics.Quaternion>(value);
    }
}

/// <summary>
/// The set of <see cref="ComInterfaceEntry"/> values for <see cref="global::System.Numerics.Quaternion"/>.
/// </summary>
file struct QuaternionInterfaceEntries
{
    public ComInterfaceEntry IReferenceOfQuaternion;
    public ComInterfaceEntry IPropertyValue;
    public ComInterfaceEntry IStringable;
    public ComInterfaceEntry IWeakReferenceSource;
    public ComInterfaceEntry IMarshal;
    public ComInterfaceEntry IAgileObject;
    public ComInterfaceEntry IInspectable;
    public ComInterfaceEntry IUnknown;
}

/// <summary>
/// The implementation of <see cref="QuaternionInterfaceEntries"/>.
/// </summary>
file static class QuaternionInterfaceEntriesImpl
{
    /// <summary>
    /// The <see cref="QuaternionInterfaceEntries"/> value for <see cref="global::System.Numerics.Quaternion"/>.
    /// </summary>
    [FixedAddressValueType]
    public static readonly QuaternionInterfaceEntries Entries;

    /// <summary>
    /// Initializes <see cref="Entries"/>.
    /// </summary>
    static QuaternionInterfaceEntriesImpl()
    {
        Entries.IReferenceOfQuaternion.IID = WellKnownInterfaceIIDs.IID_IReferenceOfQuaternion;
        Entries.IReferenceOfQuaternion.Vtable = QuaternionReferenceImpl.Vtable;
        Entries.IPropertyValue.IID = WellKnownInterfaceIIDs.IID_IPropertyValue;
        Entries.IPropertyValue.Vtable = IPropertyValueImpl.OtherTypeVtable;
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
/// A custom <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> implementation for <see cref="global::System.Numerics.Quaternion"/>.
/// </summary>
internal sealed unsafe class QuaternionComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
{
    /// <inheritdoc/>
    public override void* GetOrCreateComInterfaceForObject(object value)
    {
        return WindowsRuntimeComWrappersMarshal.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.None);
    }

    /// <inheritdoc/>
    public override ComInterfaceEntry* ComputeVtables(out int count)
    {
        count = sizeof(QuaternionInterfaceEntries) / sizeof(ComInterfaceEntry);

        return (ComInterfaceEntry*)Unsafe.AsPointer(in QuaternionInterfaceEntriesImpl.Entries);
    }

    /// <inheritdoc/>
    public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
    {
        wrapperFlags = CreatedWrapperFlags.NonWrapping;

        return WindowsRuntimeValueTypeMarshaller.UnboxToManagedUnsafe<global::System.Numerics.Quaternion>(value, in WellKnownInterfaceIIDs.IID_IReferenceOfQuaternion);
    }
}

/// <summary>
/// Binding type for the <c>IReference`1</c> implementation for <see cref="global::System.Numerics.Quaternion"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
file unsafe struct QuaternionReferenceVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, global::System.Numerics.Quaternion*, HRESULT> get_Value;
}

/// <summary>
/// The <c>IReference`1</c> implementation for <see cref="global::System.Numerics.Quaternion"/>.
/// </summary>
file static unsafe class QuaternionReferenceImpl
{
    /// <summary>
    /// The <see cref="QuaternionReferenceVftbl"/> value for the managed <c>IReference`1</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly QuaternionReferenceVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static QuaternionReferenceImpl()
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
    private static HRESULT get_Value(void* thisPtr, global::System.Numerics.Quaternion* result)
    {
        if (result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            *result = (global::System.Numerics.Quaternion)ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}
