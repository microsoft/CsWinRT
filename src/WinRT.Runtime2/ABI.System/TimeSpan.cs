// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma warning disable CS1591

using System;
using System.Runtime.InteropServices;
using WindowsRuntime;
using WindowsRuntime.InteropServices;

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
[TimeSpanMarshaller]
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
    public static TimeSpan ConvertToUnmanaged(global::System.TimeSpan value)
    {
        return new() { Duration = (ulong)value.Ticks };
    }

    public static global::System.TimeSpan ConvertToManaged(TimeSpan value)
    {
        return global::System.TimeSpan.FromTicks((long)value.Duration);
    }
}

/// <summary>
/// A custom <see cref="WindowsRuntimeMarshallerAttribute"/> implementation for <see cref="global::System.TimeSpan"/>.
/// </summary>
public sealed class TimeSpanMarshallerAttribute : WindowsRuntimeMarshallerAttribute
{
    /// <inheritdoc/>
    public override unsafe void* ConvertToUnmanaged(object? value)
    {
        throw null!;
    }
}
