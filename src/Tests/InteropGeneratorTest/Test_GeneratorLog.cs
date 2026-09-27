// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AsmResolver.DotNet;
using InteropGeneratorTest.Helpers;

namespace InteropGeneratorTest;

[TestClass]
public sealed class Test_GeneratorLog
{
    [TestMethod]
    public async Task LogContainsDiscoveredAndEmittedTypesOnlyWhenEnabled()
    {
        using InteropGeneratorRunner runner = new();
        string plainOutput = await runner.GenerateAsync("plain");
        Assert.IsFalse(File.Exists(Path.Combine(Path.GetDirectoryName(plainOutput)!, "interop-log.json")));

        string logDirectory = Directory.CreateDirectory(Path.Combine(runner.Root, "logs")).FullName;
        string output = await runner.GenerateAsync("logged", logDirectory: logDirectory);
        string logPath = Path.Combine(logDirectory, "interop-log.json");
        Assert.IsTrue(File.Exists(logPath));
        CollectionAssert.AreEqual(File.ReadAllBytes(plainOutput), File.ReadAllBytes(output),
            "Enabling the log must not alter the generated assembly.");

        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(logPath));
        JsonElement root = document.RootElement;
        Assert.AreEqual(1, root.GetProperty("schema").GetInt32());
        Assert.AreEqual(new FileInfo(output).Length, root.GetProperty("assemblySizeBytes").GetInt64());

        Assert.IsTrue(root.GetProperty("typeHierarchy").EnumerateArray().Any(entry =>
            entry.GetProperty("type").GetString() == "ProjectionInput.Derived" &&
            entry.GetProperty("baseType").GetString() == "ProjectionInput.Base"));

        JsonElement generics = root.GetProperty("genericInstantiations");
        foreach (string category in new[]
        {
            "genericDelegates", "enumerators", "enumerables", "lists", "readOnlyLists",
            "dictionaries", "readOnlyDictionaries", "observableVectors", "observableMaps",
            "mapChangedEventArgs", "asyncActionsWithProgress", "asyncOperations",
            "asyncOperationsWithProgress", "keyValuePairs"
        })
        {
            Assert.AreEqual(JsonValueKind.Array, generics.GetProperty(category).ValueKind, category);
        }

        Assert.IsTrue(generics.GetProperty("dictionaries").EnumerateArray().Any(type =>
            type.GetString()!.Contains("System.Collections.Generic.IDictionary", StringComparison.Ordinal)));

        JsonElement userTypes = root.GetProperty("userDefinedTypes");
        Assert.AreEqual(2, userTypes.EnumerateArray().Count(entry =>
            entry.GetProperty("type").GetString()!.Contains(":IdentityControl.SameName`1<", StringComparison.Ordinal)));
        Assert.IsTrue(userTypes.EnumerateArray().Any(entry =>
            entry.GetProperty("interfaces").EnumerateArray().Any()));
        Assert.IsTrue(root.GetProperty("arrayTypes").EnumerateArray().Any(entry =>
            entry.GetProperty("interfaces").EnumerateArray().Any()));

