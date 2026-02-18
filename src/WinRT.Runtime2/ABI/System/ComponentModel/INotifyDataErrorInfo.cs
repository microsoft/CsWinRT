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

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
[assembly: TypeMap<WindowsRuntimeMetadataTypeMapGroup>(
    value: "Microsoft.UI.Xaml.Data.INotifyDataErrorInfo",
    target: typeof(ABI.System.ComponentModel.INotifyDataErrorInfo),
    trimTarget: typeof(INotifyDataErrorInfo))]
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code

[assembly: TypeMapAssociation<WindowsRuntimeMetadataTypeMapGroup>(
    source: typeof(INotifyDataErrorInfo),
    proxy: typeof(ABI.System.ComponentModel.INotifyDataErrorInfo))]

[assembly: TypeMapAssociation<DynamicInterfaceCastableImplementationTypeMapGroup>(
    source: typeof(INotifyDataErrorInfo),
    proxy: typeof(ABI.System.ComponentModel.INotifyDataErrorInfoInterfaceImpl))]

namespace ABI.System.ComponentModel;

/// <summary>
/// ABI type for <see cref="global::System.ComponentModel.INotifyDataErrorInfo"/>.
/// </summary>
[WindowsRuntimeMappedMetadata("Microsoft.UI.Xaml.WinUIContract")]
[WindowsRuntimeMetadataTypeName("Microsoft.UI.Xaml.Data.INotifyDataErrorInfo")]
[WindowsRuntimeMappedType(typeof(global::System.ComponentModel.INotifyDataErrorInfo))]
file static class INotifyDataErrorInfo;

/// <summary>
/// Marshaller for <see cref="global::System.ComponentModel.INotifyDataErrorInfo"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class INotifyDataErrorInfoMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeObjectMarshaller.ConvertToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(global::System.ComponentModel.INotifyDataErrorInfo? value)
    {
        return WindowsRuntimeInterfaceMarshaller<global::System.ComponentModel.INotifyDataErrorInfo>.ConvertToUnmanaged(
            value: value,
            iid: in WellKnownWindowsInterfaceIIDs.IID_INotifyDataErrorInfo);
    }

    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.ConvertToManaged"/>
    public static global::System.ComponentModel.INotifyDataErrorInfo? ConvertToManaged(void* value)
    {
        return (global::System.ComponentModel.INotifyDataErrorInfo?)WindowsRuntimeObjectMarshaller.ConvertToManaged(value);
    }
}

/// <summary>
/// Interop methods for <see cref="global::System.ComponentModel.INotifyDataErrorInfo"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class INotifyDataErrorInfoMethods
{
    /// <summary>
    /// The <see cref="EventSource{T}"/> table for <see cref="global::System.ComponentModel.INotifyDataErrorInfo.ErrorsChanged"/>.
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

    /// <see cref="global::System.ComponentModel.INotifyDataErrorInfo.HasErrors"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool HasErrors(WindowsRuntimeObjectReference thisReference)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        bool result = false;

        RestrictedErrorInfo.ThrowExceptionForHR(((INotifyDataErrorInfoVftbl*)*(void***)thisPtr)->get_HasErrors(thisPtr, &result));

        return Unsafe.BitCast<bool, byte>(result) != 0;
    }

    /// <see cref="global::System.ComponentModel.INotifyDataErrorInfo.ErrorsChanged"/>
    public static EventHandlerEventSource<DataErrorsChangedEventArgs> ErrorsChanged(WindowsRuntimeObject thisObject, WindowsRuntimeObjectReference thisReference)
    {
        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
        [return: UnsafeAccessorType("ABI.WindowsRuntime.InteropServices.<#CsWinRT>EventHandlerEventSource'1<<System-ObjectModel>System-ComponentModel-DataErrorsChangedEventArgs>, WinRT.Interop")]
        static extern object ctor(WindowsRuntimeObjectReference nativeObjectReference, int index);

        return ErrorsChangedTable.GetOrAdd(
            key: thisObject,
            valueFactory: static (_, thisReference) => Unsafe.As<EventHandlerEventSource<DataErrorsChangedEventArgs>>(ctor(thisReference, 7)),
            factoryArgument: thisReference);
    }

    /// <see cref="global::System.ComponentModel.INotifyDataErrorInfo.GetErrors"/>
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
                [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = nameof(ConvertToManaged))]
                static extern IEnumerable<object>? ConvertToManaged(
                    [UnsafeAccessorType("ABI.System.Collections.Generic.<#corlib>IEnumerable'1<object>Marshaller, WinRT.Interop")] object? _,
                    void* value);

                return ConvertToManaged(null, result)!;
            }
            finally
            {
                WindowsRuntimeUnknownMarshaller.Free(result);
            }
        }
    }
}

