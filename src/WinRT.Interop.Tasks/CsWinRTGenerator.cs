// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace WinRT.Interop.Tasks;

/// <summary>
/// The custom MSBuild task that invokes the 'cswinrtgen' tool.
/// </summary>
public sealed class CsWinRTGenerator : ToolTask
{
    /// <summary>
    /// Gets or sets the paths to assembly files that are reference assemblies, representing
    /// the entire surface area for compilation. These assemblies are the full set of assemblies
    /// that will contribute to the interop .dll being generated.
    /// </summary>
    [Required]
    public ITaskItem[]? ReferenceAssemblyPaths { get; set; }

    /// <summary>
    /// Gets or sets the path to the output assembly that was produced by the build (for the current project).
    /// </summary>
    [Required]
    public ITaskItem? OutputAssemblyPath { get; set; }

    /// <summary>
    /// Gets or sets the output directory where the generated interop assembly will be placed.
    /// </summary>
    /// <remarks>If not set, the same directory as <see cref="OutputAssemblyPath"/> will be used.</remarks>
    public string? OutputDirectory { get; set; }

    /// <summary>
    /// Gets or sets the tools directory where the 'cswinrtgen' tool is located.
    /// </summary>
    [Required]
    public string? CsWinRTToolsDirectory { get; set; }

    /// <summary>
    /// Gets or sets whether to use <c>Windows.UI.Xaml</c> projections.
    /// </summary>
    /// <remarks>If not set, it will default to <see langword="false"/> (i.e. using <c>Microsoft.UI.Xaml</c> projections).</remarks>
    public bool UseWindowsUIXamlProjections { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum number of parallel tasks to use for execution.
    /// </summary>
    /// <remarks>If not set, the default will match the number of available processor cores.</remarks>
    public int MaxDegreesOfParallelism { get; set; } = -1;

    /// <inheritdoc/>
    protected override string ToolName => "cswinrtgen.exe";

    /// <inheritdoc/>
    [MemberNotNullWhen(true, nameof(ReferenceAssemblyPaths))]
    [MemberNotNullWhen(true, nameof(OutputAssemblyPath))]
    [MemberNotNullWhen(true, nameof(CsWinRTToolsDirectory))]
    protected override bool ValidateParameters()
    {
        if (!base.ValidateParameters())
        {
            return false;
        }

        if (ReferenceAssemblyPaths is not { Length: > 0 })
        {
            Log.LogWarning("Invalid 'ReferenceAssemblyPaths' input(s).");

            return false;
        }

        if (OutputAssemblyPath is null)
        {
            Log.LogWarning("Invalid 'OutputAssemblyPath' input.");

            return false;
        }

        if (OutputDirectory is not null && !Directory.Exists(OutputDirectory))
        {
            Log.LogWarning("Output directory '{0}' does not exist.", OutputDirectory);

            return false;
        }

        if (CsWinRTToolsDirectory is null || !Directory.Exists(CsWinRTToolsDirectory))
        {
            Log.LogWarning("Tools directory '{0}' does not exist.", CsWinRTToolsDirectory);

            return false;
        }

        if (MaxDegreesOfParallelism is 0 or < -1)
        {
            Log.LogWarning("Invalid 'MaxDegreesOfParallelism' value. It must be '-1' or greater than '0' (but was '{0}').", MaxDegreesOfParallelism);

            return false;
        }

        return true;
    }

    /// <inheritdoc/>
    [SuppressMessage("Style", "IDE0072", Justification = "We always use 'x86' as a fallback for all other CPU architectures.")]
    protected override string GenerateFullPathToTool()
    {
        // The tool is inside an architecture-specific subfolder, as it's a native binary
        string architectureDirectory = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "win-x64",
            Architecture.Arm64 => "win-arm64",
            _ => "win-x86"
        };

        return Path.Combine(CsWinRTToolsDirectory!, architectureDirectory, ToolName);
    }

    /// <inheritdoc/>
    protected override string GenerateResponseFileCommands()
    {
        StringBuilder args = new();

        IEnumerable<string> referenceAssemblyPaths = ReferenceAssemblyPaths!.Select(static path => path.ItemSpec);
        string referenceAssemblyPathsArg = string.Join(",", referenceAssemblyPaths);

        _ = args.Append("--reference-assembly-paths ").AppendLine(referenceAssemblyPathsArg);
        _ = args.Append("--output-assembly-path ").AppendLine(OutputAssemblyPath!.ItemSpec);

        string outputDirectory = OutputDirectory ?? Path.GetDirectoryName(OutputAssemblyPath!.ItemSpec)!;

        _ = args.Append("--output-directory ").AppendLine(outputDirectory);

        _ = args.Append("--use-windows-ui-xaml-projections").AppendLine(UseWindowsUIXamlProjections.ToString());
        _ = args.Append("--max-degrees-of-parallelism").AppendLine(MaxDegreesOfParallelism.ToString());

        return args.ToString();
    }
}
