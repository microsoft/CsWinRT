// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions for <see cref="File"/>.
/// </summary>
internal static class FileExtensions
{
    extension(File)
    {
        /// <summary>
        /// Reads all lines from a file.
        /// </summary>
        /// <returns>The lines from the input file.</returns>
        public static string[] ReadAllLines(Stream stream)
        {
            List<string> lines = [];

            using (StreamReader reader = new(stream, Encoding.UTF8))
            {
                string? line;

                while ((line = reader.ReadLine()) is not null)
                {
                    lines.Add(line);
                }
            }

            return [.. lines];
        }
    }
}
