// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Foundation;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;
using static System.Runtime.InteropServices.ComWrappers;

#pragma warning disable IDE0008, IDE1006

[assembly: TypeMapAssociation<WindowsRuntimeComWrappersTypeMapGroup>(
    typeof(EventHandler),
    typeof(ABI.System.EventHandler))]

namespace ABI.System;

// The 'EventHandler' type is special, in that it doesn't exist in the Windows Runtime ABI (only 'EventHandler<T>' exists).
// However, we still need to support it, because it's a built-in type in .NET that is also used by projected (or, mapped)
// Windows Runtime interfaces, 'ICommand' being the most common one. For instance, the 'ICommand.CanExecuteChanged' event is
// just of type 'EventHandler', and we can't change that, since that interface is also just built into the .NET BCL.
// On the Windows Runtime side, that handler type is actually just 'EventHandler<Object>'.
//
// To fix this, we treat 'EventHandler' as a custom mapped type with special semantics. Specifically:
//   - We support marshalling 'EventHandler' instances as CCWs, which will implement 'EventHandler<Object>' at the ABI level.
//   - We don't support creating RCWs in the opaque 'object' scenario, ie. when we don't have statically visible type information.
//     All native objects reporting their runtime class name as 'Windows.Foundation.IReference<Windows.Foundation.EventHandler<Object>>'
//     will be marshalled as 'EventHandler<Object>'. We only special case marshalling to managed from an exact pointer to a native
//     delegate instance. This is mostly just needed to allow implementing 'ICommand.CanExecuteChanged' over native objects.
//
// This is also why some ABI methods for 'EventHandler' are either missing or not implemented.

/// <summary>
/// ABI type for <see cref="global::System.EventHandler"/>.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.eventhandler-1"/>
[WindowsRuntimeClassName("Windows.Foundation.IReference<Windows.Foundation.EventHandler<Object>>")]
[EventHandlerComWrappersMarshaller]
file static class EventHandler;

/// <summary>
/// Marshaller for <see cref="global::System.EventHandler"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class EventHandlerMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.ConvertToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(global::System.EventHandler? value)
    {
        return WindowsRuntimeDelegateMarshaller.ConvertToUnmanaged(value, in WellKnownInterfaceIds.IID_EventHandler);
    }

    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.ConvertToManaged"/>
    public static global::System.EventHandler? ConvertToManaged(void* value)
    {
        object? result = WindowsRuntimeDelegateMarshaller.ConvertToManaged<EventHandlerComWrappersCallback>(value);

        return Unsafe.As<global::System.EventHandler?>(result);
    }
}

/// <summary>
/// The <see cref="WindowsRuntimeObject"/> implementation for <see cref="global::System.EventHandler"/>.
/// </summary>
file static unsafe class EventHandlerNativeDelegate
{
    /// <inheritdoc cref="global::System.EventHandler"/>
    public static void Invoke(this WindowsRuntimeObjectReference objectReference, object? sender, EventArgs e)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = objectReference.AsValue();
        using WindowsRuntimeObjectReferenceValue senderValue = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(sender);
        using WindowsRuntimeObjectReferenceValue eValue = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(e);

        void* thisPtr = thisValue.GetThisPtrUnsafe();

        HRESULT hresult = ((EventHandlerVftbl*)*(void***)thisPtr)->Invoke(
            thisPtr,
            senderValue.GetThisPtrUnsafe(),
            eValue.GetThisPtrUnsafe());

        RestrictedErrorInfo.ThrowExceptionForHR(hresult);
    }
}

/// <summary>
/// A custom <see cref="IWindowsRuntimeObjectComWrappersCallback"/> implementation for <see cref="global::System.EventHandler"/>.
/// </summary>
file abstract unsafe class EventHandlerComWrappersCallback : IWindowsRuntimeObjectComWrappersCallback
{
    /// <inheritdoc/>
    public static object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
    {
        WindowsRuntimeObjectReference valueReference = WindowsRuntimeMarshal.CreateObjectReferenceUnsafe(
            externalComObject: value,
            iid: in WellKnownInterfaceIds.IID_EventHandler,
            wrapperFlags: out wrapperFlags);

        return new global::System.EventHandler(valueReference.Invoke);
    }
}

/// <summary>
/// The set of <see cref="ComInterfaceEntry"/> values for <see cref="global::System.EventHandler"/>.
/// </summary>
file struct EventHandlerInterfaceEntries
{
    public ComInterfaceEntry EventHandler;
    public ComInterfaceEntry IReferenceOfEventHandler;
    public ComInterfaceEntry IPropertyValue;
    public ComInterfaceEntry IStringable;
    public ComInterfaceEntry IWeakReferenceSource;
    public ComInterfaceEntry IMarshal;
    public ComInterfaceEntry IAgileObject;
    public ComInterfaceEntry IInspectable;
    public ComInterfaceEntry IUnknown;
}

