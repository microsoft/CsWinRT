// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using static System.Runtime.InteropServices.ComWrappers;

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
[assembly: TypeMap<WindowsRuntimeMetadataTypeMapGroup>(
    value: "Object",
    target: typeof(ABI.System.Object),
    trimTarget: typeof(object))]
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code

[assembly: TypeMapAssociation<WindowsRuntimeComWrappersTypeMapGroup>(typeof(object), typeof(ABI.System.Object))]

namespace ABI.System;

/// <summary>
/// ABI type for <see cref="object"/>.
/// </summary>
[WindowsRuntimeType]
[WindowsRuntimeClassName("Object")]
[WindowsRuntimeMappedType(typeof(object))]
[ObjectComWrappersMarshaller]
file static class Object;

/// <summary>
/// The set of <see cref="ComInterfaceEntry"/> values for <see cref="object"/>.
/// </summary>
file struct ObjectInterfaceEntries
{
    public ComInterfaceEntry IStringable;
    public ComInterfaceEntry IWeakReferenceSource;
    public ComInterfaceEntry IMarshal;
    public ComInterfaceEntry IAgileObject;
    public ComInterfaceEntry IInspectable;
    public ComInterfaceEntry IUnknown;
}

/// <summary>
/// The implementation of <see cref="ObjectInterfaceEntries"/>.
/// </summary>
file static class ObjectInterfaceEntriesImpl
{
    /// <summary>
    /// The <see cref="ObjectInterfaceEntries"/> value for <see cref="object"/>.
    /// </summary>
    [FixedAddressValueType]
    public static readonly ObjectInterfaceEntries Entries;

    /// <summary>
    /// Initializes <see cref="Entries"/>.
    /// </summary>
    static ObjectInterfaceEntriesImpl()
    {
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
/// The set of <see cref="ComInterfaceEntry"/> values for <see cref="object"/> with the default custom property provider.
/// </summary>
file struct ObjectWithCustomPropertyProviderInterfaceEntries
{
    public ComInterfaceEntry ICustomPropertyProvider;
    public ComInterfaceEntry IStringable;
    public ComInterfaceEntry IWeakReferenceSource;
    public ComInterfaceEntry IMarshal;
    public ComInterfaceEntry IAgileObject;
    public ComInterfaceEntry IInspectable;
    public ComInterfaceEntry IUnknown;
}

/// <summary>
/// The implementation of <see cref="ObjectWithCustomPropertyProviderInterfaceEntries"/>.
/// </summary>
file static class ObjectWithCustomPropertyProviderInterfaceEntriesImpl
{
    /// <summary>
    /// The <see cref="ObjectWithCustomPropertyProviderInterfaceEntries"/> value for <see cref="object"/>.
    /// </summary>
    [FixedAddressValueType]
    public static readonly ObjectWithCustomPropertyProviderInterfaceEntries Entries;

    /// <summary>
    /// Initializes <see cref="Entries"/>.
    /// </summary>
    static ObjectWithCustomPropertyProviderInterfaceEntriesImpl()
    {
        Entries.ICustomPropertyProvider.IID = WellKnownWindowsInterfaceIIDs.IID_ICustomPropertyProvider;
        Entries.ICustomPropertyProvider.Vtable = ICustomPropertyProviderImpl.Vtable;
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
/// A custom <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> implementation for <see cref="object"/>.
/// </summary>
file sealed unsafe class ObjectComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
{
    /// <inheritdoc/>
    public override void* GetOrCreateComInterfaceForObject(object value)
    {
        return WindowsRuntimeComWrappersMarshal.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.TrackerSupport);
    }

    /// <inheritdoc/>
    public override ComInterfaceEntry* ComputeVtables(out int count)
    {
        // Keep the switch outside both initializers so ILC can preinitialize their complete tables
        if (WindowsRuntimeFeatureSwitches.EnableDefaultCustomPropertyProviderSupport)
        {
            count = sizeof(ObjectWithCustomPropertyProviderInterfaceEntries) / sizeof(ComInterfaceEntry);

            return (ComInterfaceEntry*)Unsafe.AsPointer(in ObjectWithCustomPropertyProviderInterfaceEntriesImpl.Entries);
        }

        count = sizeof(ObjectInterfaceEntries) / sizeof(ComInterfaceEntry);

        return (ComInterfaceEntry*)Unsafe.AsPointer(in ObjectInterfaceEntriesImpl.Entries);
    }

    // Marshalling 'object' instances is not supported, and it should just never happen
}