// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
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
    value: "Windows.Storage.Streams.IRandomAccessStream",
    target: typeof(IRandomAccessStream),
    trimTarget: typeof(IRandomAccessStream))]
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code

[assembly: TypeMapAssociation<DynamicInterfaceCastableImplementationTypeMapGroup>(
    source: typeof(IRandomAccessStream),
    proxy: typeof(ABI.Windows.Storage.Streams.IRandomAccessStreamInterfaceImpl))]

namespace ABI.Windows.Storage.Streams;

/// <summary>
/// Marshaller for <see cref="IRandomAccessStream"/>.
/// </summary>
public static unsafe class IRandomAccessStreamMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeObjectMarshaller.ConvertToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(IRandomAccessStream? value)
    {
        return WindowsRuntimeInterfaceMarshaller<IRandomAccessStream>.ConvertToUnmanaged(value, in WellKnownWindowsInterfaceIIDs.IID_IRandomAccessStream);
    }

    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.ConvertToManaged"/>
    public static IRandomAccessStream? ConvertToManaged(void* value)
    {
        return (IRandomAccessStream?)WindowsRuntimeUnsealedObjectMarshaller.ConvertToManaged<IRandomAccessStreamComWrappersCallback>(value);
    }
}

/// <summary>
/// A custom <see cref="IWindowsRuntimeUnsealedObjectComWrappersCallback"/> implementation for <see cref="IRandomAccessStream"/>.
/// </summary>
file abstract unsafe class IRandomAccessStreamComWrappersCallback : IWindowsRuntimeUnsealedObjectComWrappersCallback
{
    /// <inheritdoc/>
    public static bool TryCreateObject(
        void* value,
        ReadOnlySpan<char> runtimeClassName,
        [NotNullWhen(true)] out object? wrapperObject,
        out CreatedWrapperFlags wrapperFlags)
    {
        if (runtimeClassName.SequenceEqual("Windows.Storage.Streams.IRandomAccessStream"))
        {
            WindowsRuntimeObjectReference objectReference = WindowsRuntimeComWrappersMarshal.CreateObjectReferenceUnsafe(
                externalComObject: value,
                iid: in WellKnownWindowsInterfaceIIDs.IID_IRandomAccessStream,
                wrapperFlags: out wrapperFlags);

            wrapperObject = new WindowsRuntimeRandomAccessStream(objectReference);

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
            iid: in WellKnownWindowsInterfaceIIDs.IID_IRandomAccessStream,
            wrapperFlags: out wrapperFlags);

        return new WindowsRuntimeRandomAccessStream(objectReference);
    }
}

/// <summary>
/// Interop methods for <see cref="IRandomAccessStream"/>.
/// </summary>
public static unsafe class IRandomAccessStreamMethods
{
    /// <see cref="IRandomAccessStream.CanRead"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool CanRead(WindowsRuntimeObjectReference thisReference)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        bool result;

        HRESULT hresult = ((IRandomAccessStreamVftbl*)*(void***)thisPtr)->get_CanRead(thisPtr, &result);

        RestrictedErrorInfo.ThrowExceptionForHR(hresult);

