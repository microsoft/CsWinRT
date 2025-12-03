// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using static System.Runtime.InteropServices.ComWrappers;

[assembly: TypeMapAssociation<WindowsRuntimeComWrappersTypeMapGroup>(typeof(object), typeof(ABI.System.Object))]

namespace ABI.System;

/// <summary>
/// ABI type for <see cref="object"/>.
/// </summary>
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
[WindowsRuntimeClassName("Object")]
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
        count = sizeof(ObjectInterfaceEntries) / sizeof(ComInterfaceEntry);

        return (ComInterfaceEntry*)Unsafe.AsPointer(in ObjectInterfaceEntriesImpl.Entries);
    }

    // Marshalling 'object' instances is not supported, and it should just never happen
}