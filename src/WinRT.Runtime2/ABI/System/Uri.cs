// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;
using static System.Runtime.InteropServices.ComWrappers;

[assembly: TypeMap<WindowsRuntimeTypeMapUniverse>(
    value: "Windows.Foundation.Uri",
    target: typeof(ABI.System.Uri),
    trimTarget: typeof(Uri))]

[assembly: TypeMapAssociation<WindowsRuntimeTypeMapUniverse>(
    typeof(Uri),
    typeof(ABI.System.Uri))]

namespace ABI.System;

/// <summary>
/// ABI type for <see cref="global::System.Uri"/>.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.uri"/>
[WindowsRuntimeClassName("Windows.Foundation.Uri")]
[UriComWrappersMarshaller]
file static class Uri;

/// <summary>
/// Marshaller for <see cref="global::System.Uri"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class UriMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeObjectMarshaller.ConvertToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(global::System.Uri? value)
    {
        return value is null ? default : new(UriRuntimeClassFactory.CreateInstance(value.OriginalString));
    }

    /// <inheritdoc cref="WindowsRuntimeObjectMarshaller.ConvertToManaged"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static global::System.Uri? ConvertToManaged(void* value)
    {
        if (value is null)
        {
            return null;
        }

        // Extract the raw URI string from the native object
        HSTRING rawUri;
        HRESULT hresult = IUriRuntimeClassVftbl.get_RawUriUnsafe(value, &rawUri);

        Marshal.ThrowExceptionForHR(hresult);

        // Convert to a managed 'string' and create the managed object for the args as well
        try
        {
            return new global::System.Uri(HStringMarshaller.ConvertToManaged(rawUri));
        }
        finally
        {
            HStringMarshaller.Free(rawUri);
        }
    }
}

/// <summary>
/// A custom <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> implementation for <see cref="global::System.Uri"/>.
/// </summary>
file sealed unsafe class UriComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
{
    /// <inheritdoc/>
    public override void* GetOrCreateComInterfaceForObject(object value)
    {
        return UriRuntimeClassFactory.CreateInstance(((global::System.Uri)value).OriginalString);
    }

    /// <inheritdoc/>
    public override ComInterfaceEntry* ComputeVtables(out int count)
    {
        // All managed 'Uri' instances are marshalled as fully native objects
        throw new UnreachableException();
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public override object CreateObject(void* value)
    {
        HRESULT hresult = IUnknownVftbl.QueryInterfaceUnsafe(value, in WellKnownInterfaceIds.IID_UriRuntimeClass, out void* result);

        Marshal.ThrowExceptionForHR(hresult);

        try
        {
            return UriMarshaller.ConvertToManaged(value)!;
        }
        finally
        {
            _ = IUnknownVftbl.ReleaseUnsafe(result);
        }
    }
}

/// <summary>
/// The runtime class factory for <see cref="global::System.Uri"/>.
/// </summary>
file static unsafe class UriRuntimeClassFactory
{
    /// <summary>
    /// The singleton instance for the activation factory.
    /// </summary>
    private static readonly WindowsRuntimeObjectReference NativeObject = WindowsRuntimeActivationFactory.GetActivationFactory(
        "Windows.Foundation.Uri",
        in WellKnownInterfaceIds.IID_UriRuntimeClassFactory);

    /// <summary>
    /// Creates a new native instance for <see cref="global::System.Uri"/>.
    /// </summary>
    /// <param name="uri">The raw URI text to use.</param>
    /// <returns>The new native instance for <see cref="global::System.Uri"/>.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void* CreateInstance(string? uri)
    {
        WindowsRuntimeActivationHelper.ActivateInstanceUnsafe(
            activationFactoryObjectReference: NativeObject,
            param0: uri,
            defaultInterface: out void* defaultInterface);

        return defaultInterface;
    }
}
