﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;
using static System.Runtime.InteropServices.ComWrappers;

#pragma warning disable CS0649, IDE0008

[assembly: TypeMap<WindowsRuntimeTypeMapUniverse>(
    value: "Windows.Foundation.IReference<Windows.UI.Xaml.Data.PropertyChangedEventHandler>",
    target: typeof(ABI.System.ComponentModel.PropertyChangedEventHandler),
    trimTarget: typeof(PropertyChangedEventHandler))]

[assembly: TypeMap<WindowsRuntimeTypeMapUniverse>(
    value: "Windows.Foundation.IReference<Microsoft.UI.Xaml.Data.PropertyChangedEventHandler>",
    target: typeof(ABI.System.ComponentModel.PropertyChangedEventHandler),
    trimTarget: typeof(PropertyChangedEventHandler))]

[assembly: TypeMapAssociation<WindowsRuntimeTypeMapUniverse>(
    typeof(PropertyChangedEventHandler),
    typeof(ABI.System.ComponentModel.PropertyChangedEventHandler))]

namespace ABI.System.ComponentModel;

/// <summary>
/// ABI type for <see cref="global::System.ComponentModel.PropertyChangedEventHandler"/>.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.data.propertychangedeventhandler"/>
/// <seealso href="https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.data.propertychangedeventhandler"/>
[WindowsRuntimeClassName("Windows.Foundation.IReference<Microsoft.UI.Xaml.Data.PropertyChangedEventHandler>")]
[PropertyChangedEventHandlerComWrappersMarshaller]
file static class PropertyChangedEventHandler;

/// <summary>
/// Marshaller for <see cref="global::System.ComponentModel.PropertyChangedEventHandler"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class PropertyChangedEventHandlerMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.ConvertToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(global::System.ComponentModel.PropertyChangedEventHandler? value)
    {
        return WindowsRuntimeDelegateMarshaller.ConvertToUnmanaged(value, in PropertyChangedEventHandlerImpl.IID);
    }

    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.ConvertToManaged"/>
    public static global::System.ComponentModel.PropertyChangedEventHandler? ConvertToManaged(void* value)
    {
        object? result = WindowsRuntimeDelegateMarshaller.ConvertToManaged<PropertyChangedEventHandlerComWrappersCallback>(value);

        return Unsafe.As<global::System.ComponentModel.PropertyChangedEventHandler?>(result);
    }

    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.BoxToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue BoxToUnmanaged(global::System.ComponentModel.PropertyChangedEventHandler? value)
    {
        return WindowsRuntimeDelegateMarshaller.BoxToUnmanaged(value, in PropertyChangedEventHandlerReferenceImpl.IID);
    }

    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.UnboxToManaged(void*)"/>
    public static global::System.ComponentModel.PropertyChangedEventHandler? UnboxToManaged(void* value)
    {
        object? result = WindowsRuntimeDelegateMarshaller.UnboxToManaged<PropertyChangedEventHandlerComWrappersCallback>(value);

        return Unsafe.As<global::System.ComponentModel.PropertyChangedEventHandler?>(result);
    }
}

/// <summary>
/// The <see cref="WindowsRuntimeObject"/> implementation for <see cref="global::System.ComponentModel.PropertyChangedEventHandler"/>.
/// </summary>
file static unsafe class PropertyChangedEventHandlerNativeDelegate
{
    /// <inheritdoc cref="global::System.ComponentModel.PropertyChangedEventHandler"/>
    public static void Invoke(this WindowsRuntimeObjectReference objectReference, object? sender, PropertyChangedEventArgs e)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = objectReference.AsValue();
        using WindowsRuntimeObjectReferenceValue senderValue = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(sender);
        using WindowsRuntimeObjectReferenceValue eValue = PropertyChangedEventArgsMarshaller.ConvertToUnmanaged(e);

        HRESULT hresult = ((delegate* unmanaged[MemberFunction]<void*, void*, void*, HRESULT>)(*(void***)thisValue.GetThisPtrUnsafe())[3])(
            thisValue.GetThisPtrUnsafe(),
            senderValue.GetThisPtrUnsafe(),
            eValue.GetThisPtrUnsafe());

        RestrictedErrorInfo.ThrowExceptionForHR(hresult);
    }
}

