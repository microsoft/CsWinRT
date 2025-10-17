// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.Foundation;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;
using static System.Runtime.InteropServices.ComWrappers;

#pragma warning disable IDE0008, IDE1006

namespace ABI.System.ComponentModel;

/// <summary>
/// Marshaller for <see cref="INotifyDataErrorInfo"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class INotifyDataErrorInfoMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeObjectMarshaller.ConvertToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(INotifyDataErrorInfo? value)
    {
        return WindowsRuntimeInterfaceMarshaller<INotifyDataErrorInfo>.ConvertToUnmanaged(
            value: value,
            iid: in WellKnownInterfaceIds.IID_INotifyDataErrorInfo);
    }

    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.ConvertToManaged"/>
    public static INotifyDataErrorInfo? ConvertToManaged(void* value)
    {
        return (INotifyDataErrorInfo?)WindowsRuntimeObjectMarshaller.ConvertToManaged(value);
    }
}

/// <summary>
/// Interop methods for <see cref="INotifyDataErrorInfo"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class INotifyDataErrorInfoMethods
{
    /// <summary>
    /// The <see cref="EventSource{T}"/> table for <see cref="INotifyDataErrorInfo.ErrorsChanged"/>.
    /// </summary>
    private static ConditionalWeakTable<WindowsRuntimeObject, EventHandlerEventSource<DataErrorsChangedEventArgs>> ErrorsChangedTable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            static ConditionalWeakTable<WindowsRuntimeObject, EventHandlerEventSource<DataErrorsChangedEventArgs>> MakeErrorsChangedTable()
            {
                _ = Interlocked.CompareExchange(ref field, [], null);

                return Volatile.Read(in field);
            }

            return Volatile.Read(in field) ?? MakeErrorsChangedTable();
        }
    }

    /// <see cref="INotifyDataErrorInfo.HasErrors"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool HasErrors(WindowsRuntimeObjectReference thisReference)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        bool result = false;

        RestrictedErrorInfo.ThrowExceptionForHR(((INotifyDataErrorInfoVftbl*)*(void***)thisPtr)->get_HasErrors(thisPtr, &result));

        return Unsafe.BitCast<bool, byte>(result) != 0;
    }

    /// <see cref="INotifyDataErrorInfo.ErrorsChanged"/>
    public static EventHandlerEventSource<DataErrorsChangedEventArgs> ErrorsChanged(WindowsRuntimeObject thisObject, WindowsRuntimeObjectReference thisReference)
    {
        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
        [return: UnsafeAccessorType("ABI.WindowsRuntime.InteropServices.<#CsWinRT>EventHandlerEventSource`1<<#corlib>System-ComponentModel-DataErrorsChangedEventArgs>, WinRT.Interop.dll")]
        static extern object ctor(WindowsRuntimeObjectReference nativeObjectReference, int index);

        return ErrorsChangedTable.GetOrAdd(
            key: thisObject,
            valueFactory: static (_, thisReference) => Unsafe.As<EventHandlerEventSource<DataErrorsChangedEventArgs>>(ctor(thisReference, 7)),
            factoryArgument: thisReference);
    }

    /// <see cref="INotifyDataErrorInfo.GetErrors"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IEnumerable GetErrors(WindowsRuntimeObjectReference thisReference, string? propertyName)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        fixed (char* propertyNamePtr = propertyName)
        {
            HStringMarshaller.ConvertToUnmanagedUnsafe(propertyNamePtr, propertyName?.Length, out HStringReference propertyNameReference);

            void* thisPtr = thisValue.GetThisPtrUnsafe();
            void* result = null;

            RestrictedErrorInfo.ThrowExceptionForHR(((INotifyDataErrorInfoVftbl*)*(void***)thisPtr)->GetErrors(thisPtr, propertyNameReference.HString, &result));

            try
            {
                [UnsafeAccessor(UnsafeAccessorKind.StaticMethod)]
                static extern IEnumerable<object>? ConvertToManaged(
                    [UnsafeAccessorType("ABI.System.Collections.Generic.<#corlib>IEnumerable`1<object>Marshaller, WinRT.Interop.dll")] object? _,
                    void* value);

                return ConvertToManaged(null, result)!;
            }
            finally
            {
                WindowsRuntimeObjectMarshaller.Free(result);
            }
        }
    }
}

/// <summary>
/// Binding type for <see cref="INotifyDataErrorInfo"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct INotifyDataErrorInfoVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, bool*, HRESULT> get_HasErrors;
    public delegate* unmanaged[MemberFunction]<void*, void*, EventRegistrationToken*, HRESULT> add_ErrorsChanged;
    public delegate* unmanaged[MemberFunction]<void*, EventRegistrationToken, HRESULT> remove_ErrorsChanged;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING, void**, HRESULT> GetErrors;
}

