// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions for the <see cref="Path"/> type.
/// </summary>
internal static class PathExtensions
{
    extension(Path)
    {
        /// <summary>
        /// Normalizes a path so that file separators can be handled correctly on all platforms.
        /// </summary>
        /// <param name="path">The input path to normalize.</param>
        /// <returns>The normalized path.</returns>
        [return: NotNullIfNotNull(nameof(path))]
        public static string? Normalize(string? path)
        {
            // If on Windows, no normalization is needed. Paths in debug repros will use this format.
            // Note: 'cswinrtgen' is only meant to be used on Windows (because CsWinRT itself is
            // only supported on Windows), but this allows debugging repros on other platforms too.
            if (OperatingSystem.IsWindows())
            {
                return path;
            }

            // For non-Windows platforms, just adjust the separator character.
            // This ensures that full paths won't be treated as file names.
            return path?.Replace('\\', '/');
        }

        /// <inheritdoc cref="Normalize(string?)"/>
        public static ReadOnlySpan<char> Normalize(ReadOnlySpan<char> path)
        {
            if (OperatingSystem.IsWindows())
            {
                return path;
            }

            char[] buffer = new char[path.Length];

            path.Replace(buffer, '\\', '/');

            return buffer;
        }
    }
}