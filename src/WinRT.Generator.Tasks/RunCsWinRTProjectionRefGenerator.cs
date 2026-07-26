// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.NET.Build.Tasks;

/// <summary>
/// The custom MSBuild task that invokes the 'cswinrtprojectionrefgen' tool to generate
/// reference-projection C# sources. The tool produces .cs files that get included in
/// the user's library or component compilation via the 'CsWinRTIncludeProjection' target.
/// </summary>
public sealed class RunCsWinRTProjectionRefGenerator : ToolTask
{
    /// <summary>
    /// Gets or sets the input WinMD files (or directories that should be recursively scanned)
    /// providing the Windows Runtime metadata to project.
    /// </summary>
    [Required]
    public ITaskItem[]? InputWinMDPaths { get; set; }

    /// <summary>
    /// Gets or sets the directory where the generated .cs files will be placed.
    /// </summary>
    [Required]
    public string? OutputDirectory { get; set; }

    /// <summary>
    /// Gets or sets the target framework for which the projection is being generated.
    /// CsWinRT 3.0 requires this to be 'net10.0' or later.
    /// </summary>
    [Required]
    public string? TargetFramework { get; set; }

    /// <summary>
    /// Gets or sets the optional Windows metadata token (literal path, 'local', 'sdk', 'sdk+',
    /// or a version string like '10.0.26100.0'). The token is expanded in-tool to the actual
    /// set of .winmd files.
    /// </summary>
    public string? WindowsMetadata { get; set; }

    /// <summary>
    /// Gets or sets the namespace prefixes to include in the projection.
    /// </summary>
    public ITaskItem[]? IncludeNamespaces { get; set; }

    /// <summary>
    /// Gets or sets the namespace prefixes to exclude from the projection.
    /// </summary>
    public ITaskItem[]? ExcludeNamespaces { get; set; }

    /// <summary>
    /// Gets or sets the namespace prefixes to exclude from the projection additions.
    /// </summary>
    public ITaskItem[]? AdditionExcludeNamespaces { get; set; }

    /// <summary>
    /// Gets or sets whether to enable verbose progress logging.
    /// </summary>
    public bool Verbose { get; set; }

    /// <summary>
    /// Gets or sets whether to generate a Windows Runtime component projection.
    /// </summary>
    public bool Component { get; set; }

    /// <summary>
    /// Gets or sets whether to make exclusive-to interfaces public in the projection
    /// (default is internal).
    /// </summary>
    public bool PublicExclusiveTo { get; set; }

    /// <summary>
    /// Gets or sets whether to generate abstract base classes so runtime classes in the input
    /// metadata can be authored (implemented) in C#.
    /// </summary>
    public bool AuthorExclusiveToInterfaces { get; set; }

    /// <summary>
    /// Gets or sets whether to emit only the authoring surface (interfaces + abstract bases +
    /// lookup, scoped to the authored types) for compilation into a component's own assembly.
    /// </summary>
    public bool ImplementWinMDTypes { get; set; }

    /// <summary>
    /// Gets or sets whether exclusive-to interfaces should support IDynamicInterfaceCastable.
    /// </summary>
    public bool IdicExclusiveTo { get; set; }

    /// <summary>
    /// Gets or sets whether the generated projection should be a reference assembly projection.
    /// </summary>
    public bool ReferenceProjection { get; set; }

    /// <summary>
    /// Gets or sets the directory where the debug repro will be produced.
    /// </summary>
    /// <remarks>If not set, no debug repro will be produced.</remarks>
    public string? DebugReproDirectory { get; set; }

    /// <summary>
    /// Gets or sets the tools directory where the 'cswinrtprojectionrefgen' tool is located.
    /// </summary>
    [Required]
    public string? CsWinRTToolsDirectory { get; set; }

    /// <summary>
    /// Gets or sets the architecture of 'cswinrtprojectionrefgen' to use.
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
    protected override string ToolName => "cswinrtprojectionrefgen.exe";

