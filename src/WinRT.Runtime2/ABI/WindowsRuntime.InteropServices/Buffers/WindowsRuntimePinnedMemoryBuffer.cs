// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;
using static System.Runtime.InteropServices.ComWrappers;

#pragma warning disable CS0723, IDE0008, IDE0046, IDE1006

[assembly: TypeMapAssociation<WindowsRuntimeComWrappersTypeMapGroup>(
    source: typeof(WindowsRuntimePinnedMemoryBuffer),
    proxy: typeof(ABI.WindowsRuntime.InteropServices.WindowsRuntimePinnedMemoryBuffer))]

namespace ABI.WindowsRuntime.InteropServices;

/// <summary>
/// ABI type for <see cref="WindowsRuntimePinnedMemoryBuffer"/>.
/// </summary>
[WindowsRuntimeClassName("Windows.Storage.Streams.IBuffer")]
[WindowsRuntimePinnedMemoryBufferComWrappersMarshaller]
file static class WindowsRuntimePinnedMemoryBuffer;

/// <summary>
/// The set of <see cref="ComInterfaceEntry"/> values for <see cref="WindowsRuntimePinnedMemoryBuffer"/>.
/// </summary>
file struct WindowsRuntimePinnedMemoryBufferInterfaceEntries
{
    public ComInterfaceEntry IBuffer;
    public ComInterfaceEntry IBufferByteAccess;
    public ComInterfaceEntry IStringable;
    public ComInterfaceEntry IWeakReferenceSource;
    public ComInterfaceEntry IMarshal;
    public ComInterfaceEntry IAgileObject;
    public ComInterfaceEntry IInspectable;
    public ComInterfaceEntry IUnknown;
}

/// <summary>
/// The implementation of <see cref="WindowsRuntimePinnedMemoryBufferInterfaceEntries"/>.
/// </summary>
file static class WindowsRuntimePinnedMemoryBufferInterfaceEntriesImpl
{
    /// <summary>
    /// The <see cref="WindowsRuntimePinnedMemoryBufferInterfaceEntries"/> value for <see cref="WindowsRuntimePinnedMemoryBuffer"/>.
    /// </summary>
    [FixedAddressValueType]
    public static readonly WindowsRuntimePinnedMemoryBufferInterfaceEntries Entries;

    /// <summary>
    /// Initializes <see cref="Entries"/>.
    /// </summary>
    static WindowsRuntimePinnedMemoryBufferInterfaceEntriesImpl()
    {
        Entries.IBuffer.IID = WellKnownWindowsInterfaceIIDs.IID_IBuffer;
        Entries.IBuffer.Vtable = Windows.Storage.Streams.IBufferImpl.Vtable;
        Entries.IBufferByteAccess.IID = WellKnownWindowsInterfaceIIDs.IID_IBufferByteAccess;
        Entries.IBufferByteAccess.Vtable = WindowsRuntimePinnedMemoryBufferByteAccessImpl.Vtable;
        Entries.IStringable.IID = WellKnownWindowsInterfaceIIDs.IID_IStringable;
        Entries.IStringable.Vtable = IStringableImpl.Vtable;
        Entries.IWeakReferenceSource.IID = WellKnownWindowsInterfaceIIDs.IID_IWeakReferenceSource;
        Entries.IWeakReferenceSource.Vtable = IWeakReferenceSourceImpl.Vtable;
        Entries.IMarshal.IID = WellKnownWindowsInterfaceIIDs.IID_IMarshal;
        Entries.IMarshal.Vtable = IMarshalImpl.RoBufferVtable;
        Entries.IAgileObject.IID = WellKnownWindowsInterfaceIIDs.IID_IAgileObject;
        Entries.IAgileObject.Vtable = IAgileObjectImpl.Vtable;
        Entries.IInspectable.IID = WellKnownWindowsInterfaceIIDs.IID_IInspectable;
        Entries.IInspectable.Vtable = IInspectableImpl.Vtable;
        Entries.IUnknown.IID = WellKnownWindowsInterfaceIIDs.IID_IUnknown;
        Entries.IUnknown.Vtable = IUnknownImpl.Vtable;
    }
}

/// <summary>
/// A custom <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> implementation for <see cref="WindowsRuntimePinnedMemoryBuffer"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed unsafe class WindowsRuntimePinnedMemoryBufferComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
{
    /// <inheritdoc/>
    public override void* GetOrCreateComInterfaceForObject(object value)
    {
        // No reference tracking is needed, see notes in the marshaller attribute for 'WindowsRuntimePinnedArrayBuffer'
        return (void*)WindowsRuntimeComWrappers.Default.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.None);
    }

    /// <inheritdoc/>
    public override ComInterfaceEntry* ComputeVtables(out int count)
    {
        count = sizeof(WindowsRuntimePinnedMemoryBufferInterfaceEntries) / sizeof(ComInterfaceEntry);

        return (ComInterfaceEntry*)Unsafe.AsPointer(in WindowsRuntimePinnedMemoryBufferInterfaceEntriesImpl.Entries);
    }
}

/// <summary>
/// The native implementation of <c>IBufferByteAccess</c> for <see cref="WindowsRuntimePinnedMemoryBuffer"/>.
/// </summary>
file static unsafe class WindowsRuntimePinnedMemoryBufferByteAccessImpl
{
    /// <summary>
    /// The <see cref="IBufferByteAccessVftbl"/> value for the <see cref="WindowsRuntimePinnedMemoryBuffer"/> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly IBufferByteAccessVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static WindowsRuntimePinnedMemoryBufferByteAccessImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.Buffer = &Buffer;
    }

    /// <summary>
    /// Gets a pointer to the <see cref="WindowsRuntimePinnedMemoryBuffer"/> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Vftbl);
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/robuffer/nf-robuffer-ibufferbyteaccess-buffer"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT Buffer(void* thisPtr, byte** value)
    {
        if (value is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            var thisObject = ComInterfaceDispatch.GetInstance<global::WindowsRuntime.InteropServices.WindowsRuntimePinnedMemoryBuffer>((ComInterfaceDispatch*)thisPtr);

            *value = thisObject.Buffer();

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception ex)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(ex);
        }
    }
}
