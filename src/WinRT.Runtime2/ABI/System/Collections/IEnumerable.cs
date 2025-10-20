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

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
[assembly: TypeMap<WindowsRuntimeComWrappersTypeMapGroup>(
    value: "Windows.UI.Xaml.Interop.IBindableIterable",
    target: typeof(ABI.System.Collections.IEnumerable),
    trimTarget: typeof(IEnumerable))]

[assembly: TypeMap<WindowsRuntimeComWrappersTypeMapGroup>(
    value: "Microsoft.UI.Xaml.Interop.IBindableIterable",
    target: typeof(ABI.System.Collections.IEnumerable),
    trimTarget: typeof(IEnumerable))]
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code

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
file static class IEnumerable;

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
        return WindowsRuntimeInterfaceMarshaller<global::System.Collections.IEnumerable>.ConvertToUnmanaged(value, in WellKnownInterfaceIds.IID_IBindableIterable);
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
    /// <summary>
    /// Gets the runtime class name for <see cref="global::System.Collections.IEnumerable"/>.
    /// </summary>
    private static string RuntimeClassName
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections
            ? "Windows.UI.Xaml.Interop.IBindableIterable"
            : "Microsoft.UI.Xaml.Interop.IBindableIterable";
    }

    /// <inheritdoc/>
	public static unsafe bool TryCreateObject(
        void* value,
        ReadOnlySpan<char> runtimeClassName,
        [NotNullWhen(true)] out object? wrapperObject,
        out CreatedWrapperFlags wrapperFlags)
    {
        if (runtimeClassName.SequenceEqual(RuntimeClassName))
        {
            WindowsRuntimeObjectReference valueReference = WindowsRuntimeComWrappersMarshal.CreateObjectReferenceUnsafe(
                externalComObject: value,
                iid: in WellKnownInterfaceIds.IID_IBindableIterable,
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
            iid: in WellKnownInterfaceIds.IID_IBindableIterable,
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
public static unsafe class IEnumerableMethods
{
    /// <inheritdoc cref="global::System.Collections.IEnumerable.GetEnumerator"/>
    public static IEnumerator GetEnumerator(WindowsRuntimeObjectReference thisReference)
    {
        return IBindableIterableMethods.First(thisReference);
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
    /// Gets the IID for <see cref="global::System.Collections.IEnumerable"/>.
    /// </summary>
    public static ref readonly Guid IID
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref WellKnownInterfaceIds.IID_IBindableIterable;
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
            var unboxedValue = ComInterfaceDispatch.GetInstance<global::System.Collections.IEnumerable>((ComInterfaceDispatch*)thisPtr);

            IEnumerator enumerator = unboxedValue.GetEnumerator();

            *result = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(enumerator).DetachThisPtrUnsafe();

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
    IEnumerator global::System.Collections.IEnumerable.GetEnumerator()
    {
        var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(global::System.Collections.IEnumerable).TypeHandle);

        return IEnumerableMethods.GetEnumerator(thisReference);
    }
}
