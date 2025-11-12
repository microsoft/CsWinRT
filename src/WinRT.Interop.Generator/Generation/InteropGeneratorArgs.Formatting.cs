// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;

namespace WindowsRuntime.InteropGenerator.Generation;

/// <inheritdoc cref="InteropGeneratorArgs"/>
internal partial class InteropGeneratorArgs
{
    /// <summary>
    /// Formats the current <see cref="InteropGeneratorArgs"/> instance into a response file text.
    /// </summary>
    /// <returns>The resulting response file text.</returns>
    public string FormatToResponseFile()
    {
        StringBuilder builder = new();

        _ = builder.Append(GetCommandLineArgumentName(nameof(ReferenceAssemblyPaths)));
        _ = builder.Append(' ');
        _ = builder.AppendLine(string.Join(',', ReferenceAssemblyPaths));

        _ = builder.Append(GetCommandLineArgumentName(nameof(OutputAssemblyPath)));
        _ = builder.Append(' ');
        _ = builder.AppendLine(OutputAssemblyPath);

        _ = builder.Append(GetCommandLineArgumentName(nameof(GeneratedAssemblyDirectory)));
        _ = builder.Append(' ');
        _ = builder.AppendLine(GeneratedAssemblyDirectory);

        _ = builder.Append(GetCommandLineArgumentName(nameof(UseWindowsUIXamlProjections)));
        _ = builder.Append(' ');
        _ = builder.AppendLine(UseWindowsUIXamlProjections.ToString());

        _ = builder.Append(GetCommandLineArgumentName(nameof(ValidateWinRTRuntimeAssemblyVersion)));
        _ = builder.Append(' ');
        _ = builder.AppendLine(ValidateWinRTRuntimeAssemblyVersion.ToString());

        _ = builder.Append(GetCommandLineArgumentName(nameof(ValidateWinRTRuntimeDllVersion2References)));
        _ = builder.Append(' ');
        _ = builder.AppendLine(ValidateWinRTRuntimeDllVersion2References.ToString());

        _ = builder.Append(GetCommandLineArgumentName(nameof(EnableIncrementalGeneration)));
        _ = builder.Append(' ');
        _ = builder.AppendLine(EnableIncrementalGeneration.ToString());

        _ = builder.Append(GetCommandLineArgumentName(nameof(TreatWarningsAsErrors)));
        _ = builder.Append(' ');
        _ = builder.AppendLine(TreatWarningsAsErrors.ToString());

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
