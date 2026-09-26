// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using AsmResolver.DotNet;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using ProjectionWriterTest.Helpers;
using WindowsRuntime;

namespace ProjectionWriterTest;

[TestClass]
public class Test_GenericMarshallerNames
{
    [TestMethod]
    [DataRow("windowsudk")]
    [DataRow("Contoso.Metadata.Renamed")]
    public void ReferenceProjection_ChangingFileStemPreservesSources(string fileStem)
    {
        using Fixture fixture = new();

        string GenerateReferenceSources(string name, string stem)
        {
            string directory = Directory.CreateDirectory(Path.Combine(fixture.DirectoryPath, name)).FullName;
            string input = GenericMarshallerMetadata.Create(directory, stem);
            string sources = Path.Combine(directory, "sources");
            GenerateProjection(input, sources, referenceProjection: true);
            return sources;
        }

        string original = GenerateReferenceSources("original", "WindowsUdk");
        string renamed = GenerateReferenceSources("renamed", fileStem);
        string[] originalFiles = Directory.GetFiles(original, "*.cs").Select(path => Path.GetFileName(path)!).Order(StringComparer.Ordinal).ToArray();
        string[] renamedFiles = Directory.GetFiles(renamed, "*.cs").Select(path => Path.GetFileName(path)!).Order(StringComparer.Ordinal).ToArray();
        Assert.IsNotEmpty(originalFiles);
        CollectionAssert.Contains(originalFiles, "Contoso.cs");
        CollectionAssert.AreEqual(originalFiles, renamedFiles);

        foreach (string file in originalFiles)
        {
            CollectionAssert.AreEqual(File.ReadAllBytes(Path.Combine(original, file)), File.ReadAllBytes(Path.Combine(renamed, file)), file);
        }
    }

    [TestMethod]
    [DataRow("windowsudk")]
    [DataRow("Contoso.Metadata.Renamed")]
    public void ThirdPartyAccessors_UseInputFileStemRatherThanAssemblyName(string fileStem)
    {
        using Fixture fixture = CreateFixture(fileStem);
        string directory = fixture.DirectoryPath;
        Assert.AreEqual("WindowsUdk", ModuleDefinition.FromFile(Path.Combine(directory, fileStem + ".winmd")).Assembly!.Name!.Value);
        ModuleDefinition projection = ModuleDefinition.FromFile(Path.Combine(directory, "WinRT.Projection.dll"));
        ModuleDefinition interop = ModuleDefinition.FromFile(Path.Combine(directory, "WinRT.Interop.dll"));
        UnsafeAccessorContract.Target[] targets = UnsafeAccessorContract.Verify(projection, interop);
        string marker = fileStem.Replace('.', '-');
        string tile = $"<{marker}>Contoso-TileCollection";

        AssertTarget(targets, $"ABI.Windows.Foundation.<#CsWinRT>IAsyncOperation'1<{tile}>Marshaller", "ConvertToManaged");
        AssertTarget(targets, $"ABI.WindowsRuntime.InteropServices.<#CsWinRT>EventHandlerEventSource'2<{tile}|<#corlib>System-Guid>", ".ctor");
        AssertTarget(targets, $"ABI.Contoso.<<{marker}>TileCollection>ArrayMarshaller", "ConvertToManaged");
        Assert.IsTrue(targets.Any(target => target.Type.Contains($"<{marker}>Contoso-ITaskRegistration", StringComparison.Ordinal)
            && target.Type.EndsWith("Methods", StringComparison.Ordinal)));
        Assert.IsFalse(targets.Any(target => target.Type.Contains("<WindowsUdk>", StringComparison.Ordinal)));

        TypeDefinition lookup = projection.TopLevelTypes.Single(type => type.FullName == "ABI.WindowsRuntimeMetadataTypes");
        CustomAttribute[] metadata = lookup.CustomAttributes.Where(
            attribute => attribute.Constructor?.DeclaringType?.FullName == "WindowsRuntime.WindowsRuntimeMetadataAttribute").ToArray();
        Assert.IsGreaterThanOrEqualTo(4, metadata.Length);
        foreach (CustomAttribute attribute in metadata)
        {
            Assert.AreEqual(fileStem, attribute.Signature!.FixedArguments[1].Element!.ToString());
        }
        Assert.IsTrue(projection.TopLevelTypes.Any(type => type.FullName == "Contoso.TileCollection"));
        Assert.IsTrue(projection.TopLevelTypes.Any(type => type.FullName == "Contoso.ITaskRegistration"));
        Assert.HasCount(2, projection.TopLevelTypes.Single(type => type.FullName == "Contoso.TileCollection")
            .Methods.Where(method => method.Name == "GetTilesAsync"));
    }

