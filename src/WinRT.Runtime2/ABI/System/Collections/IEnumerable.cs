// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;
using static System.Runtime.InteropServices.ComWrappers;

#pragma warning disable IDE0008, IDE1006

[assembly: TypeMapAssociation<DynamicInterfaceCastableImplementationTypeMapGroup>(
    typeof(IEnumerable),
    typeof(ABI.System.Collections.IEnumerableInterfaceImpl))]

namespace ABI.System.Collections;

/// <summary>
/// ABI type for <see cref="global::System.Collections.IEnumerable"/>.
/// </summary>
/// <remarks>
/// This interface is equivalent to <see href="https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.interop.ibindableiterable"/>.
/// </remarks>
/// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindableiterable"/>
[IEnumerableComWrappersMarshaller]
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IEnumerable;

/// <summary>
/// Marshaller for <see cref="global::System.Collections.IEnumerable"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IEnumerableMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeObjectMarshaller.ConvertToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(global::System.Collections.IEnumerable? value)
    {
        return WindowsRuntimeInterfaceMarshaller<global::System.Collections.IEnumerable>.ConvertToUnmanaged(value, in WellKnownWindowsInterfaceIIDs.IID_IBindableIterable);
    }

    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.ConvertToManaged"/>
    public static global::System.Collections.IEnumerable? ConvertToManaged(void* value)
    {
        return (global::System.Collections.IEnumerable?)WindowsRuntimeUnsealedObjectMarshaller.ConvertToManaged<IEnumerableComWrappersCallback>(value);
    }
}

/// <summary>
/// The <see cref="IWindowsRuntimeUnsealedObjectComWrappersCallback"/> implementation for <see cref="global::System.Collections.IEnumerable"/>.
/// </summary>
file abstract class IEnumerableComWrappersCallback : IWindowsRuntimeUnsealedObjectComWrappersCallback
{
    /// <inheritdoc/>
    public static unsafe bool TryCreateObject(
        void* value,
        ReadOnlySpan<char> runtimeClassName,
        [NotNullWhen(true)] out object? wrapperObject,
        out CreatedWrapperFlags wrapperFlags)
    {
        if (runtimeClassName.SequenceEqual(WellKnownXamlRuntimeClassNames.IBindableIterable))
        {
            WindowsRuntimeObjectReference valueReference = WindowsRuntimeComWrappersMarshal.CreateObjectReferenceUnsafe(
                externalComObject: value,
                iid: in WellKnownWindowsInterfaceIIDs.IID_IBindableIterable,
                wrapperFlags: out wrapperFlags);

            wrapperObject = new WindowsRuntimeEnumerable(valueReference);

            return true;
        }

        wrapperFlags = CreatedWrapperFlags.None;
        wrapperObject = null;

        return false;
    }
}

/// <summary>
/// A custom <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> implementation for <see cref="global::System.Collections.IEnumerable"/>.
/// </summary>
file sealed unsafe class IEnumerableComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
{
    /// <inheritdoc/>
    public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
    {
        WindowsRuntimeObjectReference valueReference = WindowsRuntimeComWrappersMarshal.CreateObjectReference(
            externalComObject: value,
            iid: in WellKnownWindowsInterfaceIIDs.IID_IBindableIterable,
            wrapperFlags: out wrapperFlags);

        return new WindowsRuntimeEnumerable(valueReference);
    }
}

/// <summary>
/// Interop methods for <see cref="global::System.Collections.IEnumerable"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IEnumerableMethods
{
    /// <inheritdoc cref="global::System.Collections.IEnumerable.GetEnumerator"/>
    public static global::System.Collections.IEnumerator GetEnumerator(WindowsRuntimeObjectReference thisReference)
    {
        return IBindableIterableMethods.First(thisReference);
    }
}

/// <summary>
/// Interop methods for <see cref="global::System.Collections.IEnumerable"/> invoked over constructed iterator types.
/// </summary>
file static class IEnumerableInstanceMethods
{
    /// <inheritdoc cref="global::System.Collections.IEnumerable.GetEnumerator"/>
    public static global::System.Collections.IEnumerator GetEnumerator(WindowsRuntimeObjectReference thisReference)
    {
        return global::WindowsRuntime.InteropServices.IEnumerableMethods.GetEnumerator(thisReference);
    }
}

