// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

/// <summary>
/// Replays a fixed interop response file in fresh processes and output directories.
/// Usage: BuildDeterminismTest --interop &lt;generator.exe|generator.dll&gt; &lt;response.rsp&gt;.
/// Relative input paths are resolved from the current working directory, as with the generator.
/// </summary>
internal static class InteropDeterminismRunner
{
    internal static readonly int[] DegreesOfParallelism = [-1, 1, 2, -1, 1];

    internal static int Run(string generatorPath, string responseFilePath)
    {
        generatorPath = Path.GetFullPath(generatorPath);
        responseFilePath = Path.GetFullPath(responseFilePath);

        if (!File.Exists(generatorPath))
        {
            throw new FileNotFoundException("Interop generator not found.", generatorPath);
        }

        string[] originalLines = File.ReadAllLines(responseFilePath);
        string root = Path.Combine(Path.GetTempPath(), $"CsWinRT.InteropDeterminism.{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        Console.WriteLine($"Interop determinism outputs: {root}");

        bool succeeded = false;

        try
        {
            string? expectedHash = null;

            for (int i = 0; i < DegreesOfParallelism.Length; i++)
            {
                int degree = DegreesOfParallelism[i];
                string label = $"run-{i + 1}-dop-{degree}";
                string directory = Path.Combine(root, label);
                Directory.CreateDirectory(directory);

                // Do not overwrite a debug archive belonging to the original invocation
                List<string> lines = originalLines
                    .Where(line => !line.StartsWith("--debug-repro-directory ", StringComparison.Ordinal))
                    .ToList();

                ReplaceArgument(lines, "--generated-assembly-directory", directory);
                ReplaceArgument(lines, "--enable-incremental-generation", "False");
                ReplaceArgument(lines, "--max-degrees-of-parallelism", degree.ToString());

                string responsePath = Path.Combine(directory, "cswinrtinteropgen.rsp");
                File.WriteAllLines(responsePath, lines);

                bool isManagedTool = Path.GetExtension(generatorPath).Equals(".dll", StringComparison.OrdinalIgnoreCase);
                ProcessStartInfo startInfo = new(isManagedTool ? "dotnet" : generatorPath);

                if (isManagedTool)
                {
                    startInfo.ArgumentList.Add(generatorPath);
                }

                startInfo.ArgumentList.Add($"@{responsePath}");
                BuildDeterminismRunner.RunProcess(startInfo);

                string hash = BuildDeterminismRunner.HashOutput(Path.Combine(directory, "WinRT.Interop.dll"), label);
                expectedHash ??= hash;

                if (hash != expectedHash)
                {
                    Console.Error.WriteLine($"Interop output differs for maximum parallelism {degree}.");
                    return 1;
                }
            }

            succeeded = true;
            Console.WriteLine("Interop output is deterministic across repeated serial and parallel runs.");
            return 0;
        }
        finally
        {
            if (succeeded)
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static void ReplaceArgument(List<string> lines, string name, string value)
    {
        string prefix = name + " ";
        int index = lines.FindIndex(line => line.StartsWith(prefix, StringComparison.Ordinal));

        if (index < 0 || lines.FindIndex(index + 1, line => line.StartsWith(prefix, StringComparison.Ordinal)) >= 0)
        {
            throw new InvalidDataException($"The response file must specify '{name}' exactly once.");
        }

        lines[index] = prefix + value;
    }
}
