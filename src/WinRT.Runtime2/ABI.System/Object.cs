// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using static System.Runtime.InteropServices.ComWrappers;

#pragma warning disable CS0649

[assembly: TypeMapAssociation<WindowsRuntimeTypeMapUniverse>(typeof(object), typeof(ABI.System.Object))]

namespace ABI.System;

/// <summary>
/// ABI type for <see cref="object"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
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
        Entries.IStringable.IID = WellKnownInterfaceIds.IID_IStringable;
        Entries.IStringable.Vtable = IStringableImpl.AbiToProjectionVftablePtr;
        Entries.IWeakReferenceSource.IID = WellKnownInterfaceIds.IID_IWeakReferenceSource;
        Entries.IWeakReferenceSource.Vtable = IWeakReferenceSourceImpl.AbiToProjectionVftablePtr;
        Entries.IMarshal.IID = WellKnownInterfaceIds.IID_IMarshal;
        Entries.IMarshal.Vtable = IMarshalImpl.AbiToProjectionVftablePtr;
        Entries.IAgileObject.IID = WellKnownInterfaceIds.IID_IAgileObject;
        Entries.IAgileObject.Vtable = IUnknownImpl.AbiToProjectionVftablePtr;
        Entries.IInspectable.IID = WellKnownInterfaceIds.IID_IInspectable;
        Entries.IInspectable.Vtable = IInspectableImpl.AbiToProjectionVftablePtr;
        Entries.IUnknown.IID = WellKnownInterfaceIds.IID_IUnknown;
        Entries.IUnknown.Vtable = IUnknownImpl.AbiToProjectionVftablePtr;
    }
}

/// <summary>
/// A custom <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> implementation for <see cref="object"/>.
/// </summary>
file sealed unsafe class ObjectComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
{
    /// <inheritdoc/>
    public override ComInterfaceEntry* ComputeVtables(out int count)
    {
        count = sizeof(ObjectInterfaceEntries) / sizeof(ComInterfaceEntry);

        return (ComInterfaceEntry*)Unsafe.AsPointer(ref Unsafe.AsRef(in ObjectInterfaceEntriesImpl.Entries));
    }

    /// <inheritdoc/>
    public override unsafe object CreateObject(void* value)
    {
        throw new NotSupportedException("Marshalling 'object' instances is not supported.");
    }
}
