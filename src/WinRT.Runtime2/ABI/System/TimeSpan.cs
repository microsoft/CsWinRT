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

#pragma warning disable CS0649

[assembly: TypeMap<WindowsRuntimeTypeMapGroup>(
    value: "Windows.Foundation.IReference<Windows.Foundation.TimeSpan>",
    target: typeof(ABI.System.TimeSpan),
    trimTarget: typeof(TimeSpan))]

[assembly: TypeMapAssociation<WindowsRuntimeTypeMapGroup>(typeof(TimeSpan), typeof(ABI.System.TimeSpan))]

namespace ABI.System;

/// <summary>
/// ABI type for <see cref="global::System.TimeSpan"/>.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.timespan"/>
[EditorBrowsable(EditorBrowsableState.Never)]
[WindowsRuntimeClassName("Windows.Foundation.IReference<Windows.Foundation.TimeSpan>")]
[TimeSpanComWrappersMarshaller]
public struct TimeSpan
{
    /// <summary>
    /// A time period expressed in 100-nanosecond units.
    /// </summary>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.timespan.duration"/>
    public ulong Duration;
}

/// <summary>
/// Marshaller for <see cref="global::System.TimeSpan"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class TimeSpanMarshaller
{
    /// <summary>
    /// Converts a managed <see cref="global::System.TimeSpan"/> to an unmanaged <see cref="TimeSpan"/>.
    /// </summary>
    /// <param name="value">The managed <see cref="global::System.TimeSpan"/> value.</param>
    /// <returns>The unmanaged <see cref="TimeSpan"/> value.</returns>
    public static TimeSpan ConvertToUnmanaged(global::System.TimeSpan value)
    {
        return new() { Duration = (ulong)value.Ticks };
    }

    /// <summary>
    /// Converts an unmanaged <see cref="TimeSpan"/> to a managed <see cref="global::System.TimeSpan"/>.
    /// </summary>
    /// <param name="value">The unmanaged <see cref="TimeSpan"/> value.</param>
    /// <returns>The managed <see cref="global::System.TimeSpan"/> value</returns>
    public static global::System.TimeSpan ConvertToManaged(TimeSpan value)
    {
        return global::System.TimeSpan.FromTicks((long)value.Duration);
    }

    /// <inheritdoc cref="WindowsRuntimeValueTypeMarshaller.BoxToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue BoxToUnmanaged(global::System.TimeSpan? value)
    {
        return WindowsRuntimeValueTypeMarshaller.BoxToUnmanaged(value, in WellKnownInterfaceIds.IID_IReferenceOfTimeSpan);
    }

    /// <inheritdoc cref="WindowsRuntimeValueTypeMarshaller.UnboxToManaged(void*)"/>
    public static global::System.TimeSpan? UnboxToManaged(void* value)
    {
        TimeSpan? abi = WindowsRuntimeValueTypeMarshaller.UnboxToManaged<TimeSpan>(value);

        return abi.HasValue ? ConvertToManaged(abi.Value) : null;
    }
}

/// <summary>
/// The set of <see cref="ComInterfaceEntry"/> values for <see cref="global::System.TimeSpan"/>.
/// </summary>
file struct TimeSpanInterfaceEntries
{
    public ComInterfaceEntry IReferenceOfTimeSpan;
    public ComInterfaceEntry IStringable;
    public ComInterfaceEntry IWeakReferenceSource;
    public ComInterfaceEntry IMarshal;
    public ComInterfaceEntry IAgileObject;
    public ComInterfaceEntry IInspectable;
    public ComInterfaceEntry IUnknown;
}

