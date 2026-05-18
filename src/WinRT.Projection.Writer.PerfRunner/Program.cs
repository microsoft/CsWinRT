// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using WindowsRuntime.ProjectionWriter;

namespace WindowsRuntime.ProjectionWriter.PerfRunner;

/// <summary>
/// Tiny driver for profiling <see cref="ProjectionWriter.Run"/> against the same
/// per-namespace Windows SDK <c>.winmd</c> set that the real
/// <c>WinRT.Sdk.Projection</c> build uses. Loops the requested number of iterations,
/// with optional multithreading, and reports per-iteration + total wall-clock time.
///
/// Edit <see cref="Iterations"/> and <see cref="EnableMultithreading"/> directly in this
/// file to control the run; rebuild and execute.
///
/// Output is written to <c>%TEMP%\WinRTProjectionPerf</c> and overwritten on each
/// iteration (the directory is cleared on startup so disk doesn't fill up).
/// </summary>
internal static class Program
{
    /// <summary>How many times to invoke <see cref="ProjectionWriter.Run"/> back-to-back.</summary>
    private const int Iterations = 3;

    /// <summary>
    /// When <see langword="true"/>, the writer uses its default parallelism (one work item per
    /// CPU core via <see cref="System.Threading.Tasks.Parallel"/>). When <see langword="false"/>,
    /// the writer is forced to single-threaded operation via <c>MaxDegreesOfParallelism = 1</c>.
    /// </summary>
    private const bool EnableMultithreading = true;

    /// <summary>
    /// The Windows SDK build number that selects which <c>Microsoft.Windows.SDK.Contracts</c>
    /// NuGet package version to source <c>.winmd</c> files from. Mirrors the
    /// <c>WindowsSdkBuild</c> property in <c>src\WinRT.Sdk.Projection\WinRT.Sdk.Projection.csproj</c>.
    /// </summary>
    private const string WindowsSdkBuild = "26100";

    /// <summary>
    /// The <c>Microsoft.Windows.SDK.Contracts</c> NuGet package version to use. Mirrors the
    /// fallback in <c>WinRT.Sdk.Projection.csproj</c>; updated to <c>10.0.26100.1</c> to match
    /// the package version actually present in the NuGet cache used by this build.
    /// </summary>
    private const string SdkContractsPackageVersion = "10.0.26100.1";

    /// <summary>
    /// WinMDs excluded from the SDK projection build (these contract WinMDs aren't included in
    /// the <c>Microsoft.Windows.SDK.NET.Ref</c> package). Mirrors the <c>Exclude</c> list in
    /// <c>WinRT.Sdk.Projection.csproj</c>'s <c>_SdkPackageWinMDs</c> item group.
    /// </summary>
    private static readonly string[] s_excludedWinMds =
    [
        "Windows.winmd",
        "Windows.Embedded.DeviceLockdown.DeviceLockdownContract.winmd",
        "Windows.Networking.NetworkOperators.NetworkOperatorsFdnContract.winmd",
        "Windows.Phone.PhoneContract.winmd",
        "Windows.Phone.StartScreen.DualSimTileContract.winmd",
        "Windows.UI.WebUI.Core.WebUICommandBarContract.winmd",
    ];

