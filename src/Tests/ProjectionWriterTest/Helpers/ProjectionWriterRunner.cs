// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ProjectionWriterTest.Helpers;

/// <summary>
/// Runs the projection writer end-to-end (through <c>cswinrtprojectionrefgen</c>) and exposes the
/// generated C# sources to tests.
/// </summary>
/// <remarks>
/// The tool is invoked as a separate process with a response file, exactly as the
/// <c>CsWinRTGenerateProjection</c> MSBuild target does, so the tests cover the real code path. Both
/// reference/implementation modes and UWP/WinUI inputs are cached separately, since generating them
/// is the expensive part and every test only inspects the resulting text.
/// </remarks>
internal static class ProjectionWriterRunner
{
    /// <summary>
    /// The generated sources, cached separately for each output mode and XAML input.
    /// </summary>
    /// <remarks>
    /// Lazy initialization prevents concurrent tests from generating the same sources more than once.
    /// </remarks>
    private static readonly ConcurrentDictionary<(bool ReferenceProjection, bool UseWinUI), Lazy<string>> Sources = new();

    /// <summary>
    /// Gets all generated C# sources for the requested projection mode, concatenated.
    /// </summary>
    /// <param name="referenceProjection">Whether to get the reference projection (rather than the implementation projection).</param>
    /// <param name="useWinUI">Whether to use WinUI metadata and collection types instead of UWP XAML.</param>
    /// <returns>The concatenated contents of every generated <c>.cs</c> file.</returns>
    public static string GetSources(bool referenceProjection, bool useWinUI = false)
    {
        return Sources.GetOrAdd(
            (referenceProjection, useWinUI),
            static input => new(() => Generate(input.ReferenceProjection, input.UseWinUI))).Value;
    }

    /// <summary>
    /// Counts the applications of a given attribute in the generated sources.
    /// </summary>
    /// <remarks>
    /// Attributes are matched on the emitted text (<c>[global::&lt;name&gt;</c>), which is how the writer
    /// emits every carried-over attribute. Matching the fully qualified name avoids false positives from
    /// same-named attributes in other namespaces.
    /// </remarks>
    /// <param name="referenceProjection">Whether to inspect the reference projection (rather than the implementation projection).</param>
    /// <param name="attributeName">The fully qualified attribute name (without the <c>Attribute</c> suffix), e.g. <c>Windows.Foundation.Metadata.Overload</c>.</param>
    /// <returns>The number of applications found.</returns>
    public static int CountGlobalAttribute(bool referenceProjection, string attributeName)
    {
        return CountOccurrences(GetSources(referenceProjection), $"[global::{attributeName}");
    }

    /// <summary>
    /// Counts the applications of a given attribute emitted without a <c>global::</c> prefix.
    /// </summary>
    /// <remarks>
    /// The CsWinRT markers the implementation projection needs at runtime (e.g.
    /// <c>[WindowsRuntimeType]</c>) are emitted unqualified, relying on the file's <c>using</c> directives.
    /// </remarks>
    /// <param name="referenceProjection">Whether to inspect the reference projection (rather than the implementation projection).</param>
    /// <param name="attributeText">The emitted attribute text, e.g. <c>[WindowsRuntimeType]</c>.</param>
    /// <returns>The number of applications found.</returns>
    public static int CountAttributeText(bool referenceProjection, string attributeText)
    {
        return CountOccurrences(GetSources(referenceProjection), attributeText);
    }

