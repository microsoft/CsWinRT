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
[EditorBrowsable(EditorBrowsableState.Never)]
[WindowsRuntimeClassName("Windows.Foundation.IReference<Microsoft.UI.Xaml.Data.PropertyChangedEventHandler>")]
[PropertyChangedEventHandlerVtableProvider]
file static class PropertyChangedEventHandler
{
    /// <summary>
    /// Gets the IID for The IID for <see cref="global::System.ComponentModel.PropertyChangedEventHandler"/>.
    /// </summary>
    public static ref readonly Guid IID => ref WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections
        ? ref WellKnownInterfaceIds.IID_WUX_PropertyChangedEventHandler
        : ref WellKnownInterfaceIds.IID_MUX_PropertyChangedEventHandler;
}

/// <summary>
/// Marshaller for <see cref="global::System.ComponentModel.PropertyChangedEventHandler"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class PropertyChangedEventHandlerMarshaller
{
    /// <summary>
    /// Converts a managed <see cref="global::System.ComponentModel.PropertyChangedEventHandler"/> to an unmanaged object.
    /// </summary>
    /// <param name="value">The managed <see cref="global::System.ComponentModel.PropertyChangedEventHandler"/> value.</param>
    /// <returns>The unmanaged object.</returns>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(global::System.ComponentModel.PropertyChangedEventHandler? value)
    {
        return WindowsRuntimeDelegateMarshaller.ConvertToUnmanagedUnsafe(value, in PropertyChangedEventHandler.IID);
    }

    /// <summary>
    /// Converts an unmanaged object to a managed <see cref="global::System.ComponentModel.PropertyChangedEventHandler"/>.
    /// </summary>
    /// <param name="value">The unmanaged object.</param>
    /// <returns>The managed <see cref="global::System.ComponentModel.PropertyChangedEventHandler"/> value</returns>
    public static global::System.ComponentModel.PropertyChangedEventHandler? ConvertToManaged(in WindowsRuntimeObjectReferenceValue value)
    {
        return WindowsRuntimeDelegateMarshaller.ConvertToManagedUnsafe<global::System.ComponentModel.PropertyChangedEventHandler>(in value);
    }
}

/// <summary>
/// The <see cref="WindowsRuntimeObject"/> implementation for <see cref="global::System.ComponentModel.PropertyChangedEventHandler"/>.
/// </summary>
file sealed unsafe class PropertyChangedEventHandlerNativeDelegate : WindowsRuntimeObject
{
    /// <summary>
    /// Creates a new <see cref="PropertyChangedEventHandlerNativeDelegate"/> instance with the specified parameters.
    /// </summary>
    /// <param name="objectReference">The <see cref="WindowsRuntimeObjectReference"/> for the delegate instance.</param>
    public PropertyChangedEventHandlerNativeDelegate(WindowsRuntimeObjectReference objectReference)
        : base(objectReference.As(in PropertyChangedEventHandler.IID))
    {
    }

    /// <inheritdoc cref="global::System.ComponentModel.PropertyChangedEventHandler"/>
    public void Invoke(object? sender, global::System.ComponentModel.PropertyChangedEventHandler e)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = NativeObjectReference.AsValue();
        using WindowsRuntimeObjectReferenceValue senderValue = WindowsRuntimeObjectMarshaller.ConvertToUnmanagedUnsafe(sender);
        using WindowsRuntimeObjectReferenceValue eValue = default; // TODO

        HRESULT hresult = ((delegate* unmanaged[MemberFunction]<void*, void*, void*, HRESULT>)(*(void***)thisValue.GetThisPtrUnsafe())[3])(
            thisValue.GetThisPtrUnsafe(),
            senderValue.GetThisPtrUnsafe(),
            eValue.GetThisPtrUnsafe());

        RestrictedErrorInfo.ThrowExceptionForHR(hresult);
    }

    /// <inheritdoc/>
    protected internal override bool HasUnwrappableNativeObjectReference => true;

    /// <inheritdoc/>
    protected override bool IsOverridableInterface(in Guid iid)
    {
        return false;
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
        bufferWriter.Write([new ComInterfaceEntry
        {
            IID = PropertyChangedEventHandlerReference.IID,
            Vtable = PropertyChangedEventHandlerReference.AbiToProjectionVftablePtr
        }]);
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
