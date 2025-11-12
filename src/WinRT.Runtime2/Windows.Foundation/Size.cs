// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using Windows.Foundation.Metadata;
using WindowsRuntime.InteropServices;

namespace Windows.Foundation;

/// <summary>
/// Represents number values that specify a height and width.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.size"/>
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
[WindowsRuntimeClassName("Windows.Foundation.IReference<Windows.Foundation.Size>")]
[SupportedOSPlatform("Windows10.0.10240.0")]
[ContractVersion(typeof(FoundationContract), 65536u)]
[ABI.Windows.Foundation.SizeComWrappersMarshaller]
public struct Size : IEquatable<Size>, IFormattable
{
    /// <summary>
    /// Creates a new <see cref="Size"/> value with the specified parameters.
    /// </summary>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="width"/> or <paramref name="height"/> are less than zero.</exception>
    public Size(float width, float height)
    {
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Gets or sets the width.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the value is less than zero.</exception>
    public float Width
    {
        readonly get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);

            field = value;
        }
    }

    /// <summary>
    /// Gets or sets the height.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the value is less than zero.</exception>
    public float Height
    {
        readonly get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);

            field = value;
        }
    }

    /// <summary>
    /// Gets a value that indicates whether this <see cref="Size"/> value is equal to <see cref="Empty"/>.
    /// </summary>
    public readonly bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Width < 0;
    }

    /// <summary>
    /// Gets a value that represents an empty <see cref="Size"/>.
    /// </summary>
    /// <remarks>
    /// This is a special <see cref="Size"/> value that is not the same as one with <see cref="Width"/> and <see cref="Height"/> equal to 0.
    /// </remarks>
    public static Size Empty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Unsafe.BitCast<Vector2, Size>(new Vector2(float.NegativeInfinity));
    }

    /// <summary>
    /// Deconstructs the current <see cref="Size"/> value into its components.
    /// </summary>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly void Deconstruct(out float width, out float height)
    {
        width = Width;
        height = Height;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Size other)
    {
        // Same implementation as for 'Point', see notes there
        return Unsafe.BitCast<Size, Vector2>(this).Equals(Unsafe.BitCast<Size, Vector2>(other));
    }

    /// <inheritdoc/>
    public override readonly bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is Size other && Equals(other);
    }

    /// <inheritdoc/>
    public override readonly int GetHashCode()
    {
        return HashCode.Combine(Width, Height);
    }

    /// <inheritdoc/>
    public override readonly string ToString()
    {
        return IsEmpty ? "Empty" : $"{Width},{Height}";
    }

    /// <inheritdoc/>
    public readonly string ToString(string? format, IFormatProvider? formatProvider)
    {
        // Fast path if both arguments are 'null'
        if (format is null && formatProvider is null)
        {
            return ToString();
        }

        DefaultInterpolatedStringHandler handler = new(
            literalLength: 1,
            formattedCount: 2,
            provider: formatProvider,
            initialBuffer: stackalloc char[128]);

        handler.AppendFormatted(Width, format);
        handler.AppendLiteral(",");
        handler.AppendFormatted(Height, format);

        return handler.ToStringAndClear();
    }

    /// <summary>
    /// Returns a value that indicates whether two <see cref="Size"/> values are equal.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are equal, otherwise <see langword="false"/>.</returns>
    public static bool operator ==(Size left, Size right) => left.Equals(right);

    /// <summary>
    /// Returns a value that indicates whether two <see cref="Size"/> values are not equal.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are not equal, otherwise <see langword="false"/>.</returns>
    public static bool operator !=(Size left, Size right) => !left.Equals(right);
}
