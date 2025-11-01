// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Represents a reference to a delegate that receives change notifications.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.eventregistrationtoken"/>
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
[WindowsRuntimeClassName("Windows.Foundation.IReference<Windows.Foundation.EventRegistrationToken>")]
[ABI.WindowsRuntime.InteropServices.EventRegistrationTokenComWrappersMarshaller]
public struct EventRegistrationToken : IEquatable<EventRegistrationToken>
{
    /// <summary>
    /// Creates a new <see cref="EventRegistrationToken"/> value with the specified parameters.
    /// </summary>
    /// <param name="value">The reference to the delegate. A valid reference will not have a value of zero.</param>
    public EventRegistrationToken(long value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets or sets the reference to the delegate. A valid reference will not have a value of zero.
    /// </summary>
    public long Value { readonly get; set; }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(EventRegistrationToken other)
    {
        return Value == other.Value;
    }

    /// <inheritdoc/>
    public override readonly bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is EventRegistrationToken other && this == other;
    }

    /// <inheritdoc/>
    public override readonly int GetHashCode()
    {
        return Value.GetHashCode();
    }

    /// <inheritdoc/>
    public override readonly string ToString()
    {
        return Value.ToString();
    }

    /// <summary>
    /// Returns a value that indicates whether two <see cref="EventRegistrationToken"/> values are equal.
    /// </summary>
    /// <param name="left">The first <see cref="EventRegistrationToken"/> value to compare.</param>
    /// <param name="right">The second <see cref="EventRegistrationToken"/> value to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are equal, otherwise <see langword="false"/>.</returns>
    public static bool operator ==(EventRegistrationToken left, EventRegistrationToken right) => left.Equals(right);

    /// <summary>
    /// Returns a value that indicates whether two <see cref="EventRegistrationToken"/> values are not equal.
    /// </summary>
    /// <param name="left">The first <see cref="EventRegistrationToken"/> value to compare.</param>
    /// <param name="right">The second <see cref="EventRegistrationToken"/> value to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are not equal, otherwise <see langword="false"/>.</returns>
    public static bool operator !=(EventRegistrationToken left, EventRegistrationToken right) => !left.Equals(right);
}
