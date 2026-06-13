// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;

namespace WindowsRuntime.WinMDGenerator.Generation;

/// <inheritdoc cref="WinMDGeneratorArgs"/>
internal partial class WinMDGeneratorArgs
{
    /// <summary>
    /// Formats the current <see cref="WinMDGeneratorArgs"/> instance into a response file text.
    /// </summary>
    /// <returns>The resulting response file text.</returns>
    public string FormatToResponseFile()
    {
        StringBuilder builder = new();

        _ = builder.Append(GetCommandLineArgumentName(nameof(InputAssemblyPath)));
        _ = builder.Append(' ');
        _ = builder.AppendLine(InputAssemblyPath);

        _ = builder.Append(GetCommandLineArgumentName(nameof(ReferenceAssemblyPaths)));
        _ = builder.Append(' ');
        _ = builder.AppendLine(string.Join(',', ReferenceAssemblyPaths));

        _ = builder.Append(GetCommandLineArgumentName(nameof(OutputWinmdPath)));
        _ = builder.Append(' ');
        _ = builder.AppendLine(OutputWinmdPath);

        _ = builder.Append(GetCommandLineArgumentName(nameof(AssemblyVersion)));
        _ = builder.Append(' ');
        _ = builder.AppendLine(AssemblyVersion);

        _ = builder.Append(GetCommandLineArgumentName(nameof(UseWindowsUIXamlProjections)));
        _ = builder.Append(' ');
        _ = builder.AppendLine(UseWindowsUIXamlProjections.ToString());

        if (DebugReproDirectory is not null)
        {
            _ = builder.Append(GetCommandLineArgumentName(nameof(DebugReproDirectory)));
            _ = builder.Append(' ');
            _ = builder.AppendLine(DebugReproDirectory);
        }

        return builder.ToString();
    }
}
