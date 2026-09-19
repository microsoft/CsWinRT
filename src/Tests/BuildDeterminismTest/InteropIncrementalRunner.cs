// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AsmResolver.DotNet;
using static InteropIncrementalProbe;

/// <summary>
/// Exercises persistent interop caching in fresh processes using private copies of all response-file inputs.
/// </summary>
internal static class InteropIncrementalRunner
{
    private const string DllName = "WinRT.Interop.dll";
    private const string CacheName = "WinRT.Interop.cache";
    private const string ReferencePaths = "--reference-assembly-paths";
    private const string ImplementationPaths = "--implementation-assembly-paths";
    private const string OutputDirectory = "--generated-assembly-directory";

    private static readonly string[] Scenarios =
    [
        "cold", "unchanged", "reordered-paths", "generic-argument-scopes",
        "missing-output", "missing-cache", "malformed-cache", "truncated-cache", "corrupt-dll", "truncated-dll",
        "opt-out-cold", "opt-out", "re-enable",
        "method-body", "unrelated-type", "type-identity", "assembly-identity", "assembly-version",
        "runtime-class-name", "interop-usage",
        "runtime-bytes", "framework-bytes", "projection-bytes", "reference-projection-bytes"
    ];

    private static readonly string[] SingleInputPaths =
    [
        "--output-assembly-path",
        "--winrt-sdk-projection-assembly-path",
        "--winrt-sdk-xaml-projection-assembly-path",
        "--winrt-projection-assembly-path",
        "--winrt-component-assembly-path"
    ];