    /// <summary>
    /// Generates a projection for the requested mode and returns all generated sources, concatenated.
    /// </summary>
    /// <remarks>
    /// Each fixture projects 'Windows.Foundation' and two collection types from the selected XAML
    /// namespace. WinUI metadata is an additional input, not a replacement for its Windows SDK dependencies.
    /// The SDK input is whichever version is installed, so tests must assert stable projection behavior
    /// rather than incidental metadata such as whether a particular API is experimental.
    /// </remarks>
    /// <param name="referenceProjection">Whether to generate a reference projection.</param>
    /// <param name="useWinUI">Whether to use WinUI metadata and collection types instead of UWP XAML.</param>
    /// <returns>The concatenated contents of every generated <c>.cs</c> file.</returns>
    private static string Generate(bool referenceProjection, bool useWinUI)
    {
        string toolPath = GetRequiredFilePath("ProjectionRefGeneratorAssemblyPath");
        string inputPaths = useWinUI ? $"sdk,{GetRequiredFilePath("WinUIMetadataPath")}" : "sdk";
        string xamlNamespace = useWinUI ? "Microsoft.UI.Xaml" : "Windows.UI.Xaml";
        string includeNamespaces = $"Windows.Foundation,{xamlNamespace}.DependencyObjectCollection,{xamlNamespace}.Controls.ItemCollection";
        string workingDirectory = Path.Combine(Path.GetTempPath(), $"ProjectionWriterTest_{Guid.NewGuid():N}");
        string outputDirectory = Path.Combine(workingDirectory, "Generated");

        _ = Directory.CreateDirectory(outputDirectory);

        try
        {
            string responseFile = Path.Combine(workingDirectory, "projection.rsp");

            // Each line holds a single '--argument value' pair, matching the MSBuild task's format. The 'sdk'
            // input token makes the tool resolve the Windows SDK metadata installed on the machine.
            File.WriteAllLines(responseFile,
            [
                $"--input-paths {inputPaths}",
                $"--output-directory {outputDirectory}",
                "--target-framework net10.0",
                $"--include-namespaces {includeNamespaces}",
                $"--reference-projection {(referenceProjection ? "true" : "false")}"
            ]);

            (int exitCode, string output) = Run(toolPath, $"@{responseFile}");

            Assert.AreEqual(0, exitCode, $"The projection writer failed for '{includeNamespaces}':{Environment.NewLine}{output}");

            string[] sourceFiles = Directory.GetFiles(outputDirectory, "*.cs", SearchOption.AllDirectories);

            Assert.AreNotEqual(0, sourceFiles.Length, "The projection writer produced no sources.");

            return string.Join(Environment.NewLine, sourceFiles.Select(File.ReadAllText));
        }
        finally
        {
            TryDeleteDirectory(workingDirectory);
        }
    }

    /// <summary>
    /// Runs the projection generator tool with a single argument.
    /// </summary>
    /// <param name="toolPath">The path of the tool assembly to run.</param>
    /// <param name="argument">The single command line argument to pass.</param>
    /// <returns>The process exit code and its combined standard output and error.</returns>
    private static (int ExitCode, string Output) Run(string toolPath, string argument)
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

        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start the projection generator process.");

        string standardOutput = process.StandardOutput.ReadToEnd();
        string standardError = process.StandardError.ReadToEnd();

        process.WaitForExit();

        return (process.ExitCode, standardOutput + standardError);
    }

    /// <summary>
    /// Resolves a required tool or input file path from assembly metadata.
    /// </summary>
    /// <param name="metadataName">The assembly metadata key containing the path.</param>
    /// <returns>The full path of the required file.</returns>
    private static string GetRequiredFilePath(string metadataName)
    {
        string? path = typeof(ProjectionWriterRunner).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == metadataName)?.Value;

        Assert.IsFalse(string.IsNullOrEmpty(path), $"The '{metadataName}' assembly metadata was not found.");

        string fullPath = Path.GetFullPath(path!);

        Assert.IsTrue(File.Exists(fullPath), $"The file specified by '{metadataName}' was not found at '{fullPath}'.");

        return fullPath;
    }

    /// <summary>
    /// Counts the non-overlapping occurrences of a literal value in some text.
    /// </summary>
    /// <param name="text">The text to search.</param>
    /// <param name="value">The literal value to count.</param>
    /// <returns>The number of occurrences.</returns>
    private static int CountOccurrences(string text, string value)
    {
        int count = 0;
        int index = 0;

        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    /// <summary>
    /// Deletes a directory recursively, ignoring failures (best effort cleanup).
    /// </summary>
    /// <param name="directory">The directory to delete.</param>
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
