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
    value: "Windows.Foundation.IReference`1<Windows.Foundation.PropertyType>",
    target: typeof(PropertyType),
    trimTarget: typeof(PropertyType))]
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code

namespace ABI.Windows.Foundation;

/// <summary>
/// Marshaller for <see cref="PropertyType"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class PropertyTypeMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeValueTypeMarshaller.BoxToUnmanaged{T}(T?, CreateComInterfaceFlags, in Guid)"/>
    public static WindowsRuntimeObjectReferenceValue BoxToUnmanaged(PropertyType? value)
    {
        return WindowsRuntimeValueTypeMarshaller.BoxToUnmanaged(value, CreateComInterfaceFlags.None, in WellKnownWindowsInterfaceIIDs.IID_IReferenceOfPropertyType);
    }

    /// <inheritdoc cref="WindowsRuntimeValueTypeMarshaller.UnboxToManaged(void*)"/>
    public static PropertyType? UnboxToManaged(void* value)
    {
        return WindowsRuntimeValueTypeMarshaller.UnboxToManaged<PropertyType>(value);
    }
}

/// <summary>
/// The set of <see cref="ComInterfaceEntry"/> values for <see cref="PropertyType"/>.
/// </summary>
file struct PropertyTypeInterfaceEntries
{
    public ComInterfaceEntry IReferenceOfPropertyType;
    public ComInterfaceEntry IPropertyValue;
    public ComInterfaceEntry IStringable;
    public ComInterfaceEntry IWeakReferenceSource;
    public ComInterfaceEntry IMarshal;
    public ComInterfaceEntry IAgileObject;
    public ComInterfaceEntry IInspectable;
    public ComInterfaceEntry IUnknown;
}

/// <summary>
/// The implementation of <see cref="PropertyTypeInterfaceEntries"/>.
/// </summary>
file static class PropertyTypeInterfaceEntriesImpl
{
    /// <summary>
    /// The <see cref="PropertyTypeInterfaceEntries"/> value for <see cref="PropertyType"/>.
    /// </summary>
    [FixedAddressValueType]
    public static readonly PropertyTypeInterfaceEntries Entries;

    /// <summary>
    /// Initializes <see cref="Entries"/>.
    /// </summary>
    static PropertyTypeInterfaceEntriesImpl()
    {
        Entries.IReferenceOfPropertyType.IID = WellKnownWindowsInterfaceIIDs.IID_IReferenceOfPropertyType;
        Entries.IReferenceOfPropertyType.Vtable = IReferenceImpl.UInt32Enum;
        Entries.IPropertyValue.IID = WellKnownWindowsInterfaceIIDs.IID_IPropertyValue;
        Entries.IPropertyValue.Vtable = IPropertyValueImpl.OtherTypeVtable;
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
/// A custom <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> implementation for <see cref="PropertyType"/>.
/// </summary>
internal sealed unsafe class PropertyTypeComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
{
    /// <inheritdoc/>
    public override void* GetOrCreateComInterfaceForObject(object value)
    {
        return WindowsRuntimeComWrappersMarshal.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.None);
    }

    /// <inheritdoc/>
    public override ComInterfaceEntry* ComputeVtables(out int count)
    {
        count = sizeof(PropertyTypeInterfaceEntries) / sizeof(ComInterfaceEntry);

        return (ComInterfaceEntry*)Unsafe.AsPointer(in PropertyTypeInterfaceEntriesImpl.Entries);
    }

    /// <inheritdoc/>
    public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
    {
        wrapperFlags = CreatedWrapperFlags.NonWrapping;

        return WindowsRuntimeValueTypeMarshaller.UnboxToManagedUnsafe<PropertyType>(value, in WellKnownWindowsInterfaceIIDs.IID_IReferenceOfPropertyType);
    }
}