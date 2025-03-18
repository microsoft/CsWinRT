// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Buffers;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;
using static System.Runtime.InteropServices.ComWrappers;

#pragma warning disable IDE0008

[assembly: TypeMap<WindowsRuntimeTypeMapUniverse>(
    value: "Windows.Foundation.IReference<Windows.UI.Xaml.Data.PropertyChangedEventHandler>",
    target: typeof(ABI.System.ComponentModel.PropertyChangedEventHandler),
    trimTarget: typeof(PropertyChangedEventHandler))]

[assembly: TypeMap<WindowsRuntimeTypeMapUniverse>(
    value: "Windows.Foundation.IReference<Microsoft.UI.Xaml.Data.PropertyChangedEventHandler>",
    target: typeof(ABI.System.ComponentModel.PropertyChangedEventHandler),
    trimTarget: typeof(PropertyChangedEventHandler))]

[assembly: TypeMapAssociation<WindowsRuntimeTypeMapUniverse>(
    typeof(PropertyChangedEventHandler),
    typeof(ABI.System.ComponentModel.PropertyChangedEventHandler))]

namespace ABI.System.ComponentModel;

/// <summary>
/// ABI type for <see cref="global::System.ComponentModel.PropertyChangedEventHandler"/>.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.data.propertychangedeventhandler"/>
/// <seealso href="https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.data.propertychangedeventhandler"/>
[WindowsRuntimeClassName("Windows.Foundation.IReference<Microsoft.UI.Xaml.Data.PropertyChangedEventHandler>")]
[PropertyChangedEventHandlerObjectMarshaller]
[PropertyChangedEventHandlerVtableProvider]
file static class PropertyChangedEventHandler;

/// <summary>
/// Marshaller for <see cref="global::System.ComponentModel.PropertyChangedEventHandler"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class PropertyChangedEventHandlerMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.ConvertToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(global::System.ComponentModel.PropertyChangedEventHandler? value)
    {
        return WindowsRuntimeDelegateMarshaller.ConvertToUnmanaged(value, in PropertyChangedEventHandlerImpl.IID);
    }

    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.ConvertToManaged"/>
    public static global::System.ComponentModel.PropertyChangedEventHandler? ConvertToManaged(void* value)
    {
        object? result = WindowsRuntimeDelegateMarshaller.ConvertToManaged<PropertyChangedEventHandlerComWrappersCallback>(value);

        return Unsafe.As<global::System.ComponentModel.PropertyChangedEventHandler?>(result);
    }

    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.BoxToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue BoxToUnmanaged(global::System.ComponentModel.PropertyChangedEventHandler? value)
    {
        return WindowsRuntimeDelegateMarshaller.BoxToUnmanaged(value, in PropertyChangedEventHandlerReference.IID);
    }

    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.UnboxToManaged(void*)"/>
    public static global::System.ComponentModel.PropertyChangedEventHandler? UnboxToManaged(void* value)
    {
        object? result = WindowsRuntimeDelegateMarshaller.UnboxToManaged<PropertyChangedEventHandlerComWrappersCallback>(value);

        return Unsafe.As<global::System.ComponentModel.PropertyChangedEventHandler?>(result);
    }
}

/// <summary>
/// The <see cref="WindowsRuntimeObject"/> implementation for <see cref="global::System.ComponentModel.PropertyChangedEventHandler"/>.
/// </summary>
file static unsafe class PropertyChangedEventHandlerNativeDelegate
{
    /// <inheritdoc cref="global::System.ComponentModel.PropertyChangedEventHandler"/>
    public static void Invoke(this WindowsRuntimeObjectReference objectReference, object? sender, PropertyChangedEventArgs e)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = objectReference.AsValue();
        using WindowsRuntimeObjectReferenceValue senderValue = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(sender);
        using WindowsRuntimeObjectReferenceValue eValue = default; // TODO

        HRESULT hresult = ((delegate* unmanaged[MemberFunction]<void*, void*, void*, HRESULT>)(*(void***)thisValue.GetThisPtrUnsafe())[3])(
            thisValue.GetThisPtrUnsafe(),
            senderValue.GetThisPtrUnsafe(),
            eValue.GetThisPtrUnsafe());

        RestrictedErrorInfo.ThrowExceptionForHR(hresult);
    }
}

/// <summary>
/// A custom <see cref="WindowsRuntimeDelegateMarshallerAttribute"/> implementation for <see cref="global::System.ComponentModel.PropertyChangedEventHandler"/>.
/// </summary>
file abstract unsafe class PropertyChangedEventHandlerComWrappersCallback : IWindowsRuntimeComWrappersCallback
{
    /// <inheritdoc/>
    public static object CreateObject(void* value)
    {
        WindowsRuntimeObjectReference valueReference = WindowsRuntimeObjectReference.CreateUnsafe(value, in PropertyChangedEventHandlerImpl.IID)!;

        return new global::System.ComponentModel.PropertyChangedEventHandler(valueReference.Invoke);
    }
}

