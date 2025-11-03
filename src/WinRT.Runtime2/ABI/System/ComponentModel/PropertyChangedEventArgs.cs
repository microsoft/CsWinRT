// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;

[assembly: TypeMapAssociation<WindowsRuntimeComWrappersTypeMapGroup>(
    typeof(PropertyChangedEventArgs),
    typeof(ABI.System.ComponentModel.PropertyChangedEventArgs))]

namespace ABI.System.ComponentModel;

/// <summary>
/// ABI type for <see cref="global::System.ComponentModel.PropertyChangedEventArgs"/>.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.data.propertychangedeventargs"/>
/// <seealso href="https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.data.propertychangedeventargs"/>
[PropertyChangedEventArgsComWrappersMarshaller]
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class PropertyChangedEventArgs;

/// <summary>
/// Marshaller for <see cref="global::System.ComponentModel.PropertyChangedEventArgs"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
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

        IPropertyChangedEventArgsVftbl.get_PropertyNameUnsafe(value, &propertyName).Assert();

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
    public override void* GetOrCreateComInterfaceForObject(object value)
    {
        return PropertyChangedEventArgsRuntimeClassFactory.CreateInstance(((global::System.ComponentModel.PropertyChangedEventArgs)value).PropertyName);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
    {
        wrapperFlags = CreatedWrapperFlags.NonWrapping;

        IUnknownVftbl.QueryInterfaceUnsafe(
            thisPtr: value,
            iid: in WellKnownXamlInterfaceIIDs.IID_PropertyChangedEventArgs,
            pvObject: out void* result).Assert();

        try
        {
            return PropertyChangedEventArgsMarshaller.ConvertToManaged(result)!;
        }
        finally
        {
            WindowsRuntimeObjectMarshaller.Free(result);
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
    private static readonly WindowsRuntimeObjectReference NativeObject = WindowsRuntimeActivationFactory.GetActivationFactory(
        runtimeClassName: WellKnownXamlRuntimeClassNames.PropertyChangedEventArgs,
        iid: in WellKnownXamlInterfaceIIDs.IID_PropertyChangedEventArgsRuntimeClassFactory);

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
        WindowsRuntimeObjectMarshaller.Free(innerInterface);

        return defaultInterface;
    }
}