/// <summary>
/// The implementation of <see cref="TimeSpanInterfaceEntries"/>.
/// </summary>
file static class TimeSpanInterfaceEntriesImpl
{
    /// <summary>
    /// The <see cref="TimeSpanInterfaceEntries"/> value for <see cref="global::System.TimeSpan"/>.
    /// </summary>
    [FixedAddressValueType]
    public static readonly TimeSpanInterfaceEntries Entries;

    /// <summary>
    /// Initializes <see cref="Entries"/>.
    /// </summary>
    static TimeSpanInterfaceEntriesImpl()
    {
        Entries.IReferenceOfTimeSpan.IID = WellKnownInterfaceIds.IID_IReferenceOfTimeSpan;
        Entries.IReferenceOfTimeSpan.Vtable = TimeSpanReferenceImpl.Vtable;
        Entries.IStringable.IID = WellKnownInterfaceIds.IID_IStringable;
        Entries.IStringable.Vtable = IStringableImpl.Vtable;
        Entries.IWeakReferenceSource.IID = WellKnownInterfaceIds.IID_IWeakReferenceSource;
        Entries.IWeakReferenceSource.Vtable = IWeakReferenceSourceImpl.Vtable;
        Entries.IMarshal.IID = WellKnownInterfaceIds.IID_IMarshal;
        Entries.IMarshal.Vtable = IMarshalImpl.Vtable;
        Entries.IAgileObject.IID = WellKnownInterfaceIds.IID_IAgileObject;
        Entries.IAgileObject.Vtable = IUnknownImpl.Vtable;
        Entries.IInspectable.IID = WellKnownInterfaceIds.IID_IInspectable;
        Entries.IInspectable.Vtable = IInspectableImpl.Vtable;
        Entries.IUnknown.IID = WellKnownInterfaceIds.IID_IUnknown;
        Entries.IUnknown.Vtable = IUnknownImpl.Vtable;
    }
}

/// <summary>
/// A custom <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> implementation for <see cref="global::System.TimeSpan"/>.
/// </summary>
internal sealed unsafe class TimeSpanComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
{
    /// <inheritdoc/>
    public override void* GetOrCreateComInterfaceForObject(object value)
    {
        return (void*)WindowsRuntimeComWrappers.Default.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.None);
    }

    /// <inheritdoc/>
    public override ComInterfaceEntry* ComputeVtables(out int count)
    {
        count = sizeof(TimeSpanInterfaceEntries) / sizeof(ComInterfaceEntry);

        return (ComInterfaceEntry*)Unsafe.AsPointer(ref Unsafe.AsRef(in TimeSpanInterfaceEntriesImpl.Entries));
    }

    /// <inheritdoc/>
    public override object CreateObject(void* value)
    {
        TimeSpan abi = WindowsRuntimeValueTypeMarshaller.UnboxToManagedUnsafe<TimeSpan>(value, in WellKnownInterfaceIds.IID_IReferenceOfTimeSpan);

        return TimeSpanMarshaller.ConvertToManaged(abi);
    }
}

/// <summary>
/// Binding type for the <c>IReference`1</c> implementation for <see cref="global::System.TimeSpan"/>.
/// </summary>
file unsafe struct TimeSpanReferenceVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, TimeSpan*, HRESULT> Value;
}

/// <summary>
/// The <c>IReference`1</c> implementation for <see cref="global::System.TimeSpan"/>.
/// </summary>
file static unsafe class TimeSpanReferenceImpl
{
    /// <summary>
    /// The <see cref="TimeSpanReferenceVftbl"/> value for the managed <c>IReference`1</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly TimeSpanReferenceVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static TimeSpanReferenceImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.Value = &Value;
    }

    /// <summary>
    /// Gets a pointer to the managed <c>IReference`1</c> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(ref Unsafe.AsRef(in Vftbl));
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ireference-1.value"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT Value(void* thisPtr, TimeSpan* result)
    {
        if (result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            global::System.TimeSpan unboxedValue = (global::System.TimeSpan)ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            Unsafe.WriteUnaligned(result, TimeSpanMarshaller.ConvertToUnmanaged(unboxedValue));

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            Unsafe.WriteUnaligned(result, default(TimeSpan));

            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}
