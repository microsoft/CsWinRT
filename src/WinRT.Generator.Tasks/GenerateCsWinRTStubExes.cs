// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.NET.Build.Tasks;

/// <summary>
/// An MSBuild task that generates one or more stub <c>.exe</c> files by invoking MSVC (<c>cl.exe</c>).
/// </summary>
/// <remarks>
/// <para>
/// A "stub <c>.exe</c>" is a small native executable whose only job is to call into a method
/// exported from a companion <c>.dll</c> produced by the Native AOT toolchain. This is needed
/// when the project is compiled as a shared library (i.e. <c>NativeLib=Shared</c>), and the
/// application still needs a standard <c>.exe</c> entry point.
/// </para>
/// <para>
/// Each item in <see cref="StubExes"/> describes one stub to generate. The item identity
/// (<c>Include</c>) is used as the output binary name (i.e. <c>{Identity}.exe</c>), and
/// optional metadata controls per-stub behavior:
/// </para>
/// <list type="table">
///   <listheader>
///     <term>Metadata</term>
///     <description>Description</description>
///   </listheader>
///   <item>
///     <term><c>OutputType</c></term>
///     <description>
///       The output type for the stub, controlling the PE subsystem.
///       <c>WinExe</c> produces a <c>/SUBSYSTEM:WINDOWS</c> binary;
///       any other value (including <c>Exe</c>) produces <c>/SUBSYSTEM:CONSOLE</c>.
///       Defaults to the project's <c>OutputType</c> (see <see cref="DefaultOutputType"/>).
///     </description>
///   </item>
///   <item>
///     <term><c>Win32Manifest</c></term>
///     <description>
///       Path to a Win32 manifest file to embed into the stub <c>.exe</c>.
///       If empty, the stub is produced with <c>/MANIFEST:NO</c>.
///       Defaults to the project's <c>Win32Manifest</c> (see <see cref="DefaultWin32Manifest"/>).
///     </description>
///   </item>
///   <item>
///     <term><c>AppContainer</c></term>
///     <description>
///       Whether to set the <c>/APPCONTAINER</c> linker flag, for UWP apps.
///       Defaults to <see cref="DefaultAppContainer"/>.
///     </description>
///   </item>
///   <item>
///     <term><c>Platform</c></term>
///     <description>
///       The target platform (<c>x64</c>, <c>x86</c>, or <c>arm64</c>).
///       Defaults to <see cref="DefaultPlatform"/>.
///     </description>
///   </item>
///   <item>
///     <term><c>SourceText</c></term>
///     <description>
///       Inline C source code to use for this stub. When set, the text is written
///       to a <c>.c</c> file in the intermediate directory and compiled. Takes
///       precedence over <c>SourceFile</c> and the default <see cref="DefaultSourceFilePath"/>.
///     </description>
///   </item>
///   <item>
///     <term><c>SourceFile</c></term>
///     <description>
///       Path to a custom <c>.c</c> source file to use for this stub. When set,
///       the file is copied to the intermediate directory and compiled. Takes
///       precedence over the default <see cref="DefaultSourceFilePath"/>, but is
///       overridden by <c>SourceText</c> if both are specified.
///     </description>
///   </item>
/// </list>
/// </remarks>
public sealed class GenerateCsWinRTStubExes : Microsoft.Build.Utilities.Task
{
    /// <summary>
    /// Gets or sets the collection of stub executables to generate.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The item identity (<c>Include</c>) is the stub name (used as the output <c>.exe</c> filename).
    /// Supported metadata: <c>OutputType</c>, <c>Win32Manifest</c>, <c>AppContainer</c>, <c>Platform</c>,
    /// <c>SourceText</c>, <c>SourceFile</c>.
    /// </para>
    /// </remarks>
    [Required]
    public ITaskItem[]? StubExes { get; set; }

