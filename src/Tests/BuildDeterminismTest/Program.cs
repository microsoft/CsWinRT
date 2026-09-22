#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using AsmResolver.DotNet;

if (args.Length > 0 && args[0] == "--interop-incremental")
{
    return InteropIncrementalRunner.Run(args.AsSpan(1));
}

if (args.Length > 0 && args[0] == "--interop")
{
    if (args.Length != 3)
    {
        Console.Error.WriteLine("Usage: BuildDeterminismTest --interop <generator.exe|generator.dll> <response.rsp>");
        return 1;
    }

    return InteropDeterminismRunner.Run(args[1], args[2]);
}

// Other command-line arguments are MSBuild properties forwarded from CI
return BuildDeterminismRunner.Run(args);

/// <summary>
/// Builds BuildDeterminismComponent from clean state with varying parallelism and compares the output DLL hashes.
/// Accepts optional MSBuild arguments (e.g., /p:...)
/// passed through from the CI pipeline invocation.
/// </summary>
internal sealed class BuildDeterminismRunner
{
    private const string OutputDllName = "WinRT.Interop.dll";
    private const string TargetFramework = "net10.0";

    private readonly string _msbuildPath;
    private readonly string _projectPath;
    private readonly string _config;
    private readonly string _platform;
    // Additional MSBuild arguments forwarded from the command line (CI pipeline properties).
    private readonly string _msbuildArgs;

    private BuildDeterminismRunner(string msbuildPath, string projectPath, string config, string platform, string msbuildArgs)
    {
        _msbuildPath = msbuildPath;
        _projectPath = projectPath;
        _config = config;
        _platform = platform;
        _msbuildArgs = msbuildArgs;
    }

    /// <summary>
    /// Resolves build settings, locates the target project, and runs repeated clean builds
    /// to compare output hashes. Returns 0 if deterministic, 1 otherwise.
    /// </summary>
    internal static int Run(string[] args)
    {
        // Join all command-line args into a single string to append to MSBuild invocations.
        string msbuildArgs = args.Length > 0 ? string.Join(" ", args) : "";
        string config =
#if DEBUG
            "Debug";
#else
            "Release";
#endif

        // Resolve platform from the current process architecture.
        string platform = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X86 => "x86",
            Architecture.X64 => "x64",
            _ => "x64"
        };

        // Navigate from bin output back to the test project root, then into the companion component project.
        string projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        string projectPath = Path.Combine(projectDir, "BuildDeterminismComponent", "BuildDeterminismComponent.csproj");

        if (!File.Exists(projectPath))
        {
            Console.Error.WriteLine($"Target project not found: {projectPath}");
            return 1;
        }

        var runner = new BuildDeterminismRunner(FindMSBuild(), projectPath, config, platform, msbuildArgs);

        Console.WriteLine($"Target project: {projectPath}");
        Console.WriteLine($"Configuration: {config}, Platform: {platform}");

        string? expectedHash = null;
        for (int i = 0; i < InteropDeterminismRunner.DegreesOfParallelism.Length; i++)
        {
            int degree = InteropDeterminismRunner.DegreesOfParallelism[i];
            string hash = runner.CleanBuildAndHash($"run-{i + 1}-dop-{degree}", degree);
            expectedHash ??= hash;

            if (hash != expectedHash)
            {
                Console.Error.WriteLine($"Build is NOT deterministic (maximum parallelism {degree})!");
                return 1;
            }
        }

