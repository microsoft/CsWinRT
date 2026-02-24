#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

string msbuildPath = FindMSBuild();

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

// Find the BuildDeterminism project directory by walking up from working directory
string projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
string targetProject = Path.Combine(projectDir, "BuildDeterminismComponent", "BuildDeterminismComponent.csproj");

if (!File.Exists(targetProject))
{
    Console.Error.WriteLine($"Target project not found: {targetProject}");
    return 1;
}

Console.WriteLine($"Target project: {targetProject}");
Console.WriteLine($"Configuration: {config}, Platform: {platform}");

// First build
Console.WriteLine("Cleaning...");
RunMSBuild($"\"{targetProject}\" -t:Clean", msbuildPath);
Console.WriteLine("Building (first pass)...");
string hash1 = BuildAndHash(targetProject, config, platform, msbuildPath);
Console.WriteLine($"First build SHA256: {hash1}");

// Second build
Console.WriteLine("Cleaning...");
RunMSBuild($"\"{targetProject}\" -t:Clean", msbuildPath);
Console.WriteLine("Building (second pass)...");
string hash2 = BuildAndHash(targetProject, config, platform, msbuildPath);
Console.WriteLine($"Second build SHA256: {hash2}");

if (hash1 == hash2)
{
    Console.WriteLine("Build is deterministic!");
    return 0;
}
else
{
    Console.Error.WriteLine("Build is NOT deterministic!");
    return 1;
}

static string BuildAndHash(string projectPath, string config, string platform, string msbuildPath)
{
    RunMSBuild($"\"{projectPath}\" -p:Platform={platform},Configuration={config}", msbuildPath);

    string outputDir = Path.Combine(
        Path.GetDirectoryName(projectPath)!,
        "bin", platform, config, "net10.0");

    string dllPath = Path.Combine(outputDir, "WinRT.Interop.dll");
    if (!File.Exists(dllPath))
    {
        throw new FileNotFoundException($"Output DLL not found: {dllPath}");
    }

    byte[] fileBytes = File.ReadAllBytes(dllPath);
    byte[] hashBytes = SHA256.HashData(fileBytes);
    return Convert.ToHexString(hashBytes);
}

static string FindMSBuild()
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

static void RunMSBuild(string arguments, string msbuildPath)
{
    var psi = new ProcessStartInfo
    {
        FileName = msbuildPath,
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