    /// <summary>
    /// Gets or sets the path to the default <c>.c</c> source file used for building stub executables.
    /// </summary>
    /// <remarks>
    /// This is used as a fallback when a stub item does not specify <c>SourceText</c> or <c>SourceFile</c>
    /// metadata. If all items provide their own source, this property can be left empty.
    /// </remarks>
    public string? DefaultSourceFilePath { get; set; }

    /// <summary>
    /// Gets or sets the path to the <c>.lib</c> file produced by the Native AOT toolchain,
    /// which the linker needs to resolve the managed entry point import.
    /// </summary>
    [Required]
    public string? NativeLibraryPath { get; set; }

    /// <summary>
    /// Gets or sets the intermediate output directory where generated files are placed.
    /// </summary>
    [Required]
    public string? IntermediateOutputDirectory { get; set; }

    /// <summary>
    /// Gets or sets the destination directory for the compiled stub <c>.exe</c> files
    /// (typically the native output directory alongside the AOT-compiled <c>.dll</c>).
    /// </summary>
    [Required]
    public string? DestinationDirectory { get; set; }

    /// <summary>
    /// Gets or sets the path to the MSVC compiler (<c>cl.exe</c>).
    /// </summary>
    /// <remarks>
    /// When <see cref="UseEnvironmentalTools"/> is <see langword="true"/>, this can be just <c>"cl"</c>
    /// (resolved from the environment <c>PATH</c>). Otherwise, it should be the full path to <c>cl.exe</c>
    /// from the Native AOT tooling's <c>_CppToolsDirectory</c>.
    /// </remarks>
    [Required]
    public string? ClExePath { get; set; }

    /// <summary>
    /// Gets or sets whether environmental tools are being used (i.e. building from a VS Developer Command Prompt).
    /// </summary>
    /// <remarks>
    /// When <see langword="true"/>, the task does not need to manually resolve MSVC and Windows SDK include/lib
    /// paths, because they are already available on the <c>PATH</c> and in environment variables.
    /// </remarks>
    public bool UseEnvironmentalTools { get; set; }

    /// <summary>
    /// Gets or sets the path to the MSVC include directory.
    /// </summary>
    /// <remarks>Only used when <see cref="UseEnvironmentalTools"/> is <see langword="false"/>.</remarks>
    public string? MsvcIncludePath { get; set; }

    /// <summary>
    /// Gets or sets the path to the MSVC lib directory (architecture-specific).
    /// </summary>
    /// <remarks>Only used when <see cref="UseEnvironmentalTools"/> is <see langword="false"/>.</remarks>
    public string? MsvcLibPath { get; set; }

    /// <summary>
    /// Gets or sets the path to the Windows SDK UCRT include directory.
    /// </summary>
    /// <remarks>Only used when <see cref="UseEnvironmentalTools"/> is <see langword="false"/>.</remarks>
    public string? WindowsSdkUcrtIncludePath { get; set; }

    /// <summary>
    /// Gets or sets the path to the Windows SDK UM include directory.
    /// </summary>
    /// <remarks>Only used when <see cref="UseEnvironmentalTools"/> is <see langword="false"/>.</remarks>
    public string? WindowsSdkUmIncludePath { get; set; }

    /// <summary>
    /// Gets or sets the path to the Windows SDK UCRT lib directory (architecture-specific).
    /// </summary>
    /// <remarks>Only used when <see cref="UseEnvironmentalTools"/> is <see langword="false"/>.</remarks>
    public string? WindowsSdkUcrtLibPath { get; set; }

    /// <summary>
    /// Gets or sets the path to the Windows SDK UM lib directory (architecture-specific).
    /// </summary>
    /// <remarks>Only used when <see cref="UseEnvironmentalTools"/> is <see langword="false"/>.</remarks>
    public string? WindowsSdkUmLibPath { get; set; }

    /// <summary>
    /// Gets or sets additional directories to prepend to the <c>PATH</c> when invoking <c>cl.exe</c>.
    /// </summary>
    /// <remarks>
    /// This is used when not using environmental tools and the Windows SDK build tools directory
    /// (containing <c>mt.exe</c> and <c>rc.exe</c>) is not already on the <c>PATH</c>.
    /// </remarks>
    public string? AdditionalPath { get; set; }