/// <summary>
/// The <see cref="global::System.Collections.IEnumerable"/> implementation.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IEnumerableImpl
{
    /// <summary>
    /// The <see cref="IBindableIterableVftbl"/> value for the managed <see cref="global::System.Collections.IEnumerable"/> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly IBindableIterableVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static IEnumerableImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.First = &First;
    }

    /// <summary>
    /// Gets a pointer to the managed <see cref="global::System.Collections.IEnumerable"/> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Vftbl);
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindableiterable.first"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT First(void* thisPtr, void** result)
    {
        if (result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            var thisObject = ComInterfaceDispatch.GetInstance<global::System.Collections.IEnumerable>((ComInterfaceDispatch*)thisPtr);

            global::System.Collections.IEnumerator enumerator = thisObject.GetEnumerator();

            // See IEnumerableAdapter<T>.First for details.
            if (WindowsRuntimeInterfaceMarshaller<global::System.Collections.IEnumerator>.TryConvertToUnmanagedExact(
                value: enumerator,
                iid: in WellKnownWindowsInterfaceIIDs.IID_IBindableIterator,
                result: out WindowsRuntimeObjectReferenceValue enumeratorValue))
            {
                *result = enumeratorValue.DetachThisPtrUnsafe();
            }
            else
            {
                // In addition to the details in IEnumerableAdapter<T>.First, we wrap non generic enumerator
                // as a generic enumerator given there are WinUI scenarios where it is expected to be able to QI
                // for the generic one. This also allows us to take advantage of the same adapter.
                IEnumeratorAdapter<object> enumeratorAdapter = new(new NonGenericToGenericEnumerator(enumerator));

                *result = (void*)WindowsRuntimeComWrappers.GetOrCreateComInterfaceForObjectExact(enumeratorAdapter, in WellKnownWindowsInterfaceIIDs.IID_IBindableIterator);
            }

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}

/// <summary>
/// The <see cref="IDynamicInterfaceCastable"/> implementation for <see cref="global::System.Collections.IEnumerable"/>.
/// </summary>
[DynamicInterfaceCastableImplementation]
file interface IEnumerableInterfaceImpl : global::System.Collections.IEnumerable
{
    global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator()
    {
        WindowsRuntimeObject thisObject = (WindowsRuntimeObject)this;

        // First, try to lookup for the actual 'IEnumerable' interface being implemented
        if (thisObject.TryGetObjectReferenceForInterface(
            interfaceType: typeof(global::System.Collections.IEnumerable).TypeHandle,
            interfaceReference: out WindowsRuntimeObjectReference? interfaceReference))
        {
            return IEnumerableMethods.GetEnumerator(interfaceReference);
        }

        // If that fails, try to get an object reference to some 'IEnumerable<T>' instance
        interfaceReference = thisObject.GetObjectReferenceForIEnumerableInterfaceInstance();

        // Return an enumerator through that. We don't know the 'T', so we need to marshal without type info.
        // However, this in practice is fine, as the RCW would also always implement 'IEnumerable' in metadata.
        return IEnumerableInstanceMethods.GetEnumerator(interfaceReference);
    }
}

/// <summary>
/// Wraps an <see cref="global::System.Collections.IEnumerator"/> and exposes it as an <see cref="IEnumerator{Object}"/>.
/// </summary>
file sealed class NonGenericToGenericEnumerator : IEnumerator<object>
{
    private readonly global::System.Collections.IEnumerator enumerator;

    public NonGenericToGenericEnumerator(global::System.Collections.IEnumerator enumerator)
    {
        this.enumerator = enumerator;
    }

    public object Current => enumerator.Current;

    public bool MoveNext()
    {
        return enumerator.MoveNext();
    }

    public void Reset()
    {
        enumerator.Reset();
    }

    public void Dispose()
    {
    }
}
