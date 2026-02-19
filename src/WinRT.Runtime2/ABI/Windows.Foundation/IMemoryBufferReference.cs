// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.Foundation;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;
using static System.Runtime.InteropServices.ComWrappers;

#pragma warning disable CA2256, IDE0008, IDE1006

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
[assembly: TypeMap<WindowsRuntimeMetadataTypeMapGroup>(
    value: "Windows.Foundation.IMemoryBufferReference",
    target: typeof(IMemoryBufferReference),
    trimTarget: typeof(IMemoryBufferReference))]
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code

[assembly: TypeMapAssociation<DynamicInterfaceCastableImplementationTypeMapGroup>(
    source: typeof(IMemoryBufferReference),
    proxy: typeof(ABI.Windows.Foundation.IMemoryBufferReferenceInterfaceImpl))]

namespace ABI.Windows.Foundation;

/// <summary>
/// Marshaller for <see cref="IMemoryBufferReference"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IMemoryBufferReferenceMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeObjectMarshaller.ConvertToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(IMemoryBufferReference? value)
    {
        return WindowsRuntimeInterfaceMarshaller<IMemoryBufferReference>.ConvertToUnmanaged(value, in WellKnownWindowsInterfaceIIDs.IID_IMemoryBufferReference);
    }

    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.ConvertToManaged"/>
    public static IMemoryBufferReference? ConvertToManaged(void* value)
    {
        return (IMemoryBufferReference?)WindowsRuntimeUnsealedObjectMarshaller.ConvertToManaged<IMemoryBufferReferenceComWrappersCallback>(value);
    }
}

/// <summary>
/// A custom <see cref="IWindowsRuntimeUnsealedObjectComWrappersCallback"/> implementation for <see cref="IMemoryBufferReference"/>.
/// </summary>
file abstract unsafe class IMemoryBufferReferenceComWrappersCallback : IWindowsRuntimeUnsealedObjectComWrappersCallback
{
    /// <inheritdoc/>
    public static bool TryCreateObject(
        void* value,
        ReadOnlySpan<char> runtimeClassName,
        [NotNullWhen(true)] out object? wrapperObject,
        out CreatedWrapperFlags wrapperFlags)
    {
        if (runtimeClassName.SequenceEqual("Windows.Foundation.IMemoryBufferReference"))
        {
            WindowsRuntimeObjectReference objectReference = WindowsRuntimeComWrappersMarshal.CreateObjectReferenceUnsafe(
                externalComObject: value,
                iid: in WellKnownWindowsInterfaceIIDs.IID_IMemoryBufferReference,
                wrapperFlags: out wrapperFlags);

            wrapperObject = new WindowsRuntimeMemoryBufferReference(objectReference);

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
            iid: in WellKnownWindowsInterfaceIIDs.IID_IMemoryBufferReference,
            wrapperFlags: out wrapperFlags);

        return new WindowsRuntimeMemoryBufferReference(objectReference);
    }
}

/// <summary>
/// Interop methods for <see cref="IMemoryBufferReference"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IMemoryBufferReferenceMethods
{
    /// <summary>
    /// The <see cref="EventSource{T}"/> table for <see cref="IMemoryBufferReference.Closed"/>.
    /// </summary>
    private static ConditionalWeakTable<WindowsRuntimeObject, EventHandlerEventSource<IMemoryBufferReference, object>> ClosedTable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            static ConditionalWeakTable<WindowsRuntimeObject, EventHandlerEventSource<IMemoryBufferReference, object>> MakeClosedTable()
            {
                _ = Interlocked.CompareExchange(ref field, [], null);

                return Volatile.Read(in field);
            }

            return Volatile.Read(in field) ?? MakeClosedTable();
        }
    }

    /// <see cref="IMemoryBufferReference.Capacity"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static uint Capacity(WindowsRuntimeObjectReference thisReference)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        uint result;

        HRESULT hresult = ((IMemoryBufferReferenceVftbl*)*(void***)thisPtr)->get_Capacity(thisPtr, &result);

        RestrictedErrorInfo.ThrowExceptionForHR(hresult);

        return result;
    }

    /// <see cref="IMemoryBufferReference.Closed"/>
    public static EventHandlerEventSource<IMemoryBufferReference, object> Closed(WindowsRuntimeObject thisObject, WindowsRuntimeObjectReference thisReference)
    {
        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
        [return: UnsafeAccessorType("ABI.WindowsRuntime.InteropServices.<#CsWinRT>EventHandlerEventSource'1<<CsWinRT>Windows-Foundation-IMemoryBufferReference|object>, WinRT.Interop")]
        static extern object ctor(WindowsRuntimeObjectReference nativeObjectReference, int index);

        return ClosedTable.GetOrAdd(
            key: thisObject,
            valueFactory: static (_, thisReference) => Unsafe.As<EventHandlerEventSource<IMemoryBufferReference, object>>(ctor(thisReference, 7)),
            factoryArgument: thisReference);
    }
}

