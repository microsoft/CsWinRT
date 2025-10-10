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

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
[assembly: TypeMap<WindowsRuntimeComWrappersTypeMapGroup>(
    value: "Windows.Foundation.IReference<Windows.Foundation.AsyncActionCompletedHandler>",
    target: typeof(AsyncActionCompletedHandler),
    trimTarget: typeof(AsyncActionCompletedHandler))]
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code

namespace ABI.Windows.Foundation;

/// <summary>
/// Marshaller for <see cref="AsyncActionCompletedHandler"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class AsyncActionCompletedHandlerMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.ConvertToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(AsyncActionCompletedHandler? value)
    {
        return WindowsRuntimeDelegateMarshaller.ConvertToUnmanaged(value, in WellKnownInterfaceIds.IID_AsyncActionCompletedHandler);
    }

    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.ConvertToManaged"/>
    public static AsyncActionCompletedHandler? ConvertToManaged(void* value)
    {
        object? result = WindowsRuntimeDelegateMarshaller.ConvertToManaged<AsyncActionCompletedHandlerComWrappersCallback>(value);

        return Unsafe.As<AsyncActionCompletedHandler?>(result);
    }

    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.BoxToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue BoxToUnmanaged(AsyncActionCompletedHandler? value)
    {
        return WindowsRuntimeDelegateMarshaller.BoxToUnmanaged(value, in WellKnownInterfaceIds.IID_IReferenceOfAsyncActionCompletedHandler);
    }

    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.UnboxToManaged(void*)"/>
    public static AsyncActionCompletedHandler? UnboxToManaged(void* value)
    {
        object? result = WindowsRuntimeDelegateMarshaller.UnboxToManaged<AsyncActionCompletedHandlerComWrappersCallback>(value);

        return Unsafe.As<AsyncActionCompletedHandler?>(result);
    }
}

/// <summary>
/// The <see cref="WindowsRuntimeObject"/> implementation for <see cref="AsyncActionCompletedHandler"/>.
/// </summary>
file static unsafe class AsyncActionCompletedHandlerNativeDelegate
{
    /// <inheritdoc cref="AsyncActionCompletedHandler"/>
    public static void Invoke(this WindowsRuntimeObjectReference objectReference, IAsyncAction asyncInfo, AsyncStatus asyncStatus)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = objectReference.AsValue();
        using WindowsRuntimeObjectReferenceValue asyncInfoValue = IAsyncActionMarshaller.ConvertToUnmanaged(asyncInfo);

        void* thisPtr = thisValue.GetThisPtrUnsafe();

        HRESULT hresult = ((AsyncActionCompletedHandlerVftbl*)*(void***)thisPtr)->Invoke(
            thisPtr,
            asyncInfoValue.GetThisPtrUnsafe(),
            asyncStatus);

        RestrictedErrorInfo.ThrowExceptionForHR(hresult);
    }
}

/// <summary>
/// A custom <see cref="IWindowsRuntimeObjectComWrappersCallback"/> implementation for <see cref="AsyncActionCompletedHandler"/>.
/// </summary>
file abstract unsafe class AsyncActionCompletedHandlerComWrappersCallback : IWindowsRuntimeObjectComWrappersCallback
{
    /// <inheritdoc/>
    public static object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
    {
        WindowsRuntimeObjectReference valueReference = WindowsRuntimeComWrappersMarshal.CreateObjectReferenceUnsafe(
            externalComObject: value,
            iid: in WellKnownInterfaceIds.IID_AsyncActionCompletedHandler,
            wrapperFlags: out wrapperFlags);

        return new AsyncActionCompletedHandler(valueReference.Invoke);
    }
}

/// <summary>
/// The set of <see cref="ComInterfaceEntry"/> values for <see cref="AsyncActionCompletedHandler"/>.
/// </summary>
file struct AsyncActionCompletedHandlerInterfaceEntries
{
    public ComInterfaceEntry AsyncActionCompletedHandler;
    public ComInterfaceEntry IReferenceOfAsyncActionCompletedHandler;
    public ComInterfaceEntry IPropertyValue;
    public ComInterfaceEntry IStringable;
    public ComInterfaceEntry IWeakReferenceSource;
    public ComInterfaceEntry IMarshal;
    public ComInterfaceEntry IAgileObject;
    public ComInterfaceEntry IInspectable;
    public ComInterfaceEntry IUnknown;
}

