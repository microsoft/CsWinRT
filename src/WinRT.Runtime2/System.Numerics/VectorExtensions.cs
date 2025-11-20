// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Versioning;
using Windows.Foundation;

namespace System.Numerics;

/// <summary>
/// Provides extension methods for converting between <see cref="Vector2"/> and Windows Foundation types.
/// </summary>
[SupportedOSPlatform("Windows10.0.10240.0")]
public static class VectorExtensions
{
    /// <summary>
    /// Converts a <see cref="Vector2"/> to a <see cref="Point"/>.
    /// </summary>
    /// <param name="vector">The <see cref="Vector2"/> to convert.</param>
    /// <returns>A <see cref="Point"/> with X and Y coordinates from the vector's X and Y components.</returns>
    public static Point ToPoint(this Vector2 vector)
    {
        return new(vector.X, vector.Y);
    }

    /// <summary>
    /// Converts a <see cref="Vector2"/> to a <see cref="Size"/>.
    /// </summary>
    /// <param name="vector">The <see cref="Vector2"/> to convert.</param>
    /// <returns>A <see cref="Size"/> with Width and Height from the vector's X and Y components.</returns>
    public static Size ToSize(this Vector2 vector)
    {
        return new(vector.X, vector.Y);
    }

    /// <summary>
    /// Converts a <see cref="Point"/> to a <see cref="Vector2"/>.
    /// </summary>
    /// <param name="point">The <see cref="Point"/> to convert.</param>
    /// <returns>A <see cref="Vector2"/> with X and Y components from the point's X and Y coordinates.</returns>
    public static Vector2 ToVector2(this Point point)
    {
        return new(point.X, point.Y);
    }

    /// <summary>
    /// Converts a <see cref="Size"/> to a <see cref="Vector2"/>.
    /// </summary>
    /// <param name="size">The <see cref="Size"/> to convert.</param>
    /// <returns>A <see cref="Vector2"/> with X and Y components from the size's Width and Height properties.</returns>
    public static Vector2 ToVector2(this Size size)
    {
        return new(size.Width, size.Height);
    }
}