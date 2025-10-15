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
    typeof(WindowsRuntimeBindableVectorViewProxy),
    typeof(ABI.WindowsRuntime.WindowsRuntimeBindableVectorViewProxy))]

namespace ABI.WindowsRuntime;

/// <summary>
/// ABI type for <see cref="global::WindowsRuntime.WindowsRuntimeBindableVectorViewProxy"/>.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.data.propertychangedeventhandler"/>
/// <seealso href="https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.data.propertychangedeventhandler"/>
[WindowsRuntimeClassName("Windows.UI.Xaml.Interop.IBindableVectorView")] // TODO: handle WinUI 3 as well
[WindowsRuntimeBindableVectorViewProxyComWrappersMarshaller]
file static class WindowsRuntimeBindableVectorViewProxy;

/// <summary>
/// Marshaller for <see cref="global::WindowsRuntime.WindowsRuntimeBindableVectorViewProxy"/>.
/// </summary>
internal static unsafe class WindowsRuntimeBindableVectorViewProxyMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.ConvertToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(global::WindowsRuntime.WindowsRuntimeBindableVectorViewProxy? value)
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
/// The set of <see cref="ComInterfaceEntry"/> values for <see cref="global::WindowsRuntime.WindowsRuntimeBindableVectorViewProxy"/>.
/// </summary>
file struct WindowsRuntimeBindableVectorViewProxyInterfaceEntries
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
/// The implementation of <see cref="WindowsRuntimeBindableVectorViewProxyInterfaceEntries"/>.
/// </summary>
file static class WindowsRuntimeBindableVectorViewProxyInterfaceEntriesImpl
{
    /// <summary>
    /// The <see cref="WindowsRuntimeBindableVectorViewProxyInterfaceEntries"/> value for <see cref="global::WindowsRuntime.WindowsRuntimeBindableVectorViewProxy"/>.
    /// </summary>
    [FixedAddressValueType]
    public static readonly WindowsRuntimeBindableVectorViewProxyInterfaceEntries Entries;

    /// <summary>
    /// Initializes <see cref="Entries"/>.
    /// </summary>
    static WindowsRuntimeBindableVectorViewProxyInterfaceEntriesImpl()
    {
        Entries.IBindableVectorView.IID = WindowsRuntimeBindableVectorViewProxyImpl.IID;
        Entries.IBindableVectorView.Vtable = WindowsRuntimeBindableVectorViewProxyImpl.Vtable;
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
/// A custom <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> implementation for <see cref="global::WindowsRuntime.WindowsRuntimeBindableVectorViewProxy"/>.
/// </summary>
file sealed unsafe class WindowsRuntimeBindableVectorViewProxyComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
{
    /// <inheritdoc/>
    public override void* GetOrCreateComInterfaceForObject(object value)
    {
        return (void*)WindowsRuntimeComWrappers.Default.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.TrackerSupport);
    }

    /// <inheritdoc/>
    public override ComInterfaceEntry* ComputeVtables(out int count)
    {
        count = sizeof(WindowsRuntimeBindableVectorViewProxyInterfaceEntries) / sizeof(ComInterfaceEntry);

        return (ComInterfaceEntry*)Unsafe.AsPointer(in WindowsRuntimeBindableVectorViewProxyInterfaceEntriesImpl.Entries);
    }
}

/// <summary>
/// The native implementation for <see cref="global::WindowsRuntime.WindowsRuntimeBindableVectorViewProxy"/>.
/// </summary>
file static unsafe class WindowsRuntimeBindableVectorViewProxyImpl
{
    /// <summary>
    /// The <see cref="IBindableVectorViewVftbl"/> value for the <see cref="global::WindowsRuntime.WindowsRuntimeBindableVectorViewProxy"/> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly IBindableVectorViewVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static WindowsRuntimeBindableVectorViewProxyImpl()
    {
        *(IUnknownVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IUnknownVftbl*)IUnknownImpl.Vtable;

        Vftbl.GetAt = &GetAt;
        Vftbl.get_Size = &get_Size;
        Vftbl.IndexOf = &IndexOf;
    }

    /// <summary>
    /// Gets the IID for <see cref="global::WindowsRuntime.WindowsRuntimeBindableVectorViewProxy"/>.
    /// </summary>
    public static ref readonly Guid IID
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref WellKnownInterfaceIds.IID_IBindableVectorView;
    }

    /// <summary>
    /// Gets a pointer to the <see cref="global::WindowsRuntime.WindowsRuntimeBindableVectorViewProxy"/> implementation.
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
            var unboxedValue = ComInterfaceDispatch.GetInstance<global::WindowsRuntime.WindowsRuntimeBindableVectorViewProxy>((ComInterfaceDispatch*)thisPtr);

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
            var unboxedValue = ComInterfaceDispatch.GetInstance<global::WindowsRuntime.WindowsRuntimeBindableVectorViewProxy>((ComInterfaceDispatch*)thisPtr);

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
            var unboxedValue = ComInterfaceDispatch.GetInstance<global::WindowsRuntime.WindowsRuntimeBindableVectorViewProxy>((ComInterfaceDispatch*)thisPtr);

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
