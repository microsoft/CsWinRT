// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;

namespace WindowsRuntime.ProjectionGenerator.Generation;

/// <inheritdoc cref="ProjectionGeneratorArgs"/>
internal partial class ProjectionGeneratorArgs
{
    /// <summary>
    /// Formats the current <see cref="ProjectionGeneratorArgs"/> instance into a response file text.
    /// </summary>
    /// <returns>The resulting response file text.</returns>
    public string FormatToResponseFile()
    {
        StringBuilder builder = new();

        _ = builder.Append(GetCommandLineArgumentName(nameof(ReferenceAssemblyPaths)));
        _ = builder.Append(' ');
        _ = builder.AppendLine(string.Join(',', ReferenceAssemblyPaths));

        _ = builder.Append(GetCommandLineArgumentName(nameof(GeneratedAssemblyDirectory)));
        _ = builder.Append(' ');
        _ = builder.AppendLine(GeneratedAssemblyDirectory);

        _ = builder.Append(GetCommandLineArgumentName(nameof(WinMDPaths)));
        _ = builder.Append(' ');
        _ = builder.AppendLine(string.Join(',', WinMDPaths));

        _ = builder.Append(GetCommandLineArgumentName(nameof(TargetFramework)));
        _ = builder.Append(' ');
        _ = builder.AppendLine(TargetFramework);

        _ = builder.Append(GetCommandLineArgumentName(nameof(WindowsMetadata)));
        _ = builder.Append(' ');
        _ = builder.AppendLine(WindowsMetadata);

        _ = builder.Append(GetCommandLineArgumentName(nameof(AssemblyName)));
        _ = builder.Append(' ');
        _ = builder.AppendLine(AssemblyName);

        if (WindowsSdkOnly)
        {
            _ = builder.Append(GetCommandLineArgumentName(nameof(WindowsSdkOnly)));
            _ = builder.Append(' ');
            _ = builder.AppendLine(WindowsSdkOnly.ToString());
        }

        if (WindowsUIXamlProjection)
        {
            _ = builder.Append(GetCommandLineArgumentName(nameof(WindowsUIXamlProjection)));
            _ = builder.Append(' ');
            _ = builder.AppendLine(WindowsUIXamlProjection.ToString());
        }

        _ = builder.Append(GetCommandLineArgumentName(nameof(MaxDegreesOfParallelism)));
        _ = builder.Append(' ');
        _ = builder.AppendLine(MaxDegreesOfParallelism.ToString());

        if (DebugReproDirectory is not null)
        {
            _ = builder.Append(GetCommandLineArgumentName(nameof(DebugReproDirectory)));
            _ = builder.Append(' ');
            _ = builder.AppendLine(DebugReproDirectory);
        }

        return builder.ToString();
    }
}