/// <summary>
/// A custom <see cref="IWindowsRuntimeComWrappersCallback"/> implementation for <see cref="global::System.ComponentModel.PropertyChangedEventHandler"/>.
/// </summary>
file abstract unsafe class PropertyChangedEventHandlerComWrappersCallback : IWindowsRuntimeComWrappersCallback
{
    /// <inheritdoc/>
    public static object CreateObject(void* value)
    {
        WindowsRuntimeObjectReference valueReference = WindowsRuntimeObjectReference.CreateUnsafe(value, in PropertyChangedEventHandlerImpl.IID)!;

        return new global::System.ComponentModel.PropertyChangedEventHandler(valueReference.Invoke);
    }
}

/// <summary>
/// The set of <see cref="ComInterfaceEntry"/> values for <see cref="global::System.ComponentModel.PropertyChangedEventHandler"/>.
/// </summary>
file struct PropertyChangedEventHandlerInterfaceEntries
{
    public ComInterfaceEntry PropertyChangedEventHandler;
    public ComInterfaceEntry IReferenceOfPropertyChangedEventHandler;
    public ComInterfaceEntry IStringable;
    public ComInterfaceEntry ICustomPropertyProvider;
    public ComInterfaceEntry IWeakReferenceSource;
    public ComInterfaceEntry IMarshal;
    public ComInterfaceEntry IAgileObject;
    public ComInterfaceEntry IInspectable;
    public ComInterfaceEntry IUnknown;
}

/// <summary>
/// The implementation of <see cref="PropertyChangedEventHandlerInterfaceEntries"/>.
/// </summary>
file static class PropertyChangedEventHandlerInterfaceEntriesImpl
{
    /// <summary>
    /// The <see cref="PropertyChangedEventHandlerInterfaceEntries"/> value for <see cref="global::System.ComponentModel.PropertyChangedEventHandler"/>.
    /// </summary>
    [FixedAddressValueType]
    public static readonly PropertyChangedEventHandlerInterfaceEntries Entries;

    /// <summary>
    /// Initializes <see cref="Entries"/>.
    /// </summary>
    static PropertyChangedEventHandlerInterfaceEntriesImpl()
    {
        Entries.PropertyChangedEventHandler.IID = PropertyChangedEventHandlerImpl.IID;
        Entries.PropertyChangedEventHandler.Vtable = PropertyChangedEventHandlerImpl.AbiToProjectionVftablePtr;
        Entries.IReferenceOfPropertyChangedEventHandler.IID = PropertyChangedEventHandlerReferenceImpl.IID;
        Entries.IReferenceOfPropertyChangedEventHandler.Vtable = PropertyChangedEventHandlerReferenceImpl.AbiToProjectionVftablePtr;
        Entries.IStringable.IID = WellKnownInterfaceIds.IID_IStringable;
        Entries.IStringable.Vtable = IStringableImpl.AbiToProjectionVftablePtr;
        Entries.ICustomPropertyProvider.IID = WellKnownInterfaceIds.IID_ICustomPropertyProvider;
        Entries.ICustomPropertyProvider.Vtable = 0; // TODO
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
/// A custom <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> implementation for <see cref="global::System.ComponentModel.PropertyChangedEventHandler"/>.
/// </summary>
file sealed unsafe class PropertyChangedEventHandlerComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
{
    /// <inheritdoc/>
    public override ComInterfaceEntry* ComputeVtables(out int count)
    {
        count = sizeof(PropertyChangedEventHandlerInterfaceEntries) / sizeof(ComInterfaceEntry);

        return (ComInterfaceEntry*)Unsafe.AsPointer(ref Unsafe.AsRef(in PropertyChangedEventHandlerInterfaceEntriesImpl.Entries));
    }

    /// <inheritdoc/>
    public override object CreateObject(void* value)
    {
        return WindowsRuntimeDelegateMarshaller.UnboxToManaged<PropertyChangedEventHandlerComWrappersCallback>(value, in PropertyChangedEventHandlerReferenceImpl.IID)!;
    }
}

/// <summary>
/// Binding type for the <see cref="global::System.ComponentModel.PropertyChangedEventHandler"/> implementation.
/// </summary>
file unsafe struct PropertyChangedEventHandlerVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, void*, void*, HRESULT> Invoke;
}

/// <summary>
/// The native implementation for <see cref="global::System.ComponentModel.PropertyChangedEventHandler"/>.
/// </summary>
file static unsafe class PropertyChangedEventHandlerImpl
{
    /// <summary>
    /// The <see cref="PropertyChangedEventHandlerVftbl"/> value for the <see cref="global::System.ComponentModel.PropertyChangedEventHandler"/> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly PropertyChangedEventHandlerVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static PropertyChangedEventHandlerImpl()
    {
        *(IUnknownVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IUnknownVftbl*)IUnknownImpl.AbiToProjectionVftablePtr;

        Vftbl.Invoke = &Invoke;
    }

    /// <summary>
    /// Gets the IID for <see cref="global::System.ComponentModel.PropertyChangedEventHandler"/>.
    /// </summary>
    public static ref readonly Guid IID => ref WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections
        ? ref WellKnownInterfaceIds.IID_WUX_PropertyChangedEventHandler
        : ref WellKnownInterfaceIds.IID_MUX_PropertyChangedEventHandler;

    /// <summary>
    /// Gets a pointer to the <see cref="global::System.ComponentModel.PropertyChangedEventHandler"/> implementation.
    /// </summary>
    public static nint AbiToProjectionVftablePtr
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(ref Unsafe.AsRef(in Vftbl));
    }

    /// <inheritdoc cref="global::System.ComponentModel.PropertyChangedEventHandler"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT Invoke(void* thisPtr, void* sender, void* e)
    {
        try
        {
            var unboxedValue = (global::System.ComponentModel.PropertyChangedEventHandler)ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            unboxedValue(
                WindowsRuntimeObjectMarshaller.ConvertToManaged(sender),
                PropertyChangedEventArgsMarshaller.ConvertToManaged(e)!);

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception ex)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(ex);
        }
    }
}

