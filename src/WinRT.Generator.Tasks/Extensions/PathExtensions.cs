// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.NET.Build.Tasks;

/// <summary>
/// Extensions for the <see cref="Path"/> type.
/// </summary>
internal static class PathExtensions
{
    extension(Path)
    {
        /// <summary>
        /// Checks whether a path is relative to the root of the current drive (eg. <c>\foo</c>).
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <returns>Whether <paramref name="path"/> is root-relative.</returns>
        /// <remarks>
        /// UNC and device paths (eg. <c>\\server\share</c>, <c>\\?\C:\foo</c>) are fully qualified, and NT object paths
        /// (eg. <c>\??\C:\foo</c>) don't resolve against the current drive either, so none of those are root-relative.
        /// </remarks>
        public static bool IsRootRelative(ReadOnlySpan<char> path)
        {
            return
                path.Length > 0 &&
                IsDirectorySeparator(path[0]) &&
                (path.Length == 1 || !IsDirectorySeparator(path[1])) &&
                !path.StartsWith(@"\??\", StringComparison.Ordinal);
        }

        /// <summary>
        /// Checks whether a path is relative to the current directory of a specific drive (eg. <c>C:foo</c>).
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <returns>Whether <paramref name="path"/> is drive-relative.</returns>
        public static bool IsDriveRelative(ReadOnlySpan<char> path)
        {
            return path.Length >= 2 && path[1] == ':' && (path.Length == 2 || !IsDirectorySeparator(path[2]));
        }

        /// <summary>
        /// Checks whether two paths start with the same drive letter (eg. <c>C:foo</c> and <c>c:\bar</c>).
        /// </summary>
        /// <param name="path">The first path to compare.</param>
        /// <param name="other">The second path to compare.</param>
        /// <returns>Whether both paths start with the same drive letter.</returns>
        public static bool IsOnSameDrive(ReadOnlySpan<char> path, ReadOnlySpan<char> other)
        {
            return
                path.Length >= 2 &&
                other.Length >= 2 &&
                path[1] == ':' &&
                other[1] == ':' &&
                char.ToUpperInvariant(path[0]) == char.ToUpperInvariant(other[0]);
        }
    }

    /// <summary>
    /// Checks whether a character is a directory separator.
    /// </summary>
    private static bool IsDirectorySeparator(char c)
    {
        return c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar;
    }
}