/// <summary>
/// The <see cref="INotifyDataErrorInfo"/> implementation.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class INotifyDataErrorInfoImpl
{
    /// <summary>
    /// The <see cref="INotifyDataErrorInfoVftbl"/> value for the managed <see cref="INotifyDataErrorInfo"/> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly INotifyDataErrorInfoVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static INotifyDataErrorInfoImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.get_HasErrors = &get_HasErrors;
        Vftbl.add_ErrorsChanged = &add_ErrorsChanged;
        Vftbl.remove_ErrorsChanged = &remove_ErrorsChanged;
        Vftbl.GetErrors = &GetErrors;
    }

    /// <summary>
    /// Gets a pointer to the managed <see cref="INotifyDataErrorInfo"/> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Vftbl);
    }

    /// <summary>
    /// The <see cref="EventRegistrationTokenTable{T}"/> table for <see cref="INotifyDataErrorInfo.ErrorsChanged"/>.
    /// </summary>
    private static ConditionalWeakTable<INotifyDataErrorInfo, EventRegistrationTokenTable<EventHandler<DataErrorsChangedEventArgs>>> ErrorsChanged
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            static ConditionalWeakTable<INotifyDataErrorInfo, EventRegistrationTokenTable<EventHandler<DataErrorsChangedEventArgs>>> MakeErrorsChanged()
            {
                _ = Interlocked.CompareExchange(ref field, [], null);

                return Volatile.Read(in field);
            }

            return Volatile.Read(in field) ?? MakeErrorsChanged();
        }
    }

    /// <see href="https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.data.inotifydataerrorinfo.haserrors"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT get_HasErrors(void* thisPtr, bool* result)
    {
        *result = false;

        try
        {
            var unboxedValue = ComInterfaceDispatch.GetInstance<INotifyDataErrorInfo>((ComInterfaceDispatch*)thisPtr);

            *result = unboxedValue.HasErrors;

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <see href="https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.data.inotifydataerrorinfo.errorschanged"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT add_ErrorsChanged(void* thisPtr, void* handler, EventRegistrationToken* token)
    {
        *token = default;

        try
        {
            var unboxedValue = ComInterfaceDispatch.GetInstance<INotifyDataErrorInfo>((ComInterfaceDispatch*)thisPtr);

            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod)]
            static extern EventHandler<DataErrorsChangedEventArgs>? ConvertToManaged(
                [UnsafeAccessorType("ABI.System.<#corlib>EventHandler`1<<#corlib>System-ComponentModel-DataErrorsChangedEventArgs>Marshaller, WinRT.Interop.dll")] object? _,
                void* value);

            EventHandler<DataErrorsChangedEventArgs>? managedHandler = ConvertToManaged(null, handler);

            *token = ErrorsChanged.GetOrCreateValue(unboxedValue).AddEventHandler(managedHandler);

            unboxedValue.ErrorsChanged += managedHandler;

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return e.HResult;
        }
    }

    /// <see href="https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.data.inotifydataerrorinfo.errorschanged"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT remove_ErrorsChanged(void* thisPtr, EventRegistrationToken token)
    {
        try
        {
            var unboxedValue = ComInterfaceDispatch.GetInstance<INotifyDataErrorInfo>((ComInterfaceDispatch*)thisPtr);

            if (unboxedValue is not null && ErrorsChanged.TryGetValue(unboxedValue, out var table) && table.RemoveEventHandler(token, out EventHandler<DataErrorsChangedEventArgs>? managedHandler))
            {
                unboxedValue.ErrorsChanged -= managedHandler;
            }

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return e.HResult;
        }
    }

    /// <see href="https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.data.inotifydataerrorinfo.geterrors"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT GetErrors(void* thisPtr, HSTRING propertyName, void** result)
    {
        *result = null;

        try
        {
            var unboxedValue = ComInterfaceDispatch.GetInstance<INotifyDataErrorInfo>((ComInterfaceDispatch*)thisPtr);

            IEnumerable managedResult = unboxedValue.GetErrors(HStringMarshaller.ConvertToManaged(propertyName));

            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod)]
            static extern WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(
                [UnsafeAccessorType("ABI.System.Collections.Generic.<#corlib>IEnumerable`1<object>Marshaller, WinRT.Interop.dll")] object? _,
                IEnumerable<object>? value);

            *result = ConvertToUnmanaged(null, (IEnumerable<object>)managedResult).DetachThisPtrUnsafe();

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}

/// <summary>
/// The <see cref="IDynamicInterfaceCastable"/> implementation for <see cref="INotifyDataErrorInfo"/>.
/// </summary>
[DynamicInterfaceCastableImplementation]
file interface INotifyDataErrorInfoInterfaceImpl : INotifyDataErrorInfo
{
    /// <inheritdoc/>
    event EventHandler<DataErrorsChangedEventArgs>? INotifyDataErrorInfo.ErrorsChanged
    {
        add
        {
            var thisObject = (WindowsRuntimeObject)this;
            var thisReference = thisObject.GetObjectReferenceForInterface(typeof(INotifyDataErrorInfo).TypeHandle);

            INotifyDataErrorInfoMethods.ErrorsChanged((WindowsRuntimeObject)this, thisReference).Subscribe(value);
        }
        remove
        {
            var thisObject = (WindowsRuntimeObject)this;
            var thisReference = thisObject.GetObjectReferenceForInterface(typeof(INotifyDataErrorInfo).TypeHandle);

            INotifyDataErrorInfoMethods.ErrorsChanged(thisObject, thisReference).Unsubscribe(value);
        }
    }

    /// <inheritdoc/>
    bool INotifyDataErrorInfo.HasErrors
    {
        get
        {
            var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(INotifyDataErrorInfo).TypeHandle);

            return INotifyDataErrorInfoMethods.HasErrors(thisReference);
        }
    }

    /// <inheritdoc/>
    IEnumerable INotifyDataErrorInfo.GetErrors(string? propertyName)
    {
        var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(INotifyDataErrorInfo).TypeHandle);

        return INotifyDataErrorInfoMethods.GetErrors(thisReference, propertyName);
    }
}
