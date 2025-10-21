// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using static System.Runtime.InteropServices.ComWrappers;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// The <c>IWeakReferenceSource</c> implementation for managed types.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/weakreference/nn-weakreference-iweakreferencesource"/>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IWeakReferenceSourceImpl
{
    /// <summary>
    /// The <see cref="IWeakReferenceSourceVftbl"/> value for the managed <c>IWeakReferenceSource</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly IWeakReferenceSourceVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static IWeakReferenceSourceImpl()
    {
        *(IUnknownVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IUnknownVftbl*)IUnknownImpl.Vtable;

        Vftbl.GetWeakReference = &GetWeakReference;
    }

    /// <summary>
    /// Gets the IID for the <c>IWeakReferenceSource</c> interface.
    /// </summary>
    public static ref readonly Guid IID
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref WellKnownInterfaceIIDs.IID_IWeakReferenceSource;
    }

    /// <summary>
    /// Gets a pointer to the managed <c>IWeakReferenceSource</c> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Vftbl);
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/weakreference/nf-weakreference-iweakreferencesource-getweakreference"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT GetWeakReference(void* thisPtr, void** weakReference)
    {
        *weakReference = null;

        try
        {
            object instance = ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            ManagedWeakReference managedReference = new(instance);

            // We delegate to 'ComInterfaceMarshaller<T>' to marshal the managed weak reference to a
            // native object. This will use the default 'ComWrappers' instance, not the CsWinRT one.
            // This allows 'WindowsRuntimeComWrappers' to only ever have to handle Windows Runtime types.
            *weakReference = ComInterfaceMarshaller<IWeakReference>.ConvertToUnmanaged(managedReference);

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return e.HResult;
        }
    }
}
