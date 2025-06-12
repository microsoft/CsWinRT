// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;

[assembly: TypeMap<WindowsRuntimeComWrappersTypeMapGroup>(
    value: "Microsoft.UI.Xaml.Data.DataErrorsChangedEventArgs",
    target: typeof(ABI.System.ComponentModel.DataErrorsChangedEventArgs),
    trimTarget: typeof(DataErrorsChangedEventArgs))]

[assembly: TypeMapAssociation<WindowsRuntimeComWrappersTypeMapGroup>(
    typeof(DataErrorsChangedEventArgs),
    typeof(ABI.System.ComponentModel.DataErrorsChangedEventArgs))]

namespace ABI.System.ComponentModel;

/// <summary>
/// ABI type for <see cref="global::System.ComponentModel.DataErrorsChangedEventArgs"/>.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.data.dataerrorschangedeventargs"/>
[WindowsRuntimeClassName("Microsoft.UI.Xaml.Data.DataErrorsChangedEventArgs")]
[DataErrorsChangedEventArgsComWrappersMarshaller]
file static class DataErrorsChangedEventArgs;

/// <summary>
/// Marshaller for <see cref="global::System.ComponentModel.DataErrorsChangedEventArgs"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class DataErrorsChangedEventArgsMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeObjectMarshaller.ConvertToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(global::System.ComponentModel.DataErrorsChangedEventArgs? value)
    {
        return value is null ? default : new(DataErrorsChangedEventArgsRuntimeClassFactory.CreateInstance(value.PropertyName));
    }

    /// <inheritdoc cref="WindowsRuntimeObjectMarshaller.ConvertToManaged"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static global::System.ComponentModel.DataErrorsChangedEventArgs? ConvertToManaged(void* value)
    {
        if (value is null)
        {
            return null;
        }

        // Extract the property name from the native object
        HSTRING propertyName;
        HRESULT hresult = IDataErrorsChangedEventArgsVftbl.get_PropertyNameUnsafe(value, &propertyName);

        Marshal.ThrowExceptionForHR(hresult);

        // Convert to a managed 'string' and create the managed object for the args as well
        try
        {
            return new global::System.ComponentModel.DataErrorsChangedEventArgs(HStringMarshaller.ConvertToManaged(propertyName));
        }
        finally
        {
            HStringMarshaller.Free(propertyName);
        }
    }
}

/// <summary>
/// A custom <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> implementation for <see cref="global::System.ComponentModel.DataErrorsChangedEventArgs"/>.
/// </summary>
file sealed unsafe class DataErrorsChangedEventArgsComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
{
    /// <inheritdoc/>
    public override void* GetOrCreateComInterfaceForObject(object value)
    {
        return DataErrorsChangedEventArgsRuntimeClassFactory.CreateInstance(((global::System.ComponentModel.DataErrorsChangedEventArgs)value).PropertyName);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
    {
        wrapperFlags = CreatedWrapperFlags.NonWrapping;

        HRESULT hresult = IUnknownVftbl.QueryInterfaceUnsafe(value, in WellKnownInterfaceIds.IID_DataErrorsChangedEventArgs, out void* result);

        Marshal.ThrowExceptionForHR(hresult);

        try
        {
            return DataErrorsChangedEventArgsMarshaller.ConvertToManaged(value)!;
        }
        finally
        {
            WindowsRuntimeObjectMarshaller.Free(result);
        }
    }
}

/// <summary>
/// The runtime class factory for <see cref="global::System.ComponentModel.DataErrorsChangedEventArgs"/>.
/// </summary>
file static unsafe class DataErrorsChangedEventArgsRuntimeClassFactory
{
    /// <summary>
    /// The singleton instance for the activation factory.
    /// </summary>
    private static readonly WindowsRuntimeObjectReference NativeObject = WindowsRuntimeActivationFactory.GetActivationFactory(
        "Microsoft.UI.Xaml.Data.DataErrorsChangedEventArgs",
        in WellKnownInterfaceIds.IID_DataErrorsChangedEventArgsRuntimeClassFactory);

    /// <summary>
    /// Creates a new native instance for <see cref="global::System.ComponentModel.DataErrorsChangedEventArgs"/>.
    /// </summary>
    /// <param name="propertyName">The property name to use.</param>
    /// <returns>The new native instance for <see cref="global::System.ComponentModel.DataErrorsChangedEventArgs"/>.</returns>
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