        return result;
    }

    /// <see cref="IRandomAccessStream.CanWrite"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool CanWrite(WindowsRuntimeObjectReference thisReference)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        bool result;

        HRESULT hresult = ((IRandomAccessStreamVftbl*)*(void***)thisPtr)->get_CanWrite(thisPtr, &result);

        RestrictedErrorInfo.ThrowExceptionForHR(hresult);

        return result;
    }

    /// <see cref="IRandomAccessStream.Position"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ulong Position(WindowsRuntimeObjectReference thisReference)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        ulong result;

        HRESULT hresult = ((IRandomAccessStreamVftbl*)*(void***)thisPtr)->get_Position(thisPtr, &result);

        RestrictedErrorInfo.ThrowExceptionForHR(hresult);

        return result;
    }

    /// <see cref="IRandomAccessStream.Size"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ulong Size(WindowsRuntimeObjectReference thisReference)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        ulong result;

        HRESULT hresult = ((IRandomAccessStreamVftbl*)*(void***)thisPtr)->get_Size(thisPtr, &result);

        RestrictedErrorInfo.ThrowExceptionForHR(hresult);

        return result;
    }

    /// <see cref="IRandomAccessStream.Size"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Size(WindowsRuntimeObjectReference thisReference, ulong value)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();

        HRESULT hresult = ((IRandomAccessStreamVftbl*)*(void***)thisPtr)->put_Size(thisPtr, value);

        RestrictedErrorInfo.ThrowExceptionForHR(hresult);
    }

    /// <see cref="IRandomAccessStream.GetInputStreamAt"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IInputStream GetInputStreamAt(WindowsRuntimeObjectReference thisReference, ulong position)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        void* result;

        HRESULT hresult = ((IRandomAccessStreamVftbl*)*(void***)thisPtr)->GetInputStreamAt(
            thisPtr,
            position,
            &result);

        RestrictedErrorInfo.ThrowExceptionForHR(hresult);

        try
        {
            return IInputStreamMarshaller.ConvertToManaged(result)!;
        }
        finally
        {
            WindowsRuntimeUnknownMarshaller.Free(result);
        }
    }

    /// <see cref="IRandomAccessStream.GetOutputStreamAt"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IOutputStream GetOutputStreamAt(WindowsRuntimeObjectReference thisReference, ulong position)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        void* result;

        HRESULT hresult = ((IRandomAccessStreamVftbl*)*(void***)thisPtr)->GetOutputStreamAt(
            thisPtr,
            position,
            &result);

        RestrictedErrorInfo.ThrowExceptionForHR(hresult);

        try
        {
            return IOutputStreamMarshaller.ConvertToManaged(result)!;
        }
        finally
        {
            WindowsRuntimeUnknownMarshaller.Free(result);
        }
    }

    /// <see cref="IRandomAccessStream.Seek"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Seek(WindowsRuntimeObjectReference thisReference, ulong position)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();

        HRESULT hresult = ((IRandomAccessStreamVftbl*)*(void***)thisPtr)->Seek(thisPtr, position);

        RestrictedErrorInfo.ThrowExceptionForHR(hresult);
    }

    /// <see cref="IRandomAccessStream.CloneStream"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IRandomAccessStream CloneStream(WindowsRuntimeObjectReference thisReference)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        void* result;

        HRESULT hresult = ((IRandomAccessStreamVftbl*)*(void***)thisPtr)->CloneStream(thisPtr, &result);

        RestrictedErrorInfo.ThrowExceptionForHR(hresult);

        try
        {
            return IRandomAccessStreamMarshaller.ConvertToManaged(result)!;
        }
        finally
        {
            WindowsRuntimeUnknownMarshaller.Free(result);
        }
    }
}

/// <summary>
/// Binding type for <see cref="IRandomAccessStream"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct IRandomAccessStreamVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, ulong*, HRESULT> get_Size;
    public delegate* unmanaged[MemberFunction]<void*, ulong, HRESULT> put_Size;
    public delegate* unmanaged[MemberFunction]<void*, ulong, void**, HRESULT> GetInputStreamAt;
    public delegate* unmanaged[MemberFunction]<void*, ulong, void**, HRESULT> GetOutputStreamAt;
    public delegate* unmanaged[MemberFunction]<void*, ulong*, HRESULT> get_Position;
    public delegate* unmanaged[MemberFunction]<void*, ulong, HRESULT> Seek;
    public delegate* unmanaged[MemberFunction]<void*, void**, HRESULT> CloneStream;
    public delegate* unmanaged[MemberFunction]<void*, bool*, HRESULT> get_CanRead;
    public delegate* unmanaged[MemberFunction]<void*, bool*, HRESULT> get_CanWrite;
}