    [TestMethod]
    public void GuidArgumentsAndArrays_ResolveWithCorlibMarkersAtEachDepth()
    {
        // Matching filename/assembly casing isolates the 'Guid' regression from the stem regression.
        using Fixture fixture = CreateFixture("WindowsUdk");
        string directory = fixture.DirectoryPath;
        ModuleDefinition projection = ModuleDefinition.FromFile(Path.Combine(directory, "WinRT.Projection.dll"));
        ModuleDefinition interop = ModuleDefinition.FromFile(Path.Combine(directory, "WinRT.Interop.dll"));
        UnsafeAccessorContract.Target[] targets = UnsafeAccessorContract.Verify(projection, interop);
        const string arguments = "<#corlib>System-Guid|<WindowsUdk>Contoso-ITaskRegistration";
        const string nested = "<#corlib>System-Collections-Generic-IReadOnlyDictionary'2<" + arguments + ">";

        AssertTarget(targets, $"ABI.System.Collections.Generic.<#corlib>IReadOnlyDictionary'2<{arguments}>Marshaller", "ConvertToManaged");
        AssertTarget(targets, $"ABI.System.Collections.Generic.<#corlib>IReadOnlyDictionary'2<<#corlib>System-Guid|{nested}>Marshaller", "ConvertToManaged");
        AssertTarget(targets, $"ABI.System.Collections.Generic.<<#corlib>IReadOnlyDictionary'2<{arguments}>>ArrayMarshaller", "CopyToUnmanaged");
        AssertTarget(targets, $"ABI.System.<#corlib>EventHandler'1<{nested}>Marshaller", "ConvertToManaged");
        AssertTarget(targets, "ABI.System.<#corlib>EventHandler'1<<#corlib>System-Guid>Marshaller", "ConvertToManaged");
        AssertTarget(targets, "ABI.System.<<#corlib>EventHandler'1<<#corlib>System-Guid>>ArrayMarshaller", "CopyToUnmanaged");
        AssertTarget(targets, "ABI.System.<<#corlib>Guid>ArrayMarshaller", "ConvertToManaged");
        AssertTarget(targets, "ABI.WindowsRuntime.InteropServices.<#CsWinRT>EventHandlerEventSource'1<<#corlib>System-Guid>", ".ctor");
        AssertTarget(targets, "ABI.InterfaceIIDs", $"get_IID_<#corlib>IReadOnlyDictionary'2<{arguments}>");
    }

    [TestMethod]
    public void NativeBackedAccessors_InvokeAsyncMapsEventsAndArrays()
    {
        using Fixture fixture = CreateFixture("windowsudk");
        string directory = fixture.DirectoryPath;
        (int exitCode, string log) = ProjectionWriterRunner.Run(Path.Combine(directory, "GenericMarshallerTests.dll"), "run");
        Assert.AreEqual(0, exitCode, log);
        StringAssert.Contains(log, "Generic marshaller accessors passed.");
    }

    [TestMethod]
    [TestCategory("NativeAOT")]
#if !CSWINRT_GENERIC_MARSHALLERS_AOT
    [Ignore("Build with CSWINRT_TEST_GENERIC_MARSHALLERS_AOT=1 to publish and run the same consumer with Native AOT.")]
#endif
    public void NativeBackedAccessors_NativeAot()
    {
        using Fixture fixture = CreateFixture("windowsudk");
        string directory = fixture.DirectoryPath;
        string aotDirectory = Directory.CreateDirectory(Path.Combine(directory, "aot")).FullName;
        File.WriteAllText(Path.Combine(aotDirectory, "Directory.Build.props"), "<Project />");
        File.WriteAllText(Path.Combine(aotDirectory, "Directory.Build.targets"), "<Project />");
        File.WriteAllText(Path.Combine(aotDirectory, "GenericMarshallerTests.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0</TargetFramework>
                <LangVersion>14.0</LangVersion>
                <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
                <PublishAot>true</PublishAot>
                <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
                <GenerateTargetFrameworkAttribute>false</GenerateTargetFrameworkAttribute>
              </PropertyGroup>
              <ItemGroup>
                <Compile Include="..\GenericMarshallerTests.cs" />
                <Reference Include="WinRT.Runtime" HintPath="..\WinRT.Runtime.dll" />
                <Reference Include="WinRT.Projection" HintPath="..\WinRT.Projection.dll" />
                <Reference Include="WinRT.Sdk.Projection" HintPath="..\WinRT.Sdk.Projection.dll" />
                <Reference Include="WinRT.Interop" HintPath="..\WinRT.Interop.dll" />
                <TrimmerRootAssembly Include="WinRT.Interop" />
              </ItemGroup>
            </Project>
            """);
        string rid = $"win-{RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant()}";
        string binlog = Path.GetFullPath(Path.Combine("TestResults", "generic-marshallers-aot-{}.binlog"));
        (int publishExitCode, string publishLog) = Run("dotnet",
            ["publish", Path.Combine(aotDirectory, "GenericMarshallerTests.csproj"), "-c", "Release", "-r", rid,
                "-o", Path.Combine(aotDirectory, "publish"), $"-bl:{binlog}"]);
        Assert.AreEqual(0, publishExitCode, publishLog);
        (int exitCode, string log) = Run(Path.Combine(aotDirectory, "publish", "GenericMarshallerTests.exe"), []);
        Assert.AreEqual(0, exitCode, log);
        StringAssert.Contains(log, "Generic marshaller accessors passed.");
    }

    private static void AssertTarget(UnsafeAccessorContract.Target[] targets, string type, string method)
    {
        Assert.IsTrue(targets.Contains(new UnsafeAccessorContract.Target(type, method)), $"Missing coverage for '{type}.{method}'.");
    }

    private static Fixture CreateFixture(string fileStem)
    {
        Fixture fixture = new();
        string directory = fixture.DirectoryPath;
        try
        {
            string input = GenericMarshallerMetadata.Create(directory, fileStem);
            string projection = CompileProjection(input, Path.Combine(directory, "projection"), Path.Combine(directory, "WinRT.Projection.dll"));
            string referenceDirectory = Directory.CreateDirectory(Path.Combine(directory, "reference")).FullName;

            // Reuse the small SDK fixture: 'cswinrtinteropgen' requires an SDK reference and a nontrivial class hierarchy.
            string sdkDirectory = Directory.CreateDirectory(Path.Combine(directory, "sdk")).FullName;
            string sdkInput = ArrayPropertyMetadata.Create(sdkDirectory, exclusiveTo: true, includeArrayProperties: false);
            string sdkProjection = CompileProjection(sdkInput, Path.Combine(directory, "sdk-sources"),
                Path.Combine(directory, "WinRT.Sdk.Projection.dll"));
            string sdkReference = CompileProjection(sdkInput, Path.Combine(directory, "sdk-reference-sources"),
                Path.Combine(referenceDirectory, "Microsoft.Windows.SDK.NET.dll"), referenceProjection: true);

            using Stream resource = typeof(Test_GenericMarshallerNames).Assembly.GetManifestResourceStream(
                "ProjectionWriterTest.Resources.GenericMarshallerTests.cs")!;
            using StreamReader reader = new(resource);
            string consumer = reader.ReadToEnd();
            File.WriteAllText(Path.Combine(directory, "GenericMarshallerTests.cs"), consumer);
            string app = ProjectionWriterRunner.CompileSources([consumer], Path.Combine(directory, "GenericMarshallerTests.dll"),
                outputKind: OutputKind.ConsoleApplication, additionalReferences: [projection]);

            // Use the same BCL reference images as 'CompileSources', not implementation assemblies with forwarded corelib scopes.
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
            File.WriteAllText(Path.Combine(directory, "GenericMarshallerTests.runtimeconfig.json"), """
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

    private sealed class Fixture : IDisposable
    {
        public string DirectoryPath { get; } = Directory.CreateDirectory(
            Path.Combine("TestResults", $"GenericMarshallerNames_{Guid.NewGuid():N}")).FullName;

        public void Dispose() => Directory.Delete(DirectoryPath, recursive: true);
    }

    private static string CompileProjection(string input, string output, string assembly, bool referenceProjection = false)
    {
        GenerateProjection(input, output, referenceProjection);
        return ProjectionWriterRunner.CompileSources(Directory.GetFiles(output, "*.cs").Select(File.ReadAllText),
            assembly, referenceProjection);
    }

    private static void GenerateProjection(string input, string output, bool referenceProjection)
    {
        Directory.CreateDirectory(output);
        string response = Path.Combine(output, "projection.rsp");
        File.WriteAllLines(response,
        [
            $"--input-paths {input}",
            $"--output-directory {output}",
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

    private static (int ExitCode, string Output) Run(string command, string[] arguments)
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
