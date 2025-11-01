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

#pragma warning disable IDE0008, IDE0046, IDE1006

namespace ABI.WindowsRuntime.InteropServices;

/// <summary>
/// Marshaller for <see cref="BindableIReadOnlyListAdapter"/>.
/// </summary>
internal static unsafe class BindableIReadOnlyListAdapterMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.ConvertToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(BindableIReadOnlyListAdapter? value)
    {
        if (value is null)
        {
            return default;
        }

        return new((void*)WindowsRuntimeComWrappers.Default.GetOrCreateComInterfaceForObject(
            instance: value,
            flags: CreateComInterfaceFlags.TrackerSupport,
            iid: in WellKnownWindowsInterfaceIIDs.IID_IBindableVectorView));
    }
}

/// <summary>
/// The set of <see cref="ComInterfaceEntry"/> values for <see cref="BindableIReadOnlyListAdapter"/>.
/// </summary>
file struct BindableIReadOnlyListAdapterInterfaceEntries
{
    public ComInterfaceEntry IBindableVectorView;
    public ComInterfaceEntry IBindableEnumerable;
    public ComInterfaceEntry IStringable;
    public ComInterfaceEntry IWeakReferenceSource;
    public ComInterfaceEntry IMarshal;
    public ComInterfaceEntry IAgileObject;
    public ComInterfaceEntry IInspectable;
    public ComInterfaceEntry IUnknown;
}

/// <summary>
/// The implementation of <see cref="BindableIReadOnlyListAdapterInterfaceEntries"/>.
/// </summary>
file static class BindableIReadOnlyListAdapterInterfaceEntriesImpl
{
    /// <summary>
    /// The <see cref="BindableIReadOnlyListAdapterInterfaceEntries"/> value for <see cref="BindableIReadOnlyListAdapter"/>.
    /// </summary>
    [FixedAddressValueType]
    public static readonly BindableIReadOnlyListAdapterInterfaceEntries Entries;

    /// <summary>
    /// Initializes <see cref="Entries"/>.
    /// </summary>
    static BindableIReadOnlyListAdapterInterfaceEntriesImpl()
    {
        Entries.IBindableVectorView.IID = WellKnownWindowsInterfaceIIDs.IID_IBindableVectorView;
        Entries.IBindableVectorView.Vtable = BindableIReadOnlyListAdapterImpl.Vtable;
        Entries.IBindableEnumerable.IID = WellKnownWindowsInterfaceIIDs.IID_IBindableIterable;
        Entries.IBindableEnumerable.Vtable = System.Collections.IEnumerableImpl.Vtable;
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
/// A custom <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> implementation for <see cref="BindableIReadOnlyListAdapter"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed unsafe class BindableIReadOnlyListAdapterComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
{
    /// <inheritdoc/>
    public override void* GetOrCreateComInterfaceForObject(object value)
    {
        return (void*)WindowsRuntimeComWrappers.Default.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.TrackerSupport);
    }

    /// <inheritdoc/>
    public override ComInterfaceEntry* ComputeVtables(out int count)
    {
        count = sizeof(BindableIReadOnlyListAdapterInterfaceEntries) / sizeof(ComInterfaceEntry);

        return (ComInterfaceEntry*)Unsafe.AsPointer(in BindableIReadOnlyListAdapterInterfaceEntriesImpl.Entries);
    }
}

/// <summary>
/// The native implementation for <see cref="BindableIReadOnlyListAdapter"/>.
/// </summary>
file static unsafe class BindableIReadOnlyListAdapterImpl
{
    /// <summary>
    /// The <see cref="IBindableVectorViewVftbl"/> value for the <see cref="BindableIReadOnlyListAdapter"/> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly IBindableVectorViewVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static BindableIReadOnlyListAdapterImpl()
    {
        *(IUnknownVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IUnknownVftbl*)IUnknownImpl.Vtable;

        Vftbl.GetAt = &GetAt;
        Vftbl.get_Size = &get_Size;
        Vftbl.IndexOf = &IndexOf;
    }

    /// <summary>
    /// Gets a pointer to the <see cref="BindableIReadOnlyListAdapter"/> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Vftbl);
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindablevectorview.getat"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT GetAt(void* thisPtr, uint index, void** result)
    {
        if (result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            var unboxedValue = ComInterfaceDispatch.GetInstance<BindableIReadOnlyListAdapter>((ComInterfaceDispatch*)thisPtr);

            object? item = unboxedValue.GetAt(index);

            *result = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(item).DetachThisPtrUnsafe();

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception ex)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(ex);
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindablevectorview.size"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT get_Size(void* thisPtr, uint* size)
    {
        if (size is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            var unboxedValue = ComInterfaceDispatch.GetInstance<BindableIReadOnlyListAdapter>((ComInterfaceDispatch*)thisPtr);

            *size = unboxedValue.Size;

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception ex)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(ex);
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindablevectorview.indexof"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT IndexOf(void* thisPtr, void* value, uint* index, bool* result)
    {
        if (value is null || index is null || result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            var unboxedValue = ComInterfaceDispatch.GetInstance<BindableIReadOnlyListAdapter>((ComInterfaceDispatch*)thisPtr);

            object? target = WindowsRuntimeObjectMarshaller.ConvertToManaged(value);

            *result = unboxedValue.IndexOf(target, out *index);

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception ex)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(ex);
        }
    }
}
