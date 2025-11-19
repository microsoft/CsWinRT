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

namespace ABI.System.ComponentModel;

/// <summary>
/// Marshaller for <see cref="PropertyChangedEventHandler"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class PropertyChangedEventHandlerMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.ConvertToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(PropertyChangedEventHandler? value)
    {
        return WindowsRuntimeDelegateMarshaller.ConvertToUnmanaged(value, in WellKnownXamlInterfaceIIDs.IID_PropertyChangedEventHandler);
    }

    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.ConvertToManaged"/>
    public static PropertyChangedEventHandler? ConvertToManaged(void* value)
    {
        return (PropertyChangedEventHandler?)WindowsRuntimeDelegateMarshaller.ConvertToManaged<PropertyChangedEventHandlerComWrappersCallback>(value);
    }

    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.BoxToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue BoxToUnmanaged(PropertyChangedEventHandler? value)
    {
        return WindowsRuntimeDelegateMarshaller.BoxToUnmanaged(value, in WellKnownXamlInterfaceIIDs.IID_IReferenceOfPropertyChangedEventHandler);
    }

    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.UnboxToManaged(void*)"/>
    public static PropertyChangedEventHandler? UnboxToManaged(void* value)
    {
        return (PropertyChangedEventHandler?)WindowsRuntimeDelegateMarshaller.UnboxToManaged<PropertyChangedEventHandlerComWrappersCallback>(value);
    }
}

/// <summary>
/// The <see cref="WindowsRuntimeObject"/> implementation for <see cref="PropertyChangedEventHandler"/>.
/// </summary>
file static unsafe class PropertyChangedEventHandlerNativeDelegate
{
    /// <inheritdoc cref="PropertyChangedEventHandler"/>
    public static void Invoke(this WindowsRuntimeObjectReference objectReference, object? sender, global::System.ComponentModel.PropertyChangedEventArgs e)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = objectReference.AsValue();
        using WindowsRuntimeObjectReferenceValue senderValue = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(sender);
        using WindowsRuntimeObjectReferenceValue eValue = PropertyChangedEventArgsMarshaller.ConvertToUnmanaged(e);

        void* thisPtr = thisValue.GetThisPtrUnsafe();

        HRESULT hresult = ((PropertyChangedEventHandlerVftbl*)*(void***)thisPtr)->Invoke(
            thisPtr,
            senderValue.GetThisPtrUnsafe(),
            eValue.GetThisPtrUnsafe());

        RestrictedErrorInfo.ThrowExceptionForHR(hresult);
    }
}

/// <summary>
/// A custom <see cref="IWindowsRuntimeObjectComWrappersCallback"/> implementation for <see cref="PropertyChangedEventHandler"/>.
/// </summary>
file abstract unsafe class PropertyChangedEventHandlerComWrappersCallback : IWindowsRuntimeObjectComWrappersCallback
{
    /// <inheritdoc/>
    public static object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
    {
        WindowsRuntimeObjectReference valueReference = WindowsRuntimeComWrappersMarshal.CreateObjectReferenceUnsafe(
            externalComObject: value,
            iid: in WellKnownXamlInterfaceIIDs.IID_PropertyChangedEventHandler,
            wrapperFlags: out wrapperFlags);

        return new PropertyChangedEventHandler(valueReference.Invoke);
    }
}

