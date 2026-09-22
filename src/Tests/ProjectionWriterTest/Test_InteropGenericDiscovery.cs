// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis;
using ProjectionWriterTest.Helpers;
using WindowsRuntime.ProjectionWriter;

namespace ProjectionWriterTest;

[TestClass]
public class Test_InteropGenericDiscovery
{
    private const string RecursionWarning = "CSWINRTINTEROPGEN0104";
    private const string ComplexityWarning = "CSWINRTINTEROPGEN0105";
    private static readonly Lazy<string> SdkFixture = new(CreateSdkFixture);

    [TestMethod]
    [DataRow("Node<T>", 1)]
    [DataRow("Node<string>", 2)]
    public void FiniteGenericRecursion_PreservesClosedContexts(string returnType, int expectedTypes)
    {
        AssertGeneration(CreateNodeSource(returnType), expectedTypes);
    }

    [TestMethod]
    public void ExpandingGenericReturns_AreBounded()
    {
        // The root and 32 member hops are expanded; the next discovered type is retained without expanding its members.
        AssertGeneration(CreateNodeSource("Node<Node<T>>"), 34, expectedWarning: RecursionWarning);
    }

    [TestMethod]
    public void ExpandingMultidimensionalArrayArguments_AreBounded()
    {
        AssertGeneration(CreateNodeSource("Node<T[,]>"), 34, expectedWarning: RecursionWarning);
    }

    [TestMethod]
    public void ExponentiallyExpandingArguments_AreBounded()
    {
        string source = CreateNodeSource("Node<Pair<T, T>>") + "\npublic sealed class Pair<TFirst, TSecond>;";

        AssertGeneration(source, 8, expectedWarning: ComplexityWarning);
    }

    [TestMethod]
    public void SignatureComplexityLimit_RespectsWarningsAsErrors()
    {
        string source = CreateNodeSource("Node<Pair<T, T>>") + "\npublic sealed class Pair<TFirst, TSecond>;";

        AssertGeneration(source, 0, expectedWarning: ComplexityWarning, treatWarningsAsErrors: true);
    }

