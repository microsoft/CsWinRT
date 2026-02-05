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
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
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
        return (global::Windows.Foundation.IStringable?)WindowsRuntimeObjectMarshaller.ConvertToManaged(value);
    }
}

/// <summary>
/// Interop methods for <see cref="global::Windows.Foundation.IStringable"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IStringableMethods
{
    /// <see cref="global::Windows.Foundation.IStringable.ToString"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string? ToString(WindowsRuntimeObjectReference thisReference)
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
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
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
[Guid("30D5A829-7FA4-4026-83BB-D75BAE4EA99E")]
file interface IStringableInterfaceImpl : global::Windows.Foundation.IStringable
{
    /// <inheritdoc/>
    string global::Windows.Foundation.IStringable.ToString()
    {
        var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(global::Windows.Foundation.IStringable).TypeHandle);

        return IStringableMethods.ToString(thisReference)!;
    }
}