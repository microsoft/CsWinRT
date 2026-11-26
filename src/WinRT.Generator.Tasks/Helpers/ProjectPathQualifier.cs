// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma warning disable IDE0046

namespace Microsoft.NET.Build.Tasks;

/// <summary>
/// Helpers to qualify paths against the project directory.
/// </summary>
internal static class ProjectPathQualifier
{
    /// <summary>
    /// Fully qualifies a path that is rooted but partially qualified, using the drive (or share) and directory of the project.
    /// </summary>
    /// <param name="path">The path to qualify.</param>
    /// <param name="projectDirectory">The project directory.</param>
    /// <returns>The qualified path, or <paramref name="path"/> if it's already fully qualified or can't be qualified.</returns>
    /// <remarks>
    /// <para>
    /// A root-relative path (eg. <c>\foo</c>) is resolved against the drive (or UNC share) of the project. A drive-relative path
    /// (eg. <c>C:foo</c>) is resolved against the project directory if it is on the drive of the project. For any other drive,
    /// the current directory of that drive is the only possible base, so the path is left unchanged. Paths that aren't partially
    /// qualified (relative paths, fully qualified paths, UNC paths, and device paths) are returned unchanged.
    /// </para>
    /// </remarks>
    public static string Qualify(string path, string projectDirectory)
    {
        if (path.Length == 0 || projectDirectory.Length == 0)
        {
            return path;
        }

        // Root-relative path (eg. '\foo'), but not a UNC or device path (eg. '\\server\share', '\\?\C:\foo'), which are fully
        // qualified, nor an NT object path (eg. '\??\C:\foo'), which is not relative to the drive of the project either.
        if (IsDirectorySeparator(path[0]) &&
            (path.Length == 1 || !IsDirectorySeparator(path[1])) &&
            !path.StartsWith(@"\??\", StringComparison.Ordinal))
        {
            string? root = Path.GetPathRoot(projectDirectory);

            if (string.IsNullOrEmpty(root))
            {
                return path;
            }

            return Path.Combine(root, path.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }

        // Drive-relative path (eg. 'C:foo'), which resolves against the project directory if it's on the same drive
        if (path.Length >= 2 &&
            path[1] == ':' &&
            (path.Length == 2 || !IsDirectorySeparator(path[2])) &&
            projectDirectory.Length >= 2 &&
            projectDirectory[1] == ':' &&
            char.ToUpperInvariant(path[0]) == char.ToUpperInvariant(projectDirectory[0]))
        {
            return Path.Combine(projectDirectory, path[2..]);
        }

        return path;
    }

    /// <summary>
    /// Checks whether a character is a directory separator.
    /// </summary>
    private static bool IsDirectorySeparator(char c)
    {
        return c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar;
    }
}