/// <summary>
/// Binding type for the <c>IReference`1</c> implementation for <see cref="global::System.ComponentModel.PropertyChangedEventHandler"/>.
/// </summary>
file unsafe struct PropertyChangedEventHandlerReferenceVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, void**, HRESULT> Value;
}

/// <summary>
/// The <c>IReference`1</c> implementation for <see cref="global::System.ComponentModel.PropertyChangedEventHandler"/>.
/// </summary>
file static unsafe class PropertyChangedEventHandlerReferenceImpl
{
    /// <summary>
    /// The <see cref="PropertyChangedEventHandlerReferenceVftbl"/> value for the managed <c>IReference`1</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly PropertyChangedEventHandlerReferenceVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static PropertyChangedEventHandlerReferenceImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.AbiToProjectionVftablePtr;

        Vftbl.Value = &Value;
    }

    /// <summary>
    /// Gets the IID for The IID for <c>IReference`1</c> of <see cref="global::System.ComponentModel.PropertyChangedEventHandler"/>.
    /// </summary>
    public static ref readonly Guid IID => ref WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections
        ? ref WellKnownInterfaceIds.IID_WUX_IReferenceOfPropertyChangedEventHandler
        : ref WellKnownInterfaceIds.IID_MUX_IReferenceOfPropertyChangedEventHandler;

    /// <summary>
    /// Gets a pointer to the managed <c>IReference`1</c> implementation.
    /// </summary>
    public static nint AbiToProjectionVftablePtr
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(ref Unsafe.AsRef(in Vftbl));
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ireference-1.value"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT Value(void* thisPtr, void** result)
    {
        if (result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            var unboxedValue = (global::System.ComponentModel.PropertyChangedEventHandler)ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            *result = PropertyChangedEventHandlerMarshaller.ConvertToUnmanaged(unboxedValue).GetThisPtrUnsafe();

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            *result = null;

            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}
