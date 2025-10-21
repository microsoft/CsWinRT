// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ABI.System.Collections;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;
using static System.Runtime.InteropServices.ComWrappers;

#pragma warning disable IDE0008, IDE0046, IDE1006

[assembly: TypeMapAssociation<WindowsRuntimeComWrappersTypeMapGroup>(
    typeof(BindableIReadOnlyListAdapter),
    typeof(ABI.WindowsRuntime.BindableIReadOnlyListAdapter))]

namespace ABI.WindowsRuntime;

/// <summary>
/// ABI type for <see cref="global::WindowsRuntime.InteropServices.BindableIReadOnlyListAdapter"/>.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.data.propertychangedeventhandler"/>
/// <seealso href="https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.data.propertychangedeventhandler"/>
[WindowsRuntimeClassName("Windows.UI.Xaml.Interop.IBindableVectorView")] // TODO: handle WinUI 3 as well
[BindableIReadOnlyListAdapterComWrappersMarshaller]
file static class BindableIReadOnlyListAdapter;

/// <summary>
/// Marshaller for <see cref="global::WindowsRuntime.InteropServices.BindableIReadOnlyListAdapter"/>.
/// </summary>
internal static unsafe class BindableIReadOnlyListAdapterMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.ConvertToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(global::WindowsRuntime.InteropServices.BindableIReadOnlyListAdapter? value)
    {
        if (value is null)
        {
            return default;
        }

        return new((void*)WindowsRuntimeComWrappers.Default.GetOrCreateComInterfaceForObject(
            instance: value,
            flags: CreateComInterfaceFlags.TrackerSupport,
            iid: in WellKnownInterfaceIIDs.IID_IBindableVectorView));
    }
}

/// <summary>
/// The set of <see cref="ComInterfaceEntry"/> values for <see cref="global::WindowsRuntime.InteropServices.BindableIReadOnlyListAdapter"/>.
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
    /// The <see cref="BindableIReadOnlyListAdapterInterfaceEntries"/> value for <see cref="global::WindowsRuntime.InteropServices.BindableIReadOnlyListAdapter"/>.
    /// </summary>
    [FixedAddressValueType]
    public static readonly BindableIReadOnlyListAdapterInterfaceEntries Entries;

    /// <summary>
    /// Initializes <see cref="Entries"/>.
    /// </summary>
    static BindableIReadOnlyListAdapterInterfaceEntriesImpl()
    {
        Entries.IBindableVectorView.IID = WellKnownInterfaceIIDs.IID_IBindableVectorView;
        Entries.IBindableVectorView.Vtable = BindableIReadOnlyListAdapterImpl.Vtable;
        Entries.IBindableEnumerable.IID = WellKnownInterfaceIIDs.IID_IBindableIterable;
        Entries.IBindableEnumerable.Vtable = IEnumerableImpl.Vtable;
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
/// A custom <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> implementation for <see cref="global::WindowsRuntime.InteropServices.BindableIReadOnlyListAdapter"/>.
/// </summary>
file sealed unsafe class BindableIReadOnlyListAdapterComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
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
/// The native implementation for <see cref="global::WindowsRuntime.InteropServices.BindableIReadOnlyListAdapter"/>.
/// </summary>
file static unsafe class BindableIReadOnlyListAdapterImpl
{
    /// <summary>
    /// The <see cref="IBindableVectorViewVftbl"/> value for the <see cref="global::WindowsRuntime.InteropServices.BindableIReadOnlyListAdapter"/> implementation.
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
    /// Gets a pointer to the <see cref="global::WindowsRuntime.InteropServices.BindableIReadOnlyListAdapter"/> implementation.
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
            var unboxedValue = ComInterfaceDispatch.GetInstance<global::WindowsRuntime.InteropServices.BindableIReadOnlyListAdapter>((ComInterfaceDispatch*)thisPtr);

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
            var unboxedValue = ComInterfaceDispatch.GetInstance<global::WindowsRuntime.InteropServices.BindableIReadOnlyListAdapter>((ComInterfaceDispatch*)thisPtr);

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
            var unboxedValue = ComInterfaceDispatch.GetInstance<global::WindowsRuntime.InteropServices.BindableIReadOnlyListAdapter>((ComInterfaceDispatch*)thisPtr);

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
