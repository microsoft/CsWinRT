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
    private const string ExpansionWarning = "CSWINRTINTEROPGEN0106";
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
        // The root and 32 member hops are expanded. The next discovered type is retained without expanding its members.
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

    [TestMethod]
    public void BranchingGenericReturns_WarnAndPreserveExplicitRoots()
    {
        AssertBranchingGeneration(treatWarningsAsErrors: false);
    }

    [TestMethod]
    public void BranchingGenericReturns_RespectWarningsAsErrors()
    {
        AssertBranchingGeneration(treatWarningsAsErrors: true);
    }

    [TestMethod]
    public void ManyUnscannedFrameworkGenerics_DoNotExhaustTransitiveBudget()
    {
        const int rootCount = 360;
        string markers = string.Join('\n', Enumerable.Range(0, rootCount).Select(i => $"public sealed class Marker{i};"));
        string roots = string.Join('\n', Enumerable.Range(0, rootCount).Select(i => $"_ = new Wrapper<Marker{i}>();"));
        string source = $$"""
            using System;
            using Windows.Foundation;

            namespace Recursion;

            public sealed class Wrapper<T>
            {
                public Func<T> Create() => throw new NotImplementedException();
                public Action<T> Notify() => throw new NotImplementedException();
                public Predicate<T> Filter() => throw new NotImplementedException();
            }

            public sealed class Root<T> : IStringable
            {
                public override string ToString() => "root";
            }

            public sealed class Factory<T>
            {
                public Descriptor<T> Create() => new();
            }

            public sealed class Descriptor<T> : IStringable
            {
                public Final<T> Next() => new();
                public override string ToString() => "descriptor";
            }

            public sealed class Final<T> : IStringable
            {
                public override string ToString() => "final";
            }

            {{markers}}

            public static class Program
            {
                public static void Main()
                {
                    _ = new Root<long>();
                    {{roots}}
                    _ = new Factory<long>();
                }
            }
            """;
        string directory = Directory.CreateTempSubdirectory("InteropFiniteGenericDiscoveryTest_").FullName;

        try
        {
            string app = ProjectionWriterRunner.CompileSources(
                [source],
                Path.Combine(directory, "FiniteGenerics.dll"),
                outputKind: OutputKind.ConsoleApplication);
            (int exitCode, string log) = RunGenerator(directory, app, treatWarningsAsErrors: true);

            Assert.AreEqual(0, exitCode, log);
            Assert.IsFalse(log.Contains(ExpansionWarning, StringComparison.Ordinal), log);
            HashSet<string> types = GetComWrappersTypeAssociations(Path.Combine(directory, "WinRT.Interop.dll"));
            Assert.IsTrue(types.Any(type => type.StartsWith("Recursion.Root`1[[System.Int64,", StringComparison.Ordinal)), log);
            Assert.IsTrue(types.Any(type => type.StartsWith("Recursion.Final`1[[System.Int64,", StringComparison.Ordinal)), log);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    [DataRow("strict", false, false, true)]
    [DataRow("strict", true, false, false)]
    [DataRow("strict", false, true, false)]
    [DataRow("minimal", false, false, false)]
    [DataRow("all", false, false, false)]
    public void TransitiveDiscovery_RespectsModulePolicy(string mode, bool directReference, bool optIn, bool succeeds)
    {
        AssertTransitiveModulePolicy(mode, directReference, optIn, optOut: false, referencesWinRT: false, succeeds: succeeds);
    }

    [TestMethod]
    [DataRow("minimal", false, false, false, true)]
    [DataRow("all", false, false, false, true)]
    [DataRow("strict", false, true, false, true)]
    [DataRow("strict", false, false, true, true)]
    [DataRow("minimal", true, false, false, false)]
    public void TransitiveDiscovery_ExplicitExclusionSkipsIndirectMembers(
        string mode, bool directReference, bool optIn, bool referencesWinRT, bool succeeds)
    {
        AssertTransitiveModulePolicy(mode, directReference, optIn, optOut: true, referencesWinRT: referencesWinRT, succeeds: succeeds);
    }

    [TestMethod]
    public void TransitiveDiscovery_WinRTReferencedModuleIsScannedWithoutExclusion()
    {
        AssertTransitiveModulePolicy(
            "strict", directReference: false, optIn: false, optOut: false, referencesWinRT: true, succeeds: false);
    }

    [TestMethod]
    public void MixedAssembly_UnresolvedPrivateImplementationDependencyDoesNotBlockRuntimeTypes()
    {
        string directory = Directory.CreateTempSubdirectory("InteropMixedAssemblyTest_").FullName;

        try
        {
            string dependency = ProjectionWriterRunner.CompileSources(
                ["""
                using System.Collections.Generic;

                namespace Compiler;

                public sealed class Symbol;

                public sealed class SymbolEqualityComparer : IEqualityComparer<Symbol>
                {
                    public static readonly SymbolEqualityComparer Default = new();
                    public bool Equals(Symbol? x, Symbol? y) => ReferenceEquals(x, y);
                    public int GetHashCode(Symbol obj) => obj.GetHashCode();
                }

                public static class Factory
                {
                    public static T Create<T>() => default!;
                }
                """],
                Path.Combine(directory, "Compiler.dll"));
            string library = ProjectionWriterRunner.CompileSources(
                ["""
                using System.Collections.Generic;
                using Compiler;
                using Windows.Foundation;

                namespace Mixed;

                public sealed class Generator
                {
                    private readonly HashSet<Symbol> symbols = new(SymbolEqualityComparer.Default);

                    public object CreateFromGenericMethod() => Factory.Create<int>();
                }

                public sealed class ViewModel : IStringable
                {
                    public override string ToString() => "view model";
                }
                """],
                Path.Combine(directory, "Mixed.dll"),
                additionalReferences: [dependency]);
            string app = ProjectionWriterRunner.CompileSources(
                ["""
                public static class Program
                {
                    public static void Main() => _ = new Mixed.ViewModel();
                }
                """],
                Path.Combine(directory, "MixedApp.dll"),
                outputKind: OutputKind.ConsoleApplication,
                additionalReferences: [library, dependency]);

            File.Delete(dependency);

            (int exitCode, string log) = RunGenerator(directory, app, additionalReferences: [library]);

            Assert.AreEqual(0, exitCode, log);
            StringAssert.Contains(log, "CSWINRTINTEROPGEN0065");
            Assert.IsTrue(GetComWrappersTypeAssociations(Path.Combine(directory, "WinRT.Interop.dll"))
                .Any(type => type.StartsWith("Mixed.ViewModel,", StringComparison.Ordinal)), log);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void UnknownExcludedAssembly_RespectsWarningsAsErrors(bool treatWarningsAsErrors)
    {
        string directory = Directory.CreateTempSubdirectory("InteropUnknownExclusionTest_").FullName;

        try
        {
            string app = ProjectionWriterRunner.CompileSources(
                [CreateNodeSource("Node<T>")],
                Path.Combine(directory, "UnknownExclusion.dll"),
                outputKind: OutputKind.ConsoleApplication);
            (int exitCode, string log) = RunGenerator(
                directory,
                app,
                treatWarningsAsErrors: treatWarningsAsErrors,
                additionalArguments: ["--marshalling-disabled-assembly-names MissingAssembly.dll"]);

            Assert.AreEqual(treatWarningsAsErrors, exitCode != 0, log);
            StringAssert.Contains(log, "CSWINRTINTEROPGEN0107");
            StringAssert.Contains(log, "MissingAssembly.dll");
            Assert.AreEqual(!treatWarningsAsErrors, File.Exists(Path.Combine(directory, "WinRT.Interop.dll")), log);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static void AssertTransitiveModulePolicy(
        string mode, bool directReference, bool optIn, bool optOut, bool referencesWinRT, bool succeeds)
    {
        string directory = Directory.CreateTempSubdirectory("InteropModulePolicyTest_").FullName;

        try
        {
            string dependency = ProjectionWriterRunner.CompileSources(
                ["namespace Missing; public sealed class Dependency;"],
                Path.Combine(directory, "Missing.dll"));
            string foreignSource = """
                namespace Foreign;

                public sealed class Outer<T>
                {
                    public object Create() => new Inner<T>();
                }

                public sealed class Inner<T>
                {
                    public static readonly object Cached = Cache<T>.Value;
                    public object Create() => new Missing.Dependency();
                }

                public static class Cache<T>
                {
                    public static readonly object Value = new T[1];
                }
                """ + (referencesWinRT ? """

                public sealed class WinRTCallback : Windows.Foundation.IStringable
                {
                    public override string ToString() => "callback";
                }
                """ : "");
            string library = ProjectionWriterRunner.CompileSources(
                [foreignSource],
                Path.Combine(directory, "Foreign.dll"),
                additionalReferences: [dependency]);
            string app = ProjectionWriterRunner.CompileSources(
                [$$"""
                using Windows.Foundation;

                namespace Recursion;

                public struct Marker;

                public sealed class Node<T> : IStringable
                {
                    public override string ToString() => "node";
                }

                public static class Program
                {
                    public static void Main()
                    {
                        _ = new Node<int>();
                        _ = new Foreign.{{(directReference ? "Inner" : "Outer")}}<Marker>();
                    }
                }
                """],
                Path.Combine(directory, "ModulePolicy.dll"),
                outputKind: OutputKind.ConsoleApplication,
                additionalReferences: [library]);

            // The excluded library's ordinary methods require a runtime-only dependency that is not a generator input.
            File.Delete(dependency);

            (int exitCode, string log) = RunGenerator(
                directory,
                app,
                additionalReferences: [library],
                additionalArguments: [
                    $"--marshalling-mode {mode}",
                    .. optIn ? new[] { "--marshalling-enabled-assembly-names subdir\\FOREIGN.DLL" } : [],
                    .. optOut ? new[] { "--marshalling-disabled-assembly-names Foreign.dll,WinRT.Sdk.Projection.dll" } : []
                ],
                // 'all' also scans every BCL assembly, which can exceed the default limit on loaded CI agents.
                timeout: mode == "all" ? TimeSpan.FromMinutes(2) : null);

            if (optIn && optOut)
            {
                StringAssert.Contains(log, "CSWINRTINTEROPGEN0108");
            }

            if (!succeeds)
            {
                Assert.AreNotEqual(0, exitCode, log);
                StringAssert.Contains(log, "CSWINRTINTEROPGEN0015");
                StringAssert.Contains(log, "Missing.Dependency");
                return;
            }

            Assert.AreEqual(0, exitCode, log);
            HashSet<string> types = GetComWrappersTypeAssociations(Path.Combine(directory, "WinRT.Interop.dll"));
            Assert.IsTrue(types.Any(type => type.StartsWith("Recursion.Node`1[[System.Int32,", StringComparison.Ordinal)), log);

            // The one-hop scan of 'Outer<Marker>' reaches 'Inner<Marker>'. Its cache initializers expose 'Marker[]'.
            Assert.AreEqual(!optOut, types.Any(type => type.StartsWith("Recursion.Marker[],", StringComparison.Ordinal)), log);
            Assert.IsFalse(types.Any(type => type.StartsWith("Foreign.WinRTCallback,", StringComparison.Ordinal)), log);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
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

    private static void AssertBranchingGeneration(bool treatWarningsAsErrors)
    {
        string directory = Directory.CreateTempSubdirectory("InteropBranchingDiscoveryTest_").FullName;

        try
        {
            string app = ProjectionWriterRunner.CompileSources(
                ["""
                using Windows.Foundation;

                namespace Recursion;

                public sealed class A<T>;
                public sealed class B<T>;

                public sealed class Node<T>
                {
                    public Node<A<T>> NextA() => new();
                    public Node<B<T>> NextB() => new();
                }

                public sealed class Root<T> : IStringable
                {
                    public override string ToString() => "root";
                }

                public static class Program
                {
                    public static void Main()
                    {
                        _ = new Node<int>();
                        _ = new Root<long>();
                    }
                }
                """],
                Path.Combine(directory, "BranchingGenerics.dll"),
                outputKind: OutputKind.ConsoleApplication);
            (int exitCode, string log) = RunGenerator(directory, app, treatWarningsAsErrors);
            string interop = Path.Combine(directory, "WinRT.Interop.dll");

            StringAssert.Contains(log, ExpansionWarning);
            StringAssert.Contains(log, "1024");
            Assert.IsFalse(log.Contains(RecursionWarning, StringComparison.Ordinal), log);
            Assert.IsFalse(log.Contains(ComplexityWarning, StringComparison.Ordinal), log);

            if (treatWarningsAsErrors)
            {
                Assert.AreNotEqual(0, exitCode, log);
                Assert.IsFalse(File.Exists(interop), log);
            }
            else
            {
                Assert.AreEqual(0, exitCode, log);
                HashSet<string> types = GetComWrappersTypeAssociations(interop);
                Assert.IsTrue(types.Any(type => type.StartsWith("Recursion.Root`1[[System.Int64,", StringComparison.Ordinal)), log);
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
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
            string app = ProjectionWriterRunner.CompileSources(
                [source],
                Path.Combine(directory, "RecursiveGenerics.dll"),
                outputKind: OutputKind.ConsoleApplication);
            (int exitCode, string log) = RunGenerator(directory, app, treatWarningsAsErrors);
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
            string[] types = [.. GetComWrappersTypeAssociations(interop).Where(type =>
                type.StartsWith("Recursion.Node`1[", StringComparison.Ordinal) ||
                type.StartsWith("Recursion.Peer`1[", StringComparison.Ordinal))];
            Assert.HasCount(expectedTypes, types, log);
            Assert.IsTrue(types.Any(type => type.StartsWith("Recursion.Node`1[[System.Int32,", StringComparison.Ordinal)));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static (int ExitCode, string Log) RunGenerator(
        string directory,
        string app,
        bool treatWarningsAsErrors = false,
        string[]? additionalReferences = null,
        string[]? additionalArguments = null,
        TimeSpan? timeout = null)
    {
        string projection = Path.Combine(SdkFixture.Value, "WinRT.Sdk.Projection.dll");
        string sdkReference = Path.Combine(SdkFixture.Value, "Microsoft.Windows.SDK.NET.dll");
        string[] references = [projection, .. ProjectionWriterRunner.GetRuntimeReferencePaths(), .. additionalReferences ?? []];
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
            "--max-degrees-of-parallelism 1",
            .. additionalArguments ?? []
        ]);

        return ProjectionWriterRunner.Run(
            ProjectionWriterRunner.GetRequiredFilePath("InteropGeneratorAssemblyPath"),
            $"@{responseFile}",
            timeout: timeout ?? TimeSpan.FromSeconds(30));
    }

    private static HashSet<string> GetComWrappersTypeAssociations(string path)
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

            Assert.IsTrue(types.Add(source), $"Duplicate CCW association for '{source}'.");
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
