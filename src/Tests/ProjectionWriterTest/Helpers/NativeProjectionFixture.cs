// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using WindowsRuntime;

namespace ProjectionWriterTest.Helpers;

/// <summary>
/// Builds a small metadata projection and native-backed consumer through the real generators.
/// </summary>
internal sealed class NativeProjectionFixture : IDisposable
{
    public string DirectoryPath { get; } = Directory.CreateTempSubdirectory("NativeProjection_").FullName;

    public NativeProjectionFixture()
    {
        // Preserve repository restore sources when publishing projects outside the checkout
        File.Copy(ProjectionWriterRunner.GetRequiredFilePath("RepositoryNuGetConfigPath"), Path.Combine(DirectoryPath, "nuget.config"));
    }

    public static NativeProjectionFixture Create(
        string fileStem,
        string resourceName,
        Func<string, string, string> createMetadata,
        string[]? additionalResources = null,
        string[]? preprocessorSymbols = null)
    {
        NativeProjectionFixture fixture = new();
        string directory = fixture.DirectoryPath;
        try
        {
            string input = createMetadata(directory, fileStem);
            string projection = CompileProjection(input, Path.Combine(directory, "projection"),
                Path.Combine(directory, "WinRT.Projection.dll"));
            string referenceDirectory = Directory.CreateDirectory(Path.Combine(directory, "reference")).FullName;

            // The interop generator needs an SDK reference and a nontrivial projected class hierarchy.
            string sdkDirectory = Directory.CreateDirectory(Path.Combine(directory, "sdk")).FullName;
            string sdkInput = ArrayPropertyMetadata.Create(sdkDirectory, exclusiveTo: true, includeArrayProperties: false);
            string sdkProjection = CompileProjection(sdkInput, Path.Combine(directory, "sdk-sources"),
                Path.Combine(directory, "WinRT.Sdk.Projection.dll"));
            string sdkReference = CompileProjection(sdkInput, Path.Combine(directory, "sdk-reference-sources"),
                Path.Combine(referenceDirectory, "Microsoft.Windows.SDK.NET.dll"), referenceProjection: true);

            List<string> sources = [];
            foreach (string name in new[] { resourceName }.Concat(additionalResources ?? []))
            {
                using Stream resource = typeof(NativeProjectionFixture).Assembly.GetManifestResourceStream(
                    $"ProjectionWriterTest.Resources.{name}.cs")!;
                using StreamReader reader = new(resource);
                string source = reader.ReadToEnd();
                File.WriteAllText(Path.Combine(directory, name + ".cs"), source);
                sources.Add(source);
            }
            string app = ProjectionWriterRunner.CompileSources(sources, Path.Combine(directory, resourceName + ".dll"),
                outputKind: OutputKind.ConsoleApplication, preprocessorSymbols: preprocessorSymbols, additionalReferences: [projection]);

            // Use the same BCL reference images as CompileSources, not implementations with forwarded corelib scopes.
            string[] frameworkReferences = Net100.ReferenceInfos.All.Select(info =>
            {
                string path = Path.Combine(referenceDirectory, info.FileName);
                File.WriteAllBytes(path, info.ImageBytes);
                return path;
            }).ToArray();
            string runtime = typeof(WindowsRuntimeObject).Assembly.Location;
            string response = Path.Combine(directory, "interop.rsp");
            File.WriteAllLines(response,
            [
                $"--reference-assembly-paths {string.Join(",", new[] { sdkReference, projection, sdkProjection, runtime }.Concat(frameworkReferences))}",
                $"--implementation-assembly-paths {app},{runtime},{string.Join(",", frameworkReferences)}",
                $"--output-assembly-path {app}",
                $"--winrt-sdk-projection-assembly-path {sdkProjection}",
                $"--winrt-projection-assembly-path {projection}",
                $"--generated-assembly-directory {directory}",
                "--use-windows-ui-xaml-projections false",
                "--validate-winrt-runtime-assembly-version true",
                "--validate-winrt-runtime-dll-version-2-references true",
                "--enable-incremental-generation false",
                "--treat-warnings-as-errors true",
                "--max-degrees-of-parallelism 1"
            ]);
            (int exitCode, string log) = ProjectionWriterRunner.Run(
                ProjectionWriterRunner.GetRequiredFilePath("InteropGeneratorAssemblyPath"), $"@{response}");
            Assert.AreEqual(0, exitCode, log);
            Assert.IsTrue(File.Exists(Path.Combine(directory, "WinRT.Interop.dll")));
            File.Copy(runtime, Path.Combine(directory, "WinRT.Runtime.dll"));
            File.WriteAllText(Path.Combine(directory, resourceName + ".runtimeconfig.json"), """
                {"runtimeOptions":{"tfm":"net10.0","framework":{"name":"Microsoft.NETCore.App","version":"10.0.0"}}}
                """);
            return fixture;
        }
        catch
        {
            fixture.Dispose();
            throw;
        }
    }

    public void Dispose() => Directory.Delete(DirectoryPath, recursive: true);

    public static string CompileProjection(string input, string output, string assembly, bool referenceProjection = false)
    {
        GenerateProjection(input, output, referenceProjection, Path.GetFileNameWithoutExtension(assembly));
        return ProjectionWriterRunner.CompileSources(Directory.GetFiles(output, "*.cs").Select(File.ReadAllText),
            assembly, referenceProjection);
    }

    public static void GenerateProjection(string input, string output, bool referenceProjection, string outputAssemblyName = "WinRT.Sdk.Projection")
    {
        Directory.CreateDirectory(output);
        string response = Path.Combine(output, "projection.rsp");
        File.WriteAllLines(response,
        [
            $"--input-paths {input}",
            $"--output-directory {output}",
            $"--assembly-name {outputAssemblyName}",
            "--target-framework net10.0",
            "--include-namespaces Contoso",
            $"--reference-projection {referenceProjection.ToString().ToLowerInvariant()}"
        ]);
        // Allows red/green verification against a preserved tool build, without changing product sources.
        string tool = Environment.GetEnvironmentVariable("CSWINRT_TEST_PROJECTION_REFGEN")
            ?? ProjectionWriterRunner.GetRequiredFilePath("ProjectionRefGeneratorAssemblyPath");
        (int exitCode, string log) = ProjectionWriterRunner.Run(tool, $"@{response}");
        Assert.AreEqual(0, exitCode, log);
    }

    public static (int ExitCode, string Output) Run(string command, string[] arguments)
    {
        ProcessStartInfo start = new(command)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (string argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }
        using Process process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        return (process.ExitCode, stdout.GetAwaiter().GetResult() + stderr.GetAwaiter().GetResult());
    }
}
