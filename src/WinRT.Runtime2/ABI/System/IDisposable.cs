// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
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
    value: "Windows.Foundation.IClosable",
    target: typeof(ABI.System.IDisposable),
    trimTarget: typeof(IDisposable))]
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code

[assembly: TypeMapAssociation<DynamicInterfaceCastableImplementationTypeMapGroup>(
    source: typeof(IDisposable),
    proxy: typeof(ABI.System.IDisposableInterfaceImpl))]

namespace ABI.System;

/// <summary>
/// ABI type for <see cref="global::System.IDisposable"/>.
/// </summary>
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
[WindowsRuntimeMetadataTypeName("Windows.Foundation.IClosable")]
file static class IDisposable;

/// <summary>
/// Marshaller for <see cref="global::System.IDisposable"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IDisposableMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeObjectMarshaller.ConvertToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(global::System.IDisposable? value)
    {
        return WindowsRuntimeInterfaceMarshaller<global::System.IDisposable>.ConvertToUnmanaged(value, in WellKnownWindowsInterfaceIIDs.IID_IClosable);
    }

    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.ConvertToManaged"/>
    public static global::System.IDisposable? ConvertToManaged(void* value)
    {
        return (global::System.IDisposable?)WindowsRuntimeObjectMarshaller.ConvertToManaged(value);
    }
}

/// <summary>
/// Interop methods for <see cref="global::System.IDisposable"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IDisposableMethods
{
    /// <see cref="global::System.IDisposable.Dispose"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Dispose(WindowsRuntimeObjectReference thisReference)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();

        RestrictedErrorInfo.ThrowExceptionForHR(((IDisposableVftbl*)*(void***)thisPtr)->Close(thisPtr));
    }
}

/// <summary>
/// Binding type for <see cref="global::System.IDisposable"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct IDisposableVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, HRESULT> Close;
}

/// <summary>
/// The <see cref="global::System.IDisposable"/> implementation.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IDisposableImpl
{
    /// <summary>
    /// The <see cref="IDisposableVftbl"/> value for the managed <see cref="global::System.IDisposable"/> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly IDisposableVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static IDisposableImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.Close = &Close;
    }

    /// <summary>
    /// Gets a pointer to the managed <see cref="global::System.IDisposable"/> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Vftbl);
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.iclosable.close"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT Close(void* thisPtr)
    {
        try
        {
            ComInterfaceDispatch.GetInstance<global::System.IDisposable>((ComInterfaceDispatch*)thisPtr).Dispose();

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}

/// <summary>
/// The <see cref="IDynamicInterfaceCastable"/> implementation for <see cref="global::System.IDisposable"/>.
/// </summary>
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
[WindowsRuntimeClassName("Windows.Foundation.IClosable")]
[DynamicInterfaceCastableImplementation]
file interface IDisposableInterfaceImpl : global::System.IDisposable
{
    /// <inheritdoc/>
    void global::System.IDisposable.Dispose()
    {
        var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(global::System.IDisposable).TypeHandle);

        IDisposableMethods.Dispose(thisReference);
    }
}