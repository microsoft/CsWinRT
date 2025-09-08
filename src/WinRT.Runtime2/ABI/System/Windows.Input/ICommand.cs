// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.Foundation;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;
using static System.Runtime.InteropServices.ComWrappers;

#pragma warning disable IDE0008, IDE1006

namespace ABI.System.Windows.Input;

/// <summary>
/// Interop methods for <see cref="global::System.Windows.Input.ICommand"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class ICommandMethods
{
    /// <summary>
    /// The <see cref="EventSource{T}"/> table for <see cref="global::System.Windows.Input.ICommand.CanExecuteChanged"/>.
    /// </summary>
    private static ConditionalWeakTable<WindowsRuntimeObject, EventHandlerEventSource> CanExecuteChangedTable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            static ConditionalWeakTable<WindowsRuntimeObject, EventHandlerEventSource> MakeCanExecuteChangedTable()
            {
                _ = Interlocked.CompareExchange(ref field, [], null);

                return Volatile.Read(in field);
            }

            return Volatile.Read(in field) ?? MakeCanExecuteChangedTable();
        }
    }

    /// <see cref="global::System.Windows.Input.ICommand.CanExecuteChanged"/>
    public static EventHandlerEventSource CanExecuteChanged(WindowsRuntimeObject thisObject, WindowsRuntimeObjectReference thisReference)
    {
        return CanExecuteChangedTable.GetOrAdd(
            key: thisObject,
            valueFactory: static (_, thisReference) => new EventHandlerEventSource(thisReference, 6),
            factoryArgument: thisReference);
    }

    /// <see cref="global::System.Windows.Input.ICommand.CanExecute"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool CanExecute(WindowsRuntimeObjectReference thisReference, object? parameter)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();
        using WindowsRuntimeObjectReferenceValue parameterValue = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(parameter);

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        bool result;

        RestrictedErrorInfo.ThrowExceptionForHR(((ICommandVftbl*)*(void***)thisPtr)->CanExecute(thisPtr, parameterValue.GetThisPtrUnsafe(), &result));

        return Unsafe.BitCast<bool, byte>(result) != 0;
    }

    /// <see cref="global::System.Windows.Input.ICommand.Execute"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Execute(WindowsRuntimeObjectReference thisReference, object? parameter)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();
        using WindowsRuntimeObjectReferenceValue parameterValue = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(parameter);

        void* thisPtr = thisValue.GetThisPtrUnsafe();

        RestrictedErrorInfo.ThrowExceptionForHR(((ICommandVftbl*)*(void***)thisPtr)->Execute(thisPtr, parameterValue.GetThisPtrUnsafe()));
    }
}

/// <summary>
/// Binding type for <see cref="global::System.Windows.Input.ICommand"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct ICommandVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, void*, EventRegistrationToken*, HRESULT> add_CanExecuteChanged;
    public delegate* unmanaged[MemberFunction]<void*, EventRegistrationToken, HRESULT> remove_CanExecuteChanged;
    public delegate* unmanaged[MemberFunction]<void*, void*, bool*, HRESULT> CanExecute;
    public delegate* unmanaged[MemberFunction]<void*, void*, HRESULT> Execute;
}

