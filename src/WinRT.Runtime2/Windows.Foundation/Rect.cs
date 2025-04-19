// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using ABI.Windows.Foundation;
using WindowsRuntime;

namespace Windows.Foundation;

/// <summary>
/// Contains number values that represent the location and size of a rectangle.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.rect"/>
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
[WindowsRuntimeClassName("Windows.Foundation.IReference<Windows.Foundation.Rect>")]
[RectComWrappersMarshaller]
public struct Rect : IEquatable<Rect>, IFormattable
{
    /// <summary>
    /// Creates a new <see cref="Rect"/> value with the specified parameters.
    /// </summary>
    /// <param name="point1">The first point that the new rectangle must contain.</param>
    /// <param name="point2">The second point that the new rectangle must contain.</param>
    public Rect(Point point1, Point point2)
    {
        X = float.Min(point1.X, point2.X);
        Y = float.Min(point1.Y, point2.Y);

        Width = float.Max(float.Max(point1.X, point2.X) - X, 0f);
        Height = float.Max(float.Max(point1.Y, point2.Y) - Y, 0f);
    }

    /// <summary>
    /// Creates a new <see cref="Rect"/> value with the specified parameters.
    /// </summary>
    /// <param name="location">The origin of the rectangle.</param>
    /// <param name="size">The size of the rectangle.</param>
    public Rect(Point location, Size size)
    {
        if (size.IsEmpty)
        {
            this = Empty;
        }
        else
        {
            X = location.X;
            Y = location.Y;
            Width = size.Width;
            Height = size.Height;
        }
    }

    /// <summary>
    /// Creates a new <see cref="Rect"/> value with the specified parameters.
    /// </summary>
    /// <param name="x">The x-coordinate of the top-left corner of the rectangle.</param>
    /// <param name="y">The y-coordinate of the top-left corner of the rectangle.</param>
    /// <param name="width">The width of the rectangle.</param>
    /// <param name="height">The height of the rectangle.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="width"/> or <paramref name="height"/> are less than zero.</exception>
    public Rect(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Gets or sets the x-coordinate of the upper-left corner of the rectangle.
    /// </summary>
    public float X { readonly get; set; }

    /// <summary>
    /// Gets or sets the y-coordinate of the upper-left corner of the rectangle.
    /// </summary>
    public float Y { readonly get; set; }

    /// <summary>
    /// Gets or sets the width of the rectangle, in pixels.
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
    /// Gets or sets the height of the rectangle, in pixels.
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
    /// Gets the left x-coordinate of the rectangle.
    /// </summary>
    /// <remarks>
    /// This property returns a <see cref="double"/>, for consistency with <see cref="Right"/> and <see cref="Bottom"/>.
    /// </remarks>
    public readonly double Left => X;

    /// <summary>
    /// Gets the top y-coordinate of the rectangle.
    /// </summary>
    /// <remarks>
    /// This property returns a <see cref="double"/>, for consistency with <see cref="Right"/> and <see cref="Bottom"/>.
    /// </remarks>
    public readonly double Top => Y;

    /// <summary>
    /// Gets the right x-coordinate of the rectangle.
    /// </summary>
    /// <remarks>
    /// This property returns a <see cref="double"/>, as the right x-coordinate might exceed the maximum value of a <see cref="float"/> value.
    /// </remarks>
    public readonly double Right => IsEmpty ? double.NegativeInfinity : X + Width;

    /// <summary>
    /// Gets the bottom y-coordinate of the rectangle.
    /// </summary>
    /// <remarks>
    /// This property returns a <see cref="double"/>, as the bottom y-coordinate might exceed the maximum value of a <see cref="float"/> value.
    /// </remarks>
    public readonly double Bottom => IsEmpty ? double.NegativeInfinity : Y + Height;

    /// <summary>
    /// Gets a value that indicates whether this <see cref="Rect"/> value is equal to <see cref="Empty"/>.
    /// </summary>
    public readonly bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Width < 0;
    }

    /// <summary>
    /// Gets a value that represents an empty <see cref="Rect"/>.
    /// </summary>
    /// <remarks>
    /// This is a special <see cref="Rect"/> value that is not the same as one with <see cref="Width"/> and <see cref="Height"/> equal to 0.
    /// </remarks>
    public static Rect Empty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Unsafe.BitCast<Vector4, Rect>(new Vector4(float.PositiveInfinity, float.PositiveInfinity, float.NegativeInfinity, float.NegativeInfinity));
    }

    /// <summary>
    /// Deconstructs the current <see cref="Rect"/> value into its components.
    /// </summary>
    /// <param name="x">The resulting x-coordinate.</param>
    /// <param name="y">The resulting y-coordinate.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly void Deconstruct(out float x, out float y, out float width, out float height)
    {
        x = X;
        y = Y;
        width = Width;
        height = Height;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Rect other)
    {
        // Same implementation as for 'Point', see notes there
        return Unsafe.BitCast<Rect, Vector4>(this).Equals(Unsafe.BitCast<Rect, Vector4>(other));
    }

    /// <inheritdoc/>
    public override readonly bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is Rect other && Equals(other);
    }

    /// <inheritdoc/>
    public override readonly int GetHashCode()
    {
        return HashCode.Combine(X, Y, Width, Height);
    }

    /// <inheritdoc/>
    public override readonly string ToString()
    {
        if (IsEmpty)
        {
            return "Empty";
        }

        char separator = Point.GetNumericListSeparator(null);

        return $"{X}{separator}{Y}{separator}{Width}{separator}{Height}";
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
        string separator = Point.GetNumericListSeparator(formatProvider) is ',' ? "," : ";";

        DefaultInterpolatedStringHandler handler = new(
            literalLength: 3,
            formattedCount: 4,
            provider: formatProvider,
            initialBuffer: stackalloc char[128]);

        handler.AppendFormatted(X, format);
        handler.AppendLiteral(separator);
        handler.AppendFormatted(Y, format);
        handler.AppendLiteral(separator);
        handler.AppendFormatted(Width, format);
        handler.AppendLiteral(separator);
        handler.AppendFormatted(Height, format);

        return handler.ToStringAndClear();
    }

    /// <summary>
    /// Returns a value that indicates whether two <see cref="Rect"/> values are equal.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are equal, otherwise <see langword="false"/>.</returns>
    public static bool operator ==(Rect left, Rect right) => left.Equals(right);

    /// <summary>
    /// Returns a value that indicates whether two <see cref="Rect"/> values are not equal.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are not equal, otherwise <see langword="false"/>.</returns>
    public static bool operator !=(Rect left, Rect right) => !left.Equals(right);
}
