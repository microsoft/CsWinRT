// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;

#pragma warning disable IDE0008, IDE0055

namespace ABI.System.Collections.Specialized;

/// <summary>
/// Marshaller for <see cref="NotifyCollectionChangedEventArgs"/>.
/// </summary>
public static unsafe class NotifyCollectionChangedEventArgsMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeObjectMarshaller.ConvertToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(NotifyCollectionChangedEventArgs? value)
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
    public static NotifyCollectionChangedEventArgs? ConvertToManaged(void* value)
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
        static global::System.Collections.IList? GetNewItems(void* value)
        {
            void* newItems;

            HRESULT hresult = INotifyCollectionChangedEventArgsVftbl.get_NewItemsUnsafe(value, &newItems);

            RestrictedErrorInfo.ThrowExceptionForHR(hresult);

            try
            {
                return IListMarshaller.ConvertToManaged(newItems);
            }
            finally
            {
                WindowsRuntimeUnknownMarshaller.Free(newItems);
            }
        }

        // Helper for 'get_OldItems'
        static global::System.Collections.IList? GetOldItems(void* value)
        {
            void* oldItems;

            HRESULT hresult = INotifyCollectionChangedEventArgsVftbl.get_OldItemsUnsafe(value, &oldItems);

            RestrictedErrorInfo.ThrowExceptionForHR(hresult);

            try
            {
                return IListMarshaller.ConvertToManaged(oldItems);
            }
            finally
            {
                WindowsRuntimeUnknownMarshaller.Free(oldItems);
            }
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
                return new(action, GetNewItems(value), GetNewStartingIndex(value));
            }
            case NotifyCollectionChangedAction.Remove:
            {
                return new(action, GetOldItems(value), GetOldStartingIndex(value));
            }
            case NotifyCollectionChangedAction.Replace:
            {
                // The 'NotifyCollectionChangedEventArgs' constructor will perform 'null' checks for us
                return new(action, GetNewItems(value)!, GetOldItems(value)!, GetNewStartingIndex(value));
            }
            case NotifyCollectionChangedAction.Move:
            {
                return new(action, GetNewItems(value), GetNewStartingIndex(value), GetOldStartingIndex(value));
            }
            case NotifyCollectionChangedAction.Reset:
                return new(action);
            default:
                throw new ArgumentException("Invalid action.", nameof(action));
        }
    }
}

/// <summary>
/// A custom <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> implementation for <see cref="NotifyCollectionChangedEventArgs"/>.
/// </summary>
public sealed unsafe class NotifyCollectionChangedEventArgsComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
{
    /// <inheritdoc/>
    public override void* GetOrCreateComInterfaceForObject(object value)
    {
        var args = (NotifyCollectionChangedEventArgs)value;

        return NotifyCollectionChangedEventArgsRuntimeClassFactory.CreateInstance(
            args.Action,
            args.NewItems,
            args.OldItems,
            args.NewStartingIndex,
            args.OldStartingIndex);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
    {
        wrapperFlags = CreatedWrapperFlags.NonWrapping;

        IUnknownVftbl.QueryInterfaceUnsafe(
            thisPtr: value,
            iid: in WellKnownXamlInterfaceIIDs.IID_NotifyCollectionChangedEventArgs,
            pvObject: out void* result).Assert();

        try
        {
            return NotifyCollectionChangedEventArgsMarshaller.ConvertToManaged(result)!;
        }
        finally
        {
            WindowsRuntimeUnknownMarshaller.Free(result);
        }
    }
}

/// <summary>
/// The runtime class factory for <see cref="NotifyCollectionChangedEventArgs"/>.
/// </summary>
file static unsafe class NotifyCollectionChangedEventArgsRuntimeClassFactory
{
    /// <summary>
    /// The singleton instance for the activation factory.
    /// </summary>
    private static readonly WindowsRuntimeObjectReference NativeObject = WindowsRuntimeObjectReference.GetActivationFactory(
        runtimeClassName: WellKnownXamlRuntimeClassNames.NotifyCollectionChangedEventArgs,
        iid: in WellKnownXamlInterfaceIIDs.IID_INotifyCollectionChangedEventArgsFactory);

    /// <summary>
    /// Creates a new native instance for <see cref="NotifyCollectionChangedEventArgs"/>.
    /// </summary>
    /// <param name="action"><inheritdoc cref="NotifyCollectionChangedEventArgs.Action" path="/summary/node()"/></param>
    /// <param name="newItems"><inheritdoc cref="NotifyCollectionChangedEventArgs.NewItems" path="/summary/node()"/></param>
    /// <param name="oldItems"><inheritdoc cref="NotifyCollectionChangedEventArgs.OldItems" path="/summary/node()"/></param>
    /// <param name="newIndex"><inheritdoc cref="NotifyCollectionChangedEventArgs.NewStartingIndex" path="/summary/node()"/></param>
    /// <param name="oldIndex"><inheritdoc cref="NotifyCollectionChangedEventArgs.OldStartingIndex" path="/summary/node()"/></param>
    /// <returns>The new native instance for <see cref="NotifyCollectionChangedEventArgs"/>.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void* CreateInstance(
        NotifyCollectionChangedAction action,
        global::System.Collections.IList? newItems,
        global::System.Collections.IList? oldItems,
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

        WindowsRuntimeUnknownMarshaller.Free(innerInterface);

        return defaultInterface;
    }
}