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
/// Runs the WinMD generator (<c>cswinrtwinmdgen</c>) end-to-end and asserts its outcome.
/// </summary>
/// <remarks>
/// Each entry point compiles a small C# input (or builds a raw response file), runs the actual tool as
/// a separate process, and asserts the result. Tests should call <see cref="AssertSuccess"/> or one of
/// the <c>AssertFailure</c> overloads so each scenario stays a single call.
/// </remarks>
internal static class WinMDGeneratorRunner
{
    /// <summary>
    /// Asserts that the generator succeeds (exit code <c>0</c>) for the given component source.
    /// </summary>
    /// <param name="source">The C# source defining the authored component types.</param>
    /// <param name="useWindowsUIXamlProjections">Whether to use the UWP (<c>Windows.UI.Xaml</c>) projections.</param>
    public static void AssertSuccess(string source, bool useWindowsUIXamlProjections = false)
    {
        (int exitCode, string output) = Run(source, useWindowsUIXamlProjections);

        Assert.AreEqual(0, exitCode, output);
    }

    /// <summary>
    /// Asserts that the generator fails for the given component source, reporting the expected error.
    /// </summary>
    /// <param name="source">The C# source defining the authored component types.</param>
    /// <param name="error">The expected error id (e.g. <c>"CSWINRTWINMDGEN0011"</c>) the output must contain.</param>
    /// <param name="useWindowsUIXamlProjections">Whether to use the UWP (<c>Windows.UI.Xaml</c>) projections.</param>
    public static void AssertFailure(string source, string error, bool useWindowsUIXamlProjections = false)
    {
        AssertFailureResult(Run(source, useWindowsUIXamlProjections), error);
    }

    /// <summary>
    /// Asserts that the generator fails for a caller-provided response file, reporting the expected error.
    /// </summary>
    /// <remarks>
    /// The <paramref name="responseFileFactory"/> receives a fresh temporary working directory and
    /// returns the full response file content to run with (one <c>--argument value</c> pair per line). It
    /// can use <see cref="CompileComponent"/> to produce an input assembly, or reference any other path
    /// under the directory (e.g. to test invalid inputs).
    /// </remarks>
    /// <param name="responseFileFactory">Builds the full response file content for a given temporary directory.</param>
    /// <param name="error">The expected error id (e.g. <c>"CSWINRTWINMDGEN0002"</c>) the output must contain.</param>
    public static void AssertFailure(Func<string, string> responseFileFactory, string error)
    {
        AssertFailureResult(Run(responseFileFactory), error);
    }

    /// <summary>
    /// Asserts that the generator fails when pointed at a response file path that does not exist,
    /// reporting the expected error.
    /// </summary>
    /// <param name="error">The expected error id (e.g. <c>"CSWINRTWINMDGEN0001"</c>) the output must contain.</param>
    public static void AssertFailureForMissingResponseFile(string error)
    {
        string missingResponseFilePath = Path.Combine(Path.GetTempPath(), $"WinMDGeneratorTest_{Guid.NewGuid():N}.rsp");

        AssertFailureResult(RunTool(GetGeneratorPath(), missingResponseFilePath), error);
    }

    /// <summary>
    /// Compiles the given C# <paramref name="source"/> into <c>TestInput.dll</c> in <paramref name="directory"/>.
    /// </summary>
    /// <remarks>
    /// This is exposed for the <see cref="AssertFailure(Func{string, string}, string)"/>
    /// scenarios that need a valid input assembly before triggering a later-stage failure (e.g. an
    /// unwritable output path).
    /// </remarks>
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
    /// Compiles the given C# <paramref name="source"/> into an assembly and runs the WinMD generator
    /// against it with a standard response file.
    /// </summary>
    private static (int ExitCode, string Output) Run(string source, bool useWindowsUIXamlProjections)
    {
        return Run(temporaryDirectory =>
        {
            string inputAssemblyPath = CompileComponent(source, temporaryDirectory);

            return $"""
                --input-assembly-path {inputAssemblyPath}
                --reference-assembly-paths {inputAssemblyPath}
                --output-winmd-path {Path.Combine(temporaryDirectory, "TestInput.winmd")}
                --assembly-version 1.0.0.0
                --use-windows-ui-xaml-projections {useWindowsUIXamlProjections}
                """;
        });
    }

    /// <summary>
    /// Runs the WinMD generator against a caller-provided response file.
    /// </summary>
    private static (int ExitCode, string Output) Run(Func<string, string> responseFileFactory)
    {
        string toolPath = GetGeneratorPath();
        string temporaryDirectory = Directory.CreateTempSubdirectory("WinMDGeneratorTest_").FullName;

        try
        {
            string responseFileContent = responseFileFactory(temporaryDirectory);
            string responseFilePath = Path.Combine(temporaryDirectory, "args.rsp");

            File.WriteAllText(responseFilePath, responseFileContent);

            return RunTool(toolPath, responseFilePath);
        }
        finally
        {
            TryDeleteDirectory(temporaryDirectory);
        }
    }

    /// <summary>
    /// Asserts that a generator run failed (non-zero exit code) and its output contains the expected error.
    /// </summary>
    private static void AssertFailureResult((int ExitCode, string Output) result, string error)
    {
        Assert.AreNotEqual(0, result.ExitCode, result.Output);
        StringAssert.Contains(result.Output, error);
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
        string? path = typeof(WinMDGeneratorRunner).Assembly
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
