// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Buffers;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;
using static System.Runtime.InteropServices.ComWrappers;

[assembly: TypeMap<WindowsRuntimeTypeMapUniverse>(
    value: "Windows.Foundation.IReference<Windows.Foundation.DateTime>",
    target: typeof(ABI.System.DateTimeOffset),
    trimTarget: typeof(DateTimeOffset))]

[assembly: TypeMapAssociation<WindowsRuntimeTypeMapUniverse>(typeof(DateTimeOffset), typeof(ABI.System.DateTimeOffset))]

namespace ABI.System;

/// <summary>
/// ABI type for <see cref="global::System.DateTimeOffset"/>.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.datetime"/>
[EditorBrowsable(EditorBrowsableState.Never)]
[DateTimeOffsetVtableProvider]
public struct DateTimeOffset
{
    /// <summary>
    /// A 64-bit signed integer that represents a point in time as the number of 100-nanosecond intervals prior to or after midnight on January 1, 1601 (according to the Gregorian Calendar).
    /// </summary>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.datetime.universaltime"/>
    public ulong UniversalTime;
}

/// <summary>
/// Marshaller for <see cref="global::System.DateTimeOffset"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class DateTimeOffsetMarshaller
{
    /// <summary>
    /// The number of ticks counted between <c>0001-01-01, 00:00:00</c> and <c>1601-01-01, 00:00:00</c>.
    /// This is equivalent to the result of this expression:
    /// <code lang="csharp">
    /// var ticks = new DateTimeOffset(1601, 1, 1, 0, 0, 1, TimeSpan.Zero).Ticks;
    /// </code>
    /// </summary>
    private const long ManagedUtcTicksAtNativeZero = 504911232000000000;

    /// <summary>
    /// Converts a managed <see cref="global::System.DateTimeOffset"/> to an unmanaged <see cref="DateTimeOffset"/>.
    /// </summary>
    /// <param name="value">The managed <see cref="global::System.DateTimeOffset"/> value.</param>
    /// <returns>The unmanaged <see cref="DateTimeOffset"/> value.</returns>
    public static DateTimeOffset ConvertToUnmanaged(global::System.DateTimeOffset value)
    {
        return new() { UniversalTime = unchecked((ulong)value.UtcTicks - ManagedUtcTicksAtNativeZero) };
    }

    /// <summary>
    /// Converts an unmanaged <see cref="DateTimeOffset"/> to a managed <see cref="global::System.DateTimeOffset"/>.
    /// </summary>
    /// <param name="value">The unmanaged <see cref="DateTimeOffset"/> value.</param>
    /// <returns>The managed <see cref="global::System.DateTimeOffset"/> value</returns>
    public static global::System.DateTimeOffset ConvertToManaged(DateTimeOffset value)
    {
        return new global::System.DateTimeOffset(unchecked((long)value.UniversalTime + ManagedUtcTicksAtNativeZero), global::System.TimeSpan.Zero);
    }
}

/// <summary>
/// A custom <see cref="WindowsRuntimeVtableProviderAttribute"/> implementation for <see cref="global::System.DateTimeOffset"/>.
/// </summary>
file sealed class DateTimeOffsetVtableProviderAttribute : WindowsRuntimeVtableProviderAttribute
{
    /// <inheritdoc/>
    public override void ComputeVtables(IBufferWriter<ComInterfaceEntry> bufferWriter)
    {
        bufferWriter.Write([new ComInterfaceEntry
        {
            IID = WellKnownInterfaceIds.IID_IReferenceArrayOfDateTimeOffset,
            Vtable = DateTimeOffsetReference.AbiToProjectionVftablePtr
        }]);
    }
}

/// <summary>
/// The <c>IReference`1</c> implementation for <see cref="global::System.DateTimeOffset"/>.
/// </summary>
file static unsafe class DateTimeOffsetReference
{
    /// <summary>
    /// The vtable for the <c>IReference`1</c> implementation.
    /// </summary>
    public static nint AbiToProjectionVftablePtr { get; } = (nint)WindowsRuntimeHelpers.AllocateTypeAssociatedReferenceVtable(
        type: typeof(global::System.DateTimeOffset),
        fpValue: (delegate* unmanaged[MemberFunction]<void*, DateTimeOffset*, HRESULT>)&Value);

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ireference-1.value"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT Value(void* thisPtr, DateTimeOffset* result)
    {
        if (result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            global::System.DateTimeOffset unboxedValue = (global::System.DateTimeOffset)ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            Unsafe.WriteUnaligned(result, DateTimeOffsetMarshaller.ConvertToUnmanaged(unboxedValue));

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            Unsafe.WriteUnaligned(result, default(TimeSpan));

            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}
