// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Build.Framework;

namespace Microsoft.NET.Build.Tasks;

/// <summary>
/// Extensions for <see cref="TaskEnvironment"/>.
/// </summary>
internal static class TaskEnvironmentExtensions
{
    extension(TaskEnvironment taskEnvironment)
    {
        /// <summary>
        /// Resolves a path against the project directory, without relying on the process current directory.
        /// </summary>
        /// <param name="path">The path to resolve.</param>
        /// <returns>
        /// The resolved absolute path. If <paramref name="path"/> is <see langword="null"/> or empty, or it's not a valid path
        /// (eg. it contains invalid characters), it is returned unchanged. This makes the result safe to pass to APIs such
        /// as <see cref="File.Exists(string)"/> or <see cref="Directory.Exists(string)"/>, which report such inputs as missing.
        /// </returns>
        /// <remarks>
        /// <see cref="TaskEnvironment.GetAbsolutePath"/> leaves root-relative paths (eg. <c>\foo\bar.c</c>) and drive-relative paths
        /// (eg. <c>C:foo\bar.c</c>) as they are, and Windows would then resolve them against the current drive and the current directory
        /// of that drive in the MSBuild process, which aren't the ones of the project. So these are qualified against the project too.
        /// </remarks>
        [return: NotNullIfNotNull(nameof(path))]
        public string? TryGetAbsolutePath(string? path)
        {
            // 'GetAbsolutePath' doesn't report failures (it returns a struct and throws on invalid input), so validate the input
            // upfront instead of catching exceptions. Paths containing invalid characters are returned as is, like empty paths.
            return path is null || path.Length == 0 || path.IndexOfAny(Path.GetInvalidPathChars()) >= 0
                ? path
                : ProjectPathQualifier.Qualify(taskEnvironment.GetAbsolutePath(path).Value, taskEnvironment.ProjectDirectory.Value);
        }
    }
}
