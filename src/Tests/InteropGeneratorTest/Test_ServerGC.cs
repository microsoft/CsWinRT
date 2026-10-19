// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Xml.Linq;
using InteropGeneratorTest.Helpers;
using Microsoft.NET.Build.Tasks;
using Windows.Win32;

[assembly: SupportedOSPlatform("windows6.0.6000")]

namespace InteropGeneratorTest;

[TestClass]
public sealed class Test_ServerGC
{
    [TestMethod]
    public void AutomaticPolicy_ReadsInstalledSystemMemory()
    {
        bool memoryAvailable = PInvoke.GetPhysicallyInstalledSystemMemory(out ulong memoryInKilobytes);
        bool actual = InteropGeneratorGCPolicy.ShouldUseServerGC();
        bool expected = memoryAvailable && Environment.ProcessorCount >= 4 && checked((long)memoryInKilobytes * 1024) >= 16L * 1024 * 1024 * 1024;

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    [DataRow("true", "0", "1")]
    [DataRow("false", "1", "0")]
    [DataRow("TRUE", "", "1")]
    [DataRow("", "1", "1")]
    [DataRow("", "0", "0")]
    [DataRow("", "", "")]
    public async Task LaunchPolicy_PreservesPrecedenceAndParentEnvironment(string property, string inherited, string expected)
    {
        string? parentValue = Environment.GetEnvironmentVariable("DOTNET_gcServer");
        using InteropGeneratorRunner runner = new();
        string project = runner.CreateMSBuildProject();
        _ = await runner.RunMSBuildAsync(project, null, enableServerGC: property, gcServerEnvironment: inherited, maxDegreesOfParallelism: 1);
        string log = File.ReadAllText(Path.Combine(Path.GetDirectoryName(project)!, "build-1.log"));

        if (expected.Length == 0)
        {
            expected = GetExpectedAutomaticSetting();
        }

        StringAssert.Contains(log, $"Interop generator DOTNET_gcServer={expected}.");
        StringAssert.Contains(log, $"MSBuild parent DOTNET_gcServer={inherited}");
        Assert.AreEqual(parentValue, Environment.GetEnvironmentVariable("DOTNET_gcServer"));
    }

    [TestMethod]
    public async Task GCSelection_DoesNotInvalidateGeneratedOutput()
    {
        using InteropGeneratorRunner runner = new();
        string project = runner.CreateMSBuildProject();
        string output = await runner.RunMSBuildAsync(project, null, enableServerGC: "false");
        byte[] bytes = File.ReadAllBytes(output);
        DateTime timestamp = File.GetLastWriteTimeUtc(output);

        _ = await runner.RunMSBuildAsync(project, null, enableServerGC: "true");
        Assert.AreEqual(timestamp, File.GetLastWriteTimeUtc(output));
        CollectionAssert.AreEqual(bytes, File.ReadAllBytes(output));
    }

    [TestMethod]
    public async Task AutomaticPolicy_DoesNotDependOnWorkloadOrGeneratorParallelism()
    {
        using InteropGeneratorRunner runner = new();
        string project = runner.CreateMSBuildProject();
        _ = await runner.RunMSBuildAsync(project, null, gcServerEnvironment: "", maxDegreesOfParallelism: 1);
        string log = File.ReadAllText(Path.Combine(Path.GetDirectoryName(project)!, "build-1.log"));
        string expected = GetExpectedAutomaticSetting();
        StringAssert.Contains(log, $"Interop generator DOTNET_gcServer={expected}.");
    }

    [TestMethod]
    [DataRow("", "1")]
    [DataRow("false", "0")]
    public async Task TaskEnvironmentOverrides_AreAppliedBeforeSelection(string property, string expected)
    {
        using InteropGeneratorRunner runner = new();
        string projectPath = runner.CreateMSBuildProject();
        XDocument project = XDocument.Load(projectPath);
        XElement root = project.Root!;
        XNamespace ns = root.Name.Namespace;
        string outputDirectory = Path.Combine(Path.GetDirectoryName(projectPath)!, "output");

        root.Add(new XElement(ns + "Target", new XAttribute("Name", "_RunCsWinRTInteropGenerator"),
            new XElement(ns + "RunCsWinRTInteropGenerator",
                new XAttribute("ReferenceAssemblyPaths", "@(ReferencePathWithRefAssemblies)"),
                new XAttribute("ImplementationAssemblyPaths", "@(ReferencePath)"),
                new XAttribute("OutputAssemblyPath", "@(IntermediateAssembly)"),
                new XAttribute("WinRTSdkProjectionAssemblyPath", "$(_CsWinRTSdkProjectionAssemblyPath)"),
                new XAttribute("InteropAssemblyDirectory", outputDirectory),
                new XAttribute("CsWinRTToolsDirectory", "$(CsWinRTInteropGenEffectiveToolsDirectory)"),
                new XAttribute("CsWinRTToolsArchitecture", "AnyCPU"),
                new XAttribute("GenerateCollectionChangedListVtables", "false"),
                new XAttribute("EnableServerGC", "$(CsWinRTEnableInteropGeneratorServerGC)"),
                new XAttribute("EnvironmentVariables", "DOTNET_gcServer=1"))));
        project.Save(projectPath);

        _ = await runner.RunMSBuildAsync(projectPath, null, enableServerGC: property, gcServerEnvironment: "0");
        string log = File.ReadAllText(Path.Combine(Path.GetDirectoryName(projectPath)!, "build-1.log"));
        StringAssert.Contains(log, $"Interop generator DOTNET_gcServer={expected}.");
        StringAssert.Contains(log, "MSBuild parent DOTNET_gcServer=0");
    }

    [TestMethod]
    [DataRow("auto")]
    [DataRow("1")]
    [DataRow("invalid")]
    public async Task InvalidProperty_FailsEvenWhenOutputIsUpToDate(string property)
    {
        using InteropGeneratorRunner runner = new();
        string project = runner.CreateMSBuildProject();
        _ = await runner.RunMSBuildAsync(project, null);
        string log = await runner.RunMSBuildAsync(project, null, enableServerGC: property, expectFailure: true);

        StringAssert.Contains(log, "Invalid 'CsWinRTEnableInteropGeneratorServerGC' value");
    }

    private static string GetExpectedAutomaticSetting()
    {
        return PInvoke.GetPhysicallyInstalledSystemMemory(out ulong memoryInKilobytes) &&
            Environment.ProcessorCount >= 4 &&
            checked((long)memoryInKilobytes * 1024) >= 16L * 1024 * 1024 * 1024 ? "1" : "0";
    }
}
