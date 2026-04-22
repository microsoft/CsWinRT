// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.NET.Build.Tasks;

/// <summary>
/// The custom MSBuild task that invokes the 'cswinrtwinmdgen' tool.
/// </summary>
public sealed class RunCsWinRTWinMDGenerator : ToolTask
{
    /// <summary>
    /// Gets or sets the path to the compiled input assembly (.dll) to analyze.
    /// </summary>
    [Required]
    public string? InputAssemblyPath { get; set; }

    /// <summary>
    /// Gets or sets the paths to reference assembly files for type resolution.
    /// </summary>
    [Required]
    public ITaskItem[]? ReferenceAssemblyPaths { get; set; }

    /// <summary>
    /// Gets or sets the output .winmd file path.
    /// </summary>
    [Required]
    public string? OutputWinmdPath { get; set; }

    /// <summary>
    /// Gets or sets the assembly version to use for the generated WinMD.
    /// </summary>
    [Required]
    public string? AssemblyVersion { get; set; }

    /// <summary>
    /// Gets or sets whether to use <c>Windows.UI.Xaml</c> projections.
    /// </summary>
    public bool UseWindowsUIXamlProjections { get; set; } = false;

    /// <summary>
    /// Gets or sets the tools directory where the 'cswinrtwinmdgen' tool is located.
    /// </summary>
    [Required]
    public string? CsWinRTToolsDirectory { get; set; }

    /// <summary>
    /// Gets or sets the architecture of 'cswinrtwinmdgen' to use.
    /// </summary>
    public string? CsWinRTToolsArchitecture { get; set; }

    /// <inheritdoc/>
    protected override string ToolName => "cswinrtwinmdgen.exe";

    /// <inheritdoc/>
#if NET10_0_OR_GREATER
    [MemberNotNullWhen(true, nameof(InputAssemblyPath))]
    [MemberNotNullWhen(true, nameof(ReferenceAssemblyPaths))]
    [MemberNotNullWhen(true, nameof(OutputWinmdPath))]
    [MemberNotNullWhen(true, nameof(AssemblyVersion))]
    [MemberNotNullWhen(true, nameof(CsWinRTToolsDirectory))]
#endif
    protected override bool ValidateParameters()
    {
        if (!base.ValidateParameters())
        {
            return false;
        }

        if (string.IsNullOrEmpty(InputAssemblyPath) || !File.Exists(InputAssemblyPath))
        {
            Log.LogWarning("Input assembly path '{0}' is invalid or does not exist.", InputAssemblyPath);
            return false;
        }

        if (ReferenceAssemblyPaths is not { Length: > 0 })
        {
            Log.LogWarning("Invalid 'ReferenceAssemblyPaths' input(s).");
            return false;
        }

        if (string.IsNullOrEmpty(OutputWinmdPath))
        {
            Log.LogWarning("Invalid 'OutputWinmdPath' input.");
            return false;
        }

        if (string.IsNullOrEmpty(AssemblyVersion))
        {
            Log.LogWarning("Invalid 'AssemblyVersion' input.");
            return false;
        }

        if (CsWinRTToolsDirectory is null || !Directory.Exists(CsWinRTToolsDirectory))
        {
            Log.LogWarning("Tools directory '{0}' is invalid or does not exist.", CsWinRTToolsDirectory);
            return false;
        }

        return true;
    }

    /// <inheritdoc/>
    [SuppressMessage("Style", "IDE0072", Justification = "We always use 'x64' as a fallback for all other CPU architectures.")]
    protected override string GenerateFullPathToTool()
    {
        string? effectiveArchitecture = CsWinRTToolsArchitecture;

        // Special case for when 'AnyCPU' is specified (mostly for testing scenarios).
        if (effectiveArchitecture?.Equals("AnyCPU", System.StringComparison.OrdinalIgnoreCase) is true)
        {
            return Path.Combine(CsWinRTToolsDirectory!, ToolName);
        }

        // If the architecture is not specified, determine it based on the current process architecture
        effectiveArchitecture ??= RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.Arm64 => "arm64",
            _ => "x64"
        };

        string architectureDirectory = $"win-{effectiveArchitecture}";

        return Path.Combine(CsWinRTToolsDirectory!, architectureDirectory, ToolName);
    }

    /// <inheritdoc/>
    protected override string GenerateResponseFileCommands()
    {
        StringBuilder args = new();

        AppendResponseFileCommand(args, "--input-assembly-path", InputAssemblyPath!);

        string referenceAssemblyPathsArg = string.Join(",", ReferenceAssemblyPaths!.Select(static path => path.ItemSpec));
        AppendResponseFileCommand(args, "--reference-assembly-paths", referenceAssemblyPathsArg);

        AppendResponseFileCommand(args, "--output-winmd-path", OutputWinmdPath!);
        AppendResponseFileCommand(args, "--assembly-version", AssemblyVersion!);
        AppendResponseFileCommand(args, "--use-windows-ui-xaml-projections", UseWindowsUIXamlProjections.ToString());

        return args.ToString();
    }

    /// <summary>
    /// Appends a command line argument to the response file arguments, with the right format.
    /// </summary>
    private static void AppendResponseFileCommand(StringBuilder args, string commandName, string commandValue)
    {
        _ = args.Append($"{commandName} ").AppendLine(commandValue);
    }
}
