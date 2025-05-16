// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Foundation;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;
using static System.Runtime.InteropServices.ComWrappers;

#pragma warning disable CS0649, IDE0008, IDE1006

[assembly: TypeMap<WindowsRuntimeTypeMapGroup>(
    value: "Windows.Foundation.IReference<Windows.UI.Xaml.Interop.NotifyCollectionChangedEventHandler>",
    target: typeof(ABI.System.Collections.Specialized.NotifyCollectionChangedEventHandler),
    trimTarget: typeof(NotifyCollectionChangedEventHandler))]

[assembly: TypeMap<WindowsRuntimeTypeMapGroup>(
    value: "Windows.Foundation.IReference<Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventHandler>",
    target: typeof(ABI.System.Collections.Specialized.NotifyCollectionChangedEventHandler),
    trimTarget: typeof(NotifyCollectionChangedEventHandler))]

[assembly: TypeMapAssociation<WindowsRuntimeTypeMapGroup>(
    typeof(NotifyCollectionChangedEventHandler),
    typeof(ABI.System.Collections.Specialized.NotifyCollectionChangedEventHandler))]

namespace ABI.System.Collections.Specialized;

/// <summary>
/// ABI type for <see cref="global::System.Collections.Specialized.NotifyCollectionChangedEventHandler"/>.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.NotifyCollectionChangedEventHandler"/>
/// <seealso href="https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.interop.NotifyCollectionChangedEventHandler"/>
[WindowsRuntimeClassName("Windows.Foundation.IReference<Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventHandler>")]
[NotifyCollectionChangedEventHandlerComWrappersMarshaller]
file static class NotifyCollectionChangedEventHandler;

/// <summary>
/// Marshaller for <see cref="global::System.Collections.Specialized.NotifyCollectionChangedEventHandler"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class NotifyCollectionChangedEventHandlerMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.ConvertToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(global::System.Collections.Specialized.NotifyCollectionChangedEventHandler? value)
    {
        return WindowsRuntimeDelegateMarshaller.ConvertToUnmanaged(value, in NotifyCollectionChangedEventHandlerImpl.IID);
    }

    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.ConvertToManaged"/>
    public static global::System.Collections.Specialized.NotifyCollectionChangedEventHandler? ConvertToManaged(void* value)
    {
        object? result = WindowsRuntimeDelegateMarshaller.ConvertToManaged<NotifyCollectionChangedEventHandlerComWrappersCallback>(value);

        return Unsafe.As<global::System.Collections.Specialized.NotifyCollectionChangedEventHandler?>(result);
    }

    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.BoxToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue BoxToUnmanaged(global::System.Collections.Specialized.NotifyCollectionChangedEventHandler? value)
    {
        return WindowsRuntimeDelegateMarshaller.BoxToUnmanaged(value, in NotifyCollectionChangedEventHandlerReferenceImpl.IID);
    }

    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.UnboxToManaged(void*)"/>
    public static global::System.Collections.Specialized.NotifyCollectionChangedEventHandler? UnboxToManaged(void* value)
    {
        object? result = WindowsRuntimeDelegateMarshaller.UnboxToManaged<NotifyCollectionChangedEventHandlerComWrappersCallback>(value);

        return Unsafe.As<global::System.Collections.Specialized.NotifyCollectionChangedEventHandler?>(result);
    }
}

/// <summary>
/// The <see cref="WindowsRuntimeObject"/> implementation for <see cref="global::System.Collections.Specialized.NotifyCollectionChangedEventHandler"/>.
/// </summary>
file static unsafe class NotifyCollectionChangedEventHandlerNativeDelegate
{
    /// <inheritdoc cref="global::System.Collections.Specialized.NotifyCollectionChangedEventHandler"/>
    public static void Invoke(this WindowsRuntimeObjectReference objectReference, object? sender, NotifyCollectionChangedEventArgs e)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = objectReference.AsValue();
        using WindowsRuntimeObjectReferenceValue senderValue = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(sender);
        using WindowsRuntimeObjectReferenceValue eValue = NotifyCollectionChangedEventArgsMarshaller.ConvertToUnmanaged(e);

        void* thisPtr = thisValue.GetThisPtrUnsafe();

        HRESULT hresult = ((NotifyCollectionChangedEventHandlerVftbl*)*(void***)thisPtr)->Invoke(
            thisPtr,
            senderValue.GetThisPtrUnsafe(),
            eValue.GetThisPtrUnsafe());

        RestrictedErrorInfo.ThrowExceptionForHR(hresult);
    }
}

