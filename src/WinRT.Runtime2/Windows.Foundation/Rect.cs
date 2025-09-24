// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using ABI.Windows.Foundation;
using Windows.Foundation.Metadata;
using WindowsRuntime;

#pragma warning disable IDE0046

namespace Windows.Foundation;

/// <summary>
/// Contains number values that represent the location and size of a rectangle.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.rect"/>
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
[WindowsRuntimeClassName("Windows.Foundation.IReference<Windows.Foundation.Rect>")]
[ContractVersion(typeof(FoundationContract), 65536u)]
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
    public readonly float Left => X;

    /// <summary>
    /// Gets the top y-coordinate of the rectangle.
    /// </summary>
    public readonly float Top => Y;

    /// <summary>
    /// Gets the right x-coordinate of the rectangle.
    /// </summary>
    public readonly float Right => IsEmpty ? float.NegativeInfinity : X + Width;

    /// <summary>
    /// Gets the bottom y-coordinate of the rectangle.
    /// </summary>
    public readonly float Bottom => IsEmpty ? float.NegativeInfinity : Y + Height;

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

    /// <summary>
    /// Checks whether a given point falls within the area of the current rectangle.
    /// </summary>
    /// <param name="point">The input point to check.</param>
    /// <returns>Whether <paramref name="point"/> falls within the area of the current rectangle.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Contains(Point point)
    {
        return
            (point.X >= X) && (point.X - Width <= X) &&
            (point.Y >= Y) && (point.Y - Height <= Y);
    }

    /// <summary>
    /// Finds the intersection of the rectangle represented by the current <see cref="Rect"/> value and the rectangle
    /// represented by the specified <see cref="Rect"/> value, and stores the result as the current rectangle value.
    /// </summary>
    /// <param name="rect">The rectangle to intersect with the current rectangle.</param>
    public void Intersect(Rect rect)
    {
        if (!IntersectsWith(rect))
        {
            this = Empty;
        }
        else
        {
            float left = float.Max(X, rect.X);
            float top = float.Max(Y, rect.Y);

            // Max with 0 to prevent float weirdness from causing us to be in the (-epsilon, 0) range
            Width = float.Max(float.Min(X + Width, rect.X + rect.Width) - left, 0);
            Height = float.Max(float.Min(Y + Height, rect.Y + rect.Height) - top, 0);

            X = left;
            Y = top;
        }
    }

    /// <summary>
    /// Expands the rectangle represented by the current <see cref="Rect"/> value exactly enough to contain the specified rectangle.
    /// </summary>
    /// <param name="rect">The rectangle to include.</param>
    public void Union(Rect rect)
    {
        if (IsEmpty)
        {
            this = rect;
        }
        else if (!rect.IsEmpty)
        {
            float left = float.Min(Left, rect.Left);
            float top = float.Min(Top, rect.Top);


            // We need this check so that the math does not result in 'NaN'
            if ((rect.Width == float.PositiveInfinity) || (Width == float.PositiveInfinity))
            {
                Width = float.PositiveInfinity;
            }
            else
            {
                // Max with 0 to prevent float weirdness from causing us to be in the (-epsilon, 0) range
                float maxRight = float.Max(Right, rect.Right);

                Width = float.Max(maxRight - left, 0);
            }

            // Same as above, but for the height of the rectangle
            if ((rect.Height == float.PositiveInfinity) || (Height == float.PositiveInfinity))
            {
                Height = float.PositiveInfinity;
            }
            else
            {
                float maxBottom = Math.Max(Bottom, rect.Bottom);

                Height = float.Max(maxBottom - top, 0);
            }

            X = left;
            Y = top;
        }
    }

    /// <summary>
    /// Expands the rectangle represented by the current <see cref="Rect"/> value exactly enough to contain the specified point.
    /// </summary>
    /// <param name="point">The point to include.</param>
    public void Union(Point point)
    {
        Union(new Rect(point, point));
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
    /// Checks whether a given rectangle intersects with the current rectangle.
    /// </summary>
    /// <param name="rect">The input rectangle to check.</param>
    /// <returns>Whether <paramref name="rect"/> intersects with the current rectangle.</returns>
    private readonly bool IntersectsWith(Rect rect)
    {
        if (Width < 0 || rect.Width < 0)
        {
            return false;
        }

        return (rect.X <= X + Width) &&
               (rect.X + rect.Width >= X) &&
               (rect.Y <= Y + Height) &&
               (rect.Y + rect.Height >= Y);
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
