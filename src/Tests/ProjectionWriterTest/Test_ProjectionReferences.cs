// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis.CSharp;
using ProjectionWriterTest.Helpers;
using WindowsRuntime;
using WindowsRuntime.ProjectionWriter;

namespace ProjectionWriterTest;

[TestClass]
public class Test_ProjectionReferences
{
    [TestMethod]
    [DataRow("WinRT.Projection", false, false)]
    [DataRow("WinRT.Sdk.Projection", true, false)]
    [DataRow("WinRT.Sdk.Xaml.Projection", true, true)]
    [DataRow("WinRT.Sdk.Xaml.Projection", false, true)]
    public void UnrelatedReferences_DoNotChangeProjectionBytes(string assemblyName, bool windowsSdkOnly, bool xamlProjection)
    {
        WithDirectory(directory =>
        {
            bool baseSdkProjection = windowsSdkOnly && !xamlProjection;
            string metadata = baseSdkProjection
                ? ProjectionWriterRunner.GetRequiredFilePath("InternalMetadataPath")
                : CreateMetadata(directory);
            string? windowsMetadata = baseSdkProjection ? "sdk" : null;
            string? sdk = baseSdkProjection ? null : CompileSdkReference(directory, 1);
            List<string> references = [.. CreateFrameworkReferences(directory)];

            if (sdk is not null)
            {
                references.Add(sdk);
            }

            if (!windowsSdkOnly && !xamlProjection)
            {
                references.Add(CreateReferenceProjection(directory, metadata, sdk!));
            }

            byte[] baseline = RunGenerator(directory, metadata, assemblyName, references, xamlProjection, windowsSdkOnly, windowsMetadata);
            string manifestPath = Path.Combine(directory, "output", assemblyName + ".references");
            string[] manifest = File.ReadAllLines(manifestPath);
            File.SetLastWriteTimeUtc(manifestPath, DateTime.UnixEpoch);

            string unrelated = CompileUnrelatedReference(directory, 1);
            byte[] unrelatedBaseline = File.ReadAllBytes(unrelated);
            Guid? unrelatedMvid = ModuleDefinition.FromBytes(unrelatedBaseline).Mvid;
            _ = CompileUnrelatedReference(directory, 1);
            CollectionAssert.AreEqual(unrelatedBaseline, File.ReadAllBytes(unrelated));
            references.Add(unrelated);

            // A Microsoft signing key alone does not make an unrelated library a framework reference
            references.Add(typeof(CSharpCompilation).Assembly.Location);
            CollectionAssert.AreEqual(baseline, RunGenerator(directory, metadata, assemblyName, references, xamlProjection, windowsSdkOnly, windowsMetadata));

            _ = CompileUnrelatedReference(directory, 2);
            Assert.AreNotEqual(unrelatedMvid, ModuleDefinition.FromFile(unrelated).Mvid);
            references.Reverse();
            CollectionAssert.AreEqual(baseline, RunGenerator(directory, metadata, assemblyName, references, xamlProjection, windowsSdkOnly, windowsMetadata));
            CollectionAssert.AreEqual(manifest, File.ReadAllLines(manifestPath));
            Assert.AreEqual(DateTime.UnixEpoch, File.GetLastWriteTimeUtc(manifestPath));

            // A real projection dependency must remain an input to the embedded PDB
            if (sdk is not null)
            {
                _ = CompileSdkReference(directory, 2);
                Assert.IsFalse(baseline.SequenceEqual(RunGenerator(directory, metadata, assemblyName, references, xamlProjection, windowsSdkOnly, windowsMetadata)));
            }
        });
    }

    [TestMethod]
    public void RemovingLastProjection_DeletesAssemblyAndReferenceManifest()
    {
        WithDirectory(directory =>
        {
            string metadata = CreateMetadata(directory);
            string sdk = CompileSdkReference(directory, 1);
            string reference = CreateReferenceProjection(directory, metadata, sdk);
            string[] references = [.. CreateFrameworkReferences(directory), sdk];

            _ = RunGenerator(directory, metadata, "WinRT.Projection", [.. references, reference]);
            _ = RunGenerator(directory, metadata, "WinRT.Projection", references, expectProjection: false);
        });
    }

