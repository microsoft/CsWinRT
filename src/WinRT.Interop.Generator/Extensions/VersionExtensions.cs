// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions for the <see cref="Version"/> type.
/// </summary>
internal static class VersionExtensions
{
    /// <summary>
    /// Checks whether two <see cref="Version"/> values are equal, considering only <see cref="Version.Major"/> and <see cref="Version.Minor"/>.
    /// </summary>
    /// <param name="left">The first <see cref="Version"/> value to compare.</param>
    /// <param name="right">The second <see cref="Version"/> value to compare.</param>
    /// <returns>Whether <paramref name="left"/> and <paramref name="right"/> are a match.</returns>
    public static bool EqualsInMajorAndMinorOnly(this Version? left, Version? right)
    {
        return (left, right) switch
        {
            (null, null) => true,
            (not null, not null) => left.Major == right.Major && left.Minor == right.Minor,
            _ => false
        };
    }
}
