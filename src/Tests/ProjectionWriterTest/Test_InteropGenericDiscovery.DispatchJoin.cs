// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using ProjectionWriterTest.Helpers;

namespace ProjectionWriterTest;

public partial class Test_InteropGenericDiscovery
{
    [TestMethod]
    public void GenericDispatch_CompletedModuleJoinsDoNotConsumeLateBudget()
    {
        string directory = Directory.CreateTempSubdirectory("InteropCompletedJoins_").FullName;
        try
        {
            string contracts = ProjectionWriterRunner.CompileSources(
                ["""
                public interface IOuter { void Check<T>(); }
                public sealed class Outer<U> : IOuter { public void Check<T>() { } }
                """], Path.Combine(directory, "Contracts.dll"));
            string[] libraries = Enumerable.Range(0, 4).Select(module =>
            {
                string markers = string.Join('\n', Enumerable.Range(0, 300).Select(index => $"public struct Marker{index};"));
                string roots = string.Join('\n', Enumerable.Range(0, 300).Select(index => $"_ = new Outer<Marker{index}>();"));
                return ProjectionWriterRunner.CompileSources(
                    [$$"""
                    namespace Generated;
                    {{markers}}
                    public static class Entry
                    {
                        public static void Run()
                        {
                            {{roots}}
                            ((IOuter)new Outer<Marker0>()).Check<System.Guid>();
                        }
                    }
                    """], Path.Combine(directory, $"Library{module}.dll"), additionalReferences: [contracts]);
            }).ToArray();
            string app = ProjectionWriterRunner.CompileSources(
                ["""
                public sealed class Root : Windows.Foundation.IStringable { public override string ToString() => "root"; }
                public static class Program { public static void Main() => _ = new Root(); }
                """], Path.Combine(directory, "App.dll"), outputKind: OutputKind.ConsoleApplication);
            byte[]? first = null;
            foreach (bool reverse in new[] { false, true })
            {
                (int exitCode, string log) = RunGenerator(directory, app, treatWarningsAsErrors: true,
                    additionalReferences: [contracts, .. reverse ? libraries.Reverse() : libraries],
                    maxDegreesOfParallelism: reverse ? 4 : 1,
                    timeout: TimeSpan.FromMinutes(2));
                Assert.AreEqual(0, exitCode, log);
                Assert.IsFalse(log.Contains(ExpansionWarning, StringComparison.Ordinal), log);
                byte[] output = File.ReadAllBytes(Path.Combine(directory, "WinRT.Interop.dll"));
                if (first is not null)
                {
                    CollectionAssert.AreEqual(first, output, "Same-named receivers in different assemblies must retain stable join ordering.");
                }

                first = output;
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void GenericDispatch_ExcludedDerivedReceiverStillProjectsEligibleBaseArguments()
    {
        string directory = Directory.CreateTempSubdirectory("InteropReceiverProjection_").FullName;
        try
        {
            string contracts = ProjectionWriterRunner.CompileSources(
                ["""
                namespace Projection;
                public struct First;
                public struct Second;
                public sealed class Pair<U,T>;
                public sealed class Node<T> : Windows.Foundation.IStringable { public override string ToString() => "node"; }
                public interface IOuter { bool Check<T>(); }
                public class Impl<U> : IOuter { public bool Check<T>() { _ = new Node<Pair<U,T>>(); return true; } }
                """], Path.Combine(directory, "Contracts.dll"));
            string receivers = ProjectionWriterRunner.CompileSources(
                ["namespace Projection; public sealed class Derived<U> : Impl<U>;"],
                Path.Combine(directory, "Receivers.dll"), additionalReferences: [contracts]);
            string app = ProjectionWriterRunner.CompileSources(
                ["""
                namespace Projection;
                public static class Program
                {
                    public static void Main() => _ = ((IOuter)Create<First>()).Check<Second>();
                    private static object Create<T>() => new Derived<T>();
                }
                """], Path.Combine(directory, "App.dll"), outputKind: OutputKind.ConsoleApplication,
                additionalReferences: [contracts, receivers]);
            (int exitCode, string log) = RunGenerator(directory, app, treatWarningsAsErrors: true,
                additionalReferences: [contracts, receivers],
                additionalArguments: ["--marshalling-disabled-assembly-names Receivers"]);
            Assert.AreEqual(0, exitCode, log);
            Assert.IsTrue(GetComWrappersTypeAssociations(Path.Combine(directory, "WinRT.Interop.dll")).Any(type =>
                type.StartsWith("Projection.Node`1[[Projection.Pair`2[[Projection.First,", StringComparison.Ordinal)), log);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
