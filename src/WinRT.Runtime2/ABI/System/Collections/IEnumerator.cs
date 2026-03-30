// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;
using static System.Runtime.InteropServices.ComWrappers;

#pragma warning disable IDE0008, IDE1006

[assembly: TypeMapAssociation<DynamicInterfaceCastableImplementationTypeMapGroup>(
    typeof(IEnumerator),
    typeof(ABI.System.Collections.IEnumeratorInterfaceImpl))]

namespace ABI.System.Collections;

/// <summary>
/// Marshaller for <see cref="IEnumerator"/>.
/// </summary>
public static unsafe class IEnumeratorMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeObjectMarshaller.ConvertToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(IEnumerator? value)
    {
        return WindowsRuntimeInterfaceMarshaller<IEnumerator>.ConvertToUnmanaged(value, in WellKnownWindowsInterfaceIIDs.IID_IBindableIterator);
    }

    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.ConvertToManaged"/>
    public static IEnumerator? ConvertToManaged(void* value)
    {
        return (IEnumerator?)WindowsRuntimeUnsealedObjectMarshaller.ConvertToManaged<IEnumeratorComWrappersCallback>(value);
    }
}

/// <summary>
/// The <see cref="IWindowsRuntimeUnsealedObjectComWrappersCallback"/> implementation for <see cref="IEnumerator"/>.
/// </summary>
file abstract class IEnumeratorComWrappersCallback : IWindowsRuntimeUnsealedObjectComWrappersCallback
{
    /// <inheritdoc/>
    public static unsafe bool TryCreateObject(
        void* value,
        ReadOnlySpan<char> runtimeClassName,
        [NotNullWhen(true)] out object? wrapperObject,
        out CreatedWrapperFlags wrapperFlags)
    {
        if (runtimeClassName.SequenceEqual(WellKnownXamlRuntimeClassNames.IBindableIterator))
        {
            WindowsRuntimeObjectReference valueReference = WindowsRuntimeComWrappersMarshal.CreateObjectReferenceUnsafe(
                externalComObject: value,
                iid: in WellKnownWindowsInterfaceIIDs.IID_IBindableIterator,
                wrapperFlags: out wrapperFlags);

            wrapperObject = new WindowsRuntimeEnumerator(valueReference);

            return true;
        }

        wrapperFlags = CreatedWrapperFlags.None;
        wrapperObject = null;

        return false;
    }

    /// <inheritdoc/>
    public static unsafe object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
    {
        WindowsRuntimeObjectReference valueReference = WindowsRuntimeComWrappersMarshal.CreateObjectReferenceUnsafe(
            externalComObject: value,
            iid: in WellKnownWindowsInterfaceIIDs.IID_IBindableIterator,
            wrapperFlags: out wrapperFlags);

        return new WindowsRuntimeEnumerator(valueReference);
    }
}

/// <summary>
/// A custom <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> implementation for <see cref="IEnumerator"/>.
/// </summary>
public sealed unsafe class IEnumeratorComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
{
    /// <inheritdoc/>
    public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
    {
        WindowsRuntimeObjectReference valueReference = WindowsRuntimeComWrappersMarshal.CreateObjectReference(
            externalComObject: value,
            iid: in WellKnownWindowsInterfaceIIDs.IID_IBindableIterator,
            wrapperFlags: out wrapperFlags);

        return new WindowsRuntimeEnumerator(valueReference);
    }
}

/// <summary>
/// Interop methods for <see cref="IEnumerator"/>.
/// </summary>
public static class IEnumeratorMethods
{
    /// <inheritdoc cref="IEnumerator.Current"/>
    public static object? Current(WindowsRuntimeObjectReference thisReference)
    {
        return IBindableIteratorMethods.Current(thisReference);
    }

    /// <inheritdoc cref="IEnumerator.MoveNext"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool MoveNext(WindowsRuntimeObjectReference thisReference)
    {
        return IBindableIteratorMethods.MoveNext(thisReference);
    }
}

/// <summary>
/// The <see cref="IEnumerator"/> implementation.
/// </summary>
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
            var thisObject = ComInterfaceDispatch.GetInstance<IEnumerator>((ComInterfaceDispatch*)thisPtr);

            object? current = BindableIEnumeratorAdapter.GetInstance(thisObject).Current;

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
            var thisObject = ComInterfaceDispatch.GetInstance<IEnumerator>((ComInterfaceDispatch*)thisPtr);

            *result = BindableIEnumeratorAdapter.GetInstance(thisObject).HasCurrent;

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
            var thisObject = ComInterfaceDispatch.GetInstance<IEnumerator>((ComInterfaceDispatch*)thisPtr);

            *result = BindableIEnumeratorAdapter.GetInstance(thisObject).MoveNext();

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
[Guid("6A1D6C07-076D-49F2-8314-F52C9C9A8331")]
file interface IEnumeratorInterfaceImpl : IEnumerator
{
    /// <inheritdoc/>
    object? IEnumerator.Current
    {
        get
        {
            var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(IEnumerator).TypeHandle);

            return IEnumeratorMethods.Current(thisReference);
        }
    }

    /// <inheritdoc/>
    bool IEnumerator.MoveNext()
    {
        var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(IEnumerator).TypeHandle);

        return IEnumeratorMethods.MoveNext(thisReference);
    }

    /// <inheritdoc/>
    void IEnumerator.Reset()
    {
        throw new NotSupportedException();
    }
}
#endif