/// <summary>
/// Binding type for <see cref="IMemoryBufferReference"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct IMemoryBufferReferenceVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, uint*, HRESULT> get_Capacity;
    public delegate* unmanaged[MemberFunction]<void*, void*, EventRegistrationToken*, HRESULT> add_Closed;
    public delegate* unmanaged[MemberFunction]<void*, EventRegistrationToken, HRESULT> remove_Closed;
}

/// <summary>
/// The <see cref="IMemoryBufferReference"/> implementation.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IMemoryBufferReferenceImpl
{
    /// <summary>
    /// The <see cref="IMemoryBufferReferenceVftbl"/> value for the managed <see cref="IMemoryBufferReference"/> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly IMemoryBufferReferenceVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static IMemoryBufferReferenceImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.get_Capacity = &get_Capacity;
        Vftbl.add_Closed = &add_Closed;
        Vftbl.remove_Closed = &remove_Closed;
    }

    /// <summary>
    /// Gets a pointer to the managed <see cref="IMemoryBufferReference"/> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Vftbl);
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.imemorybufferreference.capacity"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT get_Capacity(void* thisPtr, uint* result)
    {
        if (result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            var thisObject = ComInterfaceDispatch.GetInstance<IMemoryBufferReference>((ComInterfaceDispatch*)thisPtr);

            *result = thisObject.Capacity;

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <summary>
    /// The <see cref="EventRegistrationTokenTable{T}"/> table for <see cref="IMemoryBufferReference.Closed"/>.
    /// </summary>
    private static ConditionalWeakTable<IMemoryBufferReference, EventRegistrationTokenTable<EventHandler<IMemoryBufferReference, object>>> ClosedTable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            static ConditionalWeakTable<IMemoryBufferReference, EventRegistrationTokenTable<EventHandler<IMemoryBufferReference, object>>> MakeClosedTable()
            {
                _ = Interlocked.CompareExchange(ref field, [], null);

                return Volatile.Read(in field);
            }

            return Volatile.Read(in field) ?? MakeClosedTable();
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.imemorybufferreference.closed"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT add_Closed(void* thisPtr, void* handler, EventRegistrationToken* token)
    {
        try
        {
            var thisObject = ComInterfaceDispatch.GetInstance<IMemoryBufferReference>((ComInterfaceDispatch*)thisPtr);

            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = nameof(ConvertToManaged))]
            static extern EventHandler<IMemoryBufferReference, object>? ConvertToManaged(
                [UnsafeAccessorType("ABI.System.<#corlib>EventHandler'2<ABI.Windows.Foundation.IMemoryBufferReference|object>Marshaller, WinRT.Interop")] object? _,
                void* value);

            EventHandler<IMemoryBufferReference, object>? managedHandler = ConvertToManaged(null, handler);

            *token = ClosedTable.GetOrCreateValue(thisObject).AddEventHandler(managedHandler);

            thisObject.Closed += managedHandler;

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return e.HResult;
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.imemorybufferreference.closed"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT remove_Closed(void* thisPtr, EventRegistrationToken token)
    {
        try
        {
            var thisObject = ComInterfaceDispatch.GetInstance<IMemoryBufferReference>((ComInterfaceDispatch*)thisPtr);

            if (thisObject is not null && ClosedTable.TryGetValue(thisObject, out var table) && table.RemoveEventHandler(token, out EventHandler<IMemoryBufferReference, object>? managedHandler))
            {
                thisObject.Closed -= managedHandler;
            }

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return e.HResult;
        }
    }
}

/// <summary>
/// The <see cref="IDynamicInterfaceCastable"/> implementation for <see cref="IMemoryBufferReference"/>.
/// </summary>
[DynamicInterfaceCastableImplementation]
[Guid("FBC4DD29-245B-11E4-AF98-689423260CF8")]
file interface IMemoryBufferReferenceInterfaceImpl : IMemoryBufferReference
{
    /// <inheritdoc/>
    uint IMemoryBufferReference.Capacity
    {
        get
        {
            var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(IMemoryBufferReference).TypeHandle);

            return IMemoryBufferReferenceMethods.Capacity(thisReference);
        }
    }

    /// <inheritdoc/>
    event EventHandler<IMemoryBufferReference, object>? IMemoryBufferReference.Closed
    {
        add
        {
            var thisObject = (WindowsRuntimeObject)this;
            var thisReference = thisObject.GetObjectReferenceForInterface(typeof(IMemoryBufferReference).TypeHandle);

            IMemoryBufferReferenceMethods.Closed((WindowsRuntimeObject)this, thisReference).Subscribe(value);
        }
        remove
        {
            var thisObject = (WindowsRuntimeObject)this;
            var thisReference = thisObject.GetObjectReferenceForInterface(typeof(IMemoryBufferReference).TypeHandle);

            IMemoryBufferReferenceMethods.Closed(thisObject, thisReference).Unsubscribe(value);
        }
    }
}