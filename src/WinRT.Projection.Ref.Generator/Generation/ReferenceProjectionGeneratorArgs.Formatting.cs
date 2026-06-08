// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;

namespace WindowsRuntime.ReferenceProjectionGenerator.Generation;

/// <inheritdoc cref="ReferenceProjectionGeneratorArgs"/>
internal partial class ReferenceProjectionGeneratorArgs
{
    /// <summary>
    /// Formats the current <see cref="ReferenceProjectionGeneratorArgs"/> instance into a response file text.
    /// </summary>
    /// <returns>The resulting response file text.</returns>
    public string FormatToResponseFile()
    {
        StringBuilder builder = new();

        _ = builder.Append(GetCommandLineArgumentName(nameof(InputPaths)));
        _ = builder.Append(' ');
        _ = builder.AppendLine(string.Join(',', InputPaths));

        _ = builder.Append(GetCommandLineArgumentName(nameof(OutputDirectory)));
        _ = builder.Append(' ');
        _ = builder.AppendLine(OutputDirectory);

        _ = builder.Append(GetCommandLineArgumentName(nameof(TargetFramework)));
        _ = builder.Append(' ');
        _ = builder.AppendLine(TargetFramework);

        if (IncludeNamespaces.Length > 0)
        {
            _ = builder.Append(GetCommandLineArgumentName(nameof(IncludeNamespaces)));
            _ = builder.Append(' ');
            _ = builder.AppendLine(string.Join(',', IncludeNamespaces));
        }

        if (ExcludeNamespaces.Length > 0)
        {
            _ = builder.Append(GetCommandLineArgumentName(nameof(ExcludeNamespaces)));
            _ = builder.Append(' ');
            _ = builder.AppendLine(string.Join(',', ExcludeNamespaces));
        }

        if (AdditionExcludeNamespaces.Length > 0)
        {
            _ = builder.Append(GetCommandLineArgumentName(nameof(AdditionExcludeNamespaces)));
            _ = builder.Append(' ');
            _ = builder.AppendLine(string.Join(',', AdditionExcludeNamespaces));
        }

        if (Verbose)
        {
            _ = builder.Append(GetCommandLineArgumentName(nameof(Verbose)));
            _ = builder.Append(' ');
            _ = builder.AppendLine(Verbose.ToString());
        }

        if (Component)
        {
            _ = builder.Append(GetCommandLineArgumentName(nameof(Component)));
            _ = builder.Append(' ');
            _ = builder.AppendLine(Component.ToString());
        }

        if (PublicExclusiveTo)
        {
            _ = builder.Append(GetCommandLineArgumentName(nameof(PublicExclusiveTo)));
            _ = builder.Append(' ');
            _ = builder.AppendLine(PublicExclusiveTo.ToString());
        }

        if (IdicExclusiveTo)
        {
            _ = builder.Append(GetCommandLineArgumentName(nameof(IdicExclusiveTo)));
            _ = builder.Append(' ');
            _ = builder.AppendLine(IdicExclusiveTo.ToString());
        }

        if (ReferenceProjection)
        {
            _ = builder.Append(GetCommandLineArgumentName(nameof(ReferenceProjection)));
            _ = builder.Append(' ');
            _ = builder.AppendLine(ReferenceProjection.ToString());
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
