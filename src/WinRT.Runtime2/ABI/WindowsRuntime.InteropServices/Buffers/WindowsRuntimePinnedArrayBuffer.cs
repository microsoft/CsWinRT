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
    source: typeof(WindowsRuntimePinnedArrayBuffer),
    proxy: typeof(ABI.WindowsRuntime.InteropServices.WindowsRuntimePinnedArrayBuffer))]

namespace ABI.WindowsRuntime.InteropServices;

/// <summary>
/// ABI type for <see cref="WindowsRuntimePinnedArrayBuffer"/>.
/// </summary>
[WindowsRuntimeClassName("Windows.Storage.Streams.IBuffer")]
[WindowsRuntimePinnedArrayBufferComWrappersMarshaller]
file static class WindowsRuntimePinnedArrayBuffer;

/// <summary>
/// The set of <see cref="ComInterfaceEntry"/> values for <see cref="WindowsRuntimePinnedArrayBuffer"/>.
/// </summary>
file struct WindowsRuntimePinnedArrayBufferInterfaceEntries
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
/// The implementation of <see cref="WindowsRuntimePinnedArrayBufferInterfaceEntries"/>.
/// </summary>
file static class WindowsRuntimePinnedArrayBufferInterfaceEntriesImpl
{
    /// <summary>
    /// The <see cref="WindowsRuntimePinnedArrayBufferInterfaceEntries"/> value for <see cref="WindowsRuntimePinnedArrayBuffer"/>.
    /// </summary>
    [FixedAddressValueType]
    public static readonly WindowsRuntimePinnedArrayBufferInterfaceEntries Entries;

    /// <summary>
    /// Initializes <see cref="Entries"/>.
    /// </summary>
    static WindowsRuntimePinnedArrayBufferInterfaceEntriesImpl()
    {
        Entries.IBuffer.IID = WellKnownWindowsInterfaceIIDs.IID_IBuffer;
        Entries.IBuffer.Vtable = Windows.Storage.Streams.IBufferImpl.Vtable;
        Entries.IBufferByteAccess.IID = WellKnownWindowsInterfaceIIDs.IID_IBufferByteAccess;
        Entries.IBufferByteAccess.Vtable = WindowsRuntimePinnedArrayBufferByteAccessImpl.Vtable;
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
/// A custom <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> implementation for <see cref="WindowsRuntimePinnedArrayBuffer"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed unsafe class WindowsRuntimePinnedArrayBufferComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
{
    /// <inheritdoc/>
    public override void* GetOrCreateComInterfaceForObject(object value)
    {
        // Note that specifically for 'WindowsRuntimePinnedArrayBuffer', we can optimize the flags to say we don't
        // need tracker support. This is because while this object wraps a managed object (a 'byte[]' array), the
        // array itself can't possibly create a cycle (since it can have no outstanding references), so this is ok.
        return (void*)WindowsRuntimeComWrappers.Default.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.None);
    }

    /// <inheritdoc/>
    public override ComInterfaceEntry* ComputeVtables(out int count)
    {
        count = sizeof(WindowsRuntimePinnedArrayBufferInterfaceEntries) / sizeof(ComInterfaceEntry);

        return (ComInterfaceEntry*)Unsafe.AsPointer(in WindowsRuntimePinnedArrayBufferInterfaceEntriesImpl.Entries);
    }
}

/// <summary>
/// The native implementation of <c>IBufferByteAccess</c> for <see cref="WindowsRuntimePinnedArrayBuffer"/>.
/// </summary>
file static unsafe class WindowsRuntimePinnedArrayBufferByteAccessImpl
{
    /// <summary>
    /// The <see cref="IBufferByteAccessVftbl"/> value for the <see cref="WindowsRuntimePinnedArrayBuffer"/> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly IBufferByteAccessVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static WindowsRuntimePinnedArrayBufferByteAccessImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.get_Buffer = &get_Buffer;
    }

    /// <summary>
    /// Gets a pointer to the <see cref="WindowsRuntimePinnedArrayBuffer"/> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Vftbl);
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/robuffer/nf-robuffer-ibufferbyteaccess-buffer"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT get_Buffer(void* thisPtr, byte** buffer)
    {
        if (buffer is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            var thisObject = ComInterfaceDispatch.GetInstance<global::WindowsRuntime.InteropServices.WindowsRuntimePinnedArrayBuffer>((ComInterfaceDispatch*)thisPtr);

            *buffer = thisObject.Buffer;

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception ex)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(ex);
        }
    }
}