/// <summary>
/// The implementation of <see cref="EventHandlerInterfaceEntries"/>.
/// </summary>
file static class EventHandlerInterfaceEntriesImpl
{
    /// <summary>
    /// The <see cref="EventHandlerInterfaceEntries"/> value for <see cref="global::System.EventHandler"/>.
    /// </summary>
    [FixedAddressValueType]
    public static readonly EventHandlerInterfaceEntries Entries;

    /// <summary>
    /// Initializes <see cref="Entries"/>.
    /// </summary>
    static EventHandlerInterfaceEntriesImpl()
    {
        Entries.EventHandler.IID = WellKnownInterfaceIds.IID_EventHandler;
        Entries.EventHandler.Vtable = EventHandlerImpl.Vtable;
        Entries.IReferenceOfEventHandler.IID = WellKnownInterfaceIds.IID_IReferenceOfEventHandler;
        Entries.IReferenceOfEventHandler.Vtable = EventHandlerReferenceImpl.Vtable;
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
/// A custom <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> implementation for <see cref="global::System.EventHandler"/>.
/// </summary>
file sealed unsafe class EventHandlerComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
{
    /// <inheritdoc/>
    public override void* GetOrCreateComInterfaceForObject(object value)
    {
        return (void*)WindowsRuntimeComWrappers.Default.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.TrackerSupport);
    }

    /// <inheritdoc/>
    public override ComInterfaceEntry* ComputeVtables(out int count)
    {
        count = sizeof(EventHandlerInterfaceEntries) / sizeof(ComInterfaceEntry);

        return (ComInterfaceEntry*)Unsafe.AsPointer(in EventHandlerInterfaceEntriesImpl.Entries);
    }

    // Marshalling 'EventHandler' from an opaque object should never happen. If a native method
    // returns a boxed 'EventHandler' delegate, the RCW we create will always be 'EventHandler<T>'.
    // We support marshalling to managed, but not in the opaque 'object' scenario that needs this.
}

/// <summary>
/// Binding type for the <see cref="global::System.EventHandler"/> implementation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
file unsafe struct EventHandlerVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, void*, void*, HRESULT> Invoke;
}

/// <summary>
/// The native implementation for <see cref="global::System.EventHandler"/>.
/// </summary>
file static unsafe class EventHandlerImpl
{
    /// <summary>
    /// The <see cref="EventHandlerVftbl"/> value for the <see cref="global::System.EventHandler"/> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly EventHandlerVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static EventHandlerImpl()
    {
        *(IUnknownVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IUnknownVftbl*)IUnknownImpl.Vtable;

        Vftbl.Invoke = &Invoke;
    }

    /// <summary>
    /// Gets a pointer to the <see cref="global::System.EventHandler"/> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Vftbl);
    }

    /// <inheritdoc cref="global::System.EventHandler"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT Invoke(void* thisPtr, void* sender, void* e)
    {
        try
        {
            var unboxedValue = ComInterfaceDispatch.GetInstance<global::System.EventHandler>((ComInterfaceDispatch*)thisPtr);

            unboxedValue(
                WindowsRuntimeObjectMarshaller.ConvertToManaged(sender),
                WindowsRuntimeObjectMarshaller.ConvertToManaged(e) as EventArgs ?? EventArgs.Empty);

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception ex)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(ex);
        }
    }
}

/// <summary>
/// Binding type for the <c>IReference`1</c> implementation for <see cref="global::System.EventHandler"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
file unsafe struct EventHandlerReferenceVftbl
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
/// The <c>IReference`1</c> implementation for <see cref="global::System.EventHandler"/>.
/// </summary>
file static unsafe class EventHandlerReferenceImpl
{
    /// <summary>
    /// The <see cref="EventHandlerReferenceVftbl"/> value for the managed <c>IReference`1</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly EventHandlerReferenceVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static EventHandlerReferenceImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.get_Value = &get_Value;
    }

    /// <summary>
    /// Gets a pointer to the managed <c>IReference`1</c> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Vftbl);
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
            var unboxedValue = ComInterfaceDispatch.GetInstance<global::System.EventHandler>((ComInterfaceDispatch*)thisPtr);

            *result = EventHandlerMarshaller.ConvertToUnmanaged(unboxedValue).DetachThisPtrUnsafe();

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            *result = null;

            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}
