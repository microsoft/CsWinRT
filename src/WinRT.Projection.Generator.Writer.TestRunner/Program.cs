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
        // Modes:
        //   1) Single .winmd path → simple test (legacy)
        //   2) "compare" <winmd-folder> <internal-winmd> <output-folder> → SDK projection mode
        //   3) "compare-xaml" <xaml-winmd-folder> <internal-winmd> <output-folder> → XAML projection mode
        if (args.Length >= 4 && args[0] == "compare")
        {
            return RunCompare(args[1], args[2], args[3]);
        }
        if (args.Length >= 4 && args[0] == "compare-xaml")
        {
            return RunCompareXaml(args[1], args[2], args[3]);
        }

        return RunSimple(args);
    }

    private static int RunSimple(string[] args)
    {
        string winmdPath = args.Length > 0 ? args[0] : @"C:\Program Files (x86)\Windows Kits\10\UnionMetadata\10.0.26100.0\Windows.winmd";
        string outputFolder = args.Length > 1 ? args[1] : Path.Combine(Path.GetTempPath(), "CsWinRTPort", $"out-{DateTime.UtcNow:yyyyMMddHHmmss}");

        if (!File.Exists(winmdPath))
        {
            Console.Error.WriteLine($"Input file not found: {winmdPath}");
            return 1;
        }

        Console.WriteLine($"Input:  {winmdPath}");
        Console.WriteLine($"Output: {outputFolder}");

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

        if (Directory.Exists(outputFolder))
        {
            string[] files = Directory.GetFiles(outputFolder, "*.cs", SearchOption.AllDirectories);
            Console.WriteLine($"Generated {files.Length} files");
        }
        return 0;
    }

    /// <summary>
    /// Runs the projection writer with the same options the build pipeline uses for the SDK
    /// projection (the truth output to compare against).
    /// </summary>
    private static int RunCompare(string winmdFolder, string internalWinmd, string output)
    {
        if (Directory.Exists(output))
        {
            Directory.Delete(output, true);
        }
        _ = Directory.CreateDirectory(output);

        try
        {
            ProjectionWriter.Run(new ProjectionWriterOptions
            {
                InputPaths = new[] { winmdFolder, internalWinmd },
                OutputFolder = output,
                Include = new[]
                {
                    "Windows",
                    "WindowsRuntime.Internal",
                    "Windows.UI.Xaml.Interop",
                    "Windows.UI.Xaml.Data.BindableAttribute",
                    "Windows.UI.Xaml.Markup.ContentPropertyAttribute",
                },
                Exclude = new[]
                {
                    "Windows.UI.Colors",
                    "Windows.UI.ColorHelper",
                    "Windows.UI.IColorHelper",
                    "Windows.UI.IColors",
                    "Windows.UI.Text.FontWeights",
                    "Windows.UI.Text.IFontWeights",
                    "Windows.UI.Xaml",
                    "Windows.ApplicationModel.Store.Preview.WebAuthenticationCoreManagerHelper",
                    "Windows.ApplicationModel.Store.Preview.IWebAuthenticationCoreManagerHelper",
                },
            });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERROR: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }

        Console.WriteLine($"Generated {Directory.GetFiles(output, "*.cs").Length} files in {output}");
        return 0;
    }

    /// <summary>
    /// Runs the projection writer with the same options the build pipeline uses for the XAML
    /// projection (the truth output to compare against). Mirrors the .rsp file in the XAML truth folder.
    /// </summary>
    private static int RunCompareXaml(string xamlWinmdFolder, string internalWinmd, string output)
    {
        if (Directory.Exists(output))
        {
            Directory.Delete(output, true);
        }
        _ = Directory.CreateDirectory(output);

        try
        {
            ProjectionWriter.Run(new ProjectionWriterOptions
            {
                InputPaths = new[] { xamlWinmdFolder, internalWinmd },
                OutputFolder = output,
                // Mirrors the XAML projection generation .rsp:
                // -exclude Windows, then -include for the specific XAML namespaces and helpers.
                Include = new[]
                {
                    "Windows.UI.Colors",
                    "Windows.UI.ColorHelper",
                    "Windows.UI.IColorHelper",
                    "Windows.UI.IColors",
                    "Windows.UI.Text.FontWeights",
                    "Windows.UI.Text.IFontWeights",
                    "Windows.UI.Xaml",
                    "Windows.ApplicationModel.Store.Preview.WebAuthenticationCoreManagerHelper",
                    "Windows.ApplicationModel.Store.Preview.IWebAuthenticationCoreManagerHelper",
                },
                Exclude = new[]
                {
                    "Windows",
                    "Windows.UI.Xaml.Interop",
                    "Windows.UI.Xaml.Data.BindableAttribute",
                    "Windows.UI.Xaml.Markup.ContentPropertyAttribute",
                },
            });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERROR: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }

        Console.WriteLine($"Generated {Directory.GetFiles(output, "*.cs").Length} files in {output}");
        return 0;
    }
}