    /// <summary>
    /// Gets or sets whether the build configuration is <c>Debug</c>.
    /// </summary>
    /// <remarks>
    /// This controls whether the debug or release variants of the C runtime libraries are linked.
    /// </remarks>
    public bool IsDebugConfiguration { get; set; }

    /// <summary>
    /// Gets or sets whether Control Flow Guard (CFG) is enabled.
    /// </summary>
    /// <remarks>Maps to the <c>ControlFlowGuard</c> MSBuild property (value <c>"Guard"</c>).</remarks>
    public bool ControlFlowGuard { get; set; }

    /// <summary>
    /// Gets or sets whether CET shadow stack compatibility is enabled.
    /// </summary>
    /// <remarks>
    /// When <see langword="true"/> (the default), the <c>/CETCOMPAT</c> linker flag is set for x64 targets.
    /// Maps to the <c>CETCompat</c> MSBuild property.
    /// </remarks>
    public bool CETCompat { get; set; } = true;

    /// <summary>
    /// Gets or sets the default output type for stubs that don't specify one.
    /// </summary>
    /// <remarks>Falls back to the project's <c>OutputType</c>.</remarks>
    public string? DefaultOutputType { get; set; }

    /// <summary>
    /// Gets or sets the default Win32 manifest path for stubs that don't specify one.
    /// </summary>
    public string? DefaultWin32Manifest { get; set; }

    /// <summary>
    /// Gets or sets the default <c>AppContainer</c> value for stubs that don't specify one.
    /// </summary>
    public bool DefaultAppContainer { get; set; }

    /// <summary>
    /// Gets or sets the default platform for stubs that don't specify one.
    /// </summary>
    public string? DefaultPlatform { get; set; }

    /// <summary>
    /// Gets or sets whether to include generated stub <c>.exe</c> files in the publish output.
    /// </summary>
    /// <remarks>Maps to <c>CopyBuildOutputToPublishDirectory</c>.</remarks>
    public bool CopyBuildOutputToPublishDirectory { get; set; } = true;

    /// <summary>
    /// Returns the list of generated stub <c>.exe</c> items to be included in <c>ResolvedFileToPublish</c>.
    /// </summary>
    /// <remarks>
    /// Each output item has <c>RelativePath</c> and <c>CopyToPublishDirectory</c> metadata set,
    /// so the calling target can merge them directly into <c>ResolvedFileToPublish</c>.
    /// </remarks>
    [Output]
    public ITaskItem[]? GeneratedStubExes { get; set; }

    /// <inheritdoc/>
    public override bool Execute()
    {
        if (StubExes is not { Length: > 0 })
        {
            Log.LogError("No stub executables were specified.");

            return false;
        }

        if (string.IsNullOrEmpty(NativeLibraryPath) || !File.Exists(NativeLibraryPath))
        {
            Log.LogError("The native library '{0}' does not exist.", NativeLibraryPath);

            return false;
        }

        if (string.IsNullOrEmpty(IntermediateOutputDirectory))
        {
            Log.LogError("The intermediate output directory was not specified.");

            return false;
        }

        if (string.IsNullOrEmpty(DestinationDirectory))
        {
            Log.LogError("The destination directory was not specified.");

            return false;
        }

        if (string.IsNullOrEmpty(ClExePath))
        {
            Log.LogError(
                "Failed to find 'cl.exe', which is needed to compile stub executables. " +
                "Try setting 'CsWinRTUseEnvironmentalTools' and building from a Visual Studio Developer Command Prompt (or PowerShell) session.");

            return false;
        }

        // Validate MSVC/SDK paths when not using environmental tools
        if (!UseEnvironmentalTools)
        {
            if (string.IsNullOrEmpty(MsvcIncludePath) || !Directory.Exists(MsvcIncludePath) ||
                string.IsNullOrEmpty(WindowsSdkUcrtIncludePath) || !Directory.Exists(WindowsSdkUcrtIncludePath))
            {
                Log.LogError(
                    "Failed to find the paths for the include folders to pass to MSVC, which are needed to compile stub executables. " +
                    "Try setting 'CsWinRTUseEnvironmentalTools' and building from a Visual Studio Developer Command Prompt (or PowerShell) session.");

                return false;
            }
        }

        List<ITaskItem> generatedItems = new();

        foreach (ITaskItem stubExe in StubExes)
        {
            if (!GenerateStubExe(stubExe, generatedItems))
            {
                return false;
            }
        }

        GeneratedStubExes = generatedItems.ToArray();

        return true;
    }

