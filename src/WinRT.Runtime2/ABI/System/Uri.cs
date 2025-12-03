// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
[assembly: TypeMap<WindowsRuntimeComWrappersTypeMapGroup>(
    value: "Windows.Foundation.Uri",
    target: typeof(ABI.System.Uri),
    trimTarget: typeof(Uri))]
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code

[assembly: TypeMapAssociation<WindowsRuntimeComWrappersTypeMapGroup>(
    typeof(Uri),
    typeof(ABI.System.Uri))]

namespace ABI.System;

/// <summary>
/// ABI type for <see cref="global::System.Uri"/>.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.uri"/>
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
[UriComWrappersMarshaller]
file static class Uri;

/// <summary>
/// Marshaller for <see cref="global::System.Uri"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
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

        IUriRuntimeClassVftbl.get_RawUriUnsafe(value, &rawUri).Assert();

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
    [MethodImpl(MethodImplOptions.NoInlining)]
    public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
    {
        wrapperFlags = CreatedWrapperFlags.NonWrapping;

        IUnknownVftbl.QueryInterfaceUnsafe(value, in WellKnownWindowsInterfaceIIDs.IID_UriRuntimeClass, out void* result).Assert();

        try
        {
            return UriMarshaller.ConvertToManaged(result)!;
        }
        finally
        {
            WindowsRuntimeUnknownMarshaller.Free(result);
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
        in WellKnownWindowsInterfaceIIDs.IID_UriRuntimeClassFactory);

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