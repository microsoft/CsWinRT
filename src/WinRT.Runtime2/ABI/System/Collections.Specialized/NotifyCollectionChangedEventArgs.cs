// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;
using static System.Runtime.InteropServices.ComWrappers;

#pragma warning disable IDE0008, IDE0055

[assembly: TypeMap<WindowsRuntimeComWrappersTypeMapGroup>(
    value: "Windows.UI.Xaml.Interop.NotifyCollectionChangedEventArgs",
    target: typeof(ABI.System.Collections.Specialized.NotifyCollectionChangedEventArgs),
    trimTarget: typeof(NotifyCollectionChangedEventArgs))]

[assembly: TypeMap<WindowsRuntimeComWrappersTypeMapGroup>(
    value: "Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventArgs",
    target: typeof(ABI.System.Collections.Specialized.NotifyCollectionChangedEventArgs),
    trimTarget: typeof(NotifyCollectionChangedEventArgs))]

[assembly: TypeMapAssociation<WindowsRuntimeComWrappersTypeMapGroup>(
    typeof(NotifyCollectionChangedEventArgs),
    typeof(ABI.System.Collections.Specialized.NotifyCollectionChangedEventArgs))]

namespace ABI.System.Collections.Specialized;

/// <summary>
/// ABI type for <see cref="global::System.Collections.Specialized.NotifyCollectionChangedEventArgs"/>.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.notifycollectionchangedeventargs"/>
/// <seealso href="https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.interop.notifycollectionchangedeventargs"/>
[WindowsRuntimeClassName("Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventArgs")]
[NotifyCollectionChangedEventArgsComWrappersMarshaller]
file static class NotifyCollectionChangedEventArgs;

/// <summary>
/// Marshaller for <see cref="global::System.Collections.Specialized.NotifyCollectionChangedEventArgs"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class NotifyCollectionChangedEventArgsMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeObjectMarshaller.ConvertToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(global::System.Collections.Specialized.NotifyCollectionChangedEventArgs? value)
    {
        if (value is null)
        {
            return default;
        }

        void* valuePtr = NotifyCollectionChangedEventArgsRuntimeClassFactory.CreateInstance(
            value.Action,
            value.NewItems,
            value.OldItems,
            value.NewStartingIndex,
            value.OldStartingIndex);

        return new(valuePtr);
    }

    /// <inheritdoc cref="WindowsRuntimeObjectMarshaller.ConvertToManaged"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static global::System.Collections.Specialized.NotifyCollectionChangedEventArgs? ConvertToManaged(void* value)
    {
        if (value is null)
        {
            return null;
        }

        NotifyCollectionChangedAction action;

        // Get the action first. Depending on the action, we can use a different constructor for the
        // managed args. Determining this first allows us to skip marshalling some of the properties.
        HRESULT hresult = INotifyCollectionChangedEventArgsVftbl.get_ActionUnsafe(value, &action);

        RestrictedErrorInfo.ThrowExceptionForHR(hresult);

        // Helper for 'get_NewItems'
        static WindowsRuntimeObjectReferenceValue GetNewItems(void* value)
        {
            void* newItems;

            HRESULT hresult = INotifyCollectionChangedEventArgsVftbl.get_NewItemsUnsafe(value, &newItems);

            RestrictedErrorInfo.ThrowExceptionForHR(hresult);

            return new(newItems);
        }

        // Helper for 'get_OldItems'
        static WindowsRuntimeObjectReferenceValue GetOldItems(void* value)
        {
            void* oldItems;

            HRESULT hresult = INotifyCollectionChangedEventArgsVftbl.get_OldItemsUnsafe(value, &oldItems);

            RestrictedErrorInfo.ThrowExceptionForHR(hresult);

            return new(oldItems);
        }

        // Helper for 'get_NewStartingIndex'
        static int GetNewStartingIndex(void* value)
        {
            int newStartingIndex;

            HRESULT hresult = INotifyCollectionChangedEventArgsVftbl.get_NewStartingIndexUnsafe(value, &newStartingIndex);

            RestrictedErrorInfo.ThrowExceptionForHR(hresult);

            return newStartingIndex;
        }

        // Helper for 'get_OldStartingIndex'
        static int GetOldStartingIndex(void* value)
        {
            int oldStartingIndex;

            HRESULT hresult = INotifyCollectionChangedEventArgsVftbl.get_OldStartingIndexUnsafe(value, &oldStartingIndex);

            RestrictedErrorInfo.ThrowExceptionForHR(hresult);

            return oldStartingIndex;
        }

        // TODO: marshal 'IList' values correctly
        switch (action)
        {
            case NotifyCollectionChangedAction.Add:
            {
                using WindowsRuntimeObjectReferenceValue newItemsValue = GetNewItems(value);

                return new(action, null, GetNewStartingIndex(value));
            }
            case NotifyCollectionChangedAction.Remove:
            {
                using WindowsRuntimeObjectReferenceValue oldItemsValue = GetOldItems(value);

                return new(action, null, GetOldStartingIndex(value));
            }
            case NotifyCollectionChangedAction.Replace:
            {
                using WindowsRuntimeObjectReferenceValue newItemsValue = GetNewItems(value);
                using WindowsRuntimeObjectReferenceValue oldItemsValue = GetOldItems(value);

                return new(action, null, GetNewStartingIndex(value));
            }
            case NotifyCollectionChangedAction.Move:
            {
                using WindowsRuntimeObjectReferenceValue newItemsValue = GetNewItems(value);

                return new(action, null, GetNewStartingIndex(value), GetOldStartingIndex(value));
            }
            case NotifyCollectionChangedAction.Reset:
                return new(action);
            default:
                throw new ArgumentException("Invalid action.", nameof(action));
        }
    }
}

