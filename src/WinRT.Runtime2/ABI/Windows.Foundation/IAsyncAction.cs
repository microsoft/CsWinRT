// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Foundation;
using WindowsRuntime;
using WindowsRuntime.AsyncInfo;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;
using static System.Runtime.InteropServices.ComWrappers;

#pragma warning disable IDE0008, IDE1006, CA2256

[assembly: TypeMapAssociation<DynamicInterfaceCastableImplementationTypeMapGroup>(
    source: typeof(IAsyncAction),
    proxy: typeof(ABI.Windows.Foundation.IAsyncActionInterfaceImpl))]

namespace ABI.Windows.Foundation;

/// <summary>
/// Marshaller for <see cref="IAsyncAction"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage, DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IAsyncActionMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeObjectMarshaller.ConvertToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(IAsyncAction? value)
    {
        return WindowsRuntimeInterfaceMarshaller<IAsyncAction>.ConvertToUnmanaged(value, in WellKnownInterfaceIds.IID_IAsyncAction);
    }

    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.ConvertToManaged"/>
    public static IAsyncAction? ConvertToManaged(void* value)
    {
        return (IAsyncAction?)WindowsRuntimeUnsealedObjectMarshaller.ConvertToManaged<IAsyncActionComWrappersCallback>(value);
    }
}

/// <summary>
/// A custom <see cref="IWindowsRuntimeUnsealedObjectComWrappersCallback"/> implementation for <see cref="IAsyncAction"/>.
/// </summary>
file abstract unsafe class IAsyncActionComWrappersCallback : IWindowsRuntimeUnsealedObjectComWrappersCallback
{
    /// <inheritdoc/>
    public static unsafe bool TryCreateObject(
        void* value,
        ReadOnlySpan<char> runtimeClassName,
        [NotNullWhen(true)] out object? wrapperObject,
        out CreatedWrapperFlags wrapperFlags)
    {
        if (runtimeClassName.SequenceEqual("Windows.Foundation.IAsyncAction"))
        {
            WindowsRuntimeObjectReference objectReference = WindowsRuntimeObjectReference.CreateUnsafe(value, in WellKnownInterfaceIds.IID_IAsyncAction)!;

            wrapperFlags = objectReference.GetReferenceTrackerPtrUnsafe() == null ? CreatedWrapperFlags.None : CreatedWrapperFlags.TrackerObject;

            wrapperObject = new WindowsRuntimeAsyncAction(objectReference);

            return true;
        }

        wrapperFlags = CreatedWrapperFlags.None;

        wrapperObject = null;

        return false;
    }
}

/// <summary>
/// A custom <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> implementation for <see cref="IAsyncAction"/>.
/// </summary>
internal sealed unsafe class IAsyncActionComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
{
    /// <inheritdoc/>
    public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
    {
        WindowsRuntimeObjectReference objectReference = WindowsRuntimeObjectReference.Create(value, in WellKnownInterfaceIds.IID_IAsyncAction)!;

        wrapperFlags = objectReference.GetReferenceTrackerPtrUnsafe() == null ? CreatedWrapperFlags.None : CreatedWrapperFlags.TrackerObject;

        return new WindowsRuntimeAsyncAction(objectReference);
    }
}

