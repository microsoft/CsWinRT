// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Foundation;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;
using static System.Runtime.InteropServices.ComWrappers;

#pragma warning disable IDE0008

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
[assembly: TypeMap<WindowsRuntimeMetadataTypeMapGroup>(
    value: "Microsoft.UI.Xaml.IXamlServiceProvider",
    target: typeof(ABI.System.IServiceProvider),
    trimTarget: typeof(IServiceProvider))]
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code

[assembly: TypeMapAssociation<DynamicInterfaceCastableImplementationTypeMapGroup>(
    source: typeof(IServiceProvider),
    proxy: typeof(ABI.System.IServiceProviderInterfaceImpl))]

namespace ABI.System;

/// <summary>
/// ABI type for <see cref="global::System.IServiceProvider"/>.
/// </summary>
[WindowsRuntimeMetadata("Microsoft.UI.Xaml.WinUIContract")]
[WindowsRuntimeMetadataTypeName("Microsoft.UI.Xaml.IXamlServiceProvider")]
file static class IServiceProvider;

/// <summary>
/// Marshaller for <see cref="global::System.IServiceProvider"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IServiceProviderMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeObjectMarshaller.ConvertToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(global::System.IServiceProvider? value)
    {
        return WindowsRuntimeInterfaceMarshaller<global::System.IServiceProvider>.ConvertToUnmanaged(value, in WellKnownWindowsInterfaceIIDs.IID_IXamlServiceProvider);
    }

    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.ConvertToManaged"/>
    public static global::System.IServiceProvider? ConvertToManaged(void* value)
    {
        return (global::System.IServiceProvider?)WindowsRuntimeObjectMarshaller.ConvertToManaged(value);
    }
}

/// <summary>
/// Interop methods for <see cref="global::System.IServiceProvider"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IServiceProviderMethods
{
    /// <see cref="global::System.IServiceProvider.GetService"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static object? GetService(WindowsRuntimeObjectReference thisReference, global::System.Type serviceType)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        void* result;

        HRESULT hresult = ((IServiceProviderVftbl*)*(void***)thisPtr)->GetService(
            thisPtr,
            TypeMarshaller.ConvertToUnmanaged(serviceType),
            &result);

        RestrictedErrorInfo.ThrowExceptionForHR(hresult);

        try
        {
            return WindowsRuntimeObjectMarshaller.ConvertToManaged(result);
        }
        finally
        {
            WindowsRuntimeUnknownMarshaller.Free(result);
        }
    }
}

/// <summary>
/// Binding type for <see cref="global::System.IServiceProvider"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct IServiceProviderVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, Type, void**, HRESULT> GetService;
}

/// <summary>
/// The <see cref="global::System.IServiceProvider"/> implementation.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IServiceProviderImpl
{
    /// <summary>
    /// The <see cref="IServiceProviderVftbl"/> value for the managed <see cref="global::System.IServiceProvider"/> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly IServiceProviderVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static IServiceProviderImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.GetService = &GetService;
    }

    /// <summary>
    /// Gets a pointer to the managed <see cref="global::System.IServiceProvider"/> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Vftbl);
    }

    /// <see href="https://learn.microsoft.com/dotnet/api/system.iserviceprovider.getservice"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT GetService(void* thisPtr, Type serviceType, void** result)
    {
        if (result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            var unboxedValue = ComInterfaceDispatch.GetInstance<global::System.IServiceProvider>((ComInterfaceDispatch*)thisPtr);

            global::System.Type? managedType = TypeMarshaller.ConvertToManaged(serviceType);

            // We must have an actual 'Type' instance to resolve a service, so fail if we can't retrieve one
            if (managedType is null)
            {
                TypeExceptions.ThrowArgumentExceptionForNullType(serviceType);
            }

            object? service = unboxedValue.GetService(managedType);

            *result = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(service).DetachThisPtrUnsafe();

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}

/// <summary>
/// The <see cref="IDynamicInterfaceCastable"/> implementation for <see cref="global::System.IServiceProvider"/>.
/// </summary>
[DynamicInterfaceCastableImplementation]
file interface IServiceProviderInterfaceImpl : global::System.IServiceProvider
{
    /// <inheritdoc/>
    object? global::System.IServiceProvider.GetService(global::System.Type serviceType)
    {
        var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(global::System.IServiceProvider).TypeHandle);

        return IServiceProviderMethods.GetService(thisReference, serviceType);
    }
}