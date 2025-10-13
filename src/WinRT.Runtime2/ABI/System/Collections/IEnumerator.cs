// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;
using static System.Runtime.InteropServices.ComWrappers;

#pragma warning disable IDE0008, IDE1006

namespace ABI.System.Collections;

/// <summary>
/// Marshaller for <see cref="IEnumerator"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IEnumeratorMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeObjectMarshaller.ConvertToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(IEnumerator? value)
    {
        return WindowsRuntimeInterfaceMarshaller<IEnumerator>.ConvertToUnmanaged(value, in WellKnownInterfaceIds.IID_IBindableIterator);
    }

    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.ConvertToManaged"/>
    public static IEnumerator? ConvertToManaged(void* value)
    {
        return (IEnumerator?)WindowsRuntimeObjectMarshaller.ConvertToManaged(value);
    }
}

/// <summary>
/// Interop methods for <see cref="IEnumerator"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IEnumeratorMethods
{
    /// <inheritdoc cref="IEnumerator.Current"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindableiterator.current"/>
    public static object? Current(WindowsRuntimeObjectReference thisReference)
    {
        return IBindableIteratorMethods.Current(thisReference);
    }

    /// <inheritdoc cref="IEnumerator.MoveNext"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool MoveNext(WindowsRuntimeObjectReference thisReference)
    {
        return IBindableIteratorMethods.MoveNext(thisReference);
    }
}

/// <summary>
/// The <see cref="IEnumerator"/> implementation.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IEnumeratorImpl
{
    /// <summary>
    /// The <see cref="IBindableIteratorVftbl"/> value for the managed <see cref="IEnumerator"/> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly IBindableIteratorVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static IEnumeratorImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.get_Current = &get_Current;
        Vftbl.get_HasCurrent = &get_HasCurrent;
        Vftbl.MoveNext = &MoveNext;
        Vftbl.GetMany = &GetMany;
    }

    /// <summary>
    /// Gets the IID for <see cref="IEnumerator"/>.
    /// </summary>
    public static ref readonly Guid IID
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref WellKnownInterfaceIds.IID_IBindableIterator;
    }

    /// <summary>
    /// Gets a pointer to the managed <see cref="IEnumerator"/> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Vftbl);
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindableiterator.current"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT get_Current(void* thisPtr, void** result)
    {
        if (result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            var unboxedValue = ComInterfaceDispatch.GetInstance<IEnumerator>((ComInterfaceDispatch*)thisPtr);

            object? current = IBindableIteratorAdapter.GetInstance(unboxedValue).Current;

            *result = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(current).DetachThisPtrUnsafe();

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindableiterator.hascurrent"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT get_HasCurrent(void* thisPtr, bool* result)
    {
        if (result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            var unboxedValue = ComInterfaceDispatch.GetInstance<IEnumerator>((ComInterfaceDispatch*)thisPtr);

            *result = IBindableIteratorAdapter.GetInstance(unboxedValue).HasCurrent;

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindableiterator.movenext"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT MoveNext(void* thisPtr, bool* result)
    {
        if (result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            var unboxedValue = ComInterfaceDispatch.GetInstance<IEnumerator>((ComInterfaceDispatch*)thisPtr);

            *result = IBindableIteratorAdapter.GetInstance(unboxedValue).MoveNext();

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    // See notes in 'IBindableIteratorVftbl.GetMany'
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT GetMany(void* thisPtr, uint index, void* items, uint* count)
    {
        return WellKnownErrorCodes.E_NOTIMPL;
    }
}

/// <summary>
/// The <see cref="IDynamicInterfaceCastable"/> implementation for <see cref="IEnumerator"/>.
/// </summary>
[DynamicInterfaceCastableImplementation]
file interface IEnumeratorInterfaceImpl : IEnumerator
{
    /// <inheritdoc/>
    object? IEnumerator.Current
    {
        get
        {
            var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(IEnumerable).TypeHandle);

            return IEnumeratorMethods.Current(thisReference);
        }
    }

    /// <inheritdoc/>
    bool IEnumerator.MoveNext()
    {
        var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(IEnumerable).TypeHandle);

        return IEnumeratorMethods.MoveNext(thisReference);
    }

    /// <inheritdoc/>
    void IEnumerator.Reset()
    {
        throw new NotSupportedException();
    }
}
