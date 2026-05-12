// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Helpers;

/// <summary>
/// Emits the IID/Vtable initializer pairs for the well-known WinRT interfaces (IPropertyValue,
/// IStringable, IWeakReferenceSource, IMarshal, IAgileObject, IInspectable, IUnknown) that
/// every CCW interface-entries struct exposes.
/// </summary>
internal static class WellKnownInterfaceEntriesEmitter
{
    /// <summary>
    /// Emits the well-known interface entries for a delegate's reference interface entries.
    /// Lines are written in the order expected by <c>DelegateReferenceInterfaceEntries</c>.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    public static void EmitDelegateReferenceWellKnownEntries(IndentedTextWriter writer)
    {
        writer.Write(isMultiline: true, """
            Entries.IPropertyValue.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IPropertyValue;
            Entries.IPropertyValue.Vtable = global::WindowsRuntime.InteropServices.IPropertyValueImpl.OtherTypeVtable;
            Entries.IStringable.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IStringable;
            Entries.IStringable.Vtable = global::WindowsRuntime.InteropServices.IStringableImpl.Vtable;
            Entries.IWeakReferenceSource.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IWeakReferenceSource;
            Entries.IWeakReferenceSource.Vtable = global::WindowsRuntime.InteropServices.IWeakReferenceSourceImpl.Vtable;
            Entries.IMarshal.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IMarshal;
            Entries.IMarshal.Vtable = global::WindowsRuntime.InteropServices.IMarshalImpl.Vtable;
            Entries.IAgileObject.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IAgileObject;
            Entries.IAgileObject.Vtable = global::WindowsRuntime.InteropServices.IAgileObjectImpl.Vtable;
            Entries.IInspectable.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IInspectable;
            Entries.IInspectable.Vtable = global::WindowsRuntime.InteropServices.IInspectableImpl.Vtable;
            Entries.IUnknown.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IUnknown;
            Entries.IUnknown.Vtable = global::WindowsRuntime.InteropServices.IUnknownImpl.Vtable;
            """);
    }
}
