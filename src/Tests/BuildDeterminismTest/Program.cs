#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

return BuildDeterminismRunner.Run(args);

internal sealed class BuildDeterminismRunner
{
    private const string OutputDllName = "WinRT.Interop.dll";
    private const string TargetFramework = "net10.0";

    private readonly string _msbuildPath;
    private readonly string _projectPath;
    private readonly string _config;
    private readonly string _platform;
    private readonly string _msbuildArgs;

    private BuildDeterminismRunner(string msbuildPath, string projectPath, string config, string platform, string msbuildArgs)
    {
        _msbuildPath = msbuildPath;
        _projectPath = projectPath;
        _config = config;
        _platform = platform;
        _msbuildArgs = msbuildArgs;
    }

    internal static int Run(string[] args)
    {
        string msbuildArgs = args.Length > 0 ? string.Join(" ", args) : "";
        string config =
#if DEBUG
            "Debug";
#else
            "Release";
#endif

        string platform = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X86 => "x86",
            Architecture.X64 => "x64",
            _ => "x64"
        };

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

        string hash1 = runner.CleanBuildAndHash("first");
        string hash2 = runner.CleanBuildAndHash("second");

        if (hash1 == hash2)
        {
            Console.WriteLine("Build is deterministic!");
            return 0;
        }

        Console.Error.WriteLine("Build is NOT deterministic!");
        return 1;
    }

    private string CleanBuildAndHash(string passLabel)
    {
        // MSBuild Clean doesn't remove the bin folders, so delete them explicitly.
        string projectFolder = Path.GetDirectoryName(_projectPath)!;
        foreach (string dir in new[] { "bin" })
        {
            string path = Path.Combine(projectFolder, dir);
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
                Console.WriteLine($"Deleted {path}");
            }
        }

        Console.WriteLine("Restoring...");
        RunMSBuild($"\"{_projectPath}\" -t:Restore");

        Console.WriteLine($"Building ({passLabel} pass)...");
        RunMSBuild($"\"{_projectPath}\" -p:Platform={_platform},Configuration={_config} {_msbuildArgs}".TrimEnd());

        string outputDir = Path.Combine(
            Path.GetDirectoryName(_projectPath)!,
            "bin", _platform, _config, TargetFramework);

        string dllPath = Path.Combine(outputDir, OutputDllName);
        if (!File.Exists(dllPath))
        {
            throw new FileNotFoundException($"Output DLL not found: {dllPath}");
        }

        string hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(dllPath)));
        Console.WriteLine($"{passLabel} build SHA256: {hash}");

        return hash;
    }

    private void RunMSBuild(string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _msbuildPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

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
            throw new Exception($"'msbuild {arguments}' failed with exit code {process.ExitCode}");
        }
    }

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
