// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AsmResolver.DotNet;
using InteropGeneratorTest.Helpers;

namespace InteropGeneratorTest;

[TestClass]
public sealed class Test_ParallelEmission
{
    [TestMethod]
    public async Task SharedMembersAndVtables_AreByteIdenticalAcrossWorkerCounts()
    {
        using InteropGeneratorRunner runner = new(
            useFrameworkImplementations: true,
            ccwGroupingCases: true,
            forwardedEnumerableAliases: true,
            applicationSource: """
                using System;
                using System.Collections.Generic;
                using Windows.Foundation;
                using WindowsRuntime.InteropServices;

                public static class Program
                {
                    public static Dictionary<int, string> First = new();
                    public static Dictionary<int, object> Second = new();
                    public static Dictionary<Guid, string> Third = new();
                    public static KeyValuePair<int, string>[] Pairs = [];
                    public static IStringable[] Values = [];
                    public static EventHandler<object, int> FirstHandler;
                    public static EventHandler<IStringable, int> SecondHandler;
                    public static IAsyncOperation<int> Operation;
                    public static IAsyncOperationWithProgress<int, double> Progress;
                    public static void Main() => WindowsRuntimeMarshal.GetIid<IList<IStringable>>();
                }
                """);

        string output = await runner.GenerateAsync("serial", parallelism: 1);
        byte[] expected = File.ReadAllBytes(output);
        (_, ModuleDefinition module) = runner.LoadOutput(output);
        TypeDefinition iids = module.TopLevelTypes.Single(type => type.FullName == "ABI.InterfaceIIDs");
        string[] names = iids.Methods.Select(method => method.Name!.ToString()).ToArray();

        CollectionAssert.AreEqual(names.Order(StringComparer.Ordinal).ToArray(), names);
        Assert.AreEqual(names.Length, names.Distinct(StringComparer.Ordinal).Count());
        Assert.IsTrue(module.TopLevelTypes.Any(type => type.Name!.ToString().EndsWith("IidProvider", StringComparison.Ordinal)),
            "The workload must also exercise deterministic IID provider names.");

        foreach (TypeDefinition provider in module.TopLevelTypes.Where(type => type.Name!.ToString().EndsWith("IidProvider", StringComparison.Ordinal)))
        {
            Guid iid = Guid.Parse(provider.CustomAttributes.Single().Signature!.FixedArguments[0].Element!.ToString()!);
            Assert.AreEqual($"<{iid.ToString("N").ToUpperInvariant()}>IidProvider", provider.Name!.ToString());
        }

        TypeDefinition keyValuePairMethods = module.TopLevelTypes.Single(type =>
            type.Name!.ToString().EndsWith(">KeyValuePairMethods", StringComparison.Ordinal));
        string[] accessorNames = keyValuePairMethods.Methods.Select(method => method.Name!.ToString()).ToArray();

        Assert.IsTrue(keyValuePairMethods.IsAbstract && keyValuePairMethods.IsSealed);
        Assert.AreEqual("System.Object", keyValuePairMethods.BaseType!.FullName);
        Assert.IsNotEmpty(accessorNames);
        Assert.IsTrue(accessorNames.All(static name =>
            name.StartsWith("get_Key(", StringComparison.Ordinal) || name.StartsWith("get_Value(", StringComparison.Ordinal)));
        Assert.AreEqual(accessorNames.Length, accessorNames.Distinct(StringComparer.Ordinal).Count());
        CollectionAssert.AreEqual(accessorNames.Order(StringComparer.Ordinal).ToArray(), accessorNames);

        foreach ((bool reverse, int parallelism) in new[] { (true, 1), (false, 2), (true, 4), (false, -1), (true, -1), (false, -1) })
        {
            byte[] actual = File.ReadAllBytes(await runner.GenerateAsync($"parallel-{reverse}-{parallelism}", reverse, parallelism));
            CollectionAssert.AreEqual(expected, actual,
                $"Shared members or vtables changed with reverse={reverse}, DOP={parallelism}.");
        }
    }

