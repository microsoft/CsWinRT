// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Reflection;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter;

/// <inheritdoc cref="ProjectionGenerator"/>
internal sealed partial class ProjectionGenerator
{
    /// <summary>
    /// Writes the embedded string resources (e.g., ComInteropExtensions.cs, InspectableVftbl.cs)
    /// to the output folder.
    /// </summary>
    private void WriteBaseStrings()
    {
        Assembly asm = typeof(ProjectionWriter).Assembly;
        foreach (string resName in asm.GetManifestResourceNames())
        {
            // Resource names look like 'WindowsRuntime.ProjectionWriter.Resources.Base.ComInteropExtensions.cs'
            if (!resName.Contains(".Resources.Base."))
            {
                continue;
            }
            // Skip ComInteropExtensions if Windows is not included
            string fileName = resName[(resName.IndexOf(".Resources.Base.", StringComparison.Ordinal) + ".Resources.Base.".Length)..];
            if (fileName == "ComInteropExtensions.cs" && !_settings.Filter.Includes("Windows"))
            {
                continue;
            }

            using Stream stream = asm.GetManifestResourceStream(resName)!;
            using StreamReader reader = new(stream);
            string content = reader.ReadToEnd();

            // For ComInteropExtensions, prepend the UAC_VERSION define
            if (fileName == "ComInteropExtensions.cs")
            {
                int uapContractVersion = _cache.Find("Windows.Graphics.Display.DisplayInformation") is not null ? 15 : 7;
                content = $"#define UAC_VERSION_{uapContractVersion}\n" + content;
            }

            // See main.cpp where 'write_file_header(ws);' is called before each base string is written.
            IndentedTextWriter headerWriter = new();
            MetadataAttributeFactory.WriteFileHeader(headerWriter);
            string header = headerWriter.FlushToString();

            string outPath = Path.Combine(_settings.OutputFolder, fileName);
            File.WriteAllText(outPath, header + content);
        }
    }
}