    internal static int Run(string[] args)
    {
        if (args is ["--list-scenarios"])
        {
            Console.WriteLine(string.Join(Environment.NewLine, Scenarios));
            return 0;
        }

        string? root = null;
        string scenario = "setup";

        try
        {
            Require(args.Length >= 2,
                "Usage: BuildDeterminismTest --interop-incremental <generator.exe|generator.dll> <response.rsp> " +
                "[--probe-only] [--scenario <name[,name...]>] [--baseline-generator <generator.exe|generator.dll>]. " +
                "Use --interop-incremental --list-scenarios to list scenarios.");

            string generator = GetGeneratorPath(args[0]);
            string responsePath = Path.GetFullPath(args[1]);
            string? baselineGenerator = null;
            bool probeOnly = false;
            HashSet<string> selected = new(StringComparer.Ordinal);

            for (int i = 2; i < args.Length; i++)
            {
                if (args[i] == "--probe-only")
                {
                    Require(!probeOnly, "--probe-only can only be specified once.");
                    probeOnly = true;
                    continue;
                }

                Require(i + 1 < args.Length, $"Missing value for '{args[i]}'.");

                if (args[i] == "--scenario")
                {
                    string[] names = args[++i].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    Require(names.Length > 0, "The scenario selector cannot be empty.");
                    foreach (string name in names)
                    {
                        Require(Scenarios.Contains(name, StringComparer.Ordinal), $"Unknown scenario '{name}'. Use --list-scenarios.");
                        Require(selected.Add(name), $"Scenario '{name}' was selected more than once.");
                    }
                }
                else if (args[i] == "--baseline-generator")
                {
                    Require(baselineGenerator is null, "--baseline-generator can only be specified once.");
                    baselineGenerator = GetGeneratorPath(args[++i]);
                }
                else
                {
                    throw new ArgumentException($"Unknown option '{args[i]}'.");
                }
            }

            string[] scenarios = selected.Count == 0 ? Scenarios : Scenarios.Where(selected.Contains).ToArray();
            Dictionary<string, string> originalArguments = ReadArguments(responsePath);

            // Never use the system temp directory, hard links, or caller-owned output folders.
            root = Path.GetFullPath($"interop-incremental-{Guid.NewGuid():N}");
            Require(!Directory.Exists(root) && !File.Exists(root), "The replay directory already exists.");
            Directory.CreateDirectory(root);
            File.Copy(responsePath, Path.Combine(root, "original.rsp"));
            Console.WriteLine($"Incremental replay artifacts: {root}");
            Console.WriteLine($"Selected scenarios ({scenarios.Length}): {string.Join(", ", scenarios)}");

            Workspace workspace = new(root, originalArguments, probeOnly);
            string seedDirectory = Path.Combine(root, "seed");
            scenario = "seed";
            Output seed = Invoke(generator, workspace.Arguments, seedDirectory, Path.Combine(root, "seed-invocation"),
                enabled: true, expectHit: false, degree: 1);
            VerifyGeneratedProxies(Path.Combine(seedDirectory, DllName));

            if (baselineGenerator is not null)
            {
                scenario = "baseline-generator";
                Output baseline = Invoke(baselineGenerator, workspace.Arguments, Path.Combine(root, "baseline"),
                    Path.Combine(root, "baseline-invocation"), enabled: false, expectHit: false, degree: 1);
                Compare(seed, baseline, scenario);
            }

            foreach (string name in scenarios)
            {
                scenario = name;
                Console.WriteLine($"Running '{name}'...");
                RunScenario(generator, workspace, seedDirectory, seed, name);
                Console.WriteLine($"PASS '{name}'");
            }

            Directory.Delete(root, recursive: true);
            Console.WriteLine($"All {scenarios.Length} incremental replay scenarios passed. Owned artifacts removed.");
            return 0;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"FAIL '{scenario}': {e.Message}");

            if (root is not null && Directory.Exists(root))
            {
                File.WriteAllText(Path.Combine(root, "failure.txt"), $"Scenario: {scenario}{Environment.NewLine}{e}");
                Console.Error.WriteLine($"Inputs, response files, output DLLs, and process logs retained at: {root}");
            }

            return 1;
        }
    }

    private static void RunScenario(string generator, Workspace workspace, string seedDirectory, Output seed, string name)
    {
        string directory = Path.Combine(workspace.Root, "scenarios", name);
        string cachedDirectory = Path.Combine(directory, "cached");
        Directory.CreateDirectory(cachedDirectory);
        Dictionary<string, string> arguments = new(workspace.Arguments, StringComparer.Ordinal);
        Dictionary<string, byte[]> inputBackups = new(StringComparer.OrdinalIgnoreCase);

        if (name is not ("cold" or "opt-out-cold"))
        {
            File.Copy(Path.Combine(seedDirectory, DllName), Path.Combine(cachedDirectory, DllName));
            File.Copy(Path.Combine(seedDirectory, CacheName), Path.Combine(cachedDirectory, CacheName));
        }

        void MutateInput(string path, Action<string> mutation)
        {
            inputBackups.Add(path, File.ReadAllBytes(path));
            RecordMutation(path, mutation, directory);
        }

        bool expectHit = name is "unchanged" or "reordered-paths" or "generic-argument-scopes" or "method-body" or "unrelated-type";
        bool enabled = name is not ("opt-out" or "opt-out-cold");
        string cachePath = Path.Combine(cachedDirectory, CacheName);
        string dllPath = Path.Combine(cachedDirectory, DllName);

        switch (name)
        {
            case "missing-output":
                File.Delete(dllPath);
                break;
            case "missing-cache":
                File.Delete(cachePath);
                break;
            case "malformed-cache":
                RecordMutation(cachePath, path => File.WriteAllText(path, "This is not an interop cache."), directory);
                break;
            case "truncated-cache":
                RecordMutation(cachePath, Truncate, directory);
                break;
            case "corrupt-dll":
                RecordMutation(dllPath, ChangePeBytesKeepingMvid, directory);
                break;
            case "truncated-dll":
                RecordMutation(dllPath, Truncate, directory);
                break;
            case "method-body":
            case "unrelated-type":
            case "type-identity":
            case "assembly-identity":
            case "assembly-version":
            case "runtime-class-name":
            case "interop-usage":
                MutateInput(workspace.ProbePath, path => Mutate(path, name));
                break;
            case "re-enable":
                MutateInput(workspace.ProbePath, path => Mutate(path, "interop-usage"));
                break;
            case "runtime-bytes":
                MutateInput(workspace.WinrtRuntimePath, ChangePeBytesKeepingMvid);
                break;
            case "framework-bytes":
                MutateInput(workspace.SystemRuntimePath, ChangePeBytesKeepingMvid);
                break;
            case "projection-bytes":
                MutateInput(arguments["--winrt-sdk-projection-assembly-path"], ChangePeBytesKeepingMvid);
                break;
            case "reference-projection-bytes":
                MutateInput(workspace.GetReferenceProjectionPath(), ChangePeBytesKeepingMvid);
                break;
            case "reordered-paths":
                foreach (string argument in new[] { ReferencePaths, ImplementationPaths })
                {
                    string[] paths = SplitPaths(arguments[argument]);
                    arguments[argument] = string.Join(",", paths.Reverse());
                }
                Require(arguments[ImplementationPaths] != workspace.Arguments[ImplementationPaths],
                    "Reordering the input paths did not change their order.");
                break;
        }

        // This run uses the same (possibly mutated) physical input files as every candidate below.
        // In particular, a neutral edit must match a NEW emission including its newly computed MVID.
        Output fresh = Invoke(generator, arguments, Path.Combine(directory, "fresh"),
            Path.Combine(directory, "fresh-disabled"), enabled: false, expectHit: false, degree: 1);

        if (name == "re-enable")
        {
            Output disabled = Invoke(generator, arguments, cachedDirectory, Path.Combine(directory, "disabled-with-stale-cache"),
                enabled: false, expectHit: false, degree: 2);
            Compare(disabled, fresh, name + "/disabled");
            Require(disabled.Hash != seed.Hash, "The disabled run must replace the DLL with genuinely different interop code.");
        }

        int[] degrees = name switch
        {
            "unchanged" => InteropDeterminismRunner.DegreesOfParallelism,
            "generic-argument-scopes" => [1, 2, -1, 1, 2],
            _ => [2]
        };
        for (int i = 0; i < degrees.Length; i++)
        {
            string label = $"candidate-{i + 1}-dop-{degrees[i]}";
            if (name == "generic-argument-scopes" && i > 0)
            {
                foreach (string argument in new[] { ReferencePaths, ImplementationPaths })
                {
                    string[] paths = SplitPaths(workspace.Arguments[argument]);
                    arguments[argument] = string.Join(",", i % 2 == 0 ? paths : paths.Reverse());
                }
                Output repeatedFresh = Invoke(generator, arguments, Path.Combine(directory, label + "-fresh-output"),
                    Path.Combine(directory, label + "-fresh-disabled"), enabled: false, expectHit: false, degree: degrees[i]);
                Compare(repeatedFresh, fresh, name + "/" + label + "/fresh");
            }
            Output candidate = Invoke(generator, arguments, cachedDirectory, Path.Combine(directory, label),
                enabled, expectHit, degrees[i]);
            Compare(candidate, fresh, name + "/" + label);

            if (name is "method-body" or "unrelated-type")
            {
                Require(candidate.Mvid != seed.Mvid && candidate.Hash != seed.Hash,
                    $"'{name}' reused the old MVID instead of refreshing it for the changed input bytes.");
            }
        }

        if (enabled && !expectHit)
        {
            // A recovered miss must publish a usable sidecar, not merely leave one present on disk.
            Output hit = Invoke(generator, arguments, cachedDirectory, Path.Combine(directory, "hit-after-miss"),
                enabled: true, expectHit: true, degree: 1);
            Compare(hit, fresh, name + "/hit-after-miss");
        }

        // Restore only after success: failure artifacts must retain the exact inputs used by the failing replay.
        foreach ((string path, byte[] bytes) in inputBackups)
        {
            File.WriteAllBytes(path, bytes);
        }
    }

    private static Output Invoke(
        string generator,
        Dictionary<string, string> arguments,
        string outputDirectory,
        string invocationDirectory,
        bool enabled,
        bool expectHit,
        int degree)
    {
        Directory.CreateDirectory(outputDirectory);
        Directory.CreateDirectory(invocationDirectory);
        Dictionary<string, string> replayArguments = new(arguments, StringComparer.Ordinal)
        {
            [OutputDirectory] = outputDirectory,
            ["--enable-incremental-generation"] = enabled.ToString(),
            ["--max-degrees-of-parallelism"] = degree.ToString(CultureInfo.InvariantCulture)
        };
        string responsePath = Path.Combine(invocationDirectory, "cswinrtinteropgen.rsp");
        File.WriteAllLines(responsePath, replayArguments.Select(pair => $"{pair.Key} {pair.Value}"));
        string stdoutPath = Path.Combine(invocationDirectory, "stdout.log");
        string stderrPath = Path.Combine(invocationDirectory, "stderr.log");
        string cachePath = Path.Combine(outputDirectory, CacheName);
        CacheStamp previousCache = GetCacheStamp(cachePath);
        bool managed = Path.GetExtension(generator).Equals(".dll", StringComparison.OrdinalIgnoreCase);
        ProcessStartInfo startInfo = new(managed ? "dotnet" : generator)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = invocationDirectory
        };
        if (managed)
        {
            startInfo.ArgumentList.Add(generator);
        }
        startInfo.ArgumentList.Add("@" + responsePath);

        Stopwatch timer = Stopwatch.StartNew();
        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("The generator did not start.");
        using (FileStream stdout = File.Create(stdoutPath))
        using (FileStream stderr = File.Create(stderrPath))
        {
            // Drain both pipes concurrently, and retain complete logs even for a failing process.
            Task.WhenAll(
                process.StandardOutput.BaseStream.CopyToAsync(stdout),
                process.StandardError.BaseStream.CopyToAsync(stderr),
                process.WaitForExitAsync()).GetAwaiter().GetResult();
        }
        File.WriteAllText(Path.Combine(invocationDirectory, "process.txt"),
            $"Generator: {generator}{Environment.NewLine}Exit code: {process.ExitCode}{Environment.NewLine}Elapsed: {timer.Elapsed}");
        Require(process.ExitCode == 0,
            $"Generator exited with {process.ExitCode}. See '{stdoutPath}' and '{stderrPath}'.");

        string dllPath = Path.Combine(outputDirectory, DllName);
        string[] lines = File.ReadAllLines(stdoutPath);
        int hitLines = lines.Count(line => line.StartsWith("Reusing cached interop code -> ", StringComparison.Ordinal));
        bool generated = lines.Contains("Generating interop code", StringComparer.Ordinal);
        bool completed = lines.Contains($"Interop code generated -> {dllPath}", StringComparer.Ordinal);

        if (expectHit)
        {
            Require(hitLines == 1 && lines.Contains($"Reusing cached interop code -> {dllPath}", StringComparer.Ordinal) &&
                    !generated && !completed,
                $"Expected only a cache-hit message, not emission. See '{stdoutPath}'.");
        }
        else
        {
            Require(hitLines == 0 && generated && completed,
                $"Expected a full emission, not a cache hit. See '{stdoutPath}'.");
        }

        if (enabled)
        {
            Require(File.Exists(cachePath) && new FileInfo(cachePath).Length > 0, "Enabled generation did not publish a cache.");
        }
        else
        {
            Require(previousCache == GetCacheStamp(cachePath),
                "Cache-disabled generation created, removed, or rewrote the cache sidecar.");
        }

        Require(File.Exists(dllPath), $"Generator output is missing: {dllPath}");
        Output output = new(Hash(dllPath), ReadMvid(dllPath));
        File.WriteAllText(Path.Combine(invocationDirectory, "output.txt"),
            $"SHA256: {output.Hash}{Environment.NewLine}MVID: {output.Mvid}{Environment.NewLine}DLL: {dllPath}");
        return output;
    }

    private static void Compare(Output actual, Output fresh, string scenario)
    {
        Require(actual.Hash == fresh.Hash,
            $"'{scenario}' differs from a cache-disabled emission of the SAME inputs. " +
            $"Actual SHA256: {actual.Hash} (MVID {actual.Mvid}); fresh SHA256: {fresh.Hash} (MVID {fresh.Mvid}).");
    }

    private static void RecordMutation(string path, Action<string> mutation, string directory)
    {
        string originalHash = Hash(path);
        long originalLength = new FileInfo(path).Length;
        mutation(path);
        string changedHash = Hash(path);
        Require(originalHash != changedHash, $"Mutation of '{path}' did not change any bytes.");
        File.AppendAllText(Path.Combine(directory, "mutations.txt"),
            $"{path}{Environment.NewLine}Before: {originalHash}, {originalLength} bytes{Environment.NewLine}" +
            $"After:  {changedHash}, {new FileInfo(path).Length} bytes{Environment.NewLine}");
    }

    private static void Truncate(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        Require(bytes.Length > 1, $"Cannot truncate '{path}': it is already too short.");
        File.WriteAllBytes(path, bytes.AsSpan(0, bytes.Length / 2).ToArray());
    }

    private static string Hash(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static CacheStamp GetCacheStamp(string path) =>
        File.Exists(path) ? new(Hash(path), File.GetLastWriteTimeUtc(path)) : default;

    private static string GetGeneratorPath(string path)
    {
        path = Path.GetFullPath(path);
        Require(File.Exists(path), $"Interop generator not found: {path}");
        return path;
    }

    private static Dictionary<string, string> ReadArguments(string path)
    {
        Dictionary<string, string> arguments = new(StringComparer.Ordinal);
        foreach (string line in File.ReadAllLines(path))
        {
            string trimmed = line.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }
            int separator = trimmed.IndexOf(' ');
            Require(separator > 0, $"Malformed response-file line: '{line}'.");
            Require(arguments.TryAdd(trimmed[..separator], trimmed[(separator + 1)..]), $"Duplicate response-file argument: '{line}'.");
        }

        foreach (string required in new[]
        {
            ReferencePaths, ImplementationPaths, "--output-assembly-path", "--winrt-sdk-projection-assembly-path",
            OutputDirectory, "--enable-incremental-generation", "--max-degrees-of-parallelism"
        })
        {
            Require(arguments.ContainsKey(required), $"The response file must specify '{required}'.");
        }

        arguments.Remove("--debug-repro-directory");
        foreach (string name in arguments.Keys)
        {
            Require(!name.EndsWith("-path", StringComparison.Ordinal) &&
                    !name.EndsWith("-paths", StringComparison.Ordinal) &&
                    !name.EndsWith("-directory", StringComparison.Ordinal) ||
                    SingleInputPaths.Contains(name, StringComparer.Ordinal) ||
                    name is ReferencePaths or ImplementationPaths or OutputDirectory,
                $"Unknown path-bearing argument '{name}'; refusing to replay without isolating it.");
        }
        return arguments;
    }

    private static string[] SplitPaths(string value) =>
        value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    private readonly record struct Output(string Hash, Guid Mvid);

    private readonly record struct CacheStamp(string? Hash, DateTime LastWriteTime);

    private sealed class Workspace
    {
        internal string Root { get; }
        internal Dictionary<string, string> Arguments { get; }
        internal string ProbePath { get; }
        internal string SystemRuntimePath { get; }
        internal string WinrtRuntimePath { get; }

        internal Workspace(string root, Dictionary<string, string> original, bool probeOnly)
        {
            Root = root;
            if (probeOnly)
            {
                original = SelectProbeInputs(original);
            }
            Arguments = new(original, StringComparer.Ordinal);
            Dictionary<string, string[]> inputArguments = new(StringComparer.Ordinal)
            {
                [ReferencePaths] = SplitPaths(original[ReferencePaths]),
                [ImplementationPaths] = SplitPaths(original[ImplementationPaths])
            };
            foreach (string argument in SingleInputPaths)
            {
                if (original.TryGetValue(argument, out string? value))
                {
                    Require(!string.IsNullOrWhiteSpace(value), $"Input path '{argument}' cannot be empty.");
                    inputArguments.Add(argument, [value]);
                }
            }

            Dictionary<string, string> copies = new(StringComparer.OrdinalIgnoreCase);
            string[] sources = inputArguments.Values.SelectMany(paths => paths).Select(Path.GetFullPath)
                .Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray();
            for (int i = 0; i < sources.Length; i++)
            {
                string source = sources[i];
                Require(File.Exists(source), $"Input binary not found: {source}. Relative paths are resolved from the current working directory.");
                string destinationDirectory = Path.Combine(root, "inputs", i.ToString("D4", CultureInfo.InvariantCulture));
                Directory.CreateDirectory(destinationDirectory);
                // Preserve DLL names, especially private projections also present in the reference/implementation lists.
                string destination = Path.Combine(destinationDirectory, Path.GetFileName(source));
                File.Copy(source, destination);
                copies.Add(source, destination);
            }
            File.WriteAllLines(Path.Combine(root, "input-paths.txt"), copies.Select(pair => $"{pair.Key} -> {pair.Value}"));
            foreach ((string argument, string[] paths) in inputArguments)
            {
                Arguments[argument] = string.Join(",", paths.Select(path => copies[Path.GetFullPath(path)]));
            }

            string[] implementationPaths = SplitPaths(Arguments[ImplementationPaths]);
            SystemRuntimePath = FindRequiredImplementation(implementationPaths, "System.Runtime.dll");
            WinrtRuntimePath = FindRequiredImplementation(implementationPaths, "WinRT.Runtime.dll");
            Require(!sources.Any(path => Path.GetFileNameWithoutExtension(path).Equals(AssemblyName, StringComparison.OrdinalIgnoreCase) ||
                ScopedAssemblyNames.Contains(Path.GetFileNameWithoutExtension(path), StringComparer.OrdinalIgnoreCase)),
                $"The inputs already contain a reserved test assembly named '{AssemblyName}' or one of its scoped argument assemblies.");
            string probeDirectory = Path.Combine(root, "inputs", "probe");
            Directory.CreateDirectory(probeDirectory);
            ProbePath = Path.Combine(probeDirectory, AssemblyName + ".dll");
            Create(ProbePath, SystemRuntimePath, WinrtRuntimePath);
            Arguments[ImplementationPaths] = string.Join(",", implementationPaths.Append(ProbePath).Concat(GetSupportingPaths(ProbePath)));

            if (probeOnly)
            {
                Arguments["--output-assembly-path"] = ProbePath;
            }

            File.WriteAllText(Path.Combine(root, "mode.txt"), probeOnly ? "Probe-only replay" : "Full application replay");
        }

        private static Dictionary<string, string> SelectProbeInputs(Dictionary<string, string> original)
        {
            Dictionary<string, string> selected = new(original, StringComparer.Ordinal);
            List<string> references = [];
            List<string> implementations = [];
            bool hasSdkReference = false;

            foreach (string path in SplitPaths(original[ReferencePaths]))
            {
                System.Reflection.AssemblyName? identity = ReadIdentity(path);
                if (identity?.Name is "Microsoft.Windows.SDK.NET" ||
                    identity?.Name is "Microsoft.Windows.UI.Xaml" && original.ContainsKey("--winrt-sdk-xaml-projection-assembly-path"))
                {
                    references.Add(path);
                    hasSdkReference |= identity.Name == "Microsoft.Windows.SDK.NET";
                }
            }

            Require(hasSdkReference, "--probe-only requires the Microsoft.Windows.SDK.NET reference projection.");

            foreach (string path in SplitPaths(original[ImplementationPaths]))
            {
                System.Reflection.AssemblyName? identity = ReadIdentity(path);
                if (identity is not null && (identity.Name == "WinRT.Runtime" || IsFrameworkAssembly(identity)))
                {
                    implementations.Add(path);
                }
            }

            selected[ReferencePaths] = string.Join(",", references);
            selected[ImplementationPaths] = string.Join(",", implementations);
            selected.Remove("--output-assembly-path");
            selected.Remove("--winrt-projection-assembly-path");
            selected.Remove("--winrt-component-assembly-path");
            // Framework assemblies remain available for resolution, but scanning their own usage in All
            // mode would defeat the small-probe workload. The probe references WinRT.Runtime explicitly.
            selected["--marshalling-mode"] = "Minimal";
            selected.Remove("--marshalling-enabled-assembly-names");

            Console.WriteLine($"Probe-only inputs: {references.Count} SDK reference(s), {implementations.Count} framework/runtime implementation(s). " +
                "Using Minimal discovery; application assemblies, custom projections/components, and explicit assembly opt-ins are excluded.");
            return selected;
        }

        private static System.Reflection.AssemblyName? ReadIdentity(string path)
        {
            try
            {
                return System.Reflection.AssemblyName.GetAssemblyName(Path.GetFullPath(path));
            }
            catch (BadImageFormatException)
            {
                return null;
            }
        }

        private static bool IsFrameworkAssembly(System.Reflection.AssemblyName identity)
        {
            string name = identity.Name ?? "";
            bool frameworkName =
                name is "mscorlib" or "netstandard" or "System" or "WindowsBase" or "Accessibility" or "ReachFramework" or
                    "PresentationCore" or "PresentationFramework" or "Microsoft.CSharp" or "Microsoft.VisualBasic" or "Microsoft.VisualBasic.Core" ||
                name.StartsWith("System.", StringComparison.Ordinal) ||
                name.StartsWith("PresentationFramework.", StringComparison.Ordinal) ||
                name.StartsWith("Microsoft.Win32.", StringComparison.Ordinal) ||
                name.StartsWith("Microsoft.Extensions.", StringComparison.Ordinal) ||
                name.StartsWith("Microsoft.AspNetCore.", StringComparison.Ordinal);

            // Use the framework keys recognized by BaseClassLibraryIdentity, with a name guard to exclude
            // Microsoft-signed application/WinUI assemblies that require the discarded custom projections.
            string token = Convert.ToHexString(identity.GetPublicKeyToken() ?? []);
            return frameworkName && token is
                "B77A5C561934E089" or "B03F5F7F11D50A3A" or "CC7B13FFCD2DDD51" or
                "7CEC85D7BEA7798E" or "31BF3856AD364E35" or "ADB9793829DDAE60";
        }

        internal string GetReferenceProjectionPath()
        {
            foreach (string path in SplitPaths(Arguments[ReferencePaths]).Order(StringComparer.Ordinal))
            {
                ModuleDefinition module = ModuleDefinition.FromBytes(File.ReadAllBytes(path));
                if (module.Assembly?.CustomAttributes.Any(attribute =>
                    attribute.Constructor?.DeclaringType?.FullName == "WindowsRuntime.InteropServices.WindowsRuntimeReferenceAssemblyAttribute") is true)
                {
                    return path;
                }
            }
            throw new InvalidDataException("'reference-projection-bytes' requires a reference assembly marked [WindowsRuntimeReferenceAssembly].");
        }

        private static string FindRequiredImplementation(string[] paths, string fileName)
        {
            string[] matches = paths.Where(path => Path.GetFileName(path).Equals(fileName, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.Ordinal).ToArray();
            Require(matches.Length == 1, $"The replay requires exactly one implementation path named '{fileName}', but found {matches.Length}.");
            return matches[0];
        }
    }
}
