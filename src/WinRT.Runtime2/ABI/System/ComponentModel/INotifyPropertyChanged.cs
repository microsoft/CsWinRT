// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
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
/// Marshaller for <see cref="INotifyPropertyChanged"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class INotifyPropertyChangedMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeObjectMarshaller.ConvertToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(INotifyPropertyChanged? value)
    {
        return WindowsRuntimeInterfaceMarshaller<INotifyPropertyChanged>.ConvertToUnmanaged(
            value: value,
            iid: in WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections
                ? ref WellKnownInterfaceIds.IID_WUX_INotifyPropertyChanged
                : ref WellKnownInterfaceIds.IID_MUX_INotifyPropertyChanged);
    }

    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.ConvertToManaged"/>
    public static INotifyPropertyChanged? ConvertToManaged(void* value)
    {
        return (INotifyPropertyChanged?)WindowsRuntimeObjectMarshaller.ConvertToManaged(value);
    }
}

/// <summary>
/// Interop methods for <see cref="INotifyPropertyChanged"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class INotifyPropertyChangedMethods
{
    /// <summary>
    /// The <see cref="EventSource{T}"/> table for <see cref="INotifyPropertyChanged.PropertyChanged"/>.
    /// </summary>
    private static ConditionalWeakTable<WindowsRuntimeObject, PropertyChangedEventSource> PropertyChangedTable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            static ConditionalWeakTable<WindowsRuntimeObject, PropertyChangedEventSource> MakePropertyChanged()
            {
                _ = Interlocked.CompareExchange(ref field, [], null);

                return Volatile.Read(in field);
            }

            return Volatile.Read(in field) ?? MakePropertyChanged();
        }
    }

    /// <see cref="INotifyPropertyChanged.PropertyChanged"/>
    public static PropertyChangedEventSource PropertyChanged(WindowsRuntimeObject thisObject, WindowsRuntimeObjectReference thisReference)
    {
        return PropertyChangedTable.GetOrAdd(
            key: thisObject,
            valueFactory: static (_, thisReference) => new PropertyChangedEventSource(thisReference, 6),
            factoryArgument: thisReference);
    }
}

/// <summary>
/// Binding type for <see cref="INotifyPropertyChanged"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct INotifyPropertyChangedVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, void*, EventRegistrationToken*, HRESULT> add_PropertyChanged;
    public delegate* unmanaged[MemberFunction]<void*, EventRegistrationToken, HRESULT> remove_PropertyChanged;
}

/// <summary>
/// The <see cref="INotifyPropertyChanged"/> implementation.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class INotifyPropertyChangedImpl
{
    /// <summary>
    /// The <see cref="INotifyPropertyChangedVftbl"/> value for the managed <see cref="INotifyPropertyChanged"/> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly INotifyPropertyChangedVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static INotifyPropertyChangedImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.add_PropertyChanged = &add_PropertyChanged;
        Vftbl.remove_PropertyChanged = &remove_PropertyChanged;
    }

    /// <summary>
    /// Gets the IID for <see cref="INotifyPropertyChanged"/>.
    /// </summary>
    public static ref readonly Guid IID => ref WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections
        ? ref WellKnownInterfaceIds.IID_WUX_INotifyPropertyChanged
        : ref WellKnownInterfaceIds.IID_MUX_INotifyPropertyChanged;

    /// <summary>
    /// Gets a pointer to the managed <see cref="INotifyPropertyChanged"/> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Vftbl);
    }

    /// <summary>
    /// The <see cref="EventRegistrationTokenTable{T}"/> table for <see cref="INotifyPropertyChanged.PropertyChanged"/>.
    /// </summary>
    private static ConditionalWeakTable<INotifyPropertyChanged, EventRegistrationTokenTable<PropertyChangedEventHandler>> PropertyChangedTable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            static ConditionalWeakTable<INotifyPropertyChanged, EventRegistrationTokenTable<PropertyChangedEventHandler>> MakePropertyChangedTable()
            {
                _ = Interlocked.CompareExchange(ref field, [], null);

                return Volatile.Read(in field);
            }

            return Volatile.Read(in field) ?? MakePropertyChangedTable();
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.data.inotifypropertychanged.propertychanged"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT add_PropertyChanged(void* thisPtr, void* handler, EventRegistrationToken* token)
    {
        *token = default;

        try
        {
            var unboxedValue = ComInterfaceDispatch.GetInstance<INotifyPropertyChanged>((ComInterfaceDispatch*)thisPtr);

            PropertyChangedEventHandler? managedHandler = PropertyChangedEventHandlerMarshaller.ConvertToManaged(handler);

            *token = PropertyChangedTable.GetOrCreateValue(unboxedValue).AddEventHandler(managedHandler);

            unboxedValue.PropertyChanged += managedHandler;

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return e.HResult;
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.data.inotifypropertychanged.propertychanged"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT remove_PropertyChanged(void* thisPtr, EventRegistrationToken token)
    {
        try
        {
            var unboxedValue = ComInterfaceDispatch.GetInstance<INotifyPropertyChanged>((ComInterfaceDispatch*)thisPtr);

            if (unboxedValue is not null && PropertyChangedTable.TryGetValue(unboxedValue, out var table) && table.RemoveEventHandler(token, out PropertyChangedEventHandler? managedHandler))
            {
                unboxedValue.PropertyChanged -= managedHandler;
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
/// The <see cref="IDynamicInterfaceCastable"/> implementation for <see cref="INotifyPropertyChanged"/>.
/// </summary>
[DynamicInterfaceCastableImplementation]
file interface INotifyPropertyChangedInterfaceImpl : INotifyPropertyChanged
{
    /// <inheritdoc/>
    event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
    {
        add
        {
            var thisObject = (WindowsRuntimeObject)this;
            var thisReference = thisObject.GetObjectReferenceForInterface(typeof(INotifyPropertyChanged).TypeHandle);

            INotifyPropertyChangedMethods.PropertyChanged((WindowsRuntimeObject)this, thisReference).Subscribe(value);
        }
        remove
        {
            var thisObject = (WindowsRuntimeObject)this;
            var thisReference = thisObject.GetObjectReferenceForInterface(typeof(INotifyPropertyChanged).TypeHandle);

            INotifyPropertyChangedMethods.PropertyChanged(thisObject, thisReference).Unsubscribe(value);
        }
    }
}