/// <summary>
/// A custom <see cref="WindowsRuntimeObjectMarshallerAttribute"/> implementation for <see cref="global::System.ComponentModel.PropertyChangedEventHandler"/>.
/// </summary>
file sealed unsafe class PropertyChangedEventHandlerObjectMarshallerAttribute : WindowsRuntimeObjectMarshallerAttribute
{
    /// <inheritdoc/>
    public override WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(object value)
    {
        return WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(value);
    }

    /// <inheritdoc/>
    public override object ConvertToManaged(void* value)
    {
        return WindowsRuntimeDelegateMarshaller.UnboxToManaged<PropertyChangedEventHandlerComWrappersCallback>(value, in PropertyChangedEventHandlerReference.IID)!;
    }
}

/// <summary>
/// A custom <see cref="WindowsRuntimeVtableProviderAttribute"/> implementation for <see cref="global::System.ComponentModel.PropertyChangedEventHandler"/>.
/// </summary>
file sealed class PropertyChangedEventHandlerVtableProviderAttribute : WindowsRuntimeVtableProviderAttribute
{
    /// <inheritdoc/>
    public override void ComputeVtables(IBufferWriter<ComInterfaceEntry> bufferWriter)
    {
        bufferWriter.Write(
        [
            new ComInterfaceEntry
            {
                IID = PropertyChangedEventHandlerImpl.IID,
                Vtable = PropertyChangedEventHandlerImpl.AbiToProjectionVftablePtr
            },
            new ComInterfaceEntry
            {
                IID = PropertyChangedEventHandlerReference.IID,
                Vtable = PropertyChangedEventHandlerReference.AbiToProjectionVftablePtr
            }
        ]);
    }
}

/// <summary>
/// The native implementation for <see cref="global::System.ComponentModel.PropertyChangedEventHandler"/>.
/// </summary>
file static unsafe class PropertyChangedEventHandlerImpl
{
    /// <summary>
    /// Gets the IID for <see cref="global::System.ComponentModel.PropertyChangedEventHandler"/>.
    /// </summary>
    public static ref readonly Guid IID => ref WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections
        ? ref WellKnownInterfaceIds.IID_WUX_PropertyChangedEventHandler
        : ref WellKnownInterfaceIds.IID_MUX_PropertyChangedEventHandler;

    /// <summary>
    /// The vtable for the <see cref="global::System.ComponentModel.PropertyChangedEventHandler"/> implementation.
    /// </summary>
    public static nint AbiToProjectionVftablePtr { get; } = (nint)WindowsRuntimeHelpers.AllocateTypeAssociatedUnknownVtableUnsafe(
        type: typeof(global::System.ComponentModel.PropertyChangedEventHandler),
        fpEntry3: (delegate* unmanaged[MemberFunction]<void*, void*, void*, HRESULT>)&Invoke);

    /// <inheritdoc cref="global::System.ComponentModel.PropertyChangedEventHandler"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT Invoke(void* thisPtr, void* sender, void* e)
    {
        try
        {
            var unboxedValue = (global::System.ComponentModel.PropertyChangedEventHandler)ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            unboxedValue(
                WindowsRuntimeObjectMarshaller.ConvertToManaged(sender),
                WindowsRuntimeObjectMarshaller.ConvertToManaged<PropertyChangedEventArgs>(e)!);

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception ex)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(ex);
        }
    }
}

/// <summary>
/// The <c>IReference`1</c> implementation for <see cref="global::System.ComponentModel.PropertyChangedEventHandler"/>.
/// </summary>
file static unsafe class PropertyChangedEventHandlerReference
{
    /// <summary>
    /// Gets the IID for The IID for <c>IReference`1</c> of <see cref="global::System.ComponentModel.PropertyChangedEventHandler"/>.
    /// </summary>
    public static ref readonly Guid IID => ref WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections
        ? ref WellKnownInterfaceIds.IID_WUX_IReferenceOfPropertyChangedEventHandler
        : ref WellKnownInterfaceIds.IID_MUX_IReferenceOfPropertyChangedEventHandler;

    /// <summary>
    /// The vtable for the <c>IReference`1</c> implementation.
    /// </summary>
    public static nint AbiToProjectionVftablePtr { get; } = (nint)WindowsRuntimeHelpers.AllocateTypeAssociatedInspectableVtableUnsafe(
        type: typeof(global::System.ComponentModel.PropertyChangedEventHandler),
        fpEntry6: (delegate* unmanaged[MemberFunction]<void*, void**, HRESULT>)&Value);

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ireference-1.value"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT Value(void* thisPtr, void** result)
    {
        if (result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            var unboxedValue = (global::System.ComponentModel.PropertyChangedEventHandler)ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            *result = PropertyChangedEventHandlerMarshaller.ConvertToUnmanaged(unboxedValue).GetThisPtrUnsafe();

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            *result = null;

            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}
