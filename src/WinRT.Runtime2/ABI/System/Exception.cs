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

#pragma warning disable IDE1006

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
[assembly: TypeMap<WindowsRuntimeComWrappersTypeMapGroup>(
    value: "Windows.Foundation.IReference<Windows.Foundation.HResult>",
    target: typeof(ABI.System.Exception),
    trimTarget: typeof(Exception))]
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code

[assembly: TypeMapAssociation<WindowsRuntimeComWrappersTypeMapGroup>(typeof(Exception), typeof(ABI.System.Exception))]

namespace ABI.System;

/// <summary>
/// ABI type for <see cref="global::System.Exception"/>.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.hresult"/>
[WindowsRuntimeClassName("Windows.Foundation.IReference<Windows.Foundation.HResult>")]
[ExceptionComWrappersMarshaller]
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public struct Exception
{
    /// <summary>
    /// An integer that describes an error.
    /// </summary>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.hresult.value"/>
    public int Value;
}

/// <summary>
/// Marshaller for <see cref="global::System.Exception"/>.
/// </summary>
/// <remarks>This marshaller is backed by the infrastructure provided by <see cref="RestrictedErrorInfo"/>.</remarks>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ExceptionMarshaller
{
    /// <summary>
    /// Converts a managed <see cref="global::System.Exception"/> to an unmanaged <see cref="Exception"/>.
    /// </summary>
    /// <param name="value">The managed <see cref="global::System.Exception"/> value.</param>
    /// <returns>The unmanaged <see cref="Exception"/> value.</returns>
    public static Exception ConvertToUnmanaged(global::System.Exception? value)
    {
        return new() { Value = RestrictedErrorInfo.GetHRForException(value) };
    }

    /// <summary>
    /// Converts an unmanaged <see cref="Exception"/> to a managed <see cref="global::System.Exception"/>.
    /// </summary>
    /// <param name="value">The unmanaged <see cref="Exception"/> value.</param>
    /// <returns>The managed <see cref="global::System.Exception"/> value</returns>
    public static global::System.Exception? ConvertToManaged(Exception value)
    {
        return RestrictedErrorInfo.GetExceptionForHR(value.Value, out _);
    }
}

/// <summary>
/// The set of <see cref="ComInterfaceEntry"/> values for <see cref="global::System.Exception"/>.
/// </summary>
file struct ExceptionInterfaceEntries
{
    public ComInterfaceEntry IReferenceOfException;
    public ComInterfaceEntry IPropertyValue;
    public ComInterfaceEntry IStringable;
    public ComInterfaceEntry IWeakReferenceSource;
    public ComInterfaceEntry IMarshal;
    public ComInterfaceEntry IAgileObject;
    public ComInterfaceEntry IInspectable;
    public ComInterfaceEntry IUnknown;
}

/// <summary>
/// The implementation of <see cref="ExceptionInterfaceEntries"/>.
/// </summary>
file static class ExceptionInterfaceEntriesImpl
{
    /// <summary>
    /// The <see cref="ExceptionInterfaceEntries"/> value for <see cref="global::System.Exception"/>.
    /// </summary>
    [FixedAddressValueType]
    public static readonly ExceptionInterfaceEntries Entries;

    /// <summary>
    /// Initializes <see cref="Entries"/>.
    /// </summary>
    static ExceptionInterfaceEntriesImpl()
    {
        Entries.IReferenceOfException.IID = WellKnownWindowsInterfaceIIDs.IID_IReferenceOfException;
        Entries.IReferenceOfException.Vtable = ExceptionReferenceImpl.Vtable;
        Entries.IPropertyValue.IID = WellKnownWindowsInterfaceIIDs.IID_IPropertyValue;
        Entries.IPropertyValue.Vtable = IPropertyValueImpl.OtherTypeVtable;
        Entries.IStringable.IID = WellKnownWindowsInterfaceIIDs.IID_IStringable;
        Entries.IStringable.Vtable = IStringableImpl.Vtable;
        Entries.IWeakReferenceSource.IID = WellKnownWindowsInterfaceIIDs.IID_IWeakReferenceSource;
        Entries.IWeakReferenceSource.Vtable = IWeakReferenceSourceImpl.Vtable;
        Entries.IMarshal.IID = WellKnownWindowsInterfaceIIDs.IID_IMarshal;
        Entries.IMarshal.Vtable = IMarshalImpl.Vtable;
        Entries.IAgileObject.IID = WellKnownWindowsInterfaceIIDs.IID_IAgileObject;
        Entries.IAgileObject.Vtable = IAgileObjectImpl.Vtable;
        Entries.IInspectable.IID = WellKnownWindowsInterfaceIIDs.IID_IInspectable;
        Entries.IInspectable.Vtable = IInspectableImpl.Vtable;
        Entries.IUnknown.IID = WellKnownWindowsInterfaceIIDs.IID_IUnknown;
        Entries.IUnknown.Vtable = IUnknownImpl.Vtable;
    }
}

/// <summary>
/// A custom <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> implementation for <see cref="global::System.Exception"/>.
/// </summary>
file sealed unsafe class ExceptionComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
{
    /// <inheritdoc/>
    public override void* GetOrCreateComInterfaceForObject(object value)
    {
        return WindowsRuntimeComWrappersMarshal.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.TrackerSupport);
    }

    /// <inheritdoc/>
    public override ComInterfaceEntry* ComputeVtables(out int count)
    {
        count = sizeof(ExceptionInterfaceEntries) / sizeof(ComInterfaceEntry);

        return (ComInterfaceEntry*)Unsafe.AsPointer(in ExceptionInterfaceEntriesImpl.Entries);
    }

    /// <inheritdoc/>
    public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
    {
        wrapperFlags = CreatedWrapperFlags.NonWrapping;

        Exception abi = WindowsRuntimeValueTypeMarshaller.UnboxToManagedUnsafe<Exception>(value, in WellKnownWindowsInterfaceIIDs.IID_IReferenceOfException);

        // Exceptions are marshalled as 'null' if the 'HRESULT' does not represent an error. However, 'ComWrappers' does not allow 'null'
        // to be returned. So in that case, we use a 'NullPlaceholder' instance, and then check that after marshalling is done, so that
        // we just return 'null' to external code if we hit that code path. See more notes about this in 'NullPlaceholder'.
        return (object?)ExceptionMarshaller.ConvertToManaged(abi) ?? NullPlaceholder.Instance;
    }
}

/// <summary>
/// Binding type for the <c>IReference`1</c> implementation for <see cref="global::System.Exception"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
file unsafe struct ExceptionReferenceVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, Exception*, HRESULT> get_Value;
}

/// <summary>
/// The <c>IReference`1</c> implementation for <see cref="global::System.Exception"/>.
/// </summary>
file static unsafe class ExceptionReferenceImpl
{
    /// <summary>
    /// The <see cref="ExceptionReferenceVftbl"/> value for the managed <c>IReference`1</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly ExceptionReferenceVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static ExceptionReferenceImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.get_Value = &get_Value;
    }

    /// <summary>
    /// Gets a pointer to the managed <c>IReference`1</c> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Vftbl);
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ireference-1.value"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT get_Value(void* thisPtr, Exception* result)
    {
        if (result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            global::System.Exception unboxedValue = ComInterfaceDispatch.GetInstance<global::System.Exception>((ComInterfaceDispatch*)thisPtr);

            *result = ExceptionMarshaller.ConvertToUnmanaged(unboxedValue);

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}
