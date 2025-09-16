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
/// Marshaller for <see cref="global::System.IDisposable"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IDisposableMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeObjectMarshaller.ConvertToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(global::System.IDisposable? value)
    {
        return WindowsRuntimeInterfaceMarshaller.ConvertToUnmanaged(value, in WellKnownInterfaceIds.IID_IDisposable);
    }

    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.ConvertToManaged"/>
    public static global::System.IDisposable? ConvertToManaged(void* value)
    {
        return (global::System.IDisposable?)WindowsRuntimeObjectMarshaller.ConvertToManaged(value);
    }
}

/// <summary>
/// Interop methods for <see cref="global::System.IDisposable"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IDisposableMethods
{
    /// <see cref="global::System.IDisposable.Dispose"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Dispose(WindowsRuntimeObjectReference thisReference)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();

        RestrictedErrorInfo.ThrowExceptionForHR(((IDisposableVftbl*)*(void***)thisPtr)->Close(thisPtr));
    }
}

/// <summary>
/// Binding type for <see cref="global::System.IDisposable"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct IDisposableVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, HRESULT> Close;
}

/// <summary>
/// The <see cref="global::System.IDisposable"/> implementation.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IDisposableImpl
{
    /// <summary>
    /// The <see cref="IDisposableVftbl"/> value for the managed <see cref="global::System.IDisposable"/> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly IDisposableVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static IDisposableImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.Close = &Close;
    }

    /// <summary>
    /// Gets the IID for <see cref="global::System.IDisposable"/>.
    /// </summary>
    public static ref readonly Guid IID => ref WellKnownInterfaceIds.IID_IDisposable;

    /// <summary>
    /// Gets a pointer to the managed <see cref="global::System.IDisposable"/> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Vftbl);
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.iclosable.close"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT Close(void* thisPtr)
    {
        try
        {
            ComInterfaceDispatch.GetInstance<global::System.IDisposable>((ComInterfaceDispatch*)thisPtr).Dispose();

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}

/// <summary>
/// The <see cref="IDynamicInterfaceCastable"/> implementation for <see cref="global::System.IDisposable"/>.
/// </summary>
[DynamicInterfaceCastableImplementation]
file interface IDisposable : global::System.IDisposable
{
    /// <inheritdoc/>
    void global::System.IDisposable.Dispose()
    {
        var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(global::System.IDisposable).TypeHandle);

        IDisposableMethods.Dispose(thisReference);
    }
}
