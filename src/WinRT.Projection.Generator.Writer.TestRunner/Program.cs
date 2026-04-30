// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using WindowsRuntime.ProjectionGenerator.Writer;

namespace WindowsRuntime.ProjectionGenerator.Writer.TestRunner;

internal static class Program
{
    public static int Main(string[] args)
    {
        // Default to using Windows.winmd from the SDK if no arg is provided.
        string winmdPath = args.Length > 0 ? args[0] : @"C:\Program Files (x86)\Windows Kits\10\UnionMetadata\10.0.26100.0\Windows.winmd";
        string outputFolder = args.Length > 1 ? args[1] : Path.Combine(Path.GetTempPath(), "CsWinRTPort", $"out-{DateTime.UtcNow:yyyyMMddHHmmss}");

        if (!File.Exists(winmdPath))
        {
            Console.Error.WriteLine($"Input file not found: {winmdPath}");
            return 1;
        }

        Console.WriteLine($"Input:  {winmdPath}");
        Console.WriteLine($"Output: {outputFolder}");
        Console.WriteLine();

        try
        {
            ProjectionWriter.Run(new ProjectionWriterOptions
            {
                InputPaths = new[] { winmdPath },
                OutputFolder = outputFolder,
                Include = new[] { "Windows", "WindowsRuntime.Internal" },
                Verbose = true,
            });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERROR: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }

        // Show summary of generated files
        if (Directory.Exists(outputFolder))
        {
            string[] files = Directory.GetFiles(outputFolder, "*.cs", SearchOption.AllDirectories);
            Console.WriteLine();
            Console.WriteLine($"Generated {files.Length} files:");
            int shown = 0;
            foreach (string f in files)
            {
                Console.WriteLine($"  {Path.GetFileName(f)}");
                if (++shown >= 20)
                {
                    Console.WriteLine($"  ... and {files.Length - shown} more");
                    break;
                }
            }
        }
        return 0;
    }
}