    [TestMethod]
    public void MergedProjection_KeepsReferencedComponentTypes()
    {
        WithDirectory(directory =>
        {
            string metadata = CreateMetadata(directory, "ComponentTypes");
            string component = ProjectionWriterRunner.CompileSources(
                ["""
                [assembly: WindowsRuntime.InteropServices.WindowsRuntimeComponentAssembly]
                namespace ComponentTypes;
                public enum ReferenceValue { Value = 1 }
                """],
                Path.Combine(directory, "ReferencedComponent.dll"));
            string reference = CreateReferenceProjection(directory, metadata, component);
            List<string> references = [.. CreateFrameworkReferences(directory), reference, component];

            byte[] baseline = RunGenerator(directory, metadata, "WinRT.Projection", references);
            references.Add(CompileUnrelatedReference(directory, 1));
            CollectionAssert.AreEqual(baseline, RunGenerator(directory, metadata, "WinRT.Projection", references));

            ModuleDefinition projection = ModuleDefinition.FromBytes(baseline);
            Assert.IsTrue(projection.AssemblyReferences.Any(reference => reference.Name == "ReferencedComponent"));
        });
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void ComponentProjection_KeepsActivationAssembliesAndManagedDependencies(bool xamlProjection)
    {
        WithDirectory(directory =>
        {
            string metadata = CreateMetadata(directory);
            string dependency = ProjectionWriterRunner.CompileSources(
                ["""
                public unsafe class ExportsBase
                {
                    public static void* GetActivationFactory(System.ReadOnlySpan<char> name) => null;
                }
                """],
                Path.Combine(directory, "ManagedDependency.dll"));
            string activation = ProjectionWriterRunner.CompileSources(
                ["""
                [assembly: WindowsRuntime.InteropServices.WindowsRuntimeComponentAssemblyExportsType(
                    typeof(ABI.ActivationOnly.ManagedExports))]
                namespace ABI.ActivationOnly;
                public class ManagedExports : ExportsBase;
                """],
                Path.Combine(directory, "ActivationOnly.dll"),
                additionalReferences: [dependency]);
            string component = ProjectionWriterRunner.CompileSources(
                ["""
                [assembly: WindowsRuntime.InteropServices.WindowsRuntimeComponentAssembly]
                public sealed class Component;
                """],
                Path.Combine(directory, "Component.dll"));
            List<string> references = [.. CreateFrameworkReferences(directory), dependency, activation, component];

            byte[] baseline = RunGenerator(directory, metadata, "WinRT.Component", references, xamlProjection);
            string unrelated = CompileUnrelatedReference(directory, 1);
            references.Add(unrelated);
            CollectionAssert.AreEqual(baseline, RunGenerator(directory, metadata, "WinRT.Component", references, xamlProjection));

            _ = CompileUnrelatedReference(directory, 2);
            references.Reverse();
            CollectionAssert.AreEqual(baseline, RunGenerator(directory, metadata, "WinRT.Component", references, xamlProjection));

            ModuleDefinition projection = ModuleDefinition.FromBytes(baseline);
            Assert.IsTrue(projection.AssemblyReferences.Any(reference => reference.Name == "ManagedDependency"));
            string[] manifest = File.ReadAllLines(Path.Combine(directory, "output", "WinRT.Component.references"));
            CollectionAssert.Contains(manifest, Path.GetFullPath(dependency));
            CollectionAssert.Contains(manifest, Path.GetFullPath(component));
            CollectionAssert.Contains(manifest, Path.GetFullPath(activation));

            // A component may only be named by an UnsafeAccessorType string, but must still be retained
            _ = ProjectionWriterRunner.CompileSources(
                ["""
                [assembly: WindowsRuntime.InteropServices.WindowsRuntimeComponentAssembly]
                public sealed class Component { public const int Version = 2; }
                """],
                component);
            Assert.IsFalse(baseline.SequenceEqual(RunGenerator(directory, metadata, "WinRT.Component", references, xamlProjection)));
        });
    }

    private static byte[] RunGenerator(
        string directory,
        string metadata,
        string assemblyName,
        IEnumerable<string> references,
        bool xamlProjection = false,
        bool windowsSdkOnly = false,
        string? windowsMetadata = null,
        bool expectProjection = true)
    {
        string output = Directory.CreateDirectory(Path.Combine(directory, "output")).FullName;
        string response = Path.Combine(directory, "projection.rsp");
        File.WriteAllLines(response,
        [
            $"--reference-assembly-paths {string.Join(",", references)}",
            $"--generated-assembly-directory {output}",
            $"--winmd-paths {metadata}",
            $"--windows-metadata {windowsMetadata ?? metadata}",
            $"--assembly-name {assemblyName}",
            $"--windows-sdk-only {windowsSdkOnly}",
            $"--windows-ui-xaml-projection {xamlProjection}",
            "--target-framework net10.0",
            "--max-degrees-of-parallelism 1"
        ]);
        (int exitCode, string log) = ProjectionWriterRunner.Run(
            ProjectionWriterRunner.GetRequiredFilePath("ProjectionGeneratorAssemblyPath"), $"@{response}");
        Assert.AreEqual(0, exitCode, log);

        string assemblyPath = Path.Combine(output, assemblyName + ".dll");
        string manifestPath = Path.Combine(output, assemblyName + ".references");

        if (!expectProjection)
        {
            Assert.IsFalse(File.Exists(assemblyPath));
            Assert.IsFalse(File.Exists(manifestPath));
            return [];
        }

        Assert.IsTrue(File.ReadAllLines(manifestPath).All(Path.IsPathFullyQualified));

        if (assemblyName == "WinRT.Sdk.Xaml.Projection")
        {
            StringAssert.Contains(log, "Processing 'Windows.UI.Xaml' .winmd references");
        }

        byte[] image = File.ReadAllBytes(assemblyPath);
        using MemoryStream stream = new(image);
        using PEReader reader = new(stream);
        Assert.IsTrue(reader.ReadDebugDirectory().Any(entry => entry.Type == DebugDirectoryEntryType.EmbeddedPortablePdb));
        return image;
    }

    private static string[] CreateFrameworkReferences(string directory)
    {
        string frameworkDirectory = Directory.CreateDirectory(Path.Combine(directory, "framework")).FullName;
        return
        [
            typeof(WindowsRuntimeObject).Assembly.Location,
            .. Net100.ReferenceInfos.All.Select(info =>
            {
                string path = Path.Combine(frameworkDirectory, info.FileName);
                File.WriteAllBytes(path, info.ImageBytes);
                return path;
            })
        ];
    }

    private static string CompileUnrelatedReference(string directory, int version)
    {
        return ProjectionWriterRunner.CompileSources(
            [$"public sealed class Unrelated {{ public const int Version = {version}; }}"],
            Path.Combine(directory, "Unrelated.dll"),
            referenceProjection: true);
    }

    private static string CompileSdkReference(string directory, int version)
    {
        return ProjectionWriterRunner.CompileSources(
            [$"namespace Windows.Foundation; public enum ReferenceValue {{ Value = {version} }}"],
            Path.Combine(directory, "WinRT.Sdk.Projection.dll"));
    }

    private static string CreateReferenceProjection(string directory, string metadata, params string[] additionalReferences)
    {
        string sources = Path.Combine(directory, "reference-sources");
        ProjectionWriter.Run(new ProjectionWriterOptions
        {
            InputPaths = [metadata],
            OutputFolder = sources,
            Include = ["Contoso"],
            ReferenceProjection = true
        });
        return ProjectionWriterRunner.CompileSources(
            Directory.GetFiles(sources, "*.cs").Select(File.ReadAllText),
            Path.Combine(directory, "Contoso.dll"),
            referenceProjection: true,
            additionalReferences: additionalReferences);
    }

    private static string CreateMetadata(string directory, string valueNamespace = "Windows.Foundation")
    {
        const string name = "Windows.Foundation.UniversalApiContract";
        ModuleDefinition module = new(name + ".winmd") { RuntimeVersion = "WindowsRuntime 1.4" };
        _ = new AssemblyDefinition(name, new Version(255, 255, 255, 255))
        {
            Modules = { module },
            Attributes = AssemblyAttributes.ContentWindowsRuntime
        };
        AssemblyReference corlib = (AssemblyReference)module.CorLibTypeFactory.CorLibScope;
        corlib.Version = new Version(255, 255, 255, 255);

        TypeDefinition value = new(valueNamespace, "ReferenceValue",
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.WindowsRuntime,
            new TypeReference(module, corlib, "System", "Enum"));
        value.Fields.Add(new FieldDefinition("value__",
            FieldAttributes.Public | FieldAttributes.SpecialName | FieldAttributes.RuntimeSpecialName,
            module.CorLibTypeFactory.Int32));
        value.Fields.Add(new FieldDefinition("Value", FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal,
            value.ToTypeSignature(true)) { Constant = Constant.FromValue(1) });
        module.TopLevelTypes.Add(value);

        foreach (string ns in new[] { "Windows.UI.Xaml", "Contoso" })
        {
            TypeDefinition type = new(ns, "DeterminismValue",
                TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout | TypeAttributes.WindowsRuntime,
                new TypeReference(module, corlib, "System", "ValueType"));
            type.Fields.Add(new FieldDefinition("Value", FieldAttributes.Public, value.ToTypeSignature(true)));
            module.TopLevelTypes.Add(type);
        }

        string path = Path.Combine(directory, name + ".winmd");
        module.Write(path);
        return path;
    }

    private static void WithDirectory(Action<string> action)
    {
        string directory = Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, $"ProjectionReferences_{Guid.NewGuid():N}")).FullName;
        try
        {
            action(directory);
        }
        finally
        {
            ProjectionWriterRunner.TryDeleteDirectory(directory);
        }
    }
}
