// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;

namespace ProjectionWriterTest.Helpers;

/// <summary>
/// Runs the projection writer end-to-end (through <c>cswinrtprojectionrefgen</c>) and exposes the
/// generated C# sources to tests.
/// </summary>
/// <remarks>
/// The tool is invoked as a separate process with a response file, exactly as the
/// <c>CsWinRTGenerateProjection</c> MSBuild target does, so the tests cover the real code path. Both
/// projection modes are generated once per test run and cached, since generating them is the
/// expensive part and every test only inspects the resulting text.
/// </remarks>
internal static partial class ProjectionWriterRunner
{
    private const int RtManifest = 24;
    private const uint LoadLibraryAsDataFile = 0x00000002;

    /// <summary>
    /// The namespace the projections are restricted to.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>Windows.Foundation</c> (which also covers <c>Windows.Foundation.Collections</c> and
    /// <c>Windows.Foundation.Metadata</c>) is small enough to generate in well under a second, while
    /// still covering every projected type kind and every carried-over attribute of interest.
    /// </para>
    /// <para>
    /// Tests must only assert on things this namespace is guaranteed to contain in every Windows SDK:
    /// the input metadata is whichever SDK is installed on the machine, so an assertion that some API
    /// carries a given attribute is really an assertion about that SDK, and breaks when an agent has a
    /// different one (an experimental API becoming stable, or not existing yet, is enough).
    /// </para>
    /// </remarks>
    private const string IncludeNamespace = "Windows.Foundation";

    /// <summary>
    /// The lazily generated reference projection sources.
    /// </summary>
    private static readonly Lazy<string> ReferenceProjectionSources = new(static () => Generate(referenceProjection: true));

    /// <summary>
    /// The lazily generated implementation projection sources.
    /// </summary>
    private static readonly Lazy<string> ImplementationProjectionSources = new(static () => Generate(referenceProjection: false));

    /// <summary>
    /// Gets all generated C# sources for the requested projection mode, concatenated.
    /// </summary>
    /// <param name="referenceProjection">Whether to get the reference projection (rather than the implementation projection).</param>
    /// <returns>The concatenated contents of every generated <c>.cs</c> file.</returns>
    public static string GetSources(bool referenceProjection)
    {
        return referenceProjection
            ? ReferenceProjectionSources.Value
            : ImplementationProjectionSources.Value;
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
    /// Runs the generator with every path involved in projection generation exceeding the legacy
    /// Windows <c>MAX_PATH</c> limit.
    /// </summary>
    /// <param name="useInputDirectory">Whether the input argument is the containing directory rather than the WinMD file itself.</param>
    /// <returns>The paths and process result needed to verify the scenario.</returns>
    internal static LongPathRunResult RunLongPathScenario(bool useInputDirectory)
    {
        string toolPath = GetGeneratorPath();
        string workingDirectory = Path.Combine(Path.GetTempPath(), $"ProjectionWriterLongPathTest_{Guid.NewGuid():N}");
        string expectedOutputPath;

        do
        {
            workingDirectory = Path.Combine(workingDirectory, "segment123456789");
            expectedOutputPath = Path.Combine(workingDirectory, "output", "Windows.Foundation.Collections.cs");
        }
        while (Path.Combine(workingDirectory, "projection.rsp").Length <= 260 ||
               Path.Combine(workingDirectory, "input").Length <= 260 ||
               Path.Combine(workingDirectory, "input", "Windows.Foundation.FoundationContract.winmd").Length <= 260 ||
               expectedOutputPath.Length <= 260);

        string inputDirectory = Path.Combine(workingDirectory, "input");
        string outputDirectory = Path.Combine(workingDirectory, "output");

        _ = Directory.CreateDirectory(inputDirectory);
        _ = Directory.CreateDirectory(outputDirectory);

        try
        {
            string inputWinmdPath = Path.Combine(inputDirectory, "Windows.Foundation.FoundationContract.winmd");
            string responseFilePath = Path.Combine(workingDirectory, "projection.rsp");
            string inputPath = useInputDirectory ? inputDirectory : inputWinmdPath;

            File.Copy(FindWindowsFoundationWinmd(), inputWinmdPath);
            File.WriteAllLines(responseFilePath,
            [
                $"--input-paths {inputPath}",
                $"--output-directory {outputDirectory}",
                "--target-framework net10.0",
                "--include-namespaces Windows.Foundation.Collections",
                "--reference-projection true"
            ]);

            (int exitCode, string output) = Run(toolPath, $"@{responseFilePath}");

            return new(
                ResponseFilePathLength: responseFilePath.Length,
                InputPathLength: inputPath.Length,
                OutputPathLength: expectedOutputPath.Length,
                ExitCode: exitCode,
                Output: output,
                OutputExists: File.Exists(expectedOutputPath));
        }
        finally
        {
            TryDeleteDirectory(workingDirectory);
        }
    }

    /// <summary>
    /// Gets the value of the generator executable's embedded <c>longPathAware</c> setting.
    /// </summary>
    internal static string? GetLongPathAwareManifestValue()
    {
        string executablePath = Path.ChangeExtension(GetGeneratorPath(), ".exe");

        if (!File.Exists(executablePath))
        {
            throw new FileNotFoundException("The projection generator executable was not found.", executablePath);
        }

        nint module = LoadLibraryEx(executablePath, 0, LoadLibraryAsDataFile);

        if (module == 0)
        {
            throw new InvalidOperationException($"Failed to load resources from '{executablePath}'.");
        }

        try
        {
            nint resource = FindResource(module, 1, RtManifest);

            if (resource == 0)
            {
                throw new InvalidOperationException($"No application manifest was found in '{executablePath}'.");
            }

            uint size = SizeofResource(module, resource);
            nint resourceData = LoadResource(module, resource);
            nint manifestData = LockResource(resourceData);

            if (size == 0 || manifestData == 0)
            {
                throw new InvalidOperationException("The embedded application manifest could not be read.");
            }

            byte[] bytes = new byte[size];
            Marshal.Copy(manifestData, bytes, 0, bytes.Length);

            string manifest = Encoding.UTF8.GetString(bytes);
            return XDocument
                .Parse(manifest)
                .Descendants()
                .FirstOrDefault(static element => element.Name.LocalName == "longPathAware")
                ?.Value;
        }
        finally
        {
            _ = FreeLibrary(module);
        }
    }

    /// <summary>
    /// Generates a projection for the requested mode and returns all generated sources, concatenated.
    /// </summary>
    /// <param name="referenceProjection">Whether to generate a reference projection.</param>
    /// <returns>The concatenated contents of every generated <c>.cs</c> file.</returns>
    private static string Generate(bool referenceProjection)
    {
        string toolPath = GetGeneratorPath();
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
                "--input-paths sdk",
                $"--output-directory {outputDirectory}",
                "--target-framework net10.0",
                $"--include-namespaces {IncludeNamespace}",
                $"--reference-projection {(referenceProjection ? "true" : "false")}"
            ]);

