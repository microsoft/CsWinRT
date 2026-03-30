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

#pragma warning disable IDE0008, IDE1006

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
[assembly: TypeMap<WindowsRuntimeMetadataTypeMapGroup>(
    value: "Windows.Storage.Streams.IBuffer",
    target: typeof(IBuffer),
    trimTarget: typeof(IBuffer))]
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code

[assembly: TypeMapAssociation<DynamicInterfaceCastableImplementationTypeMapGroup>(
    source: typeof(IBuffer),
    proxy: typeof(ABI.Windows.Storage.Streams.IBufferInterfaceImpl))]

namespace ABI.Windows.Storage.Streams;

/// <summary>
/// Marshaller for <see cref="IBuffer"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IBufferMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeObjectMarshaller.ConvertToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(IBuffer? value)
    {
        return WindowsRuntimeInterfaceMarshaller<IBuffer>.ConvertToUnmanaged(value, in WellKnownWindowsInterfaceIIDs.IID_IBuffer);
    }

    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.ConvertToManaged"/>
    public static IBuffer? ConvertToManaged(void* value)
    {
        return (IBuffer?)WindowsRuntimeUnsealedObjectMarshaller.ConvertToManaged<IBufferComWrappersCallback>(value);
    }
}

/// <summary>
/// A custom <see cref="IWindowsRuntimeUnsealedObjectComWrappersCallback"/> implementation for <see cref="IBuffer"/>.
/// </summary>
file abstract unsafe class IBufferComWrappersCallback : IWindowsRuntimeUnsealedObjectComWrappersCallback
{
    /// <inheritdoc/>
    public static bool TryCreateObject(
        void* value,
        ReadOnlySpan<char> runtimeClassName,
        [NotNullWhen(true)] out object? wrapperObject,
        out CreatedWrapperFlags wrapperFlags)
    {
        if (runtimeClassName.SequenceEqual("Windows.Storage.Streams.IBuffer"))
        {
            WindowsRuntimeObjectReference objectReference = WindowsRuntimeComWrappersMarshal.CreateObjectReferenceUnsafe(
                externalComObject: value,
                iid: in WellKnownWindowsInterfaceIIDs.IID_IBuffer,
                wrapperFlags: out wrapperFlags);

            wrapperObject = new global::WindowsRuntime.WindowsRuntimeBuffer(objectReference);

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
            iid: in WellKnownWindowsInterfaceIIDs.IID_IBuffer,
            wrapperFlags: out wrapperFlags);

        return new global::WindowsRuntime.WindowsRuntimeBuffer(objectReference);
    }
}

/// <summary>
/// Interop methods for <see cref="IBuffer"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IBufferMethods
{
    /// <see cref="IBuffer.Capacity"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static uint Capacity(WindowsRuntimeObjectReference thisReference)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        uint result;

        HRESULT hresult = ((IBufferVftbl*)*(void***)thisPtr)->get_Capacity(thisPtr, &result);

        RestrictedErrorInfo.ThrowExceptionForHR(hresult);

        return result;
    }

    /// <see cref="IBuffer.Length"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static uint Length(WindowsRuntimeObjectReference thisReference)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        uint result;

        HRESULT hresult = ((IBufferVftbl*)*(void***)thisPtr)->get_Length(thisPtr, &result);

        RestrictedErrorInfo.ThrowExceptionForHR(hresult);

        return result;
    }

    /// <see cref="IBuffer.Length"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Length(WindowsRuntimeObjectReference thisReference, uint value)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();

        HRESULT hresult = ((IBufferVftbl*)*(void***)thisPtr)->set_Length(thisPtr, value);

        RestrictedErrorInfo.ThrowExceptionForHR(hresult);
    }
}

/// <summary>
/// Binding type for <see cref="IBuffer"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct IBufferVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, uint*, HRESULT> get_Capacity;
    public delegate* unmanaged[MemberFunction]<void*, uint*, HRESULT> get_Length;
    public delegate* unmanaged[MemberFunction]<void*, uint, HRESULT> set_Length;
}

/// <summary>
/// The <see cref="IBuffer"/> implementation.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IBufferImpl
{
    /// <summary>
    /// The <see cref="IBufferVftbl"/> value for the managed <see cref="IBuffer"/> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly IBufferVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static IBufferImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.get_Capacity = &get_Capacity;
        Vftbl.get_Length = &get_Length;
        Vftbl.set_Length = &set_Length;
    }

    /// <summary>
    /// Gets a pointer to the managed <see cref="IBuffer"/> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Vftbl);
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.storage.streams.ibuffer.capacity"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT get_Capacity(void* thisPtr, uint* result)
    {
        if (result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            var thisObject = ComInterfaceDispatch.GetInstance<IBuffer>((ComInterfaceDispatch*)thisPtr);

            *result = thisObject.Capacity;

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.storage.streams.ibuffer.length"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT get_Length(void* thisPtr, uint* result)
    {
        if (result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            var thisObject = ComInterfaceDispatch.GetInstance<IBuffer>((ComInterfaceDispatch*)thisPtr);

            *result = thisObject.Length;

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.storage.streams.ibuffer.length"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT set_Length(void* thisPtr, uint value)
    {
        try
        {
            var thisObject = ComInterfaceDispatch.GetInstance<IBuffer>((ComInterfaceDispatch*)thisPtr);

            thisObject.Length = value;

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}

/// <summary>
/// The <see cref="IDynamicInterfaceCastable"/> implementation for <see cref="IBuffer"/>.
/// </summary>
[DynamicInterfaceCastableImplementation]
[Guid("905A0FE0-BC53-11DF-8C49-001E4FC686DA")]
file interface IBufferInterfaceImpl : IBuffer
{
    /// <inheritdoc/>
    uint IBuffer.Capacity
    {
        get
        {
            var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(IBuffer).TypeHandle);

            return IBufferMethods.Capacity(thisReference);
        }
    }

    /// <inheritdoc/>
    uint IBuffer.Length
    {
        get
        {
            var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(IBuffer).TypeHandle);

            return IBufferMethods.Length(thisReference);
        }
        set
        {
            var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(IBuffer).TypeHandle);

            IBufferMethods.Length(thisReference, value);
        }
    }
}
#endif