/// <summary>
/// A custom <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> implementation for <see cref="global::System.Collections.Specialized.NotifyCollectionChangedEventArgs"/>.
/// </summary>
file sealed unsafe class NotifyCollectionChangedEventArgsComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
{
    /// <inheritdoc/>
    public override void* GetOrCreateComInterfaceForObject(object value)
    {
        var args = (global::System.Collections.Specialized.NotifyCollectionChangedEventArgs)value;

        return NotifyCollectionChangedEventArgsRuntimeClassFactory.CreateInstance(
            args.Action,
            args.NewItems,
            args.OldItems,
            args.NewStartingIndex,
            args.OldStartingIndex);
    }

    /// <inheritdoc/>
    public override ComInterfaceEntry* ComputeVtables(out int count)
    {
        // All managed 'NotifyCollectionChangedEventArgs' instances are marshalled as fully native objects
        throw new UnreachableException();
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
    {
        wrapperFlags = CreatedWrapperFlags.NonWrapping;

        ref readonly Guid iid = ref WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections
            ? ref WellKnownInterfaceIds.IID_WUX_INotifyCollectionChangedEventArgs
            : ref WellKnownInterfaceIds.IID_MUX_INotifyCollectionChangedEventArgs;

        HRESULT hresult = IUnknownVftbl.QueryInterfaceUnsafe(value, in iid, out void* result);

        Marshal.ThrowExceptionForHR(hresult);

        try
        {
            return NotifyCollectionChangedEventArgsMarshaller.ConvertToManaged(value)!;
        }
        finally
        {
            WindowsRuntimeObjectMarshaller.Free(result);
        }
    }
}

/// <summary>
/// The runtime class factory for <see cref="global::System.Collections.Specialized.NotifyCollectionChangedEventArgs"/>.
/// </summary>
file static unsafe class NotifyCollectionChangedEventArgsRuntimeClassFactory
{
    /// <summary>
    /// The singleton instance for the activation factory.
    /// </summary>
    private static readonly WindowsRuntimeObjectReference NativeObject = WindowsRuntimeActivationFactory.GetActivationFactory(RuntimeClassName, in IID);

    /// <summary>
    /// Gets the IID for <see cref="NotifyCollectionChangedEventArgsRuntimeClassFactory"/>.
    /// </summary>
    private static ref readonly Guid IID => ref WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections
        ? ref WellKnownInterfaceIds.IID_WUX_INotifyCollectionChangedEventArgsFactory
        : ref WellKnownInterfaceIds.IID_MUX_INotifyCollectionChangedEventArgsFactory;

    /// <summary>
    /// Gets the runtime class name for <see cref="NotifyCollectionChangedEventArgsRuntimeClassFactory"/>.
    /// </summary>
    private static string RuntimeClassName => WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections
        ? "Windows.UI.Xaml.Interop.NotifyCollectionChangedEventArgs"
        : "Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventArgs";

    /// <summary>
    /// Creates a new native instance for <see cref="global::System.Collections.Specialized.NotifyCollectionChangedEventArgs"/>.
    /// </summary>
    /// <param name="action"><inheritdoc cref="global::System.Collections.Specialized.NotifyCollectionChangedEventArgs.Action" path="/summary/node()"/></param>
    /// <param name="newItems"><inheritdoc cref="global::System.Collections.Specialized.NotifyCollectionChangedEventArgs.NewItems" path="/summary/node()"/></param>
    /// <param name="oldItems"><inheritdoc cref="global::System.Collections.Specialized.NotifyCollectionChangedEventArgs.OldItems" path="/summary/node()"/></param>
    /// <param name="newIndex"><inheritdoc cref="global::System.Collections.Specialized.NotifyCollectionChangedEventArgs.NewStartingIndex" path="/summary/node()"/></param>
    /// <param name="oldIndex"><inheritdoc cref="global::System.Collections.Specialized.NotifyCollectionChangedEventArgs.OldStartingIndex" path="/summary/node()"/></param>
    /// <returns>The new native instance for <see cref="global::System.Collections.Specialized.NotifyCollectionChangedEventArgs"/>.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void* CreateInstance(
        NotifyCollectionChangedAction action,
        IList? newItems,
        IList? oldItems,
        int newIndex,
        int oldIndex)
    {
        WindowsRuntimeActivationHelper.ActivateInstanceUnsafe(
            activationFactoryObjectReference: NativeObject,
            param0: action,
            param1: newItems,
            param2: oldItems,
            param3: newIndex,
            param4: oldIndex,
            baseInterface: null,
            innerInterface: out void* innerInterface,
            defaultInterface: out void* defaultInterface);

        WindowsRuntimeObjectMarshaller.Free(innerInterface);

        return defaultInterface;
    }
}
