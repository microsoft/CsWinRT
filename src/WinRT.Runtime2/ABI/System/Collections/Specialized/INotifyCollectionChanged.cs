// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Specialized;
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

namespace ABI.System.Collections.Specialized;

/// <summary>
/// Marshaller for <see cref="INotifyCollectionChanged"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class INotifyCollectionChangedMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeObjectMarshaller.ConvertToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(INotifyCollectionChanged? value)
    {
        return WindowsRuntimeInterfaceMarshaller<INotifyCollectionChanged>.ConvertToUnmanaged(
            value: value,
            iid: in WellKnownXamlInterfaceIds.IID_INotifyCollectionChanged);
    }

    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.ConvertToManaged"/>
    public static INotifyCollectionChanged? ConvertToManaged(void* value)
    {
        return (INotifyCollectionChanged?)WindowsRuntimeObjectMarshaller.ConvertToManaged(value);
    }
}

/// <summary>
/// Interop methods for <see cref="INotifyCollectionChanged"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class INotifyCollectionChangedMethods
{
    /// <summary>
    /// The <see cref="EventSource{T}"/> table for <see cref="INotifyCollectionChanged.CollectionChanged"/>.
    /// </summary>
    private static ConditionalWeakTable<WindowsRuntimeObject, NotifyCollectionChangedEventSource> CollectionChangedTable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            static ConditionalWeakTable<WindowsRuntimeObject, NotifyCollectionChangedEventSource> MakeCollectionChangedTable()
            {
                _ = Interlocked.CompareExchange(ref field, [], null);

                return Volatile.Read(in field);
            }

            return Volatile.Read(in field) ?? MakeCollectionChangedTable();
        }
    }

    /// <see cref="INotifyCollectionChanged.CollectionChanged"/>
    public static NotifyCollectionChangedEventSource CollectionChanged(WindowsRuntimeObject thisObject, WindowsRuntimeObjectReference thisReference)
    {
        return CollectionChangedTable.GetOrAdd(
            key: thisObject,
            valueFactory: static (_, thisReference) => new NotifyCollectionChangedEventSource(thisReference, 6),
            factoryArgument: thisReference);
    }
}

/// <summary>
/// Binding type for <see cref="INotifyCollectionChanged"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct INotifyCollectionChangedVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, void*, EventRegistrationToken*, HRESULT> add_CollectionChanged;
    public delegate* unmanaged[MemberFunction]<void*, EventRegistrationToken, HRESULT> remove_CollectionChanged;
}

/// <summary>
/// The <see cref="INotifyCollectionChanged"/> implementation.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class INotifyCollectionChangedImpl
{
    /// <summary>
    /// The <see cref="INotifyCollectionChangedVftbl"/> value for the managed <see cref="INotifyCollectionChanged"/> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly INotifyCollectionChangedVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static INotifyCollectionChangedImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.add_CollectionChanged = &add_CollectionChanged;
        Vftbl.remove_CollectionChanged = &remove_CollectionChanged;
    }

    /// <summary>
    /// Gets a pointer to the managed <see cref="INotifyCollectionChanged"/> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Vftbl);
    }

    /// <summary>
    /// The <see cref="EventRegistrationTokenTable{T}"/> table for <see cref="INotifyCollectionChanged.CollectionChanged"/>.
    /// </summary>
    private static ConditionalWeakTable<INotifyCollectionChanged, EventRegistrationTokenTable<NotifyCollectionChangedEventHandler>> CollectionChangedTable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            static ConditionalWeakTable<INotifyCollectionChanged, EventRegistrationTokenTable<NotifyCollectionChangedEventHandler>> MakeCollectionChangedTable()
            {
                _ = Interlocked.CompareExchange(ref field, [], null);

                return Volatile.Read(in field);
            }

            return Volatile.Read(in field) ?? MakeCollectionChangedTable();
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.data.INotifyCollectionChanged.CollectionChanged"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT add_CollectionChanged(void* thisPtr, void* handler, EventRegistrationToken* token)
    {
        try
        {
            var unboxedValue = ComInterfaceDispatch.GetInstance<INotifyCollectionChanged>((ComInterfaceDispatch*)thisPtr);

            NotifyCollectionChangedEventHandler? managedHandler = NotifyCollectionChangedEventHandlerMarshaller.ConvertToManaged(handler);

            *token = CollectionChangedTable.GetOrCreateValue(unboxedValue).AddEventHandler(managedHandler);

            unboxedValue.CollectionChanged += managedHandler;

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return e.HResult;
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.data.INotifyCollectionChanged.CollectionChanged"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT remove_CollectionChanged(void* thisPtr, EventRegistrationToken token)
    {
        try
        {
            var unboxedValue = ComInterfaceDispatch.GetInstance<INotifyCollectionChanged>((ComInterfaceDispatch*)thisPtr);

            if (unboxedValue is not null && CollectionChangedTable.TryGetValue(unboxedValue, out var table) && table.RemoveEventHandler(token, out NotifyCollectionChangedEventHandler? managedHandler))
            {
                unboxedValue.CollectionChanged -= managedHandler;
            }

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return e.HResult;
        }
    }
}

/// <summary>
/// The <see cref="IDynamicInterfaceCastable"/> implementation for <see cref="INotifyCollectionChanged"/>.
/// </summary>
[DynamicInterfaceCastableImplementation]
file interface INotifyCollectionChangedInterfaceImpl : INotifyCollectionChanged
{
    /// <inheritdoc/>
    event NotifyCollectionChangedEventHandler? INotifyCollectionChanged.CollectionChanged
    {
        add
        {
            var thisObject = (WindowsRuntimeObject)this;
            var thisReference = thisObject.GetObjectReferenceForInterface(typeof(INotifyCollectionChanged).TypeHandle);

            INotifyCollectionChangedMethods.CollectionChanged((WindowsRuntimeObject)this, thisReference).Subscribe(value);
        }
        remove
        {
            var thisObject = (WindowsRuntimeObject)this;
            var thisReference = thisObject.GetObjectReferenceForInterface(typeof(INotifyCollectionChanged).TypeHandle);

            INotifyCollectionChangedMethods.CollectionChanged(thisObject, thisReference).Unsubscribe(value);
        }
    }
}
