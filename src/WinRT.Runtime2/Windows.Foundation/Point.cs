// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using Windows.Foundation.Metadata;
using WindowsRuntime;
using WindowsRuntime.InteropServices;

namespace Windows.Foundation;

/// <summary>
/// Represents X and Y coordinate values that define a point in a two-dimensional plane.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.point"/>
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
[WindowsRuntimeClassName("Windows.Foundation.IReference<Windows.Foundation.Point>")]
[SupportedOSPlatform("Windows10.0.10240.0")]
[ContractVersion(typeof(FoundationContract), 65536u)]
[ABI.Windows.Foundation.PointComWrappersMarshaller]
public struct Point : IEquatable<Point>, IFormattable
{
    /// <summary>
    /// Creates a new <see cref="Point"/> value with the specified parameters.
    /// </summary>
    /// <param name="x">The horizontal position of the point.</param>
    /// <param name="y">The vertical position of the point.</param>
    public Point(float x, float y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Gets or sets the horizontal position of the point.
    /// </summary>
    public float X { readonly get; set; }

    /// <summary>
    /// Gets or sets the vertical position of the point.
    /// </summary>
    public float Y { readonly get; set; }

    /// <summary>
    /// Deconstructs the current <see cref="Point"/> value into its components.
    /// </summary>
    /// <param name="x">The resulting horizontal position.</param>
    /// <param name="y">The resulting vertical position.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly void Deconstruct(out float x, out float y)
    {
        x = X;
        y = Y;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Point other)
    {
        // Defer to 'Vector2.Equals', which is very well optimized.
        // This results in a branch-free codegen via SIMD intrinsics.
        return Unsafe.BitCast<Point, Vector2>(this).Equals(Unsafe.BitCast<Point, Vector2>(other));
    }

    /// <inheritdoc/>
    public override readonly bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is Point other && Equals(other);
    }

    /// <inheritdoc/>
    public override readonly int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    /// <inheritdoc/>
    public override readonly string ToString()
    {
        char separator = TokenizerHelper.GetNumericListSeparator(null);

        return $"{X}{separator}{Y}";
    }

    /// <inheritdoc/>
    public readonly string ToString(string? format, IFormatProvider? formatProvider)
    {
        // Fast path if both arguments are 'null'
        if (format is null && formatProvider is null)
        {
            return ToString();
        }

        // We need the separator as a 'string', so we can pass it as a literal
        string separator = TokenizerHelper.GetNumericListSeparator(formatProvider) is ',' ? "," : ";";

        DefaultInterpolatedStringHandler handler = new(
            literalLength: 1,
            formattedCount: 2,
            provider: formatProvider,
            initialBuffer: stackalloc char[128]);

        handler.AppendFormatted(X, format);
        handler.AppendLiteral(separator);
        handler.AppendFormatted(Y, format);

        return handler.ToStringAndClear();
    }

    /// <summary>
    /// Returns a value that indicates whether each pair of elements in two specified values is equal.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are equal, otherwise <see langword="false"/>.</returns>
    /// <remarks>Two <see cref="Point"/> values are equal if each component in <paramref name="left"/> is equal to the corresponding component in <paramref name="right"/>.</remarks>
    public static bool operator ==(Point left, Point right) => left.Equals(right);

    /// <summary>
    /// Returns a value that indicates whether either pair of elements in two specified values is not equal.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are not equal, otherwise <see langword="false"/>.</returns>
    /// <remarks>Two <see cref="Point"/> values are equal if each component in <paramref name="left"/> is equal to the corresponding component in <paramref name="right"/>.</remarks>
    public static bool operator !=(Point left, Point right) => !left.Equals(right);
}
