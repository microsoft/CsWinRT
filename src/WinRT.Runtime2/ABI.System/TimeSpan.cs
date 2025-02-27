﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma warning disable CS1591

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using static System.Runtime.InteropServices.ComWrappers;

[assembly: TypeMap<WindowsRuntimeTypeMapUniverse>(
    value: "Windows.Foundation.IReference<Windows.Foundation.TimeSpan>",
    target: typeof(ABI.System.TimeSpan),
    trimTarget: typeof(TimeSpan))]

[assembly: TypeMapAssociation<WindowsRuntimeTypeMapUniverse>(typeof(TimeSpan), typeof(ABI.System.TimeSpan))]

namespace ABI.System;

/// <summary>
/// ABI type for <see cref="global::System.TimeSpan"/>.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.timespan"/>
[TimeSpanVtableProvider]
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
public static class TimeSpanMarshaller
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
}

/// <summary>
/// A custom <see cref="WindowsRuntimeVtableProviderAttribute"/> implementation for <see cref="global::System.TimeSpan"/>.
/// </summary>
file sealed class TimeSpanVtableProviderAttribute : WindowsRuntimeVtableProviderAttribute
{
    /// <inheritdoc/>
    public override void ComputeVtables(IBufferWriter<ComInterfaceEntry> bufferWriter)
    {
        bufferWriter.Write([new ComInterfaceEntry
        {
            IID = WellKnownInterfaceIds.IID_IReferenceOfTimeSpan,
            Vtable = NonBlittableReference<TimeSpan, TimeSpanReferenceValueVtableEntry>.AbiToProjectionVftablePtr
        }]);
    }
}

/// <summary>
/// The <c>IReference`1</c> implementation for <see cref="global::System.TimeSpan"/>.
/// </summary>
file abstract unsafe class TimeSpanReferenceValueVtableEntry : IReferenceVtableEntry<TimeSpan>
{
    /// <inheritdoc/>
    public static unsafe delegate* unmanaged[MemberFunction]<void*, TimeSpan*, int> Value => &GetValue;

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ireference-1.value"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT GetValue(void* thisPtr, TimeSpan* result)
    {
        if (result is null)
        {
            return unchecked((int)0x80004003);
        }

        try
        {
            global::System.TimeSpan unboxedValue = (global::System.TimeSpan)ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            Unsafe.WriteUnaligned(result, TimeSpanMarshaller.ConvertToUnmanaged(unboxedValue));

            return 0;
        }
        catch (Exception e)
        {
            Unsafe.WriteUnaligned(result, default(TimeSpan));

            // TODO: error info

            return 0;
        }
    }
}
