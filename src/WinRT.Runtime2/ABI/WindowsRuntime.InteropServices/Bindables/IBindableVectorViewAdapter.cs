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
    typeof(IBindableVectorViewAdapter),
    typeof(ABI.WindowsRuntime.IBindableVectorViewAdapter))]

namespace ABI.WindowsRuntime;

/// <summary>
/// ABI type for <see cref="global::WindowsRuntime.InteropServices.IBindableVectorViewAdapter"/>.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.data.propertychangedeventhandler"/>
/// <seealso href="https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.data.propertychangedeventhandler"/>
[WindowsRuntimeClassName("Windows.UI.Xaml.Interop.IBindableVectorView")] // TODO: handle WinUI 3 as well
[IBindableVectorViewAdapterComWrappersMarshaller]
file static class IBindableVectorViewAdapter;

/// <summary>
/// Marshaller for <see cref="global::WindowsRuntime.InteropServices.IBindableVectorViewAdapter"/>.
/// </summary>
internal static unsafe class IBindableVectorViewAdapterMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.ConvertToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(global::WindowsRuntime.InteropServices.IBindableVectorViewAdapter? value)
    {
        if (value is null)
        {
            return default;
        }

        return new((void*)WindowsRuntimeComWrappers.Default.GetOrCreateComInterfaceForObject(
            instance: value,
            flags: CreateComInterfaceFlags.TrackerSupport,
            iid: in WellKnownInterfaceIds.IID_IBindableVectorView));
    }
}

/// <summary>
/// The set of <see cref="ComInterfaceEntry"/> values for <see cref="global::WindowsRuntime.InteropServices.IBindableVectorViewAdapter"/>.
/// </summary>
file struct IBindableVectorViewAdapterInterfaceEntries
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
/// The implementation of <see cref="IBindableVectorViewAdapterInterfaceEntries"/>.
/// </summary>
file static class IBindableVectorViewAdapterInterfaceEntriesImpl
{
    /// <summary>
    /// The <see cref="IBindableVectorViewAdapterInterfaceEntries"/> value for <see cref="global::WindowsRuntime.InteropServices.IBindableVectorViewAdapter"/>.
    /// </summary>
    [FixedAddressValueType]
    public static readonly IBindableVectorViewAdapterInterfaceEntries Entries;

    /// <summary>
    /// Initializes <see cref="Entries"/>.
    /// </summary>
    static IBindableVectorViewAdapterInterfaceEntriesImpl()
    {
        Entries.IBindableVectorView.IID = IBindableVectorViewAdapterImpl.IID;
        Entries.IBindableVectorView.Vtable = IBindableVectorViewAdapterImpl.Vtable;
        Entries.IBindableEnumerable.IID = IEnumerableImpl.IID;
        Entries.IBindableEnumerable.Vtable = IEnumerableImpl.Vtable;
        Entries.IStringable.IID = WellKnownInterfaceIds.IID_IStringable;
        Entries.IStringable.Vtable = IStringableImpl.Vtable;
        Entries.IWeakReferenceSource.IID = WellKnownInterfaceIds.IID_IWeakReferenceSource;
        Entries.IWeakReferenceSource.Vtable = IWeakReferenceSourceImpl.Vtable;
        Entries.IMarshal.IID = WellKnownInterfaceIds.IID_IMarshal;
        Entries.IMarshal.Vtable = IMarshalImpl.Vtable;
        Entries.IAgileObject.IID = WellKnownInterfaceIds.IID_IAgileObject;
        Entries.IAgileObject.Vtable = IUnknownImpl.Vtable;
        Entries.IInspectable.IID = WellKnownInterfaceIds.IID_IInspectable;
        Entries.IInspectable.Vtable = IInspectableImpl.Vtable;
        Entries.IUnknown.IID = WellKnownInterfaceIds.IID_IUnknown;
        Entries.IUnknown.Vtable = IUnknownImpl.Vtable;
    }
}

/// <summary>
/// A custom <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> implementation for <see cref="global::WindowsRuntime.InteropServices.IBindableVectorViewAdapter"/>.
/// </summary>
file sealed unsafe class IBindableVectorViewAdapterComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
{
    /// <inheritdoc/>
    public override void* GetOrCreateComInterfaceForObject(object value)
    {
        return (void*)WindowsRuntimeComWrappers.Default.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.TrackerSupport);
    }

    /// <inheritdoc/>
    public override ComInterfaceEntry* ComputeVtables(out int count)
    {
        count = sizeof(IBindableVectorViewAdapterInterfaceEntries) / sizeof(ComInterfaceEntry);

        return (ComInterfaceEntry*)Unsafe.AsPointer(in IBindableVectorViewAdapterInterfaceEntriesImpl.Entries);
    }
}

/// <summary>
/// The native implementation for <see cref="global::WindowsRuntime.InteropServices.IBindableVectorViewAdapter"/>.
/// </summary>
file static unsafe class IBindableVectorViewAdapterImpl
{
    /// <summary>
    /// The <see cref="IBindableVectorViewVftbl"/> value for the <see cref="global::WindowsRuntime.InteropServices.IBindableVectorViewAdapter"/> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly IBindableVectorViewVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static IBindableVectorViewAdapterImpl()
    {
        *(IUnknownVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IUnknownVftbl*)IUnknownImpl.Vtable;

        Vftbl.GetAt = &GetAt;
        Vftbl.get_Size = &get_Size;
        Vftbl.IndexOf = &IndexOf;
    }

    /// <summary>
    /// Gets the IID for <see cref="global::WindowsRuntime.InteropServices.IBindableVectorViewAdapter"/>.
    /// </summary>
    public static ref readonly Guid IID
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref WellKnownInterfaceIds.IID_IBindableVectorView;
    }

    /// <summary>
    /// Gets a pointer to the <see cref="global::WindowsRuntime.InteropServices.IBindableVectorViewAdapter"/> implementation.
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
            var unboxedValue = ComInterfaceDispatch.GetInstance<global::WindowsRuntime.InteropServices.IBindableVectorViewAdapter>((ComInterfaceDispatch*)thisPtr);

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
            var unboxedValue = ComInterfaceDispatch.GetInstance<global::WindowsRuntime.InteropServices.IBindableVectorViewAdapter>((ComInterfaceDispatch*)thisPtr);

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
            var unboxedValue = ComInterfaceDispatch.GetInstance<global::WindowsRuntime.InteropServices.IBindableVectorViewAdapter>((ComInterfaceDispatch*)thisPtr);

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
