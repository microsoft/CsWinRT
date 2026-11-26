// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using Microsoft.Build.Framework;

namespace Microsoft.NET.Build.Tasks;

/// <summary>
/// Extensions for <see cref="ProcessStartInfo"/>.
/// </summary>
internal static class ProcessStartInfoExtensions
{
    extension(ProcessStartInfo startInfo)
    {
        /// <summary>
        /// Resolves <see cref="ProcessStartInfo.FileName"/> without relying on the current directory or the <c>PATH</c> of the MSBuild process.
        /// </summary>
        /// <param name="taskEnvironment">The <see cref="TaskEnvironment"/> of the task starting the process.</param>
        /// <remarks>
        /// <para>
        /// In multithreaded builds (<c>-mt</c>), the MSBuild process is shared by all projects, so neither its current directory nor its
        /// environment variables belong to the project being built. Starting a process would otherwise resolve a relative path against the
        /// former, and look up a bare executable name in the latter, instead of using the project directory and the environment of the task.
        /// </para>
        /// <para>
        /// A file name containing a directory is resolved against the project directory. A bare file name is looked up in the
        /// <c>PATH</c> of <see cref="ProcessStartInfo.EnvironmentVariables"/>, and left unchanged if it can't be found there.
        /// </para>
        /// </remarks>
        public void ResolveFileName(TaskEnvironment taskEnvironment)
        {
            string fileName = startInfo.FileName;

            if (string.IsNullOrEmpty(fileName) || fileName.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                return;
            }

            if (fileName.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, Path.VolumeSeparatorChar]) >= 0)
            {
                startInfo.FileName = taskEnvironment.TryGetAbsolutePath(fileName);

                return;
            }

            if (startInfo.EnvironmentVariables["PATH"] is not { Length: > 0 } path)
            {
                return;
            }

            // Like 'CreateProcess', append '.exe' if the file name has no extension
            string executableName = Path.HasExtension(fileName) ? fileName : fileName + ".exe";

            foreach (string directory in path.Split(Path.PathSeparator))
            {
                string trimmedDirectory = directory.Trim().Trim('"');

                if (trimmedDirectory.Length == 0 || trimmedDirectory.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                {
                    continue;
                }

                string candidate = Path.Combine(taskEnvironment.TryGetAbsolutePath(trimmedDirectory), executableName);

                if (File.Exists(candidate))
                {
                    startInfo.FileName = candidate;

                    return;
                }
            }
        }
    }
}
