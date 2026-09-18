// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using InteropGeneratorTest.Helpers;

namespace InteropGeneratorTest;

[TestClass]
public sealed class Test_ForwardedTypeIdentity
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task ForwardedFrameworkTypes_HaveOneResolvedAssociationPerGroup(bool useFrameworkImplementations)
    {
        using InteropGeneratorRunner runner = new(useFrameworkImplementations);
        string output = await runner.GenerateAsync("forwarded");
        (RuntimeContext context, ModuleDefinition module) = runner.LoadOutput(output);
        List<Association> associations = ReadAssociations(module, context);

        foreach (IGrouping<(string Group, string Source), Association> group in associations.GroupBy(association => (association.Group, association.ResolvedSource)))
        {
            Assert.AreEqual(1, group.Count(),
                $"Duplicate resolved association for {group.Key}:\n{string.Join("\n", group.Select(association => $"{association.Source} => {association.Target}"))}");
        }

        Association[] dictionaries = associations.Where(association =>
            association.Group.EndsWith("WindowsRuntimeComWrappersTypeMapGroup") &&
            association.Source is GenericInstanceTypeSignature generic &&
            generic.GenericType.Name == "ReadOnlyDictionary`2" &&
            generic.TypeArguments[0].FullName == "System.String" &&
            generic.TypeArguments[1].FullName is "System.Object" or "System.String").ToArray();

        Assert.AreEqual(2, dictionaries.Length, "Both observed closed dictionary instantiations must have CCWs.");
        CollectionAssert.AreEquivalent(new[] { "System.Object", "System.String" },
            dictionaries.Select(association => ((GenericInstanceTypeSignature)association.Source).TypeArguments[1].FullName).ToArray());

        foreach (Association association in associations)
        {
            foreach (TypeSignature type in InteropGeneratorRunner.EnumerateTypes(association.Source))
            {
                ITypeDescriptor namedType = type is GenericInstanceTypeSignature generic ? generic.GenericType : type;

                if (namedType.FullName is
                    "System.Collections.ObjectModel.ReadOnlyDictionary`2" or
                    "System.Collections.ObjectModel.ReadOnlyDictionary`2+KeyCollection" or
                    "System.Collections.ObjectModel.ReadOnlyDictionary`2+ValueCollection")
                {
                    Assert.IsTrue(namedType.TryResolve(context, out TypeDefinition? definition));
                    Assert.AreEqual("System.Private.CoreLib", definition!.DeclaringModule!.Assembly!.Name!.ToString(),
                        $"The emitted reference must resolve to the framework implementation of '{type}'.");
                }
            }
        }

        foreach (string nestedName in new[] { "KeyCollection", "ValueCollection" })
        {
            Assert.AreEqual(1, associations.Count(association =>
                association.Group.EndsWith("WindowsRuntimeComWrappersTypeMapGroup") &&
                association.Source is GenericInstanceTypeSignature generic &&
                generic.GenericType.FullName == "System.Collections.ObjectModel.ReadOnlyDictionary`2+" + nestedName &&
                generic.TypeArguments.All(argument => argument.FullName == "System.String")),
                $"Nested forwarded type '{nestedName}' must retain exactly one CCW.");
        }

        Association[] distinct = associations.Where(association =>
            association.Group.EndsWith("WindowsRuntimeComWrappersTypeMapGroup") &&
            association.Source is GenericInstanceTypeSignature generic &&
            generic.GenericType.Name == "SameName`1").ToArray();
        Assert.AreEqual(2, distinct.Length, "Equal names in genuinely different assemblies must not be merged.");
        Assert.AreNotEqual(distinct[0].ResolvedSource, distinct[1].ResolvedSource);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task ForwardedFrameworkTypes_AreDeterministicAcrossInputOrderAndParallelism(bool useFrameworkImplementations)
    {
        using InteropGeneratorRunner runner = new(useFrameworkImplementations);
        byte[] expected = File.ReadAllBytes(await runner.GenerateAsync("serial"));

        foreach ((bool reverse, int parallelism) in new[] { (true, 1), (false, -1), (true, 2) })
        {
            byte[] actual = File.ReadAllBytes(await runner.GenerateAsync($"order-{reverse}-dop-{parallelism}", reverse, parallelism));

            if (!expected.SequenceEqual(actual))
            {
                string directory = Directory.CreateTempSubdirectory("InteropDeterminismFailure_").FullName;
                File.WriteAllBytes(Path.Combine(directory, "expected.dll"), expected);
                File.WriteAllBytes(Path.Combine(directory, "actual.dll"), actual);
                System.Console.WriteLine($"Non-deterministic assemblies: {directory}");
            }

            CollectionAssert.AreEqual(expected, actual,
                $"Changing reference/implementation order or parallelism changed output (reverse={reverse}, parallelism={parallelism}).");
        }
    }

    [TestMethod]
    [DataRow("Minimal", false, null, 1)]
    [DataRow("Strict", true, null, 1)]
    [DataRow("Strict", false, null, 0)]
    [DataRow("Minimal", false, true, 1)]
    [DataRow("Strict", true, true, 1)]
    [DataRow("All", true, true, 1)]
    [DataRow("Minimal", false, false, 0)]
    [DataRow("Strict", true, false, 0)]
    [DataRow("Strict", false, false, 0)]
    [DataRow("All", true, false, 0)]
    public async Task NetStandard20_UsesTargetFrameworkIdentitiesAndRespectsMarshallingMode(
        string mode,
        bool optIn,
        bool? analyzeNetStandardAssemblies,
        int expectedCallbacks)
    {
        using InteropGeneratorRunner runner = new();
        string output = await runner.GenerateAsync(
            "standard", marshallingMode: mode, optInNetStandard: optIn, analyzeNetStandardAssemblies: analyzeNetStandardAssemblies);
        (RuntimeContext context, ModuleDefinition module) = runner.LoadOutput(output);
        List<Association> associations = ReadAssociations(module, context);

        Assert.AreEqual(expectedCallbacks, associations.Count(association =>
            association.Group.EndsWith("WindowsRuntimeComWrappersTypeMapGroup") &&
            association.Source.FullName == "StandardInput.Callback"));

        foreach (Association association in associations)
        {
            foreach (TypeSignature type in InteropGeneratorRunner.EnumerateTypes(association.Source))
            {
                _ = InteropGeneratorRunner.ResolvedTypeKey(type, context);
            }
        }

        AssertUniqueAssociations(associations);
    }

    [TestMethod]
    public async Task NetStandardDiscoveryOption_InvalidatesMSBuildCacheOnlyWhenChanged()
    {
        using InteropGeneratorRunner runner = new();
        string project = runner.CreateMSBuildProject();
        string output = await runner.RunMSBuildAsync(project, null);
        string cache = Path.Combine(Path.GetDirectoryName(output)!, "Discovery.cswinrtgen.cache");
        string enabledCache = File.ReadAllText(cache);
        byte[] enabledHash = SHA256.HashData(File.ReadAllBytes(output));
        DateTime enabledWriteTime = File.GetLastWriteTimeUtc(output);
        AssertPortableCallbackCount(runner, output, 1);

        _ = await runner.RunMSBuildAsync(project, true);
        Assert.AreEqual(enabledCache, File.ReadAllText(cache), "The explicit default must not change the cache.");
        Assert.AreEqual(enabledWriteTime, File.GetLastWriteTimeUtc(output), "An unchanged option must not regenerate interop.");

        _ = await runner.RunMSBuildAsync(project, false);
        string disabledCache = File.ReadAllText(cache);
        DateTime disabledWriteTime = File.GetLastWriteTimeUtc(output);
        Assert.AreNotEqual(enabledCache, disabledCache, "Disabling discovery must invalidate the property cache.");
        Assert.IsFalse(enabledHash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(output))));
        AssertPortableCallbackCount(runner, output, 0);

        _ = await runner.RunMSBuildAsync(project, false);
        Assert.AreEqual(disabledCache, File.ReadAllText(cache));
        Assert.AreEqual(disabledWriteTime, File.GetLastWriteTimeUtc(output), "An unchanged disabled option must remain incremental.");

        _ = await runner.RunMSBuildAsync(project, true);
        Assert.AreEqual(enabledCache, File.ReadAllText(cache));
        CollectionAssert.AreEqual(enabledHash, SHA256.HashData(File.ReadAllBytes(output)));
        AssertPortableCallbackCount(runner, output, 1);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task NetStandardDiscoveryOption_IsPreservedInDebugRepros(bool enabled)
    {
        using InteropGeneratorRunner runner = new();
        string directory = Directory.CreateDirectory(Path.Combine(runner.Root, "repro")).FullName;
        string output = await runner.GenerateAsync(
            "debug", marshallingMode: "Strict", optInNetStandard: true,
            analyzeNetStandardAssemblies: enabled, debugReproDirectory: directory);
        string archivePath = Path.Combine(directory, "interop-debug-repro.zip");

        using ZipArchive archive = ZipFile.OpenRead(archivePath);
        using StreamReader reader = new(archive.GetEntry("cswinrtinteropgen.rsp")!.Open());
        string response = await reader.ReadToEndAsync();
        StringAssert.Contains(response, $"--analyze-net-standard-assemblies {enabled}");
        StringAssert.Contains(response, "--marshalling-mode Strict");
        StringAssert.Contains(response, "--marshalling-enabled-assembly-names NetStandardTypes");
        StringAssert.Contains(response, "--generate-collection-changed-list-vtables False");
        AssertPortableCallbackCount(runner, output, enabled ? 1 : 0);

        (int exitCode, string log) = await InteropGeneratorRunner.InvokeGeneratorAsync(archivePath);
        Assert.AreEqual(0, exitCode, log);
        const string outputPrefix = "Interop code generated -> ";
        string replayedOutput = log.Split('\n').Single(line => line.StartsWith(outputPrefix, StringComparison.Ordinal))
            [outputPrefix.Length..].TrimEnd();
        string replayedDirectory = Path.GetDirectoryName(replayedOutput)!;
        StringAssert.StartsWith(Path.GetFileName(replayedDirectory), "cswinrtinteropgen-debug-repro-unpack-");

        try
        {
            string replayedResponse = File.ReadAllText(Path.Combine(replayedDirectory, "cswinrtinteropgen.rsp"));
            StringAssert.Contains(replayedResponse, $"--analyze-net-standard-assemblies {enabled}");
            StringAssert.Contains(replayedResponse, "--marshalling-mode Strict");
            StringAssert.Contains(replayedResponse, "--marshalling-enabled-assembly-names NetStandardTypes");
            AssertPortableCallbackCount(runner, replayedOutput, enabled ? 1 : 0);
        }
        finally
        {
            Directory.Delete(replayedDirectory, recursive: true);
        }
    }

    private static void AssertPortableCallbackCount(InteropGeneratorRunner runner, string output, int expected)
    {
        (RuntimeContext context, ModuleDefinition module) = runner.LoadOutput(output);
        List<Association> associations = ReadAssociations(module, context);
        Assert.AreEqual(expected, associations.Count(association =>
            association.Group.EndsWith("WindowsRuntimeComWrappersTypeMapGroup") &&
            association.Source.FullName == "StandardInput.Callback"));
        AssertUniqueAssociations(associations);
    }

    [TestMethod]
    public async Task OverlappingFrameworkReferenceDeclarations_UseResolvedIdentity()
    {
        using InteropGeneratorRunner runner = new(useFrameworkImplementations: true, overlappingFrameworkReferences: true);

        foreach (bool reverseInputs in new[] { false, true })
        {
            string output = await runner.GenerateAsync($"overlapping-{reverseInputs}", reverseInputs);
            (RuntimeContext context, ModuleDefinition module) = runner.LoadOutput(output);
            AssertUniqueAssociations(ReadAssociations(module, context));
        }
    }

    private static void AssertUniqueAssociations(List<Association> associations)
    {
        foreach (IGrouping<(string Group, string Source), Association> group in associations.GroupBy(association => (association.Group, association.ResolvedSource)))
        {
            Assert.AreEqual(1, group.Count(), $"Duplicate resolved association for {group.Key}.");
        }
    }

    private static List<Association> ReadAssociations(ModuleDefinition module, RuntimeContext context)
    {
        List<Association> result = [];

        foreach (CustomAttribute attribute in module.Assembly!.CustomAttributes)
        {
            if (attribute.Constructor?.DeclaringType is not TypeSpecification { Signature: GenericInstanceTypeSignature generic } ||
                generic.GenericType.Name != "TypeMapAssociationAttribute`1")
            {
                continue;
            }

            Assert.AreEqual(1, generic.TypeArguments.Count);
            TypeSignature group = generic.TypeArguments[0];
            Assert.IsNotNull(attribute.Signature);
            Assert.AreEqual(2, attribute.Signature.FixedArguments.Count);
            TypeSignature source = (TypeSignature)attribute.Signature.FixedArguments[0].Element!;
            TypeSignature target = (TypeSignature)attribute.Signature.FixedArguments[1].Element!;
            result.Add(new(group.FullName, source, target, InteropGeneratorRunner.ResolvedTypeKey(source, context)));
        }

        Assert.IsGreaterThan(0, result.Count, "An empty map cannot satisfy the uniqueness regression.");
        return result;
    }

    private sealed record Association(string Group, TypeSignature Source, TypeSignature Target, string ResolvedSource);
}
