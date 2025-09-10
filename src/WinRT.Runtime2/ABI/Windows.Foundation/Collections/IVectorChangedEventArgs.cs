﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;
using static System.Runtime.InteropServices.ComWrappers;

#pragma warning disable IDE0008, IDE1006

namespace ABI.System.ComponentModel;

/// <summary>
/// Interop methods for <see cref="global::Windows.Foundation.Collections.IVectorChangedEventArgs"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IVectorChangedEventArgsMethods
{
    /// <see cref="global::Windows.Foundation.Collections.IVectorChangedEventArgs.CollectionChange"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static CollectionChange CollectionChange(WindowsRuntimeObjectReference thisReference)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        CollectionChange result;

        RestrictedErrorInfo.ThrowExceptionForHR(((IVectorChangedEventArgsVftbl*)*(void***)thisPtr)->get_CollectionChange(thisPtr, &result));

        return result;
    }

    /// <see cref="global::Windows.Foundation.Collections.IVectorChangedEventArgs.Index"/>
    public static uint Index(WindowsRuntimeObjectReference thisReference)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        uint result;

        RestrictedErrorInfo.ThrowExceptionForHR(((IVectorChangedEventArgsVftbl*)*(void***)thisPtr)->get_Index(thisPtr, &result));

        return result;
    }
}

/// <summary>
/// Binding type for <see cref="global::Windows.Foundation.Collections.IVectorChangedEventArgs"/>.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivectorchangedeventargs"/>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct IVectorChangedEventArgsVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, CollectionChange*, HRESULT> get_CollectionChange;
    public delegate* unmanaged[MemberFunction]<void*, uint*, HRESULT> get_Index;
}

/// <summary>
/// The <see cref="global::Windows.Foundation.Collections.IVectorChangedEventArgs"/> implementation.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IVectorChangedEventArgsImpl
{
    /// <summary>
    /// The <see cref="IVectorChangedEventArgsVftbl"/> value for the managed <see cref="global::Windows.Foundation.Collections.IVectorChangedEventArgs"/> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly IVectorChangedEventArgsVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static IVectorChangedEventArgsImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.get_CollectionChange = &get_CollectionChange;
        Vftbl.get_Index = &get_Index;
    }

    /// <summary>
    /// Gets the IID for <see cref="global::Windows.Foundation.Collections.IVectorChangedEventArgs"/>.
    /// </summary>
    public static ref readonly Guid IID => ref WellKnownInterfaceIds.IID_IVectorChangedEventArgs;

    /// <summary>
    /// Gets a pointer to the managed <see cref="global::Windows.Foundation.Collections.IVectorChangedEventArgs"/> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Vftbl);
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivectorchangedeventargs.collectionchange"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]

    private static HRESULT get_CollectionChange(void* thisPtr, CollectionChange* result)
    {
        try
        {
            var unboxedValue = ComInterfaceDispatch.GetInstance<global::Windows.Foundation.Collections.IVectorChangedEventArgs>((ComInterfaceDispatch*)thisPtr);

            *result = unboxedValue.CollectionChange;

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivectorchangedeventargs.index"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]

    private static HRESULT get_Index(void* thisPtr, uint* result)
    {
        try
        {
            var unboxedValue = ComInterfaceDispatch.GetInstance<global::Windows.Foundation.Collections.IVectorChangedEventArgs>((ComInterfaceDispatch*)thisPtr);

            *result = unboxedValue.Index;

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}

/// <summary>
/// The <see cref="IDynamicInterfaceCastable"/> implementation for <see cref="global::Windows.Foundation.Collections.IVectorChangedEventArgs"/>.
/// </summary>
[DynamicInterfaceCastableImplementation]
file interface IVectorChangedEventArgs : global::Windows.Foundation.Collections.IVectorChangedEventArgs
{
    /// <inheritdoc/>
    CollectionChange global::Windows.Foundation.Collections.IVectorChangedEventArgs.CollectionChange
    {
        get
        {
            var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(global::Windows.Foundation.Collections.IVectorChangedEventArgs).TypeHandle);

            return IVectorChangedEventArgsMethods.CollectionChange(thisReference);
        }
    }

    /// <inheritdoc/>
    uint global::Windows.Foundation.Collections.IVectorChangedEventArgs.Index
    {
        get
        {
            var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(global::Windows.Foundation.Collections.IVectorChangedEventArgs).TypeHandle);

            return IVectorChangedEventArgsMethods.Index(thisReference);
        }
    }
}
