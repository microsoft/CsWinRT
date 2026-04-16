// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
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
    value: "Windows.Storage.Streams.IOutputStream",
    target: typeof(IOutputStream),
    trimTarget: typeof(IOutputStream))]
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code

[assembly: TypeMapAssociation<DynamicInterfaceCastableImplementationTypeMapGroup>(
    source: typeof(IOutputStream),
    proxy: typeof(ABI.Windows.Storage.Streams.IOutputStreamInterfaceImpl))]

namespace ABI.Windows.Storage.Streams;

/// <summary>
/// Marshaller for <see cref="IOutputStream"/>.
/// </summary>
public static unsafe class IOutputStreamMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeObjectMarshaller.ConvertToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(IOutputStream? value)
    {
        return WindowsRuntimeInterfaceMarshaller<IOutputStream>.ConvertToUnmanaged(value, in WellKnownWindowsInterfaceIIDs.IID_IOutputStream);
    }

    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.ConvertToManaged"/>
    public static IOutputStream? ConvertToManaged(void* value)
    {
        return (IOutputStream?)WindowsRuntimeUnsealedObjectMarshaller.ConvertToManaged<IOutputStreamComWrappersCallback>(value);
    }
}

/// <summary>
/// A custom <see cref="IWindowsRuntimeUnsealedObjectComWrappersCallback"/> implementation for <see cref="IOutputStream"/>.
/// </summary>
file abstract unsafe class IOutputStreamComWrappersCallback : IWindowsRuntimeUnsealedObjectComWrappersCallback
{
    /// <inheritdoc/>
    public static bool TryCreateObject(
        void* value,
        ReadOnlySpan<char> runtimeClassName,
        [NotNullWhen(true)] out object? wrapperObject,
        out CreatedWrapperFlags wrapperFlags)
    {
        if (runtimeClassName.SequenceEqual("Windows.Storage.Streams.IOutputStream"))
        {
            WindowsRuntimeObjectReference objectReference = WindowsRuntimeComWrappersMarshal.CreateObjectReferenceUnsafe(
                externalComObject: value,
                iid: in WellKnownWindowsInterfaceIIDs.IID_IOutputStream,
                wrapperFlags: out wrapperFlags);

            wrapperObject = new WindowsRuntimeOutputStream(objectReference);

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
            iid: in WellKnownWindowsInterfaceIIDs.IID_IOutputStream,
            wrapperFlags: out wrapperFlags);

        return new WindowsRuntimeOutputStream(objectReference);
    }
}

/// <summary>
/// Interop methods for <see cref="IOutputStream"/>.
/// </summary>
public static unsafe class IOutputStreamMethods
{
    /// <see cref="IOutputStream.WriteAsync"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IAsyncOperationWithProgress<uint, uint> WriteAsync(WindowsRuntimeObjectReference thisReference, IBuffer buffer)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();
        using WindowsRuntimeObjectReferenceValue bufferValue = IBufferMarshaller.ConvertToUnmanaged(buffer);

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        void* result;

        HRESULT hresult = ((IOutputStreamVftbl*)*(void***)thisPtr)->WriteAsync(
            thisPtr,
            bufferValue.GetThisPtrUnsafe(),
            &result);

        RestrictedErrorInfo.ThrowExceptionForHR(hresult);

        try
        {
            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = nameof(ConvertToManaged))]
            static extern IAsyncOperationWithProgress<uint, uint>? ConvertToManaged(
                [UnsafeAccessorType("ABI.Windows.Foundation.<#CsWinRT>IAsyncOperationWithProgress'2<uint|uint>Marshaller, WinRT.Interop")] object? _,
                void* value);

            return ConvertToManaged(null, result)!;
        }
        finally
        {
            WindowsRuntimeUnknownMarshaller.Free(result);
        }
    }

    /// <see cref="IOutputStream.FlushAsync"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IAsyncOperation<bool> FlushAsync(WindowsRuntimeObjectReference thisReference)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        void* result;

        HRESULT hresult = ((IOutputStreamVftbl*)*(void***)thisPtr)->FlushAsync(thisPtr, &result);

        RestrictedErrorInfo.ThrowExceptionForHR(hresult);

        try
        {
            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = nameof(ConvertToManaged))]
            static extern IAsyncOperation<bool>? ConvertToManaged(
                [UnsafeAccessorType("ABI.Windows.Foundation.<#CsWinRT>IAsyncOperation'1<bool>Marshaller, WinRT.Interop")] object? _,
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
/// Binding type for <see cref="IOutputStream"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct IOutputStreamVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, void*, void**, HRESULT> WriteAsync;
    public delegate* unmanaged[MemberFunction]<void*, void**, HRESULT> FlushAsync;
}

/// <summary>
/// The <see cref="IOutputStream"/> implementation.
/// </summary>
public static unsafe class IOutputStreamImpl
{
    /// <summary>
    /// The <see cref="IOutputStreamVftbl"/> value for the managed <see cref="IOutputStream"/> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly IOutputStreamVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static IOutputStreamImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.WriteAsync = &WriteAsync;
        Vftbl.FlushAsync = &FlushAsync;
    }

    /// <summary>
    /// Gets a pointer to the managed <see cref="IOutputStream"/> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Vftbl);
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.storage.streams.ioutputstream.writeasync"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT WriteAsync(void* thisPtr, void* buffer, void** result)
    {
        if (buffer is null || result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            var thisObject = ComInterfaceDispatch.GetInstance<IOutputStream>((ComInterfaceDispatch*)thisPtr);

            IAsyncOperationWithProgress<uint, uint> operation = thisObject.WriteAsync(IBufferMarshaller.ConvertToManaged(buffer)!);

            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = nameof(ConvertToUnmanaged))]
            static extern WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(
                [UnsafeAccessorType("ABI.Windows.Foundation.<#CsWinRT>IAsyncOperationWithProgress'2<uint|uint>Marshaller, WinRT.Interop")] object? _,
                IAsyncOperationWithProgress<uint, uint>? value);

            *result = ConvertToUnmanaged(null, operation).DetachThisPtrUnsafe();

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.storage.streams.IOutputStream.flushasync"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT FlushAsync(void* thisPtr, void** result)
    {
        if (result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            var thisObject = ComInterfaceDispatch.GetInstance<IOutputStream>((ComInterfaceDispatch*)thisPtr);

            IAsyncOperation<bool> operation = thisObject.FlushAsync();

            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = nameof(ConvertToUnmanaged))]
            static extern WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(
                [UnsafeAccessorType("ABI.Windows.Foundation.<#CsWinRT>IAsyncOperation'1<bool>Marshaller, WinRT.Interop")] object? _,
                IAsyncOperation<bool>? value);

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
/// The <see cref="IDynamicInterfaceCastable"/> implementation for <see cref="IOutputStream"/>.
/// </summary>
[DynamicInterfaceCastableImplementation]
[Guid("905A0FE6-BC53-11DF-8C49-001E4FC686DA")]
file interface IOutputStreamInterfaceImpl : IOutputStream
{
    /// <inheritdoc/>
    IAsyncOperationWithProgress<uint, uint> IOutputStream.WriteAsync(IBuffer buffer)
    {
        var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(IOutputStream).TypeHandle);

        return IOutputStreamMethods.WriteAsync(thisReference, buffer);
    }

    /// <inheritdoc/>
    IAsyncOperation<bool> IOutputStream.FlushAsync()
    {
        var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(IOutputStream).TypeHandle);

        return IOutputStreamMethods.FlushAsync(thisReference);
    }
}