// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
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
/// Marshaller for <see cref="IEnumerable"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IEnumerableMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeObjectMarshaller.ConvertToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(IEnumerable? value)
    {
        return WindowsRuntimeInterfaceMarshaller<IEnumerable>.ConvertToUnmanaged(value, in WellKnownWindowsInterfaceIIDs.IID_IBindableIterable);
    }

    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.ConvertToManaged"/>
    public static IEnumerable? ConvertToManaged(void* value)
    {
        return (IEnumerable?)WindowsRuntimeUnsealedObjectMarshaller.ConvertToManaged<IEnumerableComWrappersCallback>(value);
    }
}

/// <summary>
/// The <see cref="IWindowsRuntimeUnsealedObjectComWrappersCallback"/> implementation for <see cref="IEnumerable"/>.
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

    /// <inheritdoc/>
    public static unsafe object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
    {
        WindowsRuntimeObjectReference valueReference = WindowsRuntimeComWrappersMarshal.CreateObjectReferenceUnsafe(
            externalComObject: value,
            iid: in WellKnownWindowsInterfaceIIDs.IID_IBindableIterable,
            wrapperFlags: out wrapperFlags);

        return new WindowsRuntimeEnumerable(valueReference);
    }
}

/// <summary>
/// A custom <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> implementation for <see cref="IEnumerable"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed unsafe class IEnumerableComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
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
/// Interop methods for <see cref="IEnumerable"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IEnumerableMethods
{
    /// <inheritdoc cref="IEnumerable.GetEnumerator"/>
    public static IEnumerator GetEnumerator(WindowsRuntimeObjectReference thisReference)
    {
        return BindableIEnumerableMethods.GetEnumerator(thisReference);
    }
}

/// <summary>
/// The <see cref="IEnumerable"/> implementation.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IEnumerableImpl
{
    /// <summary>
    /// The <see cref="IBindableIterableVftbl"/> value for the managed <see cref="IEnumerable"/> implementation.
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
    /// Gets a pointer to the managed <see cref="IEnumerable"/> implementation.
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
            var thisObject = ComInterfaceDispatch.GetInstance<IEnumerable>((ComInterfaceDispatch*)thisPtr);

            BindableIEnumerableAdapter.First(thisObject, result);

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}

/// <summary>
/// The <see cref="IDynamicInterfaceCastable"/> implementation for <see cref="IEnumerable"/>.
/// </summary>
[DynamicInterfaceCastableImplementation]
[Guid("036D2C08-DF29-41AF-8AA2-D774BE62BA6F")]
file interface IEnumerableInterfaceImpl : IEnumerable
{
    IEnumerator IEnumerable.GetEnumerator()
    {
        WindowsRuntimeObject thisObject = (WindowsRuntimeObject)this;

        // First, try to lookup for the actual 'IEnumerable' interface being implemented
        if (thisObject.TryGetObjectReferenceForInterface(
            interfaceType: typeof(IEnumerable).TypeHandle,
            interfaceReference: out WindowsRuntimeObjectReference? interfaceReference))
        {
            return IEnumerableMethods.GetEnumerator(interfaceReference);
        }

        // If that fails, try to get an object reference to some 'IEnumerable<T>' instance
        interfaceReference = thisObject.GetObjectReferenceForIEnumerableInterfaceInstance();

        // Return an enumerator through that. We don't know the 'T', so we need to marshal without type info.
        // However, this in practice is fine, as the RCW would also always implement 'IEnumerable' in metadata.
        return global::WindowsRuntime.InteropServices.IEnumerableMethods.GetEnumerator(interfaceReference);
    }
}
