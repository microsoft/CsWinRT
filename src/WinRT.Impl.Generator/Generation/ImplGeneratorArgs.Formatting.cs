// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;

namespace WindowsRuntime.ImplGenerator.Generation;

/// <inheritdoc cref="ImplGeneratorArgs"/>
internal partial class ImplGeneratorArgs
{
    /// <summary>
    /// Formats the current <see cref="ImplGeneratorArgs"/> instance into a response file text.
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

        _ = builder.Append(GetCommandLineArgumentName(nameof(TreatWarningsAsErrors)));
        _ = builder.Append(' ');
        _ = builder.AppendLine(TreatWarningsAsErrors.ToString());

        if (AssemblyOriginatorKeyFile is not null)
        {
            _ = builder.Append(GetCommandLineArgumentName(nameof(AssemblyOriginatorKeyFile)));
            _ = builder.Append(' ');
            _ = builder.AppendLine(AssemblyOriginatorKeyFile);
        }

        if (DebugReproDirectory is not null)
        {
            _ = builder.Append(GetCommandLineArgumentName(nameof(DebugReproDirectory)));
            _ = builder.Append(' ');
            _ = builder.AppendLine(DebugReproDirectory);
        }

        return builder.ToString();
    }
}