/// <summary>
/// Interop methods for <see cref="IAsyncAction"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage, DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IAsyncActionMethods
{
    /// <see cref="IAsyncAction.Completed"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static AsyncActionCompletedHandler? Completed(WindowsRuntimeObjectReference thisReference)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        void* result;

        HRESULT hresult = ((IAsyncActionVftbl*)*(void***)thisPtr)->get_Completed(thisPtr, &result);

        RestrictedErrorInfo.ThrowExceptionForHR(hresult);

        try
        {
            return AsyncActionCompletedHandlerMarshaller.ConvertToManaged(result);
        }
        finally
        {
            WindowsRuntimeObjectMarshaller.Free(result);
        }
    }

    /// <see cref="IAsyncAction.Completed"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Completed(WindowsRuntimeObjectReference thisReference, AsyncActionCompletedHandler? handler)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();
        using WindowsRuntimeObjectReferenceValue handlerValue = AsyncActionCompletedHandlerMarshaller.ConvertToUnmanaged(handler);

        void* thisPtr = thisValue.GetThisPtrUnsafe();

        HRESULT hresult = ((IAsyncActionVftbl*)*(void***)thisPtr)->set_Completed(thisPtr, handlerValue.GetThisPtrUnsafe());

        RestrictedErrorInfo.ThrowExceptionForHR(hresult);
    }

    /// <see cref="IAsyncAction.GetResults"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void GetResults(WindowsRuntimeObjectReference thisReference)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();

        HRESULT hresult = ((IAsyncActionVftbl*)*(void***)thisPtr)->GetResults(thisPtr);

        RestrictedErrorInfo.ThrowExceptionForHR(hresult);
    }
}

/// <summary>
/// Binding type for <see cref="IAsyncAction"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct IAsyncActionVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, void**, HRESULT> get_Completed;
    public delegate* unmanaged[MemberFunction]<void*, void*, HRESULT> set_Completed;
    public delegate* unmanaged[MemberFunction]<void*, HRESULT> GetResults;
}

/// <summary>
/// The <see cref="IAsyncAction"/> implementation.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage, DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IAsyncActionImpl
{
    /// <summary>
    /// The <see cref="IAsyncActionVftbl"/> value for the managed <see cref="IAsyncAction"/> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly IAsyncActionVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static IAsyncActionImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.get_Completed = &get_Completed;
        Vftbl.set_Completed = &set_Completed;
        Vftbl.GetResults = &GetResults;
    }

    /// <summary>
    /// Gets the IID for <see cref="IAsyncAction"/>.
    /// </summary>
    public static ref readonly Guid IID
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref WellKnownInterfaceIds.IID_IAsyncAction;
    }

    /// <summary>
    /// Gets a pointer to the managed <see cref="IAsyncAction"/> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Vftbl);
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.iasyncaction.completed"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT get_Completed(void* thisPtr, void** handler)
    {
        if (handler is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            var unboxedValue = ComInterfaceDispatch.GetInstance<IAsyncAction>((ComInterfaceDispatch*)thisPtr);

            *handler = AsyncActionCompletedHandlerMarshaller.ConvertToUnmanaged(unboxedValue.Completed).DetachThisPtrUnsafe();

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.iasyncaction.completed"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT set_Completed(void* thisPtr, void* handler)
    {
        try
        {
            var unboxedValue = ComInterfaceDispatch.GetInstance<IAsyncAction>((ComInterfaceDispatch*)thisPtr);

            unboxedValue.Completed = AsyncActionCompletedHandlerMarshaller.ConvertToManaged(handler);

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.iasyncaction.getresults"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT GetResults(void* thisPtr)
    {
        try
        {
            var unboxedValue = ComInterfaceDispatch.GetInstance<IAsyncAction>((ComInterfaceDispatch*)thisPtr);

            unboxedValue.GetResults();

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}

/// <summary>
/// The <see cref="IDynamicInterfaceCastable"/> implementation for <see cref="IAsyncAction"/>.
/// </summary>
[DynamicInterfaceCastableImplementation]
file interface IAsyncActionInterfaceImpl : IAsyncAction
{
    /// <inheritdoc/>
    AsyncActionCompletedHandler? IAsyncAction.Completed
    {
        get
        {
            var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(IAsyncAction).TypeHandle);

            return IAsyncActionMethods.Completed(thisReference);
        }
        set
        {
            var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(IAsyncAction).TypeHandle);

            IAsyncActionMethods.Completed(thisReference, value);
        }
    }

    /// <inheritdoc/>
    void IAsyncAction.GetResults()
    {
        var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(IAsyncAction).TypeHandle);

        IAsyncActionMethods.GetResults(thisReference);
    }
}
