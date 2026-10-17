// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using ProjectionWriterTest.Helpers;

namespace ProjectionWriterTest;

/// <inheritdoc cref="Test_InteropGenericDiscovery"/>
public partial class Test_InteropGenericDiscovery
{
    [TestMethod]
    public void VariantInterface_LateReceiverJoin_DoesNotInventGenericOrArrayArguments()
    {
        string directory = Directory.CreateTempSubdirectory("InteropVariantLateReceiver_").FullName;

        try
        {
            string contracts = ProjectionWriterRunner.CompileSources(
                ["""
                namespace Across;
                public interface IMarker;
                public class CallMarker;
                public sealed class ReceiverMarker : CallMarker, IMarker;
                public sealed class Node<U, T> : Windows.Foundation.IStringable where U : IMarker
                {
                    public override string ToString() => "node";
                }
                public interface IOuter<out U> { object Check<T>(); }
                public sealed class Outer<U> : IOuter<U> where U : IMarker
                {
                    public object Check<T>()
                    {
                        _ = new U[1];
                        return new Node<U, T>();
                    }
                }
                """], Path.Combine(directory, "Contracts.dll"));
            string producer = ProjectionWriterRunner.CompileSources(
                ["""
                public static class Producer
                {
                    public static object Create() => new Across.Outer<Across.ReceiverMarker>();
                }
                """], Path.Combine(directory, "Producer.dll"), additionalReferences: [contracts]);
            string caller = ProjectionWriterRunner.CompileSources(
                ["""
                public static class Caller
                {
                    public static object Invoke() => ((Across.IOuter<Across.CallMarker>)null!).Check<int>();
                }
                """], Path.Combine(directory, "Caller.dll"), additionalReferences: [contracts]);
            string app = ProjectionWriterRunner.CompileSources(
                ["""
                public static class Program
                {
                    public static void Main()
                    {
                        _ = Producer.Create();
                        _ = Caller.Invoke();
                    }
                }
                """], Path.Combine(directory, "App.dll"), outputKind: OutputKind.ConsoleApplication,
                additionalReferences: [contracts, producer, caller]);
            byte[]? firstOutput = null;

            foreach (bool reverse in new[] { false, true })
            {
                (int exitCode, string log) = RunGenerator(directory, app, treatWarningsAsErrors: true,
                    additionalReferences: reverse ? [caller, producer, contracts] : [contracts, producer, caller],
                    maxDegreesOfParallelism: reverse ? 4 : 1);

                Assert.AreEqual(0, exitCode, log);

                string interop = Path.Combine(directory, "WinRT.Interop.dll");
                string[] associations = [.. GetComWrappersTypeAssociations(interop)];
                string[] nodes = [.. associations.Where(static type => type.StartsWith("Across.Node`2", StringComparison.Ordinal))];

                Assert.HasCount(1, nodes, log);
                Assert.IsTrue(nodes[0].StartsWith("Across.Node`2[[Across.ReceiverMarker,", StringComparison.Ordinal), log);
                StringAssert.Contains(nodes[0], "],[System.Int32,");
                Assert.IsTrue(associations.Any(static type => type.StartsWith("Across.ReceiverMarker[],", StringComparison.Ordinal)), log);
                Assert.IsFalse(associations.Any(static type => type.StartsWith("Across.CallMarker[],", StringComparison.Ordinal)), log);

                byte[] output = File.ReadAllBytes(interop);

                if (firstOutput is not null)
                {
                    CollectionAssert.AreEqual(firstOutput, output);
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
    public void CrossModuleInheritedNonvirtualImplementation_ClosesForwardingStub()
    {
        string directory = Directory.CreateTempSubdirectory("InteropInheritedDispatch_").FullName;

        try
        {
            string library = ProjectionWriterRunner.CompileSources(
                ["""
                namespace Across;
                public sealed class Node<U, T> : Windows.Foundation.IStringable
                {
                    public override string ToString() => "node";
                }
                public interface IOuter { object Check<T>(); }
                public class Base<U>
                {
                    public object Check<T>() => new Node<U, T>();
                }
                """], Path.Combine(directory, "Library.dll"));
            string app = ProjectionWriterRunner.CompileSources(
                ["""
                using Across;
                public sealed class Outer<U> : Base<U>, IOuter;
                public static class Program
                {
                    public static void Main() => _ = ((IOuter)new Outer<string>()).Check<int>();
                }
                """], Path.Combine(directory, "App.dll"), outputKind: OutputKind.ConsoleApplication,
                additionalReferences: [library]);
            (int exitCode, string log) = RunGenerator(directory, app, treatWarningsAsErrors: true,
                additionalReferences: [library]);

            Assert.AreEqual(0, exitCode, log);
            Assert.IsTrue(GetComWrappersTypeAssociations(Path.Combine(directory, "WinRT.Interop.dll")).Any(type =>
                type.StartsWith("Across.Node`2[[System.String,", StringComparison.Ordinal) &&
                type.Contains("],[System.Int32,", StringComparison.Ordinal)), log);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void DefaultInterfaceBody_PublishesContextsForLateDispatch(bool publishesReceiver)
    {
        string directory = Directory.CreateTempSubdirectory("InteropDefaultPublication_").FullName;

        try
        {
            string contracts = ProjectionWriterRunner.CompileSources(
                [$$"""
                namespace Across;
                public struct First;
                public struct Second;
                public sealed class Pair<X, Y>;
                public sealed class Node<T> : Windows.Foundation.IStringable
                {
                    public override string ToString() => "node";
                }
                public interface IDispatch { void Check<T>(); }
                public sealed class Target<U> : IDispatch
                {
                    public void Check<T>() => _ = new Node<Pair<U, T>>();
                }
                public interface IFactory { object Create(); }
                public interface IFactory<T> : IFactory
                {
                    object IFactory.Create()
                    {
                        {{(publishesReceiver ? "return new Target<T>();" : "((IDispatch)null!).Check<T>(); return null!;")}}
                    }
                }
                public sealed class Factory<T> : IFactory<T>;
                """], Path.Combine(directory, "Contracts.dll"));
            string producer = ProjectionWriterRunner.CompileSources(
                [$$"""
                public static class Producer
                {
                    public static object Create() => new Across.{{(publishesReceiver ? "Factory" : "Target")}}<Across.First>();
                }
                """], Path.Combine(directory, "Producer.dll"), additionalReferences: [contracts]);
            string caller = ProjectionWriterRunner.CompileSources(
                [$$"""
                public static class Caller
                {
                    public static void Invoke()
                    {
                        {{(publishesReceiver ? "((Across.IDispatch)null!).Check<Across.Second>();" : "_ = new Across.Factory<Across.Second>();")}}
                    }
                }
                """], Path.Combine(directory, "Caller.dll"), additionalReferences: [contracts]);
            string app = ProjectionWriterRunner.CompileSources(
                ["""
                public static class Program
                {
                    public static void Main()
                    {
                        _ = Producer.Create();
                        Caller.Invoke();
                    }
                }
                """], Path.Combine(directory, "App.dll"), outputKind: OutputKind.ConsoleApplication,
                additionalReferences: [contracts, producer, caller]);
            (int exitCode, string log) = RunGenerator(directory, app, treatWarningsAsErrors: true,
                additionalReferences: [contracts, producer, caller]);

            Assert.AreEqual(0, exitCode, log);
            Assert.IsTrue(GetComWrappersTypeAssociations(Path.Combine(directory, "WinRT.Interop.dll")).Any(type =>
                type.StartsWith("Across.Node`1[[Across.Pair`2[[Across.First,", StringComparison.Ordinal) &&
                type.Contains("],[Across.Second,", StringComparison.Ordinal)), log);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void DefaultInterfaceBody_PreservesGenericDispatchLateCutoff()
    {
        string directory = Directory.CreateTempSubdirectory("InteropDefaultLateCutoff_").FullName;

        try
        {
            string markers = string.Join('\n', Enumerable.Range(0, 1024).Select(index => $"public struct Marker{index};"));
            string roots = string.Join('\n', Enumerable.Range(0, 1024).Select(index => $"_ = new Target<Marker{index}>();"));
            string contracts = ProjectionWriterRunner.CompileSources(
                [$$"""
                namespace LateCutoff;
                public struct DispatchArgument;
                public struct DefaultArgument;
                {{markers}}
                public sealed class Pair<X, Y>;
                public sealed class Node<T> : Windows.Foundation.IStringable
                {
                    public override string ToString() => "node";
                }
                public interface IDispatch { void Check<T>(); }
                public sealed class Target<U> : IDispatch
                {
                    public void Check<T>() => _ = new Node<Pair<U, T>>();
                }
                public interface IFactory { void Create(); }
                public interface IFactory<T> : IFactory
                {
                    void IFactory.Create() => ((IDispatch)null!).Check<T>();
                }
                public sealed class Factory<T> : IFactory<T>;
                """], Path.Combine(directory, "Contracts.dll"));
            string defaults = ProjectionWriterRunner.CompileSources(
                ["""
                public static class Defaults
                {
                    public static object Create() => new LateCutoff.Factory<LateCutoff.DefaultArgument>();
                }
                """], Path.Combine(directory, "A_Defaults.dll"), additionalReferences: [contracts]);
            string caller = ProjectionWriterRunner.CompileSources(
                ["""
                public static class Caller
                {
                    public static void Invoke() => ((LateCutoff.IDispatch)null!).Check<LateCutoff.DispatchArgument>();
                }
                """], Path.Combine(directory, "B_Caller.dll"), additionalReferences: [contracts]);
            string receivers = ProjectionWriterRunner.CompileSources(
                [$$"""
                using LateCutoff;
                public static class Receivers
                {
                    public static void Create()
                    {
                        {{roots}}
                    }
                }
                """], Path.Combine(directory, "C_Receivers.dll"), additionalReferences: [contracts]);
            string app = ProjectionWriterRunner.CompileSources(
                ["""
                public static class Program
                {
                    public static void Main()
                    {
                        _ = Defaults.Create();
                        Caller.Invoke();
                        Receivers.Create();
                    }
                }
                """], Path.Combine(directory, "App.dll"), outputKind: OutputKind.ConsoleApplication,
                additionalReferences: [contracts, defaults, caller, receivers]);
            byte[]? firstOutput = null;

            foreach (bool reverse in new[] { false, true })
            {
                (int exitCode, string log) = RunGenerator(directory, app,
                    additionalReferences: reverse ? [receivers, caller, defaults, contracts] : [contracts, defaults, caller, receivers],
                    maxDegreesOfParallelism: reverse ? 4 : 1,
                    timeout: TimeSpan.FromMinutes(2));

                Assert.AreEqual(0, exitCode, log);
                StringAssert.Contains(log, "Supplemental generic discovery");

                string interop = Path.Combine(directory, "WinRT.Interop.dll");
                string[] nodes = [.. GetComWrappersTypeAssociations(interop).Where(static type =>
                    type.StartsWith("LateCutoff.Node`1", StringComparison.Ordinal))];

                Assert.AreEqual(1024, nodes.Length, log);
                Assert.IsTrue(nodes.All(static type => type.Contains("],[LateCutoff.DispatchArgument,", StringComparison.Ordinal)),
                    "Default-body publication displaced initial generic-dispatch cross-module contexts.");
                Assert.IsTrue(nodes.Any(static type => type.StartsWith("LateCutoff.Node`1[[LateCutoff.Pair`2[[LateCutoff.Marker1023,", StringComparison.Ordinal)));

                byte[] output = File.ReadAllBytes(interop);

                if (firstOutput is not null)
                {
                    CollectionAssert.AreEqual(firstOutput, output);
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
    [DataRow("minimal", false, false, true)]
    [DataRow("strict", false, false, false)]
    [DataRow("strict", true, false, true)]
    [DataRow("minimal", false, true, false)]
    [DataRow("strict", true, true, false)]
    public void DefaultInterfaceBody_RespectsBodyModulePolicy(string mode, bool optIn, bool optOut, bool expected)
    {
        string directory = Directory.CreateTempSubdirectory("InteropDefaultBodyPolicy_").FullName;

        try
        {
            string implementation = ProjectionWriterRunner.CompileSources(
                ["""
                namespace Policy;
                public struct Marker;
                public interface IFactory { object Create(); }
                public interface IFactory<T> : IFactory
                {
                    object IFactory.Create() => new T[1];
                }
                public sealed class Factory<T> : IFactory<T>;
                """], Path.Combine(directory, "Foreign.dll"));
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
                        _ = ((Policy.IFactory)new Policy.Factory<Policy.Marker>()).Create();
                    }
                }
                """], Path.Combine(directory, "App.dll"), outputKind: OutputKind.ConsoleApplication,
                additionalReferences: [implementation]);
            (int exitCode, string log) = RunGenerator(directory, app,
                additionalReferences: [implementation],
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

    [TestMethod]
    public void DefaultInterfaceBody_ReceiverDiscoveredByLateJoin()
    {
        string directory = Directory.CreateTempSubdirectory("InteropLateDefaultBody_").FullName;

        try
        {
            string contracts = ProjectionWriterRunner.CompileSources(
                ["""
                namespace Across;
                public struct First;
                public struct Second;
                public sealed class Pair<X, Y>;
                public sealed class Node<T> : Windows.Foundation.IStringable
                {
                    public override string ToString() => "node";
                }
                public interface IDispatch { object Create<T>(); }
                public interface IFactory { object Create(); }
                public interface IFactory<T> : IFactory
                {
                    object IFactory.Create() => new Node<T>();
                }
                public sealed class Factory<T> : IFactory<T>;
                public sealed class Outer<U> : IDispatch
                {
                    public object Create<T>() => new Factory<Pair<U, T>>();
                }
                """], Path.Combine(directory, "Contracts.dll"));
            string producer = ProjectionWriterRunner.CompileSources(
                ["""
                public static class Producer
                {
                    public static Across.IDispatch Create() => new Across.Outer<Across.First>();
                }
                """], Path.Combine(directory, "Producer.dll"), additionalReferences: [contracts]);
            string caller = ProjectionWriterRunner.CompileSources(
                ["""
                public static class Caller
                {
                    public static object Invoke<T>(Across.IDispatch value) => value.Create<T>();
                }
                """], Path.Combine(directory, "Caller.dll"), additionalReferences: [contracts]);
            string app = ProjectionWriterRunner.CompileSources(
                ["""
                public static class Program
                {
                    public static void Main() => _ = Caller.Invoke<Across.Second>(Producer.Create());
                }
                """], Path.Combine(directory, "App.dll"), outputKind: OutputKind.ConsoleApplication,
                additionalReferences: [contracts, producer, caller]);
            byte[]? firstOutput = null;

            foreach (bool reverse in new[] { false, true })
            {
                (int exitCode, string log) = RunGenerator(directory, app, treatWarningsAsErrors: true,
                    additionalReferences: reverse ? [caller, producer, contracts] : [contracts, producer, caller],
                    maxDegreesOfParallelism: reverse ? 4 : 1);

                Assert.AreEqual(0, exitCode, log);

                string interop = Path.Combine(directory, "WinRT.Interop.dll");

                Assert.IsTrue(GetComWrappersTypeAssociations(interop).Any(type =>
                    type.StartsWith("Across.Node`1[[Across.Pair`2[[Across.First,", StringComparison.Ordinal)), log);

                byte[] output = File.ReadAllBytes(interop);

                if (firstOutput is not null)
                {
                    CollectionAssert.AreEqual(firstOutput, output);
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
    [DataRow(false)]
    [DataRow(true)]
    public void GenericDispatch_CompletedModuleJoinsDoNotConsumeLateBudget(bool includeDefaults)
    {
        string directory = Directory.CreateTempSubdirectory("InteropCompletedJoins_").FullName;

        try
        {
            string contracts = ProjectionWriterRunner.CompileSources(
                [$$"""
                public interface IOuter { void Check<T>(); }
                public interface IFactory { object Create(); }
                public interface IFactory<T> : IFactory
                {
                    object IFactory.Create() => null!;
                }
                public sealed class Outer<U> : IOuter{{(includeDefaults ? ", IFactory<U>" : "")}}
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