/// <summary>
/// The <see cref="global::System.Windows.Input.ICommand"/> implementation.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class ICommandImpl
{
    /// <summary>
    /// The <see cref="ICommandVftbl"/> value for the managed <see cref="global::System.Windows.Input.ICommand"/> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly ICommandVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static ICommandImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.add_CanExecuteChanged = &add_CanExecuteChanged;
        Vftbl.remove_CanExecuteChanged = &remove_CanExecuteChanged;
        Vftbl.CanExecute = &CanExecute;
        Vftbl.Execute = &Execute;
    }

    /// <summary>
    /// Gets the IID for <see cref="global::System.Windows.Input.ICommand"/>.
    /// </summary>
    public static ref readonly Guid IID => ref WellKnownInterfaceIds.IID_ICommand;

    /// <summary>
    /// Gets a pointer to the managed <see cref="global::System.Windows.Input.ICommand"/> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Vftbl);
    }

    /// <summary>
    /// The <see cref="EventRegistrationTokenTable{T}"/> table for <see cref="global::System.Windows.Input.ICommand.CanExecuteChanged"/>.
    /// </summary>
    private static ConditionalWeakTable<global::System.Windows.Input.ICommand, EventRegistrationTokenTable<EventHandler>> CanExecuteChangedTable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            static ConditionalWeakTable<global::System.Windows.Input.ICommand, EventRegistrationTokenTable<EventHandler>> MakeCanExecuteChangedTable()
            {
                _ = Interlocked.CompareExchange(ref field, [], null);

                return Volatile.Read(in field);
            }

            return Volatile.Read(in field) ?? MakeCanExecuteChangedTable();
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.input.icommand.canexecutechanged"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]

    private static HRESULT add_CanExecuteChanged(void* thisPtr, void* handler, EventRegistrationToken* token)
    {
        *token = default;

        try
        {
            var unboxedValue = ComInterfaceDispatch.GetInstance<global::System.Windows.Input.ICommand>((ComInterfaceDispatch*)thisPtr);

            EventHandler? managedHandler = EventHandlerMarshaller.ConvertToManaged(handler);

            *token = CanExecuteChangedTable.GetOrCreateValue(unboxedValue).AddEventHandler(managedHandler);

            unboxedValue.CanExecuteChanged += managedHandler;

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return e.HResult;
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.input.icommand.canexecutechanged"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]

    private static HRESULT remove_CanExecuteChanged(void* thisPtr, EventRegistrationToken token)
    {
        try
        {
            var unboxedValue = ComInterfaceDispatch.GetInstance<global::System.Windows.Input.ICommand>((ComInterfaceDispatch*)thisPtr);

            // This 'null' check on the unboxed object is intentional, and we're only do this specifically from 'remove_EventName' methods.
            // The reason is that for tracker objects (ie. in XAML scenarios), the framework will often mark objects as not rooted, and then
            // perform a cleanup before destruction, which includes also unregistering all registered event handlers. Because the reference
            // count of the registered handlers is 0 (which is valid for tracked objects), 'ComWrappers' will allow the GC to collect them,
            // and just keep the CCW alive and in a special "destroyed" state. When that happens, trying to get the original managed object
            // back will just return 'null', which is why we have this additional check here. In all other ABI methods, it's not needed.
            if (unboxedValue is not null && CanExecuteChangedTable.TryGetValue(unboxedValue, out var table) && table.RemoveEventHandler(token, out EventHandler? managedHandler))
            {
                unboxedValue.CanExecuteChanged -= managedHandler;
            }

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return e.HResult;
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.input.icommand.canexecute"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]

    private static HRESULT CanExecute(void* thisPtr, void* parameter, bool* result)
    {
        *result = false;

        try
        {
            var unboxedValue = ComInterfaceDispatch.GetInstance<global::System.Windows.Input.ICommand>((ComInterfaceDispatch*)thisPtr);

            *result = unboxedValue.CanExecute(WindowsRuntimeObjectMarshaller.ConvertToManaged(parameter));

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.input.icommand.execute"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]

    private static HRESULT Execute(void* thisPtr, void* parameter)
    {
        try
        {
            var unboxedValue = ComInterfaceDispatch.GetInstance<global::System.Windows.Input.ICommand>((ComInterfaceDispatch*)thisPtr);

            unboxedValue.Execute(WindowsRuntimeObjectMarshaller.ConvertToManaged(parameter));

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}

/// <summary>
/// The <see cref="IDynamicInterfaceCastable"/> implementation for <see cref="global::System.Windows.Input.ICommand"/>.
/// </summary>
[DynamicInterfaceCastableImplementation]
file interface ICommand : global::System.Windows.Input.ICommand
{
    /// <inheritdoc/>
    event EventHandler? global::System.Windows.Input.ICommand.CanExecuteChanged
    {
        add
        {
            var thisObject = (WindowsRuntimeObject)this;
            var thisReference = thisObject.GetObjectReferenceForInterface(typeof(global::System.Windows.Input.ICommand).TypeHandle);

            ICommandMethods.CanExecuteChanged((WindowsRuntimeObject)this, thisReference).Subscribe(value);
        }
        remove
        {
            var thisObject = (WindowsRuntimeObject)this;
            var thisReference = thisObject.GetObjectReferenceForInterface(typeof(global::System.Windows.Input.ICommand).TypeHandle);

            ICommandMethods.CanExecuteChanged(thisObject, thisReference).Unsubscribe(value);
        }
    }

    /// <inheritdoc/>
    bool global::System.Windows.Input.ICommand.CanExecute(object? parameter)
    {
        var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(global::System.Windows.Input.ICommand).TypeHandle);

        return ICommandMethods.CanExecute(thisReference, parameter);
    }

    /// <inheritdoc/>
    void global::System.Windows.Input.ICommand.Execute(object? parameter)
    {
        var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(global::System.Windows.Input.ICommand).TypeHandle);

        ICommandMethods.Execute(thisReference, parameter);
    }
}