            (int exitCode, string output) = Run(toolPath, $"@{responseFile}");

            Assert.AreEqual(0, exitCode, $"The projection writer failed for '{IncludeNamespace}':{Environment.NewLine}{output}");

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
    /// Resolves the path to the built <c>cswinrtprojectionrefgen</c> tool from assembly metadata.
    /// </summary>
    /// <returns>The full path of the tool assembly.</returns>
    private static string GetGeneratorPath()
    {
        string? path = typeof(ProjectionWriterRunner).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(static attribute => attribute.Key == "ProjectionRefGeneratorAssemblyPath")?.Value;

        Assert.IsFalse(string.IsNullOrEmpty(path), "The 'ProjectionRefGeneratorAssemblyPath' assembly metadata was not found.");

        string fullPath = Path.GetFullPath(path!);

        Assert.IsTrue(File.Exists(fullPath), $"The projection generator was not found at '{fullPath}'.");

        return fullPath;
    }

    /// <summary>
    /// Finds the installed Windows Foundation contract used as the explicit long-path WinMD input.
    /// </summary>
    private static string FindWindowsFoundationWinmd()
    {
        string referencesDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Windows Kits",
            "10",
            "References");

        string? winmdPath = Directory
            .EnumerateFiles(referencesDirectory, "Windows.Foundation.FoundationContract.winmd", SearchOption.AllDirectories)
            .OrderByDescending(static path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        return winmdPath ?? throw new FileNotFoundException($"No Windows Foundation contract was found under '{referencesDirectory}'.");
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

    [LibraryImport("kernel32.dll", EntryPoint = "LoadLibraryExW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial nint LoadLibraryEx(string fileName, nint file, uint flags);

    [LibraryImport("kernel32.dll", EntryPoint = "FindResourceW", SetLastError = true)]
    private static partial nint FindResource(nint module, nint name, nint type);

    [LibraryImport("kernel32.dll", EntryPoint = "SizeofResource", SetLastError = true)]
    private static partial uint SizeofResource(nint module, nint resource);

    [LibraryImport("kernel32.dll", EntryPoint = "LoadResource", SetLastError = true)]
    private static partial nint LoadResource(nint module, nint resource);

    [LibraryImport("kernel32.dll", EntryPoint = "LockResource")]
    private static partial nint LockResource(nint resourceData);

    [LibraryImport("kernel32.dll", EntryPoint = "FreeLibrary")]
    private static partial int FreeLibrary(nint module);

    /// <summary>
    /// Captures the observable result of a long-path generator invocation.
    /// </summary>
    internal sealed record LongPathRunResult(
        int ResponseFilePathLength,
        int InputPathLength,
        int OutputPathLength,
        int ExitCode,
        string Output,
        bool OutputExists);
}
