// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;
using static System.Runtime.InteropServices.ComWrappers;

#pragma warning disable CS0723, IDE0008, IDE0046, IDE1006

[assembly: TypeMapAssociation<WindowsRuntimeComWrappersTypeMapGroup>(
    source: typeof(WindowsRuntimeExternalArrayBuffer),
    proxy: typeof(ABI.WindowsRuntime.InteropServices.WindowsRuntimeExternalArrayBuffer))]

namespace ABI.WindowsRuntime.InteropServices;

/// <summary>
/// ABI type for <see cref="WindowsRuntimeExternalArrayBuffer"/>.
/// </summary>
[WindowsRuntimeClassName("Windows.Storage.Streams.IBuffer")]
[WindowsRuntimeExternalArrayBufferComWrappersMarshaller]
file static class WindowsRuntimeExternalArrayBuffer;

/// <summary>
/// The set of <see cref="ComInterfaceEntry"/> values for <see cref="WindowsRuntimeExternalArrayBuffer"/>.
/// </summary>
file struct WindowsRuntimeExternalArrayBufferInterfaceEntries
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
/// The implementation of <see cref="WindowsRuntimeExternalArrayBufferInterfaceEntries"/>.
/// </summary>
file static class WindowsRuntimeExternalArrayBufferInterfaceEntriesImpl
{
    /// <summary>
    /// The <see cref="WindowsRuntimeExternalArrayBufferInterfaceEntries"/> value for <see cref="WindowsRuntimeExternalArrayBuffer"/>.
    /// </summary>
    [FixedAddressValueType]
    public static readonly WindowsRuntimeExternalArrayBufferInterfaceEntries Entries;

    /// <summary>
    /// Initializes <see cref="Entries"/>.
    /// </summary>
    static WindowsRuntimeExternalArrayBufferInterfaceEntriesImpl()
    {
        Entries.IBuffer.IID = WellKnownWindowsInterfaceIIDs.IID_IBuffer;
        Entries.IBuffer.Vtable = Windows.Storage.Streams.IBufferImpl.Vtable;
        Entries.IBufferByteAccess.IID = WellKnownWindowsInterfaceIIDs.IID_IBufferByteAccess;
        Entries.IBufferByteAccess.Vtable = WindowsRuntimeExternalArrayBufferByteAccessImpl.Vtable;
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
/// A custom <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> implementation for <see cref="WindowsRuntimeExternalArrayBuffer"/>.
/// </summary>
file sealed unsafe class WindowsRuntimeExternalArrayBufferComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
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
        count = sizeof(WindowsRuntimeExternalArrayBufferInterfaceEntries) / sizeof(ComInterfaceEntry);

        return (ComInterfaceEntry*)Unsafe.AsPointer(in WindowsRuntimeExternalArrayBufferInterfaceEntriesImpl.Entries);
    }
}

/// <summary>
/// The native implementation of <c>IBufferByteAccess</c> for <see cref="WindowsRuntimeExternalArrayBuffer"/>.
/// </summary>
file static unsafe class WindowsRuntimeExternalArrayBufferByteAccessImpl
{
    /// <summary>
    /// The <see cref="IBufferByteAccessVftbl"/> value for the <see cref="WindowsRuntimeExternalArrayBuffer"/> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly IBufferByteAccessVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static WindowsRuntimeExternalArrayBufferByteAccessImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.Buffer = &Buffer;
    }

    /// <summary>
    /// Gets a pointer to the <see cref="WindowsRuntimeExternalArrayBuffer"/> implementation.
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
            var thisObject = ComInterfaceDispatch.GetInstance<global::WindowsRuntime.InteropServices.WindowsRuntimeExternalArrayBuffer>((ComInterfaceDispatch*)thisPtr);

            *value = thisObject.Buffer();

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception ex)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(ex);
        }
    }
}