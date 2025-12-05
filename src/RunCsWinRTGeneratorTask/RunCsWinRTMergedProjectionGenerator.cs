// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.NET.Build.Tasks;

/// <summary>
/// The custom MSBuild task that invokes the 'cswinrtprojectiongen' tool.
/// </summary>
public sealed class RunCsWinRTMergedProjectionGenerator : ToolTask
{
    /// <summary>
    /// Gets or sets the paths to assembly files that are reference assemblies, representing
    /// the entire surface area for compilation. These assemblies are the full set of assemblies
    /// that will contribute to the interop .dll being generated.
    /// </summary>
    [Required]
    public ITaskItem[]? ReferenceAssemblyPaths { get; set; }

    /// <summary>
    /// Gets or sets the directory where the generated assembly will be placed.
    /// </summary>
    [Required]
    public string? GeneratedAssemblyDirectory { get; set; }

    /// <summary>
    /// Gets or sets the paths to the WinMD files needed by the cswinrt tool
    /// to generate the merged projection.
    /// </summary>
    [Required]
    public string[]? WinMDPaths { get; set; }

    /// <summary>
    /// Gets or sets the target framework for which the projection is being generated.
    /// </summary>
    public string? TargetFramework { get; set; }

    /// <summary>
    /// Gets or sets the target Windows WinMD or version for which the projection targets.
    /// </summary>
    public string? WindowsMetadata { get; set; }

    /// <summary>
    /// Gets or sets the path to the cswinrt.exe tool.
    /// </summary>
    public string? CsWinRTExePath { get; set; }

    /// <summary>
    /// Gets or sets the tools directory where the 'cswinrtprojectiongen' tool is located.
    /// </summary>
    [Required]
    public string? CsWinRTToolsDirectory { get; set; }

    /// <summary>
    /// Gets or sets the architecture of 'cswinrtprojectiongen' to use.
    /// </summary>
    /// <remarks>
    /// If not set, the architecture will be determined based on the current process architecture.
    /// </remarks>
    public string? CsWinRTToolsArchitecture { get; set; }

    /// <summary>
    /// Gets or sets additional arguments to pass to the tool.
    /// </summary>
    public ITaskItem[]? AdditionalArguments { get; set; }

    /// <inheritdoc/>
    protected override string ToolName => "cswinrtprojectiongen.exe";

    /// <inheritdoc/>
#if NET10_0_OR_GREATER
    [MemberNotNullWhen(true, nameof(ReferenceAssemblyPaths))]
    [MemberNotNullWhen(true, nameof(GeneratedAssemblyDirectory))]
    [MemberNotNullWhen(true, nameof(WinMDPaths))]
    [MemberNotNullWhen(true, nameof(TargetFramework))]
    [MemberNotNullWhen(true, nameof(WindowsMetadata))]
    [MemberNotNullWhen(true, nameof(CsWinRTPath))]
    [MemberNotNullWhen(true, nameof(CsWinRTToolsDirectory))]
#endif
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

        if (GeneratedAssemblyDirectory is null || !Directory.Exists(GeneratedAssemblyDirectory))
        {
            Log.LogWarning("Generated assembly directory '{0}' is invalid or does not exist.", GeneratedAssemblyDirectory);

            return false;
        }

        if (WinMDPaths is not { Length: > 0 })
        {
            Log.LogWarning("Invalid 'WinMDPaths' input(s).");

            return false;
        }

        if (CsWinRTToolsDirectory is null || !Directory.Exists(CsWinRTToolsDirectory))
        {
            Log.LogWarning("Tools directory '{0}' is invalid or does not exist.", CsWinRTToolsDirectory);

            return false;
        }

        if (CsWinRTToolsArchitecture is not null &&
            !CsWinRTToolsArchitecture.Equals("x86", StringComparison.OrdinalIgnoreCase) &&
            !CsWinRTToolsArchitecture.Equals("x64", StringComparison.OrdinalIgnoreCase) &&
            !CsWinRTToolsArchitecture.Equals("arm64", StringComparison.OrdinalIgnoreCase) &&
            !CsWinRTToolsArchitecture.Equals("AnyCPU", StringComparison.OrdinalIgnoreCase))
        {
            Log.LogWarning("Tools architecture '{0}' is invalid (it must be 'x86', 'x64', 'arm64', or 'AnyCPU').", CsWinRTToolsArchitecture);

            return false;
        }

        return true;
    }

    /// <inheritdoc/>
    [SuppressMessage("Style", "IDE0072", Justification = "We always use 'x86' as a fallback for all other CPU architectures.")]
    protected override string GenerateFullPathToTool()
    {
        string? effectiveArchitecture = CsWinRTToolsArchitecture;

        // Special case for when 'AnyCPU' is specified (mostly for testing scenarios).
        // We just reuse the exact input directory and assume the architecture matches.
        // This makes it easy to run the task against a local build of 'cswinrtgen'.
        if (effectiveArchitecture?.Equals("AnyCPU", StringComparison.OrdinalIgnoreCase) is true)
        {
            return Path.Combine(CsWinRTToolsDirectory!, ToolName);
        }

        // If the architecture is not specified, determine it based on the current process architecture
        effectiveArchitecture ??= RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.Arm64 => "arm64",
            _ => "x86"
        };

        // The tool is inside an architecture-specific subfolder, as it's a native binary
        string architectureDirectory = $"win-{effectiveArchitecture}";

        return Path.Combine(CsWinRTToolsDirectory!, architectureDirectory, ToolName);
    }

    /// <inheritdoc/>
    protected override string GenerateResponseFileCommands()
    {
        StringBuilder args = new();

        IEnumerable<string> referenceAssemblyPaths = ReferenceAssemblyPaths!.Select(static path => path.ItemSpec);
        string referenceAssemblyPathsArg = string.Join(",", referenceAssemblyPaths);

        string winMDPathsArg = string.Join(",", WinMDPaths);

        AppendResponseFileCommand(args, "--reference-assembly-paths", referenceAssemblyPathsArg);
        AppendResponseFileCommand(args, "--generated-assembly-directory", GeneratedAssemblyDirectory!);
        AppendResponseFileCommand(args, "--winmd-paths", winMDPathsArg);
        AppendResponseFileCommand(args, "--target-framework", TargetFramework!);
        AppendResponseFileCommand(args, "--windows-metadata", WindowsMetadata!);
        AppendResponseFileCommand(args, "--cswinrt-exe-path", CsWinRTExePath!);

        // Add any additional arguments that are not statically known
        foreach (ITaskItem additionalArgument in AdditionalArguments ?? [])
        {
            _ = args.AppendLine(additionalArgument.ItemSpec);
        }

        return args.ToString();
    }

    /// <summary>
    /// Appends a command line argument to the response file arguments, with the right format.
    /// </summary>
    /// <param name="args">The command line arguments being built.</param>
    /// <param name="commandName">The command name to append.</param>
    /// <param name="commandValue">The command value to append.</param>
    private static void AppendResponseFileCommand(StringBuilder args, string commandName, string commandValue)
    {
        _ = args.Append($"{commandName} ").AppendLine(commandValue);
    }
}