    [TestMethod]
    public void ExplicitlyNestedClosedTypes_AreIndependentRoots()
    {
        string typeArgument = "int";

        for (int i = 1; i < 40; i++)
        {
            typeArgument = $"Node<{typeArgument}>";
        }

        AssertGeneration(CreateNodeSource("Node<T>", typeArgument), 40);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void RecursiveGenericMethodCalls_Terminate(bool indirect)
    {
        string source = $$"""
            using Windows.Foundation;

            namespace Recursion;

            public sealed class Node<T> : IStringable
            {
                public override string ToString() => "node";
            }

            public static class Program
            {
                public static void Main() => _ = Create<int>();

                private static IStringable Create<T>()
                {
                    _ = {{(indirect ? "Forward" : "Create")}}<Node<T>>();
                    return new Node<T>();
                }

                private static IStringable Forward<T>()
                {
                    _ = Create<Node<T>>();
                    return new Node<T>();
                }
            }
            """;

        AssertGeneration(source, 1);
    }

    [TestMethod]
    [DataRow(false, 2)]
    [DataRow(true, 34)]
    public void IndirectGenericRecursion_IsBounded(bool expanding, int expectedTypes)
    {
        string source = $$"""
            using Windows.Foundation;

            namespace Recursion;

            public sealed class Node<T> : IStringable
            {
                public Peer<T> Next() => new();
                public override string ToString() => "node";
            }

            public sealed class Peer<T> : IStringable
            {
                public Node<{{(expanding ? "Peer<T>" : "T")}}> Next() => new();
                public override string ToString() => "peer";
            }

            public static class Program
            {
                public static void Main() => _ = Create<int>();
                private static IStringable Create<T>() => new Node<T>();
            }
            """;

        AssertGeneration(source, expectedTypes, expectedWarning: expanding ? RecursionWarning : null);
    }

    [TestMethod]
    public void RecursionLimit_RespectsWarningsAsErrors()
    {
        AssertGeneration(CreateNodeSource("Node<Node<T>>"), 0, expectedWarning: RecursionWarning, treatWarningsAsErrors: true);
    }

    [ClassCleanup]
    public static void Cleanup()
    {
        if (SdkFixture.IsValueCreated)
        {
            Directory.Delete(SdkFixture.Value, recursive: true);
        }
    }

    private static string CreateNodeSource(string returnType, string typeArgument = "int")
    {
        return $$"""
            using Windows.Foundation;

            namespace Recursion;

            public sealed class Node<T> : IStringable
            {
                public {{returnType}} Next() => new();
                public override string ToString() => "node";
            }

            public static class Program
            {
                public static void Main() => _ = Create<{{typeArgument}}>();
                private static IStringable Create<T>() => new Node<T>();
            }
            """;
    }

    private static void AssertGeneration(
        string source,
        int expectedTypes,
        string? expectedWarning = null,
        bool treatWarningsAsErrors = false)
    {
        string directory = Directory.CreateTempSubdirectory("InteropGenericDiscoveryTest_").FullName;

        try
        {
            string projection = Path.Combine(SdkFixture.Value, "WinRT.Sdk.Projection.dll");
            string sdkReference = Path.Combine(SdkFixture.Value, "Microsoft.Windows.SDK.NET.dll");
            string app = ProjectionWriterRunner.CompileSources(
                [source],
                Path.Combine(directory, "RecursiveGenerics.dll"),
                outputKind: OutputKind.ConsoleApplication);
            string[] references = [projection, .. ProjectionWriterRunner.GetRuntimeReferencePaths()];
            string responseFile = Path.Combine(directory, "interop.rsp");
            File.WriteAllLines(responseFile,
            [
                $"--reference-assembly-paths {sdkReference},{string.Join(",", references)}",
                $"--implementation-assembly-paths {app},{string.Join(",", references)}",
                $"--output-assembly-path {app}",
                $"--winrt-sdk-projection-assembly-path {projection}",
                $"--generated-assembly-directory {directory}",
                "--use-windows-ui-xaml-projections false",
                "--validate-winrt-runtime-assembly-version true",
                "--validate-winrt-runtime-dll-version-2-references true",
                "--enable-incremental-generation false",
                $"--treat-warnings-as-errors {treatWarningsAsErrors}",
                "--max-degrees-of-parallelism 1"
            ]);

            (int exitCode, string log) = ProjectionWriterRunner.Run(
                ProjectionWriterRunner.GetRequiredFilePath("InteropGeneratorAssemblyPath"),
                $"@{responseFile}",
                timeout: TimeSpan.FromSeconds(30));
            string interop = Path.Combine(directory, "WinRT.Interop.dll");

            if (treatWarningsAsErrors)
            {
                Assert.AreNotEqual(0, exitCode, log);
                Assert.IsNotNull(expectedWarning);
                StringAssert.Contains(log, expectedWarning);
                Assert.IsFalse(File.Exists(interop), log);
                return;
            }

            Assert.AreEqual(0, exitCode, log);
            Assert.AreEqual(expectedWarning == RecursionWarning, log.Contains($"warning {RecursionWarning}", StringComparison.Ordinal), log);
            Assert.AreEqual(expectedWarning == ComplexityWarning, log.Contains($"warning {ComplexityWarning}", StringComparison.Ordinal), log);
            Assert.IsTrue(File.Exists(interop), log);
            HashSet<string> types = GetRecursiveTypeAssociations(interop);
            Assert.HasCount(expectedTypes, types, log);
            Assert.IsTrue(types.Any(type => type.StartsWith("Recursion.Node`1[[System.Int32,", StringComparison.Ordinal)));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static HashSet<string> GetRecursiveTypeAssociations(string path)
    {
        using FileStream stream = File.OpenRead(path);
        using PEReader pe = new(stream);
        MetadataReader reader = pe.GetMetadataReader();
        HashSet<string> types = new(StringComparer.Ordinal);

        foreach (CustomAttributeHandle handle in reader.GetAssemblyDefinition().GetCustomAttributes())
        {
            CustomAttribute attribute = reader.GetCustomAttribute(handle);

            if (attribute.Constructor.Kind != HandleKind.MemberReference)
            {
                continue;
            }

            MemberReference constructor = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor);

            if (constructor.Parent.Kind != HandleKind.TypeSpecification)
            {
                continue;
            }

            BlobReader signature = reader.GetBlobReader(reader.GetTypeSpecification((TypeSpecificationHandle)constructor.Parent).Signature);

            if (signature.ReadByte() != 0x15 || signature.ReadByte() != 0x12)
            {
                continue;
            }

            TypeReference attributeType = reader.GetTypeReference((TypeReferenceHandle)signature.ReadTypeHandle());

            if (reader.GetString(attributeType.Name) != "TypeMapAssociationAttribute`1")
            {
                continue;
            }

            Assert.AreEqual(1, signature.ReadCompressedInteger());
            Assert.AreEqual(0x12, signature.ReadByte());
            TypeReference group = reader.GetTypeReference((TypeReferenceHandle)signature.ReadTypeHandle());

            if (reader.GetString(group.Name) != "WindowsRuntimeComWrappersTypeMapGroup")
            {
                continue;
            }

            BlobReader value = reader.GetBlobReader(attribute.Value);
            Assert.AreEqual(1, value.ReadUInt16());
            string source = value.ReadSerializedString()!;

            if (source.StartsWith("Recursion.Node`1[", StringComparison.Ordinal) ||
                source.StartsWith("Recursion.Peer`1[", StringComparison.Ordinal))
            {
                Assert.IsTrue(types.Add(source), $"Duplicate CCW association for '{source}'.");
            }
        }

        return types;
    }

    private static string CreateSdkFixture()
    {
        string directory = Directory.CreateTempSubdirectory("InteropGenericDiscoverySdk_").FullName;

        try
        {
            string metadata = ArrayPropertyMetadata.Create(directory, exclusiveTo: true, includeArrayProperties: false);

            foreach (bool referenceProjection in new[] { false, true })
            {
                string output = Path.Combine(directory, referenceProjection ? "reference" : "implementation");
                ProjectionWriter.Run(new ProjectionWriterOptions
                {
                    InputPaths = [metadata],
                    OutputFolder = output,
                    Include = ["Contoso"],
                    ReferenceProjection = referenceProjection
                });
                _ = ProjectionWriterRunner.CompileSources(
                    Directory.GetFiles(output, "*.cs").Select(File.ReadAllText),
                    Path.Combine(directory, referenceProjection ? "Microsoft.Windows.SDK.NET.dll" : "WinRT.Sdk.Projection.dll"),
                    referenceProjection);
            }

            return directory;
        }
        catch
        {
            Directory.Delete(directory, recursive: true);
            throw;
        }
    }
}