        Console.WriteLine("Build is deterministic across repeated serial and parallel runs!");
        return 0;
    }

    /// <summary>
    /// Deletes bin output, restores packages, builds, and returns the SHA256 hash
    /// of the output DLL for the given pass and maximum parallelism.
    /// </summary>
    private string CleanBuildAndHash(string passLabel, int maxDegreesOfParallelism)
    {
        // MSBuild Clean doesn't remove the bin and obj folders, so delete them explicitly.
        string projectFolder = Path.GetDirectoryName(_projectPath)!;
        foreach (string dir in new[] { "bin", "obj" })
        {
            string path = Path.Combine(projectFolder, dir);
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
                if (Directory.Exists(path))
                {
                    Console.WriteLine($"WARNING: Directory still exists after deletion: {path}");
                }
                else
                {
                    Console.WriteLine($"Deleted {path}");
                }
            }
        }

        Console.WriteLine("Restoring...");
        string restoreBinlog = Path.Combine(AppContext.BaseDirectory, $"restore-{Guid.NewGuid():N}.binlog");
        RunMSBuild(
            $"\"{_projectPath}\" -t:Restore -m -p:Platform={_platform},Configuration={_config} " +
            $"{_msbuildArgs} -bl:\"{restoreBinlog}\"");

        // Build with platform/config and any additional CI MSBuild args (e.g., /p:CIBuildReason=CI,...).
        Console.WriteLine($"Building ({passLabel} pass)...");
        string buildBinlog = Path.Combine(AppContext.BaseDirectory, $"{passLabel}-{Guid.NewGuid():N}.binlog");
        RunMSBuild(
            $"\"{_projectPath}\" -m -p:Platform={_platform},Configuration={_config} {_msbuildArgs} " +
            "-p:CsWinRTGeneratorEnableIncrementalGeneration=false " +
            $"-p:CsWinRTGeneratorMaxDegreesOfParallelism={maxDegreesOfParallelism} -bl:\"{buildBinlog}\"");

        string outputDir = Path.Combine(
            Path.GetDirectoryName(_projectPath)!,
            "bin", _platform, _config, TargetFramework);

        string dllPath = Path.Combine(outputDir, OutputDllName);
        return HashOutput(dllPath, passLabel);
    }

    internal static string HashOutput(string dllPath, string passLabel)
    {
        if (!File.Exists(dllPath))
        {
            throw new FileNotFoundException($"Output DLL not found: {dllPath}");
        }

        // Hash the output DLL to compare across builds.
        string hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(dllPath)));
        Console.WriteLine($"Checking SHA256 of file {dllPath}");
        Console.WriteLine($"{passLabel} build SHA256: {hash}");

        // Print MVID for debugging non-deterministic builds.
        var moduleDef = ModuleDefinition.FromFile(dllPath);
        Console.WriteLine($"{passLabel} build MVID: {moduleDef.Mvid}");

        return hash;
    }

    /// <summary>
    /// Launches MSBuild with the given arguments, streaming stdout/stderr in real time.
    /// Throws on non-zero exit code.
    /// </summary>
    private void RunMSBuild(string arguments)
    {
        RunProcess(new ProcessStartInfo
        {
            FileName = _msbuildPath,
            Arguments = arguments
        });
    }

    internal static void RunProcess(ProcessStartInfo psi)
    {
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;
        psi.UseShellExecute = false;

        using var process = Process.Start(psi)!;

        // Read stderr asynchronously to avoid deadlock when both buffers fill.
        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
                Console.Error.WriteLine(e.Data);
        };
        process.BeginErrorReadLine();

        // Stream stdout line-by-line so progress is visible.
        string? line;
        while ((line = process.StandardOutput.ReadLine()) != null)
        {
            Console.WriteLine(line);
        }

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new Exception($"'{psi.FileName}' failed with exit code {process.ExitCode}");
        }
    }

    /// <summary>
    /// Locates MSBuild.exe via vswhere from the latest Visual Studio installation.
    /// </summary>
    private static string FindMSBuild()
    {
        string vswhere = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Microsoft Visual Studio", "Installer", "vswhere.exe");

        var psi = new ProcessStartInfo
        {
            FileName = vswhere,
            Arguments = "-latest -requires Microsoft.Component.MSBuild -find MSBuild\\**\\Bin\\MSBuild.exe",
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        using var process = Process.Start(psi)!;
        string path = process.StandardOutput.ReadLine()!;
        process.WaitForExit();
        return path;
    }
}
