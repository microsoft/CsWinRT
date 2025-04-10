// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;
using static System.Runtime.InteropServices.ComWrappers;

#pragma warning disable CS0649

[assembly: TypeMap<WindowsRuntimeTypeMapUniverse>(
    value: "Windows.Foundation.IReference<Windows.Foundation.HResult>",
    target: typeof(ABI.System.Exception),
    trimTarget: typeof(Exception))]

[assembly: TypeMapAssociation<WindowsRuntimeTypeMapUniverse>(typeof(Exception), typeof(ABI.System.Exception))]

namespace ABI.System;

/// <summary>
/// ABI type for <see cref="global::System.Exception"/>.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.hresult"/>
[EditorBrowsable(EditorBrowsableState.Never)]
[WindowsRuntimeClassName("Windows.Foundation.IReference<Windows.Foundation.HResult>")]
[ExceptionComWrappersMarshaller]
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
        return RestrictedErrorInfo.GetExceptionForHR(value.Value);
    }
}

/// <summary>
/// The set of <see cref="ComInterfaceEntry"/> values for <see cref="global::System.Exception"/>.
/// </summary>
file struct ExceptionInterfaceEntries
{
    public ComInterfaceEntry IReferenceOfException;
    public ComInterfaceEntry IStringable;
    public ComInterfaceEntry ICustomPropertyProvider;
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
        Entries.IReferenceOfException.IID = WellKnownInterfaceIds.IID_IReferenceOfException;
        Entries.IReferenceOfException.Vtable = ExceptionReferenceImpl.AbiToProjectionVftablePtr;
        Entries.IStringable.IID = WellKnownInterfaceIds.IID_IStringable;
        Entries.IStringable.Vtable = IStringableImpl.AbiToProjectionVftablePtr;
        Entries.ICustomPropertyProvider.IID = WellKnownInterfaceIds.IID_ICustomPropertyProvider;
        Entries.ICustomPropertyProvider.Vtable = 0; // TODO
        Entries.IWeakReferenceSource.IID = WellKnownInterfaceIds.IID_IWeakReferenceSource;
        Entries.IWeakReferenceSource.Vtable = IWeakReferenceSourceImpl.AbiToProjectionVftablePtr;
        Entries.IMarshal.IID = WellKnownInterfaceIds.IID_IMarshal;
        Entries.IMarshal.Vtable = IMarshalImpl.AbiToProjectionVftablePtr;
        Entries.IAgileObject.IID = WellKnownInterfaceIds.IID_IAgileObject;
        Entries.IAgileObject.Vtable = IUnknownImpl.AbiToProjectionVftablePtr;
        Entries.IInspectable.IID = WellKnownInterfaceIds.IID_IInspectable;
        Entries.IInspectable.Vtable = IInspectableImpl.AbiToProjectionVftablePtr;
        Entries.IUnknown.IID = WellKnownInterfaceIds.IID_IUnknown;
        Entries.IUnknown.Vtable = IUnknownImpl.AbiToProjectionVftablePtr;
    }
}

/// <summary>
/// A custom <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> implementation for <see cref="global::System.Exception"/>.
/// </summary>
file sealed unsafe class ExceptionComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
{
    /// <inheritdoc/>
    public override ComInterfaceEntry* ComputeVtables(out int count)
    {
        count = sizeof(ExceptionInterfaceEntries) / sizeof(ComInterfaceEntry);

        return (ComInterfaceEntry*)Unsafe.AsPointer(ref Unsafe.AsRef(in ExceptionInterfaceEntriesImpl.Entries));
    }

    /// <inheritdoc/>
    public override object CreateObject(void* value)
    {
        Exception abi = WindowsRuntimeValueTypeMarshaller.UnboxToManagedUnsafe<Exception>(value, in WellKnownInterfaceIds.IID_IReferenceOfException);

        // Exceptions are marshalled as 'null' if the 'HRESULT' does not represent an error. However, 'ComWrappers' does not allow 'null'
        // to be returned. So in that case, we use a 'NullPlaceholder' instance, and then check that after marshalling is done, so that
        // we just return 'null' to external code if we hit that code path. See more notes about this in 'NullPlaceholder'.
        return (object?)ExceptionMarshaller.ConvertToManaged(abi) ?? NullPlaceholder.Instance;
    }
}

/// <summary>
/// Binding type for the <c>IReference`1</c> implementation for <see cref="global::System.Exception"/>.
/// </summary>
file unsafe struct ExceptionReferenceVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, Exception*, HRESULT> Value;
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
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.AbiToProjectionVftablePtr;

        Vftbl.Value = &Value;
    }

    /// <summary>
    /// Gets a pointer to the managed <c>IReference`1</c> implementation.
    /// </summary>
    public static nint AbiToProjectionVftablePtr
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(ref Unsafe.AsRef(in Vftbl));
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ireference-1.value"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT Value(void* thisPtr, Exception* result)
    {
        if (result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            global::System.Exception unboxedValue = (global::System.Exception)ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            Unsafe.WriteUnaligned(result, ExceptionMarshaller.ConvertToUnmanaged(unboxedValue));

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            Unsafe.WriteUnaligned(result, default(Exception));

            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}