    private static int Main()
    {
        // Resolve the SDK WinMD folder via the user's NuGet cache. This is the same folder the
        // real SDK projection build (WinRT.Sdk.Projection.csproj) resolves at build time via
        // '$(NuGetPackageRoot)/$(_SdkPackageName)/$(_SdkPackageVersion)/ref/netstandard2.0/'.
        string nugetPackageRoot = Environment.GetEnvironmentVariable("NUGET_PACKAGES")
                                  ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
        string sdkWinMdFolder = Path.Combine(
            nugetPackageRoot,
            "microsoft.windows.sdk.contracts",
            SdkContractsPackageVersion,
            "ref",
            "netstandard2.0");

        if (!Directory.Exists(sdkWinMdFolder))
        {
            Console.Error.WriteLine($"ERROR: SDK WinMD folder not found: {sdkWinMdFolder}");
            Console.Error.WriteLine("Restore the Microsoft.Windows.SDK.Contracts NuGet package first (e.g. by building WinRT.Sdk.Projection.csproj) or update 'SdkContractsPackageVersion' in this file.");
            return 1;
        }

        List<string> inputs = Directory
            .EnumerateFiles(sdkWinMdFolder, "*.winmd")
            .Where(p => !s_excludedWinMds.Contains(Path.GetFileName(p), StringComparer.OrdinalIgnoreCase))
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (inputs.Count == 0)
        {
            Console.Error.WriteLine($"ERROR: no .winmd files matched in {sdkWinMdFolder}");
            return 1;
        }

        string outputFolder = Path.Combine(Path.GetTempPath(), "WinRTProjectionPerf");

        Console.WriteLine($"SDK WinMD folder:     {sdkWinMdFolder}");
        Console.WriteLine($"Input .winmd count:   {inputs.Count}");
        Console.WriteLine($"Output:               {outputFolder}");
        Console.WriteLine($"Iterations:           {Iterations}");
        Console.WriteLine($"EnableMultithreading: {EnableMultithreading}");
        Console.WriteLine($"SDK build:            {WindowsSdkBuild}");
        Console.WriteLine();

        // Clear the output folder once up-front. Each iteration rewrites the contents.
        if (Directory.Exists(outputFolder))
        {
            Directory.Delete(outputFolder, recursive: true);
        }

        _ = Directory.CreateDirectory(outputFolder);

        ProjectionWriterOptions options = new()
        {
            InputPaths = inputs,
            OutputFolder = outputFolder,
            // Mirrors the SDK projection build's filter: only project Windows.* and the
            // internal interop namespace.
            Include = ["Windows", "WindowsRuntime.Internal"],
            Exclude = [],
            Component = false,
            Internal = false,
            ReferenceProjection = false,
            Verbose = false,
            MaxDegreesOfParallelism = EnableMultithreading ? -1 : 1,
        };

        // Warmup pass (not counted): forces JIT + first-run cache population so the timed
        // iterations measure steady-state behavior.
        Console.WriteLine("Warmup pass...");
        Stopwatch warmupSw = Stopwatch.StartNew();
        ProjectionWriter.Run(options);
        warmupSw.Stop();
        Console.WriteLine($"  warmup: {warmupSw.Elapsed.TotalMilliseconds,8:F1} ms");
        Console.WriteLine();

        long totalMs = 0;
        long minMs = long.MaxValue;
        long maxMs = 0;
        Stopwatch totalSw = Stopwatch.StartNew();

        for (int i = 1; i <= Iterations; i++)
        {
            Stopwatch iterSw = Stopwatch.StartNew();
            ProjectionWriter.Run(options);
            iterSw.Stop();

            long ms = iterSw.ElapsedMilliseconds;
            totalMs += ms;

            if (ms < minMs)
            {
                minMs = ms;
            }

            if (ms > maxMs)
            {
                maxMs = ms;
            }

            Console.WriteLine($"  iter {i,3}: {ms,8} ms");
        }

        totalSw.Stop();

        int fileCount = Directory.GetFiles(outputFolder, "*.cs", SearchOption.AllDirectories).Length;

        Console.WriteLine();
        Console.WriteLine($"Output files: {fileCount}");
        Console.WriteLine($"Total:        {totalSw.Elapsed.TotalMilliseconds,8:F1} ms across {Iterations} iterations");
        Console.WriteLine($"Avg / iter:   {(double)totalMs / Iterations,8:F1} ms");
        Console.WriteLine($"Min / iter:   {minMs,8} ms");
        Console.WriteLine($"Max / iter:   {maxMs,8} ms");

        return 0;
    }
}
