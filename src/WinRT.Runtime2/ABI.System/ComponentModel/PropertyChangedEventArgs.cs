﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;

[assembly: TypeMap<WindowsRuntimeTypeMapUniverse>(
    value: "Windows.UI.Xaml.Data.PropertyChangedEventArgs",
    target: typeof(ABI.System.ComponentModel.PropertyChangedEventArgs),
    trimTarget: typeof(PropertyChangedEventHandler))]

[assembly: TypeMap<WindowsRuntimeTypeMapUniverse>(
    value: "Microsoft.UI.Xaml.Data.PropertyChangedEventArgs",
    target: typeof(ABI.System.ComponentModel.PropertyChangedEventArgs),
    trimTarget: typeof(PropertyChangedEventHandler))]

[assembly: TypeMapAssociation<WindowsRuntimeTypeMapUniverse>(
    typeof(PropertyChangedEventArgs),
    typeof(ABI.System.ComponentModel.PropertyChangedEventArgs))]

namespace ABI.System.ComponentModel;

/// <summary>
/// ABI type for <see cref="global::System.ComponentModel.PropertyChangedEventArgs"/>.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.data.propertychangedeventargs"/>
/// <seealso href="https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.data.propertychangedeventargs"/>
[WindowsRuntimeClassName("Microsoft.UI.Xaml.Data.PropertyChangedEventArgs")]
[PropertyChangedEventArgsComWrappersMarshaller]
file static class PropertyChangedEventArgs;

/// <summary>
/// Marshaller for <see cref="global::System.ComponentModel.PropertyChangedEventArgs"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class PropertyChangedEventArgsMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeObjectMarshaller.ConvertToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(global::System.ComponentModel.PropertyChangedEventArgs? value)
    {
        return value is null ? default : new(PropertyChangedEventArgsRuntimeClassFactory.CreateInstance(value.PropertyName));
    }

    /// <inheritdoc cref="WindowsRuntimeObjectMarshaller.ConvertToManaged"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static global::System.ComponentModel.PropertyChangedEventArgs? ConvertToManaged(void* value)
    {
        if (value is null)
        {
            return null;
        }

        // Extract the property name from the native object
        HSTRING propertyName;
        HRESULT hresult = IPropertyChangedEventArgsVftbl.PropertyNameUnsafe(value, &propertyName);

        Marshal.ThrowExceptionForHR(hresult);

        // Convert to a managed 'string' and create the managed object for the args as well
        try
        {
            return new global::System.ComponentModel.PropertyChangedEventArgs(HStringMarshaller.ConvertToManaged(propertyName));
        }
        finally
        {
            HStringMarshaller.Free(propertyName);
        }
    }
}

/// <summary>
/// A custom <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> implementation for <see cref="global::System.ComponentModel.PropertyChangedEventArgs"/>.
/// </summary>
file sealed unsafe class PropertyChangedEventArgsComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
{
    /// <inheritdoc/>
    public override unsafe void* GetOrCreateComInterfaceForObject(object value)
    {
        return PropertyChangedEventArgsRuntimeClassFactory.CreateInstance(((global::System.ComponentModel.PropertyChangedEventArgs)value).PropertyName);
    }

    /// <inheritdoc/>
    public override void ComputeVtables(IBufferWriter<ComWrappers.ComInterfaceEntry> bufferWriter)
    {
        // All managed 'PropertyChangedEventArgs' instances are marshalled as fully native objects
        throw new UnreachableException();
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public override object CreateObject(void* value)
    {
        ref readonly Guid iid = ref WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections
            ? ref WellKnownInterfaceIds.IID_WUX_PropertyChangedEventArgs
            : ref WellKnownInterfaceIds.IID_MUX_PropertyChangedEventArgs;

        HRESULT hresult = IUnknownVftbl.QueryInterfaceUnsafe(value, in iid, out void* result);

        Marshal.ThrowExceptionForHR(hresult);

        try
        {
            return PropertyChangedEventArgsMarshaller.ConvertToManaged(value)!;
        }
        finally
        {
            _ = IUnknownVftbl.ReleaseUnsafe(result);
        }
    }
}

/// <summary>
/// The runtime class factory for <see cref="global::System.ComponentModel.PropertyChangedEventArgs"/>.
/// </summary>
file static unsafe class PropertyChangedEventArgsRuntimeClassFactory
{
    /// <summary>
    /// The singleton instance for the activation factory.
    /// </summary>
    private static readonly WindowsRuntimeObjectReference NativeObject = WindowsRuntimeActivationFactory.GetActivationFactory(RuntimeClassName, in IID);

    /// <summary>
    /// Gets the IID for <see cref="PropertyChangedEventArgsRuntimeClassFactory"/>.
    /// </summary>
    private static ref readonly Guid IID => ref WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections
        ? ref WellKnownInterfaceIds.IID_WUX_PropertyChangedEventArgsRuntimeClassFactory
        : ref WellKnownInterfaceIds.IID_MUX_PropertyChangedEventArgsRuntimeClassFactory;

    /// <summary>
    /// Gets the runtime class name for <see cref="PropertyChangedEventArgsRuntimeClassFactory"/>.
    /// </summary>
    private static string RuntimeClassName => WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections
        ? "Windows.UI.Xaml.Data.PropertyChangedEventArgs"
        : "Microsoft.UI.Xaml.Data.PropertyChangedEventArgs";

    /// <summary>
    /// Creates a new native instance for <see cref="global::System.ComponentModel.PropertyChangedEventArgs"/>.
    /// </summary>
    /// <param name="propertyName">The property name to use.</param>
    /// <returns>The new native instance for <see cref="global::System.ComponentModel.PropertyChangedEventArgs"/>.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void* CreateInstance(string? propertyName)
    {
        WindowsRuntimeActivationHelper.ActivateInstanceUnsafe(
            activationFactoryObjectReference: NativeObject,
            param0: propertyName,
            baseInterface: null,
            innerInterface: out void* innerInterface,
            defaultInterface: out void* defaultInterface);

        // The value of 'innerInterface' should always be 'null', but let's release it just in case
        if (innerInterface == null)
        {
            _ = IUnknownVftbl.ReleaseUnsafe(innerInterface);
        }

        return defaultInterface;
    }
}
