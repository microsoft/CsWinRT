// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;
using System.Diagnostics.CodeAnalysis;
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
    value: "Windows.Foundation.IStringable",
    target: typeof(ABI.Windows.Foundation.IStringable),
    trimTarget: typeof(IStringable))]
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code

[assembly: TypeMapAssociation<WindowsRuntimeMetadataTypeMapGroup>(
    source: typeof(IStringable),
    proxy: typeof(ABI.Windows.Foundation.IStringable))]

[assembly: TypeMapAssociation<DynamicInterfaceCastableImplementationTypeMapGroup>(
    source: typeof(IStringable),
    proxy: typeof(ABI.Windows.Foundation.IStringableInterfaceImpl))]

namespace ABI.Windows.Foundation;

/// <summary>
/// ABI type for <see cref="global::Windows.Foundation.IStringable"/>.
/// </summary>
[WindowsRuntimeMappedMetadata("Windows.Foundation.FoundationContract")]
[WindowsRuntimeMetadataTypeName("Windows.Foundation.IStringable")]
[WindowsRuntimeMappedType(typeof(global::Windows.Foundation.IStringable))]
file static class IStringable;

/// <summary>
/// Marshaller for <see cref="global::Windows.Foundation.IStringable"/>.
/// </summary>
public static unsafe class IStringableMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeObjectMarshaller.ConvertToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(global::Windows.Foundation.IStringable? value)
    {
        return WindowsRuntimeInterfaceMarshaller<global::Windows.Foundation.IStringable>.ConvertToUnmanaged(value, in WellKnownWindowsInterfaceIIDs.IID_IStringable);
    }

    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.ConvertToManaged"/>
    public static global::Windows.Foundation.IStringable? ConvertToManaged(void* value)
    {
        return (global::Windows.Foundation.IStringable?)WindowsRuntimeUnsealedObjectMarshaller.ConvertToManaged<IStringableComWrappersCallback>(value);
    }
}

/// <summary>
/// The <see cref="IWindowsRuntimeUnsealedObjectComWrappersCallback"/> implementation for <see cref="IStringable"/>.
/// </summary>
file abstract class IStringableComWrappersCallback : IWindowsRuntimeUnsealedObjectComWrappersCallback
{
    /// <inheritdoc/>
    public static unsafe bool TryCreateObject(
        void* value,
        ReadOnlySpan<char> runtimeClassName,
        [NotNullWhen(true)] out object? wrapperObject,
        out CreatedWrapperFlags wrapperFlags)
    {
        // Here we match the runtime class name of 'IStringable', as usual for marshalling interfaces (which handles them being
        // returned from anonymous objects implementing them), as well as 'Uri'. The reason for this special handling is that
        // 'System.Uri' is a custom-mapped type, and it doesn't implement 'Windows.Foundation.IStringable'. However, the native
        // 'Windows.Foundation.Uri' type does implement it. This specific issue only happens with this interface and this type.
        // Without this special handling, we'd get 'Windows.Foundation.Uri' as the runtime class name, which would then cause
        // our marshalling infrastructure to return a 'System.Uri' instance, resulting in the following interface cast to fail.
        if (runtimeClassName.SequenceEqual("Windows.Foundation.IStringable") ||
            runtimeClassName.SequenceEqual("Windows.Foundation.Uri"))
        {
            WindowsRuntimeObjectReference valueReference = WindowsRuntimeComWrappersMarshal.CreateObjectReferenceUnsafe(
                externalComObject: value,
                iid: in WellKnownWindowsInterfaceIIDs.IID_IStringable,
                wrapperFlags: out wrapperFlags);

            wrapperObject = new WindowsRuntimeStringable(valueReference);

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
            iid: in WellKnownWindowsInterfaceIIDs.IID_IStringable,
            wrapperFlags: out wrapperFlags);

        return new WindowsRuntimeStringable(valueReference);
    }
}

/// <summary>
/// The implementation of a projected anonymous <see cref="global::Windows.Foundation.IStringable"/> object.
/// </summary>
[WindowsRuntimeManagedOnlyType]
file sealed class WindowsRuntimeStringable : WindowsRuntimeObject,
    global::Windows.Foundation.IStringable,
    IWindowsRuntimeInterface<global::Windows.Foundation.IStringable>
{
    /// <summary>
    /// Creates a <see cref="WindowsRuntimeStringable"/> instance with the specified parameters.
    /// </summary>
    /// <param name="nativeObjectReference">The inner Windows Runtime object reference to wrap in the current instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="nativeObjectReference"/> is <see langword="null"/>.</exception>
    public WindowsRuntimeStringable(WindowsRuntimeObjectReference nativeObjectReference)
        : base(nativeObjectReference)
    {
    }

    /// <inheritdoc/>
    protected internal override bool HasUnwrappableNativeObjectReference => true;

    /// <inheritdoc/>
    public override string ToString()
    {
        return IStringableMethods.ToString(NativeObjectReference);
    }

    /// <inheritdoc/>
    WindowsRuntimeObjectReferenceValue IWindowsRuntimeInterface<global::Windows.Foundation.IStringable>.GetInterface()
    {
        return NativeObjectReference.AsValue();
    }

    /// <inheritdoc/>
    protected override bool IsOverridableInterface(in Guid iid)
    {
        return false;
    }
}

/// <summary>
/// Interop methods for <see cref="global::Windows.Foundation.IStringable"/>.
/// </summary>
public static unsafe class IStringableMethods
{
    /// <see cref="global::Windows.Foundation.IStringable.ToString"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string ToString(WindowsRuntimeObjectReference thisReference)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();

        HSTRING result;

        RestrictedErrorInfo.ThrowExceptionForHR(((IStringableVftbl*)*(void***)thisPtr)->ToString(thisPtr, &result));

        try
        {
            return HStringMarshaller.ConvertToManaged(result);
        }
        finally
        {
            HStringMarshaller.Free(result);
        }
    }
}

/// <summary>
/// Binding type for <see cref="global::Windows.Foundation.IStringable"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct IStringableVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public new delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> ToString;
}

/// <summary>
/// The <see cref="global::Windows.Foundation.IStringable"/> implementation.
/// </summary>
public static unsafe class IStringableImpl
{
    /// <summary>
    /// The <see cref="IStringableVftbl"/> value for the managed <see cref="global::Windows.Foundation.IStringable"/> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly IStringableVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static IStringableImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.ToString = &ToString;
    }

    /// <summary>
    /// Gets a pointer to the managed <see cref="global::Windows.Foundation.IStringable"/> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Vftbl);
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/windows.foundation/nf-windows-foundation-istringable-tostring"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT ToString(void* thisPtr, HSTRING* result)
    {
        if (result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            var thisObject = ComInterfaceDispatch.GetInstance<global::Windows.Foundation.IStringable>((ComInterfaceDispatch*)thisPtr);

            *result = HStringMarshaller.ConvertToUnmanaged(thisObject.ToString());

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}

/// <summary>
/// The <see cref="IDynamicInterfaceCastable"/> implementation for <see cref="global::Windows.Foundation.IStringable"/>.
/// </summary>
[DynamicInterfaceCastableImplementation]
[Guid("96369F54-8EB6-48F0-ABCE-C1B211E627C3")]
file interface IStringableInterfaceImpl : global::Windows.Foundation.IStringable
{
    /// <inheritdoc/>
    string global::Windows.Foundation.IStringable.ToString()
    {
        var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(global::Windows.Foundation.IStringable).TypeHandle);

        return IStringableMethods.ToString(thisReference);
    }
}
#endif