    /// <summary>
    /// Generates a single stub <c>.exe</c> for the given item.
    /// </summary>
    /// <param name="stubExe">The <see cref="ITaskItem"/> describing the stub to generate.</param>
    /// <param name="generatedItems">The list to add generated output items to.</param>
    /// <returns><see langword="true"/> if the stub was generated successfully; otherwise, <see langword="false"/>.</returns>
    private bool GenerateStubExe(ITaskItem stubExe, List<ITaskItem> generatedItems)
    {
        string stubName = stubExe.ItemSpec;

        // Resolve per-stub metadata, falling back to defaults
        string outputType = GetMetadataOrDefault(stubExe, "OutputType", DefaultOutputType ?? "Exe");
        string win32Manifest = GetMetadataOrDefault(stubExe, "Win32Manifest", DefaultWin32Manifest ?? "");
        bool appContainer = GetBooleanMetadataOrDefault(stubExe, "AppContainer", DefaultAppContainer);
        string platform = GetMetadataOrDefault(stubExe, "Platform", DefaultPlatform ?? "");
        string sourceText = stubExe.GetMetadata("SourceText");
        string sourceFile = stubExe.GetMetadata("SourceFile");

        // Validate the platform
        if (!UseEnvironmentalTools &&
            !string.Equals(platform, "arm64", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(platform, "x64", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(platform, "x86", StringComparison.OrdinalIgnoreCase))
        {
            Log.LogError(
                "Invalid platform '{0}' for stub '{1}'. Make sure to set 'Platform' to either 'arm64', 'x64', or 'x86'. " +
                "Alternatively, try setting 'CsWinRTUseEnvironmentalTools' and building from a Visual Studio Developer Command Prompt (or PowerShell) session.",
                platform,
                stubName);

            return false;
        }

        // Prepare paths for the intermediate working directory for this stub
        string stubIntermediateDir = Path.Combine(IntermediateOutputDirectory!, stubName);
        string sourceFileName = stubName + ".c";
        string sourceFilePath = Path.Combine(stubIntermediateDir, sourceFileName);
        string binaryFileName = stubName + ".exe";
        string binaryOutputFilePath = Path.Combine(stubIntermediateDir, binaryFileName);
        string binaryDestinationFilePath = Path.Combine(DestinationDirectory!, binaryFileName);

        Log.LogMessage(MessageImportance.Normal, "Generating stub .exe '{0}'", stubName);

        // Ensure the intermediate directory exists
        Directory.CreateDirectory(stubIntermediateDir);

        // Resolve the source for this stub:
        //   1. SourceText metadata: write inline text to the .c file
        //   2. SourceFile metadata: copy the specified file
        //   3. Default DefaultSourceFilePath: copy the default template from the NuGet package
        if (!string.IsNullOrEmpty(sourceText))
        {
            File.WriteAllText(sourceFilePath, sourceText);
        }
        else if (!string.IsNullOrEmpty(sourceFile))
        {
            if (!File.Exists(sourceFile))
            {
                Log.LogError("The source file '{0}' specified for stub '{1}' does not exist.", sourceFile, stubName);

                return false;
            }

            File.Copy(sourceFile, sourceFilePath, overwrite: true);
        }
        else if (!string.IsNullOrEmpty(DefaultSourceFilePath))
        {
            if (!File.Exists(DefaultSourceFilePath))
            {
                Log.LogError("The default stub .exe source file '{0}' does not exist.", DefaultSourceFilePath);

                return false;
            }

            File.Copy(DefaultSourceFilePath, sourceFilePath, overwrite: true);
        }
        else
        {
            Log.LogError(
                "No source was specified for stub '{0}'. Set 'SourceText' or 'SourceFile' metadata on the item, " +
                "or provide a default source file via the 'DefaultSourceFilePath' task parameter.",
                stubName);

            return false;
        }

        // Delete any previously-generated broken .exe in the destination (see https://github.com/dotnet/runtime/issues/111313)
        if (File.Exists(binaryDestinationFilePath))
        {
            File.Delete(binaryDestinationFilePath);
        }

        // Build the MSVC command-line arguments
        string arguments = BuildMsvcArguments(
            sourceFilePath: sourceFilePath,
            platform: platform,
            outputType: outputType,
            win32Manifest: win32Manifest,
            appContainer: appContainer);

        // Invoke cl.exe
        if (!InvokeCompiler(arguments, stubIntermediateDir, stubName))
        {
            return false;
        }

        // Copy the resulting executable to the native output directory
        if (!File.Exists(binaryOutputFilePath))
        {
            Log.LogError("The stub .exe '{0}' was not produced by the compiler.", binaryOutputFilePath);

            return false;
        }

        File.Copy(binaryOutputFilePath, binaryDestinationFilePath, overwrite: true);

        // If publishing, add to the output items
        if (CopyBuildOutputToPublishDirectory)
        {
            TaskItem outputItem = new(binaryDestinationFilePath);

            outputItem.SetMetadata("RelativePath", binaryFileName);
            outputItem.SetMetadata("CopyToPublishDirectory", "PreserveNewest");

            generatedItems.Add(outputItem);
        }

        return true;
    }

    /// <summary>
    /// Builds the full MSVC command-line arguments string for compiling a single stub <c>.exe</c>.
    /// </summary>
    /// <param name="sourceFilePath">The path to the <c>.c</c> source file.</param>
    /// <param name="platform">The target platform.</param>
    /// <param name="outputType">The output type (<c>WinExe</c> or <c>Exe</c>).</param>
    /// <param name="win32Manifest">The optional Win32 manifest path.</param>
    /// <param name="appContainer">Whether to enable <c>/APPCONTAINER</c>.</param>
    /// <returns>The formatted arguments string.</returns>
    private string BuildMsvcArguments(
        string sourceFilePath,
        string platform,
        string outputType,
        string win32Manifest,
        bool appContainer)
    {
        StringBuilder args = new();

        // Hide the copyright banner (https://learn.microsoft.com/cpp/build/reference/nologo-suppress-startup-banner-c-cpp)
        args.Append("/nologo ");

        // If not using environmental tools, pass the paths to the required include folders
        if (!UseEnvironmentalTools)
        {
            AppendQuoted(args, "/I", MsvcIncludePath!);
            AppendQuoted(args, "/I", WindowsSdkUcrtIncludePath!);
            AppendQuoted(args, "/I", WindowsSdkUmIncludePath!);
        }

        // Configure the C runtime library linking behavior:
        //   - Statically link the MSVC-specific runtime (VCRUNTIME140), which is small.
        //   - Dynamically link UCRT (the OS-provided C runtime), matching Native AOT behavior.
        //   We start with /MT[d] for static linking, then override the UCRT portion below.
        // See: https://learn.microsoft.com/cpp/build/reference/md-mt-ld-use-run-time-library
        args.Append(IsDebugConfiguration ? "/MTd " : "/MT ");

        // Optimize for speed (https://learn.microsoft.com/cpp/build/reference/o1-o2-minimize-size-maximize-speed)
        args.Append("/O2 ");

        // Source file and native library
        AppendQuotedPath(args, sourceFilePath);
        AppendQuotedPath(args, NativeLibraryPath!);

        // If not using environmental tools, pass the paths to the required lib folders
        if (!UseEnvironmentalTools)
        {
            AppendQuotedWildcard(args, MsvcLibPath!);
            AppendQuotedWildcard(args, WindowsSdkUcrtLibPath!);
            AppendQuotedWildcard(args, WindowsSdkUmLibPath!);
        }

        // Start linker options (https://learn.microsoft.com/cpp/build/reference/compiler-command-line-syntax)
        args.Append("/link ");

        // Hide the copyright banner for the linker
        args.Append("/NOLOGO ");

        // Embed a manifest if specified, otherwise suppress manifest generation.
        // See: https://learn.microsoft.com/cpp/build/reference/manifest-create-side-by-side-assembly-manifest
        if (!string.IsNullOrEmpty(win32Manifest))
        {
            args.AppendFormat("/MANIFEST:EMBED /MANIFESTINPUT:{0} ", win32Manifest);
        }
        else
        {
            args.Append("/MANIFEST:NO ");
        }

        // Switch UCRT from static to dynamic linking to reduce binary size (~98 KB → ~20 KB).
        // See: https://learn.microsoft.com/cpp/build/reference/defaultlib-specify-default-library
        // See: https://learn.microsoft.com/cpp/build/reference/nodefaultlib-ignore-libraries
        if (IsDebugConfiguration)
        {
            args.Append("/NODEFAULTLIB:libucrtd.lib /DEFAULTLIB:ucrtd.lib ");
        }
        else
        {
            args.Append("/NODEFAULTLIB:libucrt.lib /DEFAULTLIB:ucrt.lib ");
        }

        // Skip incremental linking (https://learn.microsoft.com/cpp/build/reference/incremental-link-incrementally)
        args.Append("/INCREMENTAL:NO ");

        // Enable COMDAT folding and unreferenced code removal (https://learn.microsoft.com/cpp/build/reference/opt-optimizations)
        args.Append("/OPT:ICF /OPT:REF ");

        // Set the AppContainer bit for UWP apps (https://learn.microsoft.com/cpp/build/reference/appcontainer-windows-store-app)
        if (appContainer)
        {
            args.Append("/APPCONTAINER ");
        }

        // Set the subsystem type (https://learn.microsoft.com/cpp/build/reference/subsystem-specify-subsystem)
        args.Append(string.Equals(outputType, "WinExe", StringComparison.OrdinalIgnoreCase)
            ? "/SUBSYSTEM:WINDOWS "
            : "/SUBSYSTEM:CONSOLE ");

        // Always use 'wmainCRTStartup' as the entry point, matching Native AOT behavior.
        // This allows using 'wmain' for all application types, including WinExe.
        // See: https://learn.microsoft.com/cpp/build/reference/entry-entry-point-symbol
        args.Append("/ENTRY:wmainCRTStartup ");

        // Enable CFG if requested (https://learn.microsoft.com/cpp/build/reference/guard-enable-control-flow-guard)
        if (ControlFlowGuard)
        {
            args.Append("/guard:cf ");
        }

        // Configure CET shadow stack compatibility (https://learn.microsoft.com/cpp/build/reference/cetcompat)
        bool cetEnabled = CETCompat;
        bool isX64 = string.Equals(platform, "x64", StringComparison.OrdinalIgnoreCase);

        if (isX64)
        {
            args.Append(cetEnabled ? "/CETCOMPAT " : "/CETCOMPAT:NO ");
        }

        // Enable EH continuation metadata if CET is enabled and CFG is active, matching Native AOT.
        // See: https://learn.microsoft.com/cpp/build/reference/guard-enable-eh-continuation-metadata
        if (cetEnabled && isX64 && ControlFlowGuard)
        {
            args.Append("/guard:ehcont ");
        }

        return args.ToString().TrimEnd();
    }

    /// <summary>
    /// Invokes <c>cl.exe</c> with the given arguments.
    /// </summary>
    /// <param name="arguments">The command-line arguments.</param>
    /// <param name="workingDirectory">The working directory for the process.</param>
    /// <param name="stubName">The name of the stub being compiled (for diagnostics).</param>
    /// <returns><see langword="true"/> if the compiler exited successfully; otherwise, <see langword="false"/>.</returns>
    private bool InvokeCompiler(string arguments, string workingDirectory, string stubName)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = ClExePath!,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        // Add extra directories to the PATH if needed (e.g. for mt.exe and rc.exe)
        if (!string.IsNullOrEmpty(AdditionalPath))
        {
            string currentPath = startInfo.EnvironmentVariables.ContainsKey("PATH")
                ? startInfo.EnvironmentVariables["PATH"]!
                : "";

            startInfo.EnvironmentVariables["PATH"] = currentPath + ";" + AdditionalPath;
        }

        try
        {
            using Process? process = Process.Start(startInfo);

            if (process is null)
            {
                Log.LogError("Failed to start cl.exe for stub '{0}'.", stubName);

                return false;
            }

            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();

            process.WaitForExit();

            // Log compiler output
            if (!string.IsNullOrWhiteSpace(stdout))
            {
                Log.LogMessage(MessageImportance.Normal, stdout.TrimEnd());
            }

            if (!string.IsNullOrWhiteSpace(stderr))
            {
                Log.LogWarning("{0}", stderr.TrimEnd());
            }

            if (process.ExitCode != 0)
            {
                Log.LogError("cl.exe failed for stub '{0}' with exit code {1}.", stubName, process.ExitCode);

                return false;
            }

            return true;
        }
        catch (Exception e)
        {
            Log.LogError("Failed to invoke cl.exe for stub '{0}': {1}", stubName, e.Message);

            return false;
        }
    }

    /// <summary>
    /// Gets a metadata value from an item, falling back to a default if the metadata is empty.
    /// </summary>
    /// <param name="item">The task item.</param>
    /// <param name="metadataName">The metadata name.</param>
    /// <param name="defaultValue">The default value if metadata is empty.</param>
    /// <returns>The effective metadata value.</returns>
    private static string GetMetadataOrDefault(ITaskItem item, string metadataName, string defaultValue)
    {
        string value = item.GetMetadata(metadataName);

        return string.IsNullOrEmpty(value) ? defaultValue : value;
    }

    /// <summary>
    /// Gets a boolean metadata value from an item, falling back to a default if the metadata is empty.
    /// </summary>
    /// <param name="item">The task item.</param>
    /// <param name="metadataName">The metadata name.</param>
    /// <param name="defaultValue">The default value if metadata is empty.</param>
    /// <returns>The effective boolean value.</returns>
    private static bool GetBooleanMetadataOrDefault(ITaskItem item, string metadataName, bool defaultValue)
    {
        string value = item.GetMetadata(metadataName);

        if (string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }

        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Appends a quoted path argument to the string builder (e.g. <c>"C:\path\file.c" </c>).
    /// </summary>
    /// <param name="args">The string builder.</param>
    /// <param name="path">The path to quote.</param>
    private static void AppendQuotedPath(StringBuilder args, string path)
    {
        args.Append('"').Append(path).Append("\" ");
    }

    /// <summary>
    /// Appends a quoted argument with a flag prefix (e.g. <c>/I "C:\include" </c>).
    /// </summary>
    /// <param name="args">The string builder.</param>
    /// <param name="flag">The compiler flag.</param>
    /// <param name="path">The path to quote.</param>
    private static void AppendQuoted(StringBuilder args, string flag, string path)
    {
        args.Append(flag).Append(" \"").Append(path).Append("\" ");
    }

    /// <summary>
    /// Appends a quoted wildcard lib path (e.g. <c>"C:\lib\*.lib" </c>).
    /// </summary>
    /// <param name="args">The string builder.</param>
    /// <param name="libDir">The library directory path.</param>
    private static void AppendQuotedWildcard(StringBuilder args, string libDir)
    {
        args.Append('"').Append(Path.Combine(libDir, "*.lib")).Append("\" ");
    }
}