/// <summary>
/// Binding type for <see cref="global::System.ComponentModel.INotifyDataErrorInfo"/>.
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
/// The <see cref="global::System.ComponentModel.INotifyDataErrorInfo"/> implementation.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class INotifyDataErrorInfoImpl
{
    /// <summary>
    /// The <see cref="INotifyDataErrorInfoVftbl"/> value for the managed <see cref="global::System.ComponentModel.INotifyDataErrorInfo"/> implementation.
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
    /// Gets a pointer to the managed <see cref="global::System.ComponentModel.INotifyDataErrorInfo"/> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Vftbl);
    }

    /// <summary>
    /// The <see cref="EventRegistrationTokenTable{T}"/> table for <see cref="global::System.ComponentModel.INotifyDataErrorInfo.ErrorsChanged"/>.
    /// </summary>
    private static ConditionalWeakTable<global::System.ComponentModel.INotifyDataErrorInfo, EventRegistrationTokenTable<EventHandler<DataErrorsChangedEventArgs>>> ErrorsChanged
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            static ConditionalWeakTable<global::System.ComponentModel.INotifyDataErrorInfo, EventRegistrationTokenTable<EventHandler<DataErrorsChangedEventArgs>>> MakeErrorsChanged()
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
            var thisObject = ComInterfaceDispatch.GetInstance<global::System.ComponentModel.INotifyDataErrorInfo>((ComInterfaceDispatch*)thisPtr);

            *result = thisObject.HasErrors;

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
            var thisObject = ComInterfaceDispatch.GetInstance<global::System.ComponentModel.INotifyDataErrorInfo>((ComInterfaceDispatch*)thisPtr);

            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = nameof(ConvertToManaged))]
            static extern EventHandler<DataErrorsChangedEventArgs>? ConvertToManaged(
                [UnsafeAccessorType("ABI.System.<#corlib>EventHandler'1<<System-ObjectModel>System-ComponentModel-DataErrorsChangedEventArgs>Marshaller, WinRT.Interop")] object? _,
                void* value);

            EventHandler<DataErrorsChangedEventArgs>? managedHandler = ConvertToManaged(null, handler);

            *token = ErrorsChanged.GetOrCreateValue(thisObject).AddEventHandler(managedHandler);

            thisObject.ErrorsChanged += managedHandler;

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
            var thisObject = ComInterfaceDispatch.GetInstance<global::System.ComponentModel.INotifyDataErrorInfo>((ComInterfaceDispatch*)thisPtr);

            if (thisObject is not null && ErrorsChanged.TryGetValue(thisObject, out var table) && table.RemoveEventHandler(token, out EventHandler<DataErrorsChangedEventArgs>? managedHandler))
            {
                thisObject.ErrorsChanged -= managedHandler;
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
            var thisObject = ComInterfaceDispatch.GetInstance<global::System.ComponentModel.INotifyDataErrorInfo>((ComInterfaceDispatch*)thisPtr);

            IEnumerable managedResult = thisObject.GetErrors(HStringMarshaller.ConvertToManaged(propertyName));

            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = nameof(ConvertToUnmanaged))]
            static extern WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(
                [UnsafeAccessorType("ABI.System.Collections.Generic.<#corlib>IEnumerable'1<object>Marshaller, WinRT.Interop")] object? _,
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
/// The <see cref="IDynamicInterfaceCastable"/> implementation for <see cref="global::System.ComponentModel.INotifyDataErrorInfo"/>.
/// </summary>
[DynamicInterfaceCastableImplementation]
[Guid("0EE6C2CC-273E-567D-BC0A-1DD87EE51EBA")]
file interface INotifyDataErrorInfoInterfaceImpl : global::System.ComponentModel.INotifyDataErrorInfo
{
    /// <inheritdoc/>
    event EventHandler<DataErrorsChangedEventArgs>? global::System.ComponentModel.INotifyDataErrorInfo.ErrorsChanged
    {
        add
        {
            var thisObject = (WindowsRuntimeObject)this;
            var thisReference = thisObject.GetObjectReferenceForInterface(typeof(global::System.ComponentModel.INotifyDataErrorInfo).TypeHandle);

            INotifyDataErrorInfoMethods.ErrorsChanged((WindowsRuntimeObject)this, thisReference).Subscribe(value);
        }
        remove
        {
            var thisObject = (WindowsRuntimeObject)this;
            var thisReference = thisObject.GetObjectReferenceForInterface(typeof(global::System.ComponentModel.INotifyDataErrorInfo).TypeHandle);

            INotifyDataErrorInfoMethods.ErrorsChanged(thisObject, thisReference).Unsubscribe(value);
        }
    }

    /// <inheritdoc/>
    bool global::System.ComponentModel.INotifyDataErrorInfo.HasErrors
    {
        get
        {
            var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(global::System.ComponentModel.INotifyDataErrorInfo).TypeHandle);

            return INotifyDataErrorInfoMethods.HasErrors(thisReference);
        }
    }

    /// <inheritdoc/>
    IEnumerable global::System.ComponentModel.INotifyDataErrorInfo.GetErrors(string? propertyName)
    {
        var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(global::System.ComponentModel.INotifyDataErrorInfo).TypeHandle);

        return INotifyDataErrorInfoMethods.GetErrors(thisReference, propertyName);
    }
}