/// <summary>
/// The set of <see cref="ComInterfaceEntry"/> values for <see cref="global::System.ComponentModel.PropertyChangedEventHandler"/>.
/// </summary>
file struct PropertyChangedEventHandlerInterfaceEntries
{
    public ComInterfaceEntry PropertyChangedEventHandler;
    public ComInterfaceEntry IReferenceOfPropertyChangedEventHandler;
    public ComInterfaceEntry IPropertyValue;
    public ComInterfaceEntry IStringable;
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
    /// The <see cref="PropertyChangedEventHandlerInterfaceEntries"/> value for <see cref="PropertyChangedEventHandler"/>.
    /// </summary>
    [FixedAddressValueType]
    public static readonly PropertyChangedEventHandlerInterfaceEntries Entries;

    /// <summary>
    /// Initializes <see cref="Entries"/>.
    /// </summary>
    static PropertyChangedEventHandlerInterfaceEntriesImpl()
    {
        Entries.PropertyChangedEventHandler.IID = WellKnownXamlInterfaceIIDs.IID_PropertyChangedEventHandler;
        Entries.PropertyChangedEventHandler.Vtable = PropertyChangedEventHandlerImpl.Vtable;
        Entries.IReferenceOfPropertyChangedEventHandler.IID = WellKnownXamlInterfaceIIDs.IID_IReferenceOfPropertyChangedEventHandler;
        Entries.IReferenceOfPropertyChangedEventHandler.Vtable = PropertyChangedEventHandlerReferenceImpl.Vtable;
        Entries.IPropertyValue.IID = WellKnownWindowsInterfaceIIDs.IID_IPropertyValue;
        Entries.IPropertyValue.Vtable = IPropertyValueImpl.OtherTypeVtable;
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
/// A custom <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> implementation for <see cref="PropertyChangedEventHandler"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed unsafe class PropertyChangedEventHandlerComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
{
    /// <inheritdoc/>
    public override void* GetOrCreateComInterfaceForObject(object value)
    {
        return WindowsRuntimeComWrappersMarshal.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.TrackerSupport);
    }

    /// <inheritdoc/>
    public override ComInterfaceEntry* ComputeVtables(out int count)
    {
        count = sizeof(PropertyChangedEventHandlerInterfaceEntries) / sizeof(ComInterfaceEntry);

        return (ComInterfaceEntry*)Unsafe.AsPointer(in PropertyChangedEventHandlerInterfaceEntriesImpl.Entries);
    }

    /// <inheritdoc/>
    public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
    {
        wrapperFlags = CreatedWrapperFlags.NonWrapping;

        return WindowsRuntimeDelegateMarshaller.UnboxToManaged<PropertyChangedEventHandlerComWrappersCallback>(value, in WellKnownXamlInterfaceIIDs.IID_IReferenceOfPropertyChangedEventHandler)!;
    }
}

/// <summary>
/// Binding type for the <see cref="PropertyChangedEventHandler"/> implementation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
file unsafe struct PropertyChangedEventHandlerVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, void*, void*, HRESULT> Invoke;
}

/// <summary>
/// The native implementation for <see cref="PropertyChangedEventHandler"/>.
/// </summary>
file static unsafe class PropertyChangedEventHandlerImpl
{
    /// <summary>
    /// The <see cref="PropertyChangedEventHandlerVftbl"/> value for the <see cref="PropertyChangedEventHandler"/> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly PropertyChangedEventHandlerVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static PropertyChangedEventHandlerImpl()
    {
        *(IUnknownVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IUnknownVftbl*)IUnknownImpl.Vtable;

        Vftbl.Invoke = &Invoke;
    }

    /// <summary>
    /// Gets a pointer to the <see cref="PropertyChangedEventHandler"/> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Vftbl);
    }

    /// <inheritdoc cref="PropertyChangedEventHandler"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT Invoke(void* thisPtr, void* sender, void* e)
    {
        try
        {
            var unboxedValue = ComInterfaceDispatch.GetInstance<PropertyChangedEventHandler>((ComInterfaceDispatch*)thisPtr);

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
/// Binding type for the <c>IReference`1</c> implementation for <see cref="PropertyChangedEventHandler"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
file unsafe struct PropertyChangedEventHandlerReferenceVftbl
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
/// The <c>IReference`1</c> implementation for <see cref="PropertyChangedEventHandler"/>.
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
            var unboxedValue = ComInterfaceDispatch.GetInstance<PropertyChangedEventHandler>((ComInterfaceDispatch*)thisPtr);

            *result = PropertyChangedEventHandlerMarshaller.ConvertToUnmanaged(unboxedValue).DetachThisPtrUnsafe();

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            *result = null;

            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}