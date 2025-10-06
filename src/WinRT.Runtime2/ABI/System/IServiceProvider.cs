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

namespace ABI.System;

/// <summary>
/// Marshaller for <see cref="IServiceProvider"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IServiceProviderMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeObjectMarshaller.ConvertToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(IServiceProvider? value)
    {
        return WindowsRuntimeInterfaceMarshaller<IServiceProvider>.ConvertToUnmanaged(value, in WellKnownInterfaceIds.IID_IServiceProvider);
    }

    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.ConvertToManaged"/>
    public static IServiceProvider? ConvertToManaged(void* value)
    {
        return (IServiceProvider?)WindowsRuntimeObjectMarshaller.ConvertToManaged(value);
    }
}

/// <summary>
/// Interop methods for <see cref="IServiceProvider"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IServiceProviderMethods
{
    /// <see cref="IServiceProvider.GetService"/>
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
            WindowsRuntimeObjectMarshaller.Free(result);
        }
    }
}

/// <summary>
/// Binding type for <see cref="IServiceProvider"/>.
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
/// The <see cref="IServiceProvider"/> implementation.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IServiceProviderImpl
{
    /// <summary>
    /// The <see cref="IServiceProviderVftbl"/> value for the managed <see cref="IServiceProvider"/> implementation.
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
    /// Gets the IID for <see cref="IServiceProvider"/>.
    /// </summary>
    public static ref readonly Guid IID => ref WellKnownInterfaceIds.IID_IServiceProvider;

    /// <summary>
    /// Gets a pointer to the managed <see cref="IServiceProvider"/> implementation.
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
            var unboxedValue = ComInterfaceDispatch.GetInstance<IServiceProvider>((ComInterfaceDispatch*)thisPtr);

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
/// The <see cref="IDynamicInterfaceCastable"/> implementation for <see cref="IServiceProvider"/>.
/// </summary>
[DynamicInterfaceCastableImplementation]
file interface IServiceProviderInterfaceImpl : IServiceProvider
{
    /// <inheritdoc/>
    object? IServiceProvider.GetService(global::System.Type serviceType)
    {
        var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(IServiceProvider).TypeHandle);

        return IServiceProviderMethods.GetService(thisReference, serviceType);
    }
}