        (RuntimeContext _, ModuleDefinition module) = runner.LoadOutput(output);
        JsonElement generatedTypes = root.GetProperty("generatedTypes");
        Assert.AreEqual(module.GetAllTypes().Count(), generatedTypes.GetArrayLength());
        Assert.IsTrue(generatedTypes.EnumerateArray().Any(type =>
            type.GetProperty("methodCount").GetInt32() > 0 &&
            type.GetProperty("ilInstructionCount").GetInt32() > 0));
        Assert.IsTrue(generatedTypes.EnumerateArray().All(type =>
            type.GetProperty("name").GetString() is not null &&
            type.GetProperty("fieldCount").GetInt32() >= 0));
    }

    [TestMethod]
    public async Task LogIsDeterministicAcrossInputOrderAndParallelism()
    {
        using InteropGeneratorRunner runner = new();
        string directory = Directory.CreateDirectory(Path.Combine(runner.Root, "logs")).FullName;
        _ = await runner.GenerateAsync("serial", logDirectory: directory);
        byte[] expected = File.ReadAllBytes(Path.Combine(directory, "interop-log.json"));

        _ = await runner.GenerateAsync("parallel", reverseInputs: true, parallelism: -1, logDirectory: directory);
        CollectionAssert.AreEqual(expected, File.ReadAllBytes(Path.Combine(directory, "interop-log.json")));
    }

    [TestMethod]
    public async Task LogDirectoryIsPreservedInDebugRepro()
    {
        using InteropGeneratorRunner runner = new();
        string directory = Directory.CreateDirectory(Path.Combine(runner.Root, "repro")).FullName;
        _ = await runner.GenerateAsync("debug", debugReproDirectory: directory, logDirectory: directory);
        string archivePath = Path.Combine(directory, "interop-debug-repro.zip");

        using (ZipArchive archive = ZipFile.OpenRead(archivePath))
        using (StreamReader reader = new(archive.GetEntry("cswinrtinteropgen.rsp")!.Open()))
        {
            string response = await reader.ReadToEndAsync();
            string logArgument = response.Split('\n').Single(line => line.StartsWith("--log-directory ", StringComparison.Ordinal)).Trim();
            Assert.AreEqual("--log-directory .", logArgument, "The repro must not embed the original log directory.");
        }

        (int exitCode, string log) = await InteropGeneratorRunner.InvokeGeneratorAsync(archivePath);
        Assert.AreEqual(0, exitCode, log);
        const string outputPrefix = "Interop code generated -> ";
        string replayedOutput = log.Split('\n').Single(line => line.StartsWith(outputPrefix, StringComparison.Ordinal))
            [outputPrefix.Length..].TrimEnd();
        string replayedDirectory = Path.GetDirectoryName(replayedOutput)!;

        try
        {
            StringAssert.Contains(File.ReadAllText(Path.Combine(replayedDirectory, "cswinrtinteropgen.rsp")),
                $"--log-directory {replayedDirectory}");
            Assert.IsTrue(File.Exists(Path.Combine(replayedDirectory, "interop-log.json")));
        }
        finally
        {
            Directory.Delete(replayedDirectory, recursive: true);
        }
    }

    [TestMethod]
    public async Task LogOptionInvalidatesMSBuildCacheAndMissingLogIsRegenerated()
    {
        using InteropGeneratorRunner runner = new();
        string project = runner.CreateMSBuildProject();
        string output = await runner.RunMSBuildAsync(project, null);
        string cache = Path.Combine(Path.GetDirectoryName(output)!, "Discovery.cswinrtgen.cache");
        string plainCache = File.ReadAllText(cache);
        byte[] plainAssembly = File.ReadAllBytes(output);

        string directory = Directory.CreateDirectory(Path.Combine(runner.Root, "logs")).FullName;
        string relativeDirectory = Path.GetRelativePath(Path.GetDirectoryName(project)!, directory);
        _ = await runner.RunMSBuildAsync(project, null, logDirectory: relativeDirectory);
        string enabledCache = File.ReadAllText(cache);
        string logPath = Path.Combine(directory, "interop-log.json");
        Assert.AreNotEqual(plainCache, enabledCache);
        Assert.IsTrue(File.Exists(logPath));
        CollectionAssert.AreEqual(plainAssembly, File.ReadAllBytes(output));

        File.Delete(logPath);
        _ = await runner.RunMSBuildAsync(project, null, logDirectory: relativeDirectory);
        Assert.IsTrue(File.Exists(logPath), "A missing report must make the interop target run again.");
        Assert.AreEqual(enabledCache, File.ReadAllText(cache));

        _ = await runner.RunMSBuildAsync(project, null);
        Assert.AreEqual(plainCache, File.ReadAllText(cache));
    }

    [TestMethod]
    public async Task MissingLogDirectoryFailsWithDiagnostic()
    {
        using InteropGeneratorRunner runner = new();
        string output = await runner.GenerateAsync("baseline");
        string response = Path.Combine(Path.GetDirectoryName(output)!, "interop.rsp");
        string missingDirectory = Path.Combine(runner.Root, "missing");
        File.AppendAllText(response, Environment.NewLine + "--log-directory " + missingDirectory);
        File.Delete(output);

        (int exitCode, string log) = await InteropGeneratorRunner.InvokeGeneratorAsync(response);
        Assert.AreNotEqual(0, exitCode);
        StringAssert.Contains(log, "CSWINRTINTEROPGEN0109");
        Assert.IsFalse(File.Exists(output), "An invalid log directory must be rejected before generating the assembly.");
    }
}