/// <summary>
/// The implementation of <see cref="AsyncActionCompletedHandlerInterfaceEntries"/>.
/// </summary>
file static class AsyncActionCompletedHandlerInterfaceEntriesImpl
{
    /// <summary>
    /// The <see cref="AsyncActionCompletedHandlerInterfaceEntries"/> value for <see cref="AsyncActionCompletedHandler"/>.
    /// </summary>
    [FixedAddressValueType]
    public static readonly AsyncActionCompletedHandlerInterfaceEntries Entries;

    /// <summary>
    /// Initializes <see cref="Entries"/>.
    /// </summary>
    static AsyncActionCompletedHandlerInterfaceEntriesImpl()
    {
        Entries.AsyncActionCompletedHandler.IID = WellKnownInterfaceIds.IID_AsyncActionCompletedHandler;
        Entries.AsyncActionCompletedHandler.Vtable = AsyncActionCompletedHandlerImpl.Vtable;
        Entries.IReferenceOfAsyncActionCompletedHandler.IID = WellKnownInterfaceIds.IID_IReferenceOfAsyncActionCompletedHandler;
        Entries.IReferenceOfAsyncActionCompletedHandler.Vtable = AsyncActionCompletedHandlerReferenceImpl.Vtable;
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
/// A custom <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> implementation for <see cref="AsyncActionCompletedHandler"/>.
/// </summary>
internal sealed unsafe class AsyncActionCompletedHandlerComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
{
    /// <inheritdoc/>
    public override void* GetOrCreateComInterfaceForObject(object value)
    {
        return WindowsRuntimeComWrappersMarshal.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.TrackerSupport);
    }

    /// <inheritdoc/>
    public override ComInterfaceEntry* ComputeVtables(out int count)
    {
        count = sizeof(AsyncActionCompletedHandlerInterfaceEntries) / sizeof(ComInterfaceEntry);

        return (ComInterfaceEntry*)Unsafe.AsPointer(in AsyncActionCompletedHandlerInterfaceEntriesImpl.Entries);
    }

    /// <inheritdoc/>
    public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
    {
        wrapperFlags = CreatedWrapperFlags.NonWrapping;

        return WindowsRuntimeDelegateMarshaller.UnboxToManaged<AsyncActionCompletedHandlerComWrappersCallback>(value, in WellKnownInterfaceIds.IID_IReferenceOfAsyncActionCompletedHandler)!;
    }
}

/// <summary>
/// Binding type for the <see cref="AsyncActionCompletedHandler"/> implementation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
file unsafe struct AsyncActionCompletedHandlerVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, void*, AsyncStatus, HRESULT> Invoke;
}

/// <summary>
/// The native implementation for <see cref="AsyncActionCompletedHandler"/>.
/// </summary>
file static unsafe class AsyncActionCompletedHandlerImpl
{
    /// <summary>
    /// The <see cref="AsyncActionCompletedHandlerVftbl"/> value for the <see cref="AsyncActionCompletedHandler"/> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly AsyncActionCompletedHandlerVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static AsyncActionCompletedHandlerImpl()
    {
        *(IUnknownVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IUnknownVftbl*)IUnknownImpl.Vtable;

        Vftbl.Invoke = &Invoke;
    }

    /// <summary>
    /// Gets the IID for <see cref="AsyncActionCompletedHandler"/>.
    /// </summary>
    public static ref readonly Guid IID
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref WellKnownInterfaceIds.IID_AsyncActionCompletedHandler;
    }

    /// <summary>
    /// Gets a pointer to the <see cref="AsyncActionCompletedHandler"/> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Vftbl);
    }

    /// <inheritdoc cref="AsyncActionCompletedHandler"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT Invoke(void* thisPtr, void* asyncInfo, AsyncStatus asyncStatus)
    {
        if (asyncInfo is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            var unboxedValue = ComInterfaceDispatch.GetInstance<AsyncActionCompletedHandler>((ComInterfaceDispatch*)thisPtr);

            unboxedValue(IAsyncActionMarshaller.ConvertToManaged(asyncInfo)!, asyncStatus);

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception ex)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(ex);
        }
    }
}

/// <summary>
/// Binding type for the <c>IReference`1</c> implementation for <see cref="AsyncActionCompletedHandler"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
file unsafe struct AsyncActionCompletedHandlerReferenceVftbl
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
/// The <c>IReference`1</c> implementation for <see cref="AsyncActionCompletedHandler"/>.
/// </summary>
file static unsafe class AsyncActionCompletedHandlerReferenceImpl
{
    /// <summary>
    /// The <see cref="AsyncActionCompletedHandlerReferenceVftbl"/> value for the managed <c>IReference`1</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly AsyncActionCompletedHandlerReferenceVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static AsyncActionCompletedHandlerReferenceImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.get_Value = &get_Value;
    }

    /// <summary>
    /// Gets the IID for <c>IReference`1</c> of <see cref="AsyncActionCompletedHandler"/>.
    /// </summary>
    public static ref readonly Guid IID
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref WellKnownInterfaceIds.IID_IReferenceOfAsyncActionCompletedHandler;
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
            var unboxedValue = ComInterfaceDispatch.GetInstance<AsyncActionCompletedHandler>((ComInterfaceDispatch*)thisPtr);

            *result = AsyncActionCompletedHandlerMarshaller.ConvertToUnmanaged(unboxedValue).DetachThisPtrUnsafe();

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            *result = null;

            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}