    /// <inheritdoc/>
#if NET10_0_OR_GREATER
    [MemberNotNullWhen(true, nameof(InputWinMDPaths))]
    [MemberNotNullWhen(true, nameof(OutputDirectory))]
    [MemberNotNullWhen(true, nameof(TargetFramework))]
    [MemberNotNullWhen(true, nameof(CsWinRTToolsDirectory))]
#endif
    protected override bool ValidateParameters()
    {
        if (!base.ValidateParameters())
        {
            return false;
        }

        if (InputWinMDPaths is not { Length: > 0 })
        {
            Log.LogWarning("Invalid 'InputWinMDPaths' input(s).");

            return false;
        }

        if (string.IsNullOrEmpty(OutputDirectory))
        {
            Log.LogWarning("'OutputDirectory' is required.");

            return false;
        }

        if (string.IsNullOrEmpty(TargetFramework))
        {
            Log.LogWarning("'TargetFramework' is required.");

            return false;
        }

        if (CsWinRTToolsDirectory is null || !Directory.Exists(CsWinRTToolsDirectory))
        {
            Log.LogWarning("Tools directory '{0}' is invalid or does not exist.", CsWinRTToolsDirectory);

            return false;
        }

        if (DebugReproDirectory is not null && !Directory.Exists(DebugReproDirectory))
        {
            Log.LogWarning("Debug repro directory '{0}' is invalid or does not exist.", DebugReproDirectory);

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
    [SuppressMessage("Style", "IDE0072", Justification = "We always use 'x64' as a fallback for all other CPU architectures.")]
    protected override string GenerateFullPathToTool()
    {
        string? effectiveArchitecture = CsWinRTToolsArchitecture;

        // Special case for when 'AnyCPU' is specified (mostly for testing scenarios).
        // We just reuse the exact input directory and assume the architecture matches.
        // This makes it easy to run the task against a local build of 'cswinrtprojectionrefgen'.
        if (effectiveArchitecture?.Equals("AnyCPU", StringComparison.OrdinalIgnoreCase) is true)
        {
            return Path.Combine(CsWinRTToolsDirectory, ToolName);
        }

        // If the architecture is not specified, determine it based on the current process architecture
        effectiveArchitecture ??= RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.Arm64 => "arm64",
            _ => "x64"
        };

        // The tool is inside an architecture-specific subfolder, as it's a native binary
        string architectureDirectory = $"win-{effectiveArchitecture}";

        return Path.Combine(CsWinRTToolsDirectory, architectureDirectory, ToolName);
    }

    /// <inheritdoc/>
    protected override string GenerateResponseFileCommands()
    {
        StringBuilder args = new();

        IEnumerable<string> inputWinMDPaths = InputWinMDPaths!.Select(static path => path.ItemSpec);
        string inputWinMDPathsArg = string.Join(",", inputWinMDPaths);

        AppendResponseFileCommand(args, "--input-paths", inputWinMDPathsArg);
        AppendResponseFileCommand(args, "--output-directory", OutputDirectory!);
        AppendResponseFileCommand(args, "--target-framework", TargetFramework!);

        if (!string.IsNullOrEmpty(WindowsMetadata))
        {
            AppendResponseFileCommand(args, "--windows-metadata", WindowsMetadata!);
        }

        if (IncludeNamespaces is { Length: > 0 })
        {
            AppendResponseFileCommand(args, "--include-namespaces", string.Join(",", IncludeNamespaces.Select(static i => i.ItemSpec)));
        }

        if (ExcludeNamespaces is { Length: > 0 })
        {
            AppendResponseFileCommand(args, "--exclude-namespaces", string.Join(",", ExcludeNamespaces.Select(static i => i.ItemSpec)));
        }

        if (AdditionExcludeNamespaces is { Length: > 0 })
        {
            AppendResponseFileCommand(args, "--addition-exclude-namespaces", string.Join(",", AdditionExcludeNamespaces.Select(static i => i.ItemSpec)));
        }

        if (Verbose)
        {
            AppendResponseFileCommand(args, "--verbose", "true");
        }

        if (Component)
        {
            AppendResponseFileCommand(args, "--component", "true");
        }

        if (PublicExclusiveTo)
        {
            AppendResponseFileCommand(args, "--public-exclusive-to", "true");
        }

        if (AuthorExclusiveToInterfaces)
        {
            AppendResponseFileCommand(args, "--author-exclusive-to", "true");
        }

        if (ImplementWinMDTypes)
        {
            AppendResponseFileCommand(args, "--implement-winmd-types", "true");
        }

        if (IdicExclusiveTo)
        {
            AppendResponseFileCommand(args, "--idic-exclusive-to", "true");
        }

        if (ReferenceProjection)
        {
            AppendResponseFileCommand(args, "--reference-projection", "true");
        }

        AppendResponseFileOptionalCommand(args, "--debug-repro-directory", DebugReproDirectory);

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

    /// <summary>
    /// Appends an optional command line argument to the response file arguments, with the right format.
    /// </summary>
    /// <param name="args">The command line arguments being built.</param>
    /// <param name="commandName">The command name to append.</param>
    /// <param name="commandValue">The optional command value to append.</param>
    /// <remarks>This method will not append the command if <paramref name="commandValue"/> is <see langword="null"/>.</remarks>
    private static void AppendResponseFileOptionalCommand(StringBuilder args, string commandName, string? commandValue)
    {
        if (commandValue is not null)
        {
            AppendResponseFileCommand(args, commandName, commandValue);
        }
    }
}