/// <summary>
/// A custom <see cref="IWindowsRuntimeComWrappersCallback"/> implementation for <see cref="global::System.Collections.Specialized.NotifyCollectionChangedEventHandler"/>.
/// </summary>
file abstract unsafe class NotifyCollectionChangedEventHandlerComWrappersCallback : IWindowsRuntimeComWrappersCallback
{
    /// <inheritdoc/>
    public static object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
    {
        WindowsRuntimeObjectReference valueReference = WindowsRuntimeObjectReference.CreateUnsafe(value, in NotifyCollectionChangedEventHandlerImpl.IID)!;

        wrapperFlags = valueReference.GetReferenceTrackerPtrUnsafe() == null ? CreatedWrapperFlags.None : CreatedWrapperFlags.TrackerObject;

        return new global::System.Collections.Specialized.NotifyCollectionChangedEventHandler(valueReference.Invoke);
    }
}

/// <summary>
/// The set of <see cref="ComInterfaceEntry"/> values for <see cref="global::System.Collections.Specialized.NotifyCollectionChangedEventHandler"/>.
/// </summary>
file struct NotifyCollectionChangedEventHandlerInterfaceEntries
{
    public ComInterfaceEntry NotifyCollectionChangedEventHandler;
    public ComInterfaceEntry IReferenceOfNotifyCollectionChangedEventHandler;
    public ComInterfaceEntry IPropertyValue;
    public ComInterfaceEntry IStringable;
    public ComInterfaceEntry IWeakReferenceSource;
    public ComInterfaceEntry IMarshal;
    public ComInterfaceEntry IAgileObject;
    public ComInterfaceEntry IInspectable;
    public ComInterfaceEntry IUnknown;
}

/// <summary>
/// The implementation of <see cref="NotifyCollectionChangedEventHandlerInterfaceEntries"/>.
/// </summary>
file static class NotifyCollectionChangedEventHandlerInterfaceEntriesImpl
{
    /// <summary>
    /// The <see cref="NotifyCollectionChangedEventHandlerInterfaceEntries"/> value for <see cref="global::System.Collections.Specialized.NotifyCollectionChangedEventHandler"/>.
    /// </summary>
    [FixedAddressValueType]
    public static readonly NotifyCollectionChangedEventHandlerInterfaceEntries Entries;

    /// <summary>
    /// Initializes <see cref="Entries"/>.
    /// </summary>
    static NotifyCollectionChangedEventHandlerInterfaceEntriesImpl()
    {
        Entries.NotifyCollectionChangedEventHandler.IID = NotifyCollectionChangedEventHandlerImpl.IID;
        Entries.NotifyCollectionChangedEventHandler.Vtable = NotifyCollectionChangedEventHandlerImpl.Vtable;
        Entries.IReferenceOfNotifyCollectionChangedEventHandler.IID = NotifyCollectionChangedEventHandlerReferenceImpl.IID;
        Entries.IReferenceOfNotifyCollectionChangedEventHandler.Vtable = NotifyCollectionChangedEventHandlerReferenceImpl.Vtable;
        Entries.IPropertyValue.IID = WellKnownInterfaceIds.IID_IPropertyValue;
        Entries.IPropertyValue.Vtable = IPropertyValueImpl.OtherTypeVtable;
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
/// A custom <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> implementation for <see cref="global::System.Collections.Specialized.NotifyCollectionChangedEventHandler"/>.
/// </summary>
file sealed unsafe class NotifyCollectionChangedEventHandlerComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
{
    /// <inheritdoc/>
    public override ComInterfaceEntry* ComputeVtables(out int count)
    {
        count = sizeof(NotifyCollectionChangedEventHandlerInterfaceEntries) / sizeof(ComInterfaceEntry);

        return (ComInterfaceEntry*)Unsafe.AsPointer(ref Unsafe.AsRef(in NotifyCollectionChangedEventHandlerInterfaceEntriesImpl.Entries));
    }

    /// <inheritdoc/>
    public override object CreateObject(void* value)
    {
        return WindowsRuntimeDelegateMarshaller.UnboxToManaged<NotifyCollectionChangedEventHandlerComWrappersCallback>(value, in NotifyCollectionChangedEventHandlerReferenceImpl.IID)!;
    }
}

/// <summary>
/// Binding type for the <see cref="global::System.Collections.Specialized.NotifyCollectionChangedEventHandler"/> implementation.
/// </summary>
file unsafe struct NotifyCollectionChangedEventHandlerVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, void*, void*, HRESULT> Invoke;
}

/// <summary>
/// The native implementation for <see cref="global::System.Collections.Specialized.NotifyCollectionChangedEventHandler"/>.
/// </summary>
file static unsafe class NotifyCollectionChangedEventHandlerImpl
{
    /// <summary>
    /// The <see cref="NotifyCollectionChangedEventHandlerVftbl"/> value for the <see cref="global::System.Collections.Specialized.NotifyCollectionChangedEventHandler"/> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly NotifyCollectionChangedEventHandlerVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static NotifyCollectionChangedEventHandlerImpl()
    {
        *(IUnknownVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IUnknownVftbl*)IUnknownImpl.Vtable;

        Vftbl.Invoke = &Invoke;
    }

    /// <summary>
    /// Gets the IID for <see cref="global::System.Collections.Specialized.NotifyCollectionChangedEventHandler"/>.
    /// </summary>
    public static ref readonly Guid IID => ref WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections
        ? ref WellKnownInterfaceIds.IID_WUX_NotifyCollectionChangedEventHandler
        : ref WellKnownInterfaceIds.IID_MUX_NotifyCollectionChangedEventHandler;

    /// <summary>
    /// Gets a pointer to the <see cref="global::System.Collections.Specialized.NotifyCollectionChangedEventHandler"/> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(ref Unsafe.AsRef(in Vftbl));
    }

    /// <inheritdoc cref="global::System.Collections.Specialized.NotifyCollectionChangedEventHandler"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT Invoke(void* thisPtr, void* sender, void* e)
    {
        try
        {
            var unboxedValue = ComInterfaceDispatch.GetInstance<global::System.Collections.Specialized.NotifyCollectionChangedEventHandler>((ComInterfaceDispatch*)thisPtr);

            unboxedValue(
                WindowsRuntimeObjectMarshaller.ConvertToManaged(sender),
                NotifyCollectionChangedEventArgsMarshaller.ConvertToManaged(e)!);

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception ex)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(ex);
        }
    }
}

/// <summary>
/// Binding type for the <c>IReference`1</c> implementation for <see cref="global::System.Collections.Specialized.NotifyCollectionChangedEventHandler"/>.
/// </summary>
file unsafe struct NotifyCollectionChangedEventHandlerReferenceVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, void**, HRESULT> get_Value;
}

/// <summary>
/// The <c>IReference`1</c> implementation for <see cref="global::System.Collections.Specialized.NotifyCollectionChangedEventHandler"/>.
/// </summary>
file static unsafe class NotifyCollectionChangedEventHandlerReferenceImpl
{
    /// <summary>
    /// The <see cref="NotifyCollectionChangedEventHandlerReferenceVftbl"/> value for the managed <c>IReference`1</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly NotifyCollectionChangedEventHandlerReferenceVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static NotifyCollectionChangedEventHandlerReferenceImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.get_Value = &get_Value;
    }

    /// <summary>
    /// Gets the IID for The IID for <c>IReference`1</c> of <see cref="global::System.Collections.Specialized.NotifyCollectionChangedEventHandler"/>.
    /// </summary>
    public static ref readonly Guid IID => ref WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections
        ? ref WellKnownInterfaceIds.IID_WUX_IReferenceOfNotifyCollectionChangedEventHandler
        : ref WellKnownInterfaceIds.IID_MUX_IReferenceOfNotifyCollectionChangedEventHandler;

    /// <summary>
    /// Gets a pointer to the managed <c>IReference`1</c> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(ref Unsafe.AsRef(in Vftbl));
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ireference-1.value"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT get_Value(void* thisPtr, void** result)
    {
        if (result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            var unboxedValue = ComInterfaceDispatch.GetInstance<global::System.Collections.Specialized.NotifyCollectionChangedEventHandler>((ComInterfaceDispatch*)thisPtr);

            *result = NotifyCollectionChangedEventHandlerMarshaller.ConvertToUnmanaged(unboxedValue).DetachThisPtrUnsafe();

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            *result = null;

            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}
