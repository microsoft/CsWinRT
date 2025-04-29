// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;
using static System.Runtime.InteropServices.ComWrappers;

#pragma warning disable CS0649, IDE0008

namespace ABI.System;

/// <summary>
/// Interop methods for <see cref="global::System.IServiceProvider"/>.
/// </summary>
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

        return WindowsRuntimeObjectMarshaller.ConvertToManaged(result);
    }
}

/// <summary>
/// Binding type for <see cref="global::System.IServiceProvider"/>.
/// </summary>
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
        get => (nint)Unsafe.AsPointer(ref Unsafe.AsRef(in Vftbl));
    }

    /// <see href="https://learn.microsoft.com/dotnet/api/system.iserviceprovider.getservice"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT GetService(void* thisPtr, Type serviceType, void** result)
    {
        *result = null;

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
file interface IServiceProvider : global::System.IServiceProvider
{
    /// <inheritdoc/>
    object? global::System.IServiceProvider.GetService(global::System.Type serviceType)
    {
        var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(global::System.IServiceProvider).TypeHandle);

        return IServiceProviderMethods.GetService(thisReference, serviceType);
    }
}
