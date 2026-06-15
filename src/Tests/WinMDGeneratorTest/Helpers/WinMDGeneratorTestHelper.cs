// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
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
    /// against it with a standard response file, returning the process exit code and combined output.
    /// </summary>
    /// <param name="source">The C# source defining the authored component types.</param>
    /// <param name="useWindowsUIXamlProjections">Whether to use the UWP (<c>Windows.UI.Xaml</c>) projections.</param>
    /// <returns>The generator process exit code and its combined standard output and error.</returns>
    public static (int ExitCode, string Output) RunGenerator(string source, bool useWindowsUIXamlProjections = false)
    {
        return RunGenerator(temporaryDirectory =>
        {
            string inputAssemblyPath = CompileComponent(source, temporaryDirectory);

            return
            [
                $"--input-assembly-path {inputAssemblyPath}",
                $"--reference-assembly-paths {inputAssemblyPath}",
                $"--output-winmd-path {Path.Combine(temporaryDirectory, "TestInput.winmd")}",
                "--assembly-version 1.0.0.0",
                $"--use-windows-ui-xaml-projections {useWindowsUIXamlProjections}",
            ];
        });
    }

    /// <summary>
    /// Runs the WinMD generator against a caller-provided response file, returning the process exit
    /// code and combined output.
    /// </summary>
    /// <remarks>
    /// The <paramref name="responseFileFactory"/> receives a fresh temporary working directory and
    /// returns the response file lines to run with. It can use <see cref="CompileComponent"/> to produce
    /// an input assembly, or reference any other path under the directory (e.g. to test invalid inputs).
    /// </remarks>
    /// <param name="responseFileFactory">Builds the response file lines for a given temporary directory.</param>
    /// <returns>The generator process exit code and its combined standard output and error.</returns>
    public static (int ExitCode, string Output) RunGenerator(Func<string, IReadOnlyList<string>> responseFileFactory)
    {
        string toolPath = GetGeneratorPath();
        string temporaryDirectory = Directory.CreateTempSubdirectory("WinMDGeneratorTest_").FullName;

        try
        {
            IReadOnlyList<string> responseFileLines = responseFileFactory(temporaryDirectory);
            string responseFilePath = Path.Combine(temporaryDirectory, "args.rsp");

            File.WriteAllLines(responseFilePath, responseFileLines);

            return RunTool(toolPath, responseFilePath);
        }
        finally
        {
            TryDeleteDirectory(temporaryDirectory);
        }
    }

    /// <summary>
    /// Runs the WinMD generator pointed at a response file path that does not exist.
    /// </summary>
    /// <returns>The generator process exit code and its combined standard output and error.</returns>
    public static (int ExitCode, string Output) RunGeneratorWithMissingResponseFile()
    {
        string missingResponseFilePath = Path.Combine(Path.GetTempPath(), $"WinMDGeneratorTest_{Guid.NewGuid():N}.rsp");

        return RunTool(GetGeneratorPath(), missingResponseFilePath);
    }

    /// <summary>
    /// Compiles the given C# <paramref name="source"/> into <c>TestInput.dll</c> in <paramref name="directory"/>.
    /// </summary>
    /// <param name="source">The C# source defining the authored component types.</param>
    /// <param name="directory">The directory to emit the assembly into.</param>
    /// <returns>The full path to the compiled assembly.</returns>
    public static string CompileComponent(string source, string directory)
    {
        string outputPath = Path.Combine(directory, "TestInput.dll");

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

        return outputPath;
    }

    /// <summary>
    /// Runs <c>dotnet exec &lt;tool&gt; &lt;argument&gt;</c> and captures the exit code and output.
    /// </summary>
    private static (int ExitCode, string Output) RunTool(string toolPath, string argument)
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
        startInfo.ArgumentList.Add(argument);

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

    /// <summary>
    /// Deletes a directory recursively, ignoring failures (best effort cleanup).
    /// </summary>
    private static void TryDeleteDirectory(string directory)
    {
        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch (IOException)
        {
            // Best effort cleanup; a leftover temp directory must not fail the test
        }
    }
}
