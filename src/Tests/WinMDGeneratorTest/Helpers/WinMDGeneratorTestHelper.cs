// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace WinMDGeneratorTest.Helpers;

/// <summary>
/// Helpers to drive the WinMD generator (<c>cswinrtwinmdgen</c>) end-to-end: compile a small C# input,
/// run the actual tool against it as a separate process, and capture its result.
/// </summary>
internal static class WinMDGeneratorTestHelper
{
    /// <summary>
    /// Compiles the given C# <paramref name="source"/> into an assembly and runs the WinMD generator
    /// against it, returning the process exit code and combined output.
    /// </summary>
    /// <param name="source">The C# source defining the authored component types.</param>
    /// <param name="useWindowsUIXamlProjections">Whether to use the UWP (<c>Windows.UI.Xaml</c>) projections.</param>
    /// <returns>The generator process exit code and its combined standard output and error.</returns>
    public static (int ExitCode, string Output) RunGenerator(string source, bool useWindowsUIXamlProjections = false)
    {
        string toolPath = GetGeneratorPath();
        string temporaryDirectory = Directory.CreateTempSubdirectory("WinMDGeneratorTest_").FullName;

        try
        {
            string inputAssemblyPath = Path.Combine(temporaryDirectory, "TestInput.dll");
            string outputWinmdPath = Path.Combine(temporaryDirectory, "TestInput.winmd");
            string responseFilePath = Path.Combine(temporaryDirectory, "args.rsp");

            CompileInputAssembly(source, inputAssemblyPath);

            // Each line of the response file is a single '<argument-name> <value>' pair. The input
            // assembly is also passed as its own reference path (its directory seeds the resolver).
            File.WriteAllLines(responseFilePath,
            [
                $"--input-assembly-path {inputAssemblyPath}",
                $"--reference-assembly-paths {inputAssemblyPath}",
                $"--output-winmd-path {outputWinmdPath}",
                "--assembly-version 1.0.0.0",
                $"--use-windows-ui-xaml-projections {useWindowsUIXamlProjections}",
            ]);

            return RunTool(toolPath, responseFilePath);
        }
        finally
        {
            try
            {
                Directory.Delete(temporaryDirectory, recursive: true);
            }
            catch (IOException)
            {
                // Best effort cleanup; a leftover temp directory must not fail the test
            }
        }
    }

    /// <summary>
    /// Compiles the given C# <paramref name="source"/> into an assembly on disk.
    /// </summary>
    private static void CompileInputAssembly(string source, string outputPath)
    {
        CSharpParseOptions parseOptions = new(LanguageVersion.Preview);

        // The target framework attribute lets the generator probe the .NET runtime version. It is added
        // in a separate syntax tree so it does not interfere with any 'using' directives in the test
        // source (an assembly attribute must precede type declarations but follow 'using' directives).
        SyntaxTree sourceTree = CSharpSyntaxTree.ParseText(source, parseOptions);
        SyntaxTree assemblyInfoTree = CSharpSyntaxTree.ParseText(
            """[assembly: System.Runtime.Versioning.TargetFramework(".NETCoreApp,Version=v10.0")]""",
            parseOptions);

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "TestInput",
            syntaxTrees: [sourceTree, assemblyInfoTree],
            references: Net100.References.All,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));

        EmitResult result = compilation.Emit(outputPath);

        Assert.IsTrue(result.Success, $"Input compilation failed:\n{string.Join("\n", result.Diagnostics)}");
    }

    /// <summary>
    /// Runs <c>dotnet exec &lt;tool&gt; &lt;responseFile&gt;</c> and captures the exit code and output.
    /// </summary>
    private static (int ExitCode, string Output) RunTool(string toolPath, string responseFilePath)
    {
        ProcessStartInfo startInfo = new("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        startInfo.ArgumentList.Add("exec");
        startInfo.ArgumentList.Add(toolPath);
        startInfo.ArgumentList.Add(responseFilePath);

        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start the WinMD generator process.");

        string standardOutput = process.StandardOutput.ReadToEnd();
        string standardError = process.StandardError.ReadToEnd();

        process.WaitForExit();

        return (process.ExitCode, standardOutput + standardError);
    }

    /// <summary>
    /// Resolves the path to the built <c>cswinrtwinmdgen</c> tool from assembly metadata.
    /// </summary>
    private static string GetGeneratorPath()
    {
        string? path = typeof(WinMDGeneratorTestHelper).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(static attribute => attribute.Key == "WinMDGeneratorAssemblyPath")?.Value;

        Assert.IsFalse(string.IsNullOrEmpty(path), "The 'WinMDGeneratorAssemblyPath' assembly metadata was not found.");

        string fullPath = Path.GetFullPath(path!);

        Assert.IsTrue(File.Exists(fullPath), $"The WinMD generator was not found at '{fullPath}'.");

        return fullPath;
    }
}