    [TestMethod]
    public async Task SmallCatalog_IsByteIdenticalAcrossWorkerCounts()
    {
        using InteropGeneratorRunner runner = new();
        string output = await runner.GenerateAsync(
            "small-serial", marshallingMode: "Strict", marshallingDisabledAssemblyNames: ["WinRT.Runtime.dll"]);
        byte[] expected = File.ReadAllBytes(output);
        (_, ModuleDefinition module) = runner.LoadOutput(output);
        TypeDefinition fields = module.TopLevelTypes.Single(type => type.Name == "<RvaFields>");
        TypeDefinition iids = module.TopLevelTypes.Single(type => type.FullName == "ABI.InterfaceIIDs");
        TypeDefinition keyValuePairMethods = module.TopLevelTypes.Single(type =>
            type.Name!.ToString().EndsWith(">KeyValuePairMethods", StringComparison.Ordinal));
        int[] counts =
        [
            module.TopLevelTypes.Count,
            fields.Fields.Count,
            iids.Methods.Count,
            iids.Properties.Count,
            keyValuePairMethods.Methods.Count,
            module.Assembly!.CustomAttributes.Count
        ];
        Assert.IsTrue(counts.All(static count => count < 256),
            $"All collection sizes must be below the former parallel-ordering threshold: {string.Join(", ", counts)}.");

        foreach (int parallelism in new[] { 1, 2, -1 })
        {
            string replay = await runner.GenerateAsync(
                $"small-{parallelism}", reverseInputs: true, parallelism: parallelism,
                marshallingMode: "Strict", marshallingDisabledAssemblyNames: ["WinRT.Runtime.dll"]);
            CollectionAssert.AreEqual(expected, File.ReadAllBytes(replay));
        }
    }

    [TestMethod]
    public async Task LargeAttributeCatalog_IsByteIdenticalAcrossParallelAndCachedWrites()
    {
        string source = """
            public static class Program
            {
                public static void Main() => WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<object>();
            }
            """ + string.Concat(Enumerable.Range(0, 1024).Select(static index => $$"""

            public sealed class Catalog{{index}} : System.IDisposable
            {
                public void Dispose() { }
            }
            """));

        using InteropGeneratorRunner runner = new(applicationSource: source);
        string output = await runner.GenerateAsync("large-serial", parallelism: 1);
        byte[] expected = File.ReadAllBytes(output);
        (_, ModuleDefinition module) = runner.LoadOutput(output);
        Assert.IsGreaterThan(1024, module.Assembly!.CustomAttributes.Count,
            "The catalog must exercise batched attribute preparation, not just the small-input path.");
        Assert.AreEqual("<Module>", module.TopLevelTypes[0].Name!.ToString());

        foreach ((bool reverse, int parallelism) in new[] { (true, 2), (false, 8), (true, -1), (false, -1) })
        {
            string replay = await runner.GenerateAsync($"large-{reverse}-{parallelism}", reverse, parallelism);
            CollectionAssert.AreEqual(expected, File.ReadAllBytes(replay));
        }

        string responsePath = Path.Combine(Path.GetDirectoryName(output)!, "interop.rsp");
        string response = File.ReadAllText(responsePath);
        File.WriteAllText(responsePath, response
            .Replace("--enable-incremental-generation False", "--enable-incremental-generation True", StringComparison.Ordinal)
            .Replace("--max-degrees-of-parallelism 1", "--max-degrees-of-parallelism -1", StringComparison.Ordinal));

        (int seedExit, string seedLog) = await InteropGeneratorRunner.InvokeGeneratorAsync(responsePath);
        Assert.AreEqual(0, seedExit, seedLog);
        StringAssert.Contains(seedLog, "Generating interop code");
        CollectionAssert.AreEqual(expected, File.ReadAllBytes(output));

        (int hitExit, string hitLog) = await InteropGeneratorRunner.InvokeGeneratorAsync(responsePath);
        Assert.AreEqual(0, hitExit, hitLog);
        StringAssert.Contains(hitLog, "Cached interop code ->");
        CollectionAssert.AreEqual(expected, File.ReadAllBytes(output));

        File.Delete(Path.Combine(Path.GetDirectoryName(output)!, "WinRT.Interop.cache"));
        (int rebuildExit, string rebuildLog) = await InteropGeneratorRunner.InvokeGeneratorAsync(responsePath);
        Assert.AreEqual(0, rebuildExit, rebuildLog);
        StringAssert.Contains(rebuildLog, "Generating interop code");
        CollectionAssert.AreEqual(expected, File.ReadAllBytes(output),
            "An uncached rebuild must not depend on a prior write's token or attribute-payload cache.");
    }
}
