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
    [DataRow(false)]
    [DataRow(true)]
    public void GenericDispatch_JoinsLateReceiverContextsAcrossModules(bool fixpoint)
    {
        string directory = Directory.CreateTempSubdirectory("InteropDispatchModules_").FullName;

        try
        {
            string contracts = ProjectionWriterRunner.CompileSources(
                ["""
                using Windows.Foundation;
                namespace Across;
                public struct First;
                public struct Second;
                public sealed class Pair<U, T>;
                public sealed class Node<T> : IStringable
                {
                    public override string ToString() => "node";
                }
                public interface IOuter { bool Check<T>(); }
                """], Path.Combine(directory, "Contracts.dll"));
            string body = fixpoint
                ? """
                  _ = new Next<U>();
                  return ((IOuter)new Next<U>()).Check<T>();
                  """
                : """
                  _ = new Node<Pair<U, T>>();
                  return true;
                  """;
            string implementation = ProjectionWriterRunner.CompileSources(
                [$$"""
                namespace Across;
                public sealed class Outer<U> : IOuter
                {
                    public bool Check<T>()
                    {
                        {{body}}
                    }
                }
                public sealed class Next<U> : IOuter
                {
                    public bool Check<T>()
                    {
                        _ = new Node<Pair<U, T>>();
                        return true;
                    }
                }
                """], Path.Combine(directory, "Implementation.dll"), additionalReferences: [contracts]);
            string producer = ProjectionWriterRunner.CompileSources(
                ["""
                namespace Across;
                public static class Producer
                {
                    public static IOuter Create() => new Outer<First>();
                }
                """], Path.Combine(directory, "Producer.dll"), additionalReferences: [contracts, implementation]);
            string caller = ProjectionWriterRunner.CompileSources(
                ["""
                namespace Across;
                public static class Caller
                {
                    public static bool Invoke<T>(IOuter value) => value.Check<T>();
                }
                """], Path.Combine(directory, "Caller.dll"), additionalReferences: [contracts]);
            string app = ProjectionWriterRunner.CompileSources(
                ["""
                public static class Program
                {
                    public static void Main() => _ = Across.Caller.Invoke<Across.Second>(Across.Producer.Create());
                }
                """], Path.Combine(directory, "App.dll"), outputKind: OutputKind.ConsoleApplication,
                additionalReferences: [contracts, implementation, producer, caller]);
            string[] inputs = [contracts, implementation, producer, caller];
            byte[]? firstOutput = null;

            foreach (bool reverse in new[] { false, true })
            {
                (int exitCode, string log) = RunGenerator(directory, app, treatWarningsAsErrors: true,
                    additionalReferences: reverse ? [.. inputs.Reverse()] : inputs,
                    maxDegreesOfParallelism: reverse ? 4 : 1);

                Assert.AreEqual(0, exitCode, log);

                string interop = Path.Combine(directory, "WinRT.Interop.dll");

                Assert.IsTrue(GetComWrappersTypeAssociations(interop).Any(type =>
                    type.StartsWith("Across.Node`1[[Across.Pair`2[[Across.First,", StringComparison.Ordinal)), log);

                byte[] output = File.ReadAllBytes(interop);

                if (firstOutput is not null)
                {
                    CollectionAssert.AreEqual(firstOutput, output, "Cross-module joins must not depend on input order or parallelism.");
                }

                firstOutput = output;
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void GenericDispatch_CompletedModuleJoinsDoNotConsumeLateBudget()
    {
        string directory = Directory.CreateTempSubdirectory("InteropCompletedJoins_").FullName;

        try
        {
            string contracts = ProjectionWriterRunner.CompileSources(
                ["""
                public interface IOuter { void Check<T>(); }
                public sealed class Outer<U> : IOuter
                {
                    public void Check<T>() { }
                }
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
                public sealed class Root : Windows.Foundation.IStringable
                {
                    public override string ToString() => "root";
                }
                public static class Program
                {
                    public static void Main() => _ = new Root();
                }
                """], Path.Combine(directory, "App.dll"), outputKind: OutputKind.ConsoleApplication);
            byte[]? firstOutput = null;

            foreach (bool reverse in new[] { false, true })
            {
                (int exitCode, string log) = RunGenerator(directory, app, treatWarningsAsErrors: true,
                    additionalReferences: [contracts, .. reverse ? libraries.Reverse() : libraries],
                    maxDegreesOfParallelism: reverse ? 4 : 1,
                    timeout: TimeSpan.FromMinutes(2));

                Assert.AreEqual(0, exitCode, log);
                Assert.IsFalse(log.Contains(ExpansionWarning, StringComparison.Ordinal), log);

                byte[] output = File.ReadAllBytes(Path.Combine(directory, "WinRT.Interop.dll"));

                if (firstOutput is not null)
                {
                    CollectionAssert.AreEqual(firstOutput, output, "Same-named receivers in different assemblies must retain stable join ordering.");
                }

                firstOutput = output;
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
                public sealed class Pair<U, T>;
                public sealed class Node<T> : Windows.Foundation.IStringable
                {
                    public override string ToString() => "node";
                }
                public interface IOuter { bool Check<T>(); }
                public class Impl<U> : IOuter
                {
                    public bool Check<T>()
                    {
                        _ = new Node<Pair<U, T>>();
                        return true;
                    }
                }
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

    [TestMethod]
    [DataRow("minimal", false, false, true)]
    [DataRow("strict", false, false, false)]
    [DataRow("strict", true, false, true)]
    [DataRow("minimal", false, true, false)]
    [DataRow("strict", true, true, false)]
    public void GenericDispatch_ImplementationsRespectModulePolicy(string mode, bool optIn, bool optOut, bool expected)
    {
        string directory = Directory.CreateTempSubdirectory("InteropDispatchPolicy_").FullName;

        try
        {
            string contracts = ProjectionWriterRunner.CompileSources(
                ["""
                namespace Policy;
                public struct Marker;
                public interface IOuter { object Create<T>(); }
                """], Path.Combine(directory, "Contracts.dll"));
            string implementation = ProjectionWriterRunner.CompileSources(
                ["""
                namespace Policy;
                public sealed class Outer : IOuter
                {
                    public object Create<T>() => new T[1];
                }
                """], Path.Combine(directory, "Foreign.dll"), additionalReferences: [contracts]);
            string app = ProjectionWriterRunner.CompileSources(
                ["""
                public sealed class Root : Windows.Foundation.IStringable
                {
                    public override string ToString() => "root";
                }
                public static class Program
                {
                    public static void Main()
                    {
                        _ = new Root();
                        _ = ((Policy.IOuter)new Policy.Outer()).Create<Policy.Marker>();
                    }
                }
                """], Path.Combine(directory, "PolicyApp.dll"), outputKind: OutputKind.ConsoleApplication,
                additionalReferences: [contracts, implementation]);
            (int exitCode, string log) = RunGenerator(directory, app,
                additionalReferences: [contracts, implementation],
                additionalArguments: [
                    $"--marshalling-mode {mode}",
                    .. optIn ? new[] { "--marshalling-enabled-assembly-names Foreign" } : [],
                    .. optOut ? new[] { "--marshalling-disabled-assembly-names Foreign" } : []]);

            Assert.AreEqual(0, exitCode, log);
            Assert.AreEqual(expected, GetComWrappersTypeAssociations(Path.Combine(directory, "WinRT.Interop.dll"))
                .Any(type => type.StartsWith("Policy.Marker[],", StringComparison.Ordinal)), log);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