/// <summary>
/// The <see cref="IRandomAccessStream"/> implementation.
/// </summary>
public static unsafe class IRandomAccessStreamImpl
{
    /// <summary>
    /// The <see cref="IRandomAccessStreamVftbl"/> value for the managed <see cref="IRandomAccessStream"/> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly IRandomAccessStreamVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static IRandomAccessStreamImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.get_Size = &get_Size;
        Vftbl.put_Size = &put_Size;
        Vftbl.GetInputStreamAt = &GetInputStreamAt;
        Vftbl.GetOutputStreamAt = &GetOutputStreamAt;
        Vftbl.get_Position = &get_Position;
        Vftbl.Seek = &Seek;
        Vftbl.CloneStream = &CloneStream;
        Vftbl.get_CanRead = &get_CanRead;
        Vftbl.get_CanWrite = &get_CanWrite;
    }

    /// <summary>
    /// Gets a pointer to the managed <see cref="IRandomAccessStream"/> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Vftbl);
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.storage.streams.irandomaccessstream.size"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT get_Size(void* thisPtr, ulong* result)
    {
        if (result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            var thisObject = ComInterfaceDispatch.GetInstance<IRandomAccessStream>((ComInterfaceDispatch*)thisPtr);

            *result = thisObject.Size;

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.storage.streams.irandomaccessstream.size"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT put_Size(void* thisPtr, ulong value)
    {
        try
        {
            var thisObject = ComInterfaceDispatch.GetInstance<IRandomAccessStream>((ComInterfaceDispatch*)thisPtr);

            thisObject.Size = value;

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.storage.streams.irandomaccessstream.getinputstreamat"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT GetInputStreamAt(void* thisPtr, ulong position, void** result)
    {
        if (result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            var thisObject = ComInterfaceDispatch.GetInstance<IRandomAccessStream>((ComInterfaceDispatch*)thisPtr);

            IInputStream inputStream = thisObject.GetInputStreamAt(position);

            *result = IInputStreamMarshaller.ConvertToUnmanaged(inputStream).DetachThisPtrUnsafe();

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.storage.streams.irandomaccessstream.getoutputstreamat"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT GetOutputStreamAt(void* thisPtr, ulong position, void** result)
    {
        if (result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            var thisObject = ComInterfaceDispatch.GetInstance<IRandomAccessStream>((ComInterfaceDispatch*)thisPtr);

            IOutputStream outputStream = thisObject.GetOutputStreamAt(position);

            *result = IOutputStreamMarshaller.ConvertToUnmanaged(outputStream).DetachThisPtrUnsafe();

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.storage.streams.irandomaccessstream.position"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT get_Position(void* thisPtr, ulong* result)
    {
        if (result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            var thisObject = ComInterfaceDispatch.GetInstance<IRandomAccessStream>((ComInterfaceDispatch*)thisPtr);

            *result = thisObject.Position;

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.storage.streams.irandomaccessstream.seek"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT Seek(void* thisPtr, ulong position)
    {
        try
        {
            var thisObject = ComInterfaceDispatch.GetInstance<IRandomAccessStream>((ComInterfaceDispatch*)thisPtr);

            thisObject.Seek(position);

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.storage.streams.irandomaccessstream.clonestream"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT CloneStream(void* thisPtr, void** result)
    {
        if (result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            var thisObject = ComInterfaceDispatch.GetInstance<IRandomAccessStream>((ComInterfaceDispatch*)thisPtr);

            IRandomAccessStream clonedStream = thisObject.CloneStream();

            *result = IRandomAccessStreamMarshaller.ConvertToUnmanaged(clonedStream).DetachThisPtrUnsafe();

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.storage.streams.irandomaccessstream.canread"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT get_CanRead(void* thisPtr, bool* result)
    {
        if (result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            var thisObject = ComInterfaceDispatch.GetInstance<IRandomAccessStream>((ComInterfaceDispatch*)thisPtr);

            *result = thisObject.CanRead;

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.storage.streams.irandomaccessstream.canwrite"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT get_CanWrite(void* thisPtr, bool* result)
    {
        if (result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            var thisObject = ComInterfaceDispatch.GetInstance<IRandomAccessStream>((ComInterfaceDispatch*)thisPtr);

            *result = thisObject.CanWrite;

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}

/// <summary>
/// The <see cref="IDynamicInterfaceCastable"/> implementation for <see cref="IRandomAccessStream"/>.
/// </summary>
[DynamicInterfaceCastableImplementation]
[Guid("905A0FE1-BC53-11DF-8C49-001E4FC686DA")]
file interface IRandomAccessStreamInterfaceImpl : IRandomAccessStream
{
    /// <inheritdoc/>
    bool IRandomAccessStream.CanRead
    {
        get
        {
            var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(IRandomAccessStream).TypeHandle);

            return IRandomAccessStreamMethods.CanRead(thisReference);
        }
    }

    /// <inheritdoc/>
    bool IRandomAccessStream.CanWrite
    {
        get
        {
            var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(IRandomAccessStream).TypeHandle);

            return IRandomAccessStreamMethods.CanWrite(thisReference);
        }
    }

    /// <inheritdoc/>
    ulong IRandomAccessStream.Position
    {
        get
        {
            var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(IRandomAccessStream).TypeHandle);

            return IRandomAccessStreamMethods.Position(thisReference);
        }
    }

    /// <inheritdoc/>
    ulong IRandomAccessStream.Size
    {
        get
        {
            var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(IRandomAccessStream).TypeHandle);

            return IRandomAccessStreamMethods.Size(thisReference);
        }
        set
        {
            var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(IRandomAccessStream).TypeHandle);

            IRandomAccessStreamMethods.Size(thisReference, value);
        }
    }

    /// <inheritdoc/>
    IInputStream IRandomAccessStream.GetInputStreamAt(ulong position)
    {
        var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(IRandomAccessStream).TypeHandle);

        return IRandomAccessStreamMethods.GetInputStreamAt(thisReference, position);
    }

    /// <inheritdoc/>
    IOutputStream IRandomAccessStream.GetOutputStreamAt(ulong position)
    {
        var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(IRandomAccessStream).TypeHandle);

        return IRandomAccessStreamMethods.GetOutputStreamAt(thisReference, position);
    }

    /// <inheritdoc/>
    void IRandomAccessStream.Seek(ulong position)
    {
        var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(IRandomAccessStream).TypeHandle);

        IRandomAccessStreamMethods.Seek(thisReference, position);
    }

    /// <inheritdoc/>
    IRandomAccessStream IRandomAccessStream.CloneStream()
    {
        var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(IRandomAccessStream).TypeHandle);

        return IRandomAccessStreamMethods.CloneStream(thisReference);
    }
}
#endif
