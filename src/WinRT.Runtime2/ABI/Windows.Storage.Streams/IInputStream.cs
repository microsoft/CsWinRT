// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Storage.Streams;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;
using static System.Runtime.InteropServices.ComWrappers;

#pragma warning disable CA2256, IDE0008, IDE1006

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
[assembly: TypeMap<WindowsRuntimeMetadataTypeMapGroup>(
    value: "Windows.Storage.Streams.IInputStream",
    target: typeof(IInputStream),
    trimTarget: typeof(IInputStream))]
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code

[assembly: TypeMapAssociation<DynamicInterfaceCastableImplementationTypeMapGroup>(
    source: typeof(IInputStream),
    proxy: typeof(ABI.Windows.Storage.Streams.IInputStreamInterfaceImpl))]

namespace ABI.Windows.Storage.Streams;

/// <summary>
/// Marshaller for <see cref="IInputStream"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IInputStreamMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeObjectMarshaller.ConvertToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(IInputStream? value)
    {
        return WindowsRuntimeInterfaceMarshaller<IInputStream>.ConvertToUnmanaged(value, in WellKnownWindowsInterfaceIIDs.IID_IInputStream);
    }

    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.ConvertToManaged"/>
    public static IInputStream? ConvertToManaged(void* value)
    {
        return (IInputStream?)WindowsRuntimeUnsealedObjectMarshaller.ConvertToManaged<IInputStreamComWrappersCallback>(value);
    }
}

/// <summary>
/// A custom <see cref="IWindowsRuntimeUnsealedObjectComWrappersCallback"/> implementation for <see cref="IInputStream"/>.
/// </summary>
file abstract unsafe class IInputStreamComWrappersCallback : IWindowsRuntimeUnsealedObjectComWrappersCallback
{
    /// <inheritdoc/>
    public static bool TryCreateObject(
        void* value,
        ReadOnlySpan<char> runtimeClassName,
        [NotNullWhen(true)] out object? wrapperObject,
        out CreatedWrapperFlags wrapperFlags)
    {
        if (runtimeClassName.SequenceEqual("Windows.Storage.Streams.IInputStream"))
        {
            WindowsRuntimeObjectReference objectReference = WindowsRuntimeComWrappersMarshal.CreateObjectReferenceUnsafe(
                externalComObject: value,
                iid: in WellKnownWindowsInterfaceIIDs.IID_IInputStream,
                wrapperFlags: out wrapperFlags);

            wrapperObject = new WindowsRuntimeInputStream(objectReference);

            return true;
        }

        wrapperFlags = CreatedWrapperFlags.None;

        wrapperObject = null;

        return false;
    }

    /// <inheritdoc/>
    public static object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
    {
        WindowsRuntimeObjectReference objectReference = WindowsRuntimeComWrappersMarshal.CreateObjectReferenceUnsafe(
            externalComObject: value,
            iid: in WellKnownWindowsInterfaceIIDs.IID_IInputStream,
            wrapperFlags: out wrapperFlags);

        return new WindowsRuntimeInputStream(objectReference);
    }
}

/// <summary>
/// Interop methods for <see cref="IInputStream"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IInputStreamMethods
{
    /// <see cref="IInputStream.ReadAsync"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(
        WindowsRuntimeObjectReference thisReference,
        IBuffer buffer,
        uint count,
        InputStreamOptions options)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();
        using WindowsRuntimeObjectReferenceValue bufferValue = IBufferMarshaller.ConvertToUnmanaged(buffer);

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        void* result;

        HRESULT hresult = ((IInputStreamVftbl*)*(void***)thisPtr)->ReadAsync(
            thisPtr,
            bufferValue.GetThisPtrUnsafe(),
            count,
            options,
            &result);

        RestrictedErrorInfo.ThrowExceptionForHR(hresult);

        try
        {
            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = nameof(ConvertToManaged))]
            static extern IAsyncOperationWithProgress<IBuffer, uint>? ConvertToManaged(
                [UnsafeAccessorType("ABI.Windows.Foundation.<#CsWinRT>IAsyncOperationWithProgress'2<<#CsWinRT>Windows-Storage-Streams-IBuffer|uint>Marshaller, WinRT.Interop")] object? _,
                void* value);

            return ConvertToManaged(null, result)!;
        }
        finally
        {
            WindowsRuntimeUnknownMarshaller.Free(result);
        }
    }
}

/// <summary>
/// Binding type for <see cref="IInputStream"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct IInputStreamVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, void*, uint, InputStreamOptions, void**, HRESULT> ReadAsync;
}

/// <summary>
/// The <see cref="IInputStream"/> implementation.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IInputStreamImpl
{
    /// <summary>
    /// The <see cref="IInputStreamVftbl"/> value for the managed <see cref="IInputStream"/> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly IInputStreamVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static IInputStreamImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.ReadAsync = &ReadAsync;
    }

    /// <summary>
    /// Gets a pointer to the managed <see cref="IInputStream"/> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Vftbl);
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.storage.streams.iinputstream.readasync"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT ReadAsync(void* thisPtr, void* buffer, uint count, InputStreamOptions options, void** result)
    {
        if (buffer is null || result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            var thisObject = ComInterfaceDispatch.GetInstance<IInputStream>((ComInterfaceDispatch*)thisPtr);

            IAsyncOperationWithProgress<IBuffer, uint> operation = thisObject.ReadAsync(
                buffer: IBufferMarshaller.ConvertToManaged(buffer)!,
                count: count,
                options: options);

            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = nameof(ConvertToUnmanaged))]
            static extern WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(
                [UnsafeAccessorType("ABI.Windows.Foundation.<#CsWinRT>IAsyncOperationWithProgress'2<<#CsWinRT>Windows-Storage-Streams-IBuffer|uint>Marshaller, WinRT.Interop")] object? _,
                IAsyncOperationWithProgress<IBuffer, uint>? value);

            *result = ConvertToUnmanaged(null, operation).DetachThisPtrUnsafe();

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}

/// <summary>
/// The <see cref="IDynamicInterfaceCastable"/> implementation for <see cref="IInputStream"/>.
/// </summary>
[DynamicInterfaceCastableImplementation]
[Guid("905A0FE2-BC53-11DF-8C49-001E4FC686DA")]
file interface IInputStreamInterfaceImpl : IInputStream
{
    /// <inheritdoc/>
    IAsyncOperationWithProgress<IBuffer, uint> IInputStream.ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
    {
        var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(IInputStream).TypeHandle);

        return IInputStreamMethods.ReadAsync(thisReference, buffer, count, options);
    }
}
#endif
