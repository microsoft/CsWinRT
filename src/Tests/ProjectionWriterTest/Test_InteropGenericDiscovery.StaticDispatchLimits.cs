// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using ProjectionWriterTest.Helpers;

namespace ProjectionWriterTest;

/// <inheritdoc cref="Test_InteropGenericDiscovery"/>
public partial class Test_InteropGenericDiscovery
{
    [TestMethod]
    [DataRow("Node<T>", RecursionWarning, false)]
    [DataRow("Node<T>", RecursionWarning, true)]
    [DataRow("Pair<T, T>", ComplexityWarning, false)]
    [DataRow("Pair<T, T>", ComplexityWarning, true)]
    public void StaticDispatch_ExpandingCallsRespectLimits(string argument, string warning, bool treatWarningsAsErrors)
    {
        AssertStaticLimit($$"""
            using Windows.Foundation;
            namespace StaticLimits;
            public sealed class Node<T> : IStringable { public override string ToString() => "node"; }
            public sealed class Pair<X, Y>;
            public interface IFactory { static abstract object Create(); }
            public interface IFactory<T> : IFactory
            {
                static object IFactory.Create()
                {
                    _ = Calls.Invoke<Factory<{{argument}}>>();
                    return new Node<T>();
                }
            }
            public sealed class Factory<T> : IFactory<T>;
            public static class Calls
            {
                public static object Invoke<TFactory>() where TFactory : IFactory => TFactory.Create();
            }
            public static class Program
            {
                public static void Main()
                {
                    _ = new Node<string>();
                    _ = Calls.Invoke<Factory<long>>();
                }
            }
            """, warning, treatWarningsAsErrors);
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public void StaticDispatch_MetadataHierarchyIsBounded(bool expanding, bool treatWarningsAsErrors)
    {
        int levels = expanding ? 10 : 40;
        string hierarchy = string.Join('\n', Enumerable.Range(0, levels).Select(index =>
            $"public interface ILevel{index}<T> : ILevel{index + 1}<{(expanding ? "Pair<T, T>" : "T")}>;"));

        AssertStaticLimit($$"""
            using Windows.Foundation;
            namespace StaticLimits;
            public sealed class Node<T> : IStringable { public override string ToString() => "node"; }
            public sealed class Pair<X, Y>;
            public interface IFactory { static abstract object Create(); }
            public interface ILevel{{levels}}<T> : IFactory
            {
                static object IFactory.Create() => new Node<T>();
            }
            {{hierarchy}}
            public sealed class Factory<T> : ILevel0<T>;
            public static class Program
            {
                public static void Main()
                {
                    _ = new Node<string>();
                    _ = Invoke<Factory<long>>();
                }
                private static object Invoke<TFactory>() where TFactory : IFactory => TFactory.Create();
            }
            """, expanding ? ComplexityWarning : RecursionWarning, treatWarningsAsErrors);
    }

    [TestMethod]
    public void StaticDispatch_IndeterminateGenericSelectionRetainsExistingCoverage()
    {
        string hierarchy = string.Join('\n', Enumerable.Range(0, 40).Select(index =>
            $"public interface ILevel{index}<T> : ILevel{index + 1}<T>;"));

        AssertStaticDispatchSource("generic-bounded-selection-control", $$"""
            public interface IFactory { static abstract object Create<TMethod>(); }
            public interface ILevel40<T> : IFactory
            {
                static object IFactory.Create<TMethod>() => new List<IAsyncOperationWithProgress<T, TMethod>>();
            }
            {{hierarchy}}
            public sealed class Factory<T> : ILevel0<T>;
            """, "Invoke<Factory<long>>()", """
            private static object Invoke<TFactory>() where TFactory : IFactory => TFactory.Create<int>();
            """, genericSlot: true,
            expected: "System.Collections.Generic.List`1[[Windows.Foundation.IAsyncOperationWithProgress`2[[System.Int64,",
            expectedArgument: "],[System.Int32,");
    }

    [TestMethod]
    public void StaticDispatch_NewBodiesShareDefaultPhaseAllowance()
    {
        string markers = string.Join('\n', Enumerable.Range(0, 1030).Select(index => $"public struct Marker{index};"));
        string roots = string.Join('\n', Enumerable.Range(0, 1030).Select(index => $"_ = Invoke<Factory<Marker{index}>>();"));
        string directory = Directory.CreateTempSubdirectory("InteropStaticBodyBudget_").FullName;

        try
        {
            string app = ProjectionWriterRunner.CompileSources(
                [$$"""
                using Windows.Foundation;
                namespace StaticBudget;
                {{markers}}
                public sealed class Node<T> : IStringable { public override string ToString() => "node"; }
                public sealed class Required<T> : IStringable { public override string ToString() => "required"; }
                public interface IInstance { object Create(); }
                public interface IInstance<T> : IInstance { object IInstance.Create() => new Required<T>(); }
                public sealed class Instance<T> : IInstance<T>;
                public interface IFactory { static abstract object Create(); }
                public interface IFactory<T> : IFactory { static object IFactory.Create() => new Node<T>(); }
                public sealed class Factory<T> : IFactory<T>;
                public static class Program
                {
                    public static void Main()
                    {
                        _ = new Instance<string>();
                        {{roots}}
                    }
                    private static object Invoke<TFactory>() where TFactory : IFactory => TFactory.Create();
                }
                """], Path.Combine(directory, "App.dll"), outputKind: OutputKind.ConsoleApplication);
            byte[]? previous = null;

            foreach (int parallelism in new[] { 1, 4 })
            {
                (int exitCode, string log) = RunGenerator(directory, app,
                    treatWarningsAsErrors: false, maxDegreesOfParallelism: parallelism, timeout: TimeSpan.FromMinutes(2));

                Assert.AreEqual(0, exitCode, log);
                StringAssert.Contains(log, "Supplemental generic discovery");

                string interop = Path.Combine(directory, "WinRT.Interop.dll");
                HashSet<string> associations = GetComWrappersTypeAssociations(interop);

                Assert.IsTrue(associations.Any(static type => type.StartsWith("StaticBudget.Required`1[[System.String,", StringComparison.Ordinal)), log);
                Assert.AreEqual(1022, associations.Count(static type => type.StartsWith("StaticBudget.Node`1", StringComparison.Ordinal)), log);
                Assert.IsFalse(associations.Any(static type => type.StartsWith("StaticBudget.Node`1[[StaticBudget.Marker1029,", StringComparison.Ordinal)), log);

                byte[] output = File.ReadAllBytes(interop);

                if (previous is not null)
                {
                    CollectionAssert.AreEqual(previous, output);
                }

                previous = output;
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void StaticDispatch_PrecisionPreservesNearCapContexts()
    {
        string declarations = string.Join('\n', Enumerable.Range(0, 1022).Select(index => $$"""
            public struct Marker{{index}};
            public sealed class Initial{{index}} : IInitial
            {
                public void Check<T>() => _ = new Existing<Pair<Marker{{index}}, T>>();
            }
            """));
        string directory = Directory.CreateTempSubdirectory("InteropStaticPrecisionBudget_").FullName;

        try
        {
            string app = ProjectionWriterRunner.CompileSources(
                [$$"""
                using Windows.Foundation;
                namespace PrecisionBudget;
                public struct Root;
                public readonly struct Pair<X, Y>;
                public sealed class Existing<T> : IStringable { public override string ToString() => "existing"; }
                public sealed class Winning<T> : IStringable { public override string ToString() => "winning"; }
                public sealed class Leaked<T> : IStringable { public override string ToString() => "leaked"; }
                public sealed class Shared<T> : IStringable { public override string ToString() => "shared"; }
                public interface IInitial { void Check<T>(); }
                {{declarations}}
                public interface IFactory { static abstract object Create<T>(); }
                public interface IFactory<U> : IFactory
                {
                    static object IFactory.Create<T>()
                    {
                        _ = new Shared<Pair<U, T>>();
                        return new Leaked<Pair<U, T>>();
                    }
                }
                public sealed class Factory<U> : IFactory<U>
                {
                    public static object Create<T>()
                    {
                        _ = new Shared<Pair<U, T>>();
                        return new Winning<Pair<U, T>>();
                    }
                }
                public static class Program
                {
                    public static void Main()
                    {
                        ((IInitial)new Initial0()).Check<int>();
                        _ = Invoke<Factory<Root>>();
                    }
                    private static object Invoke<TFactory>() where TFactory : IFactory => TFactory.Create<string>();
                }
                """], Path.Combine(directory, "App.dll"), outputKind: OutputKind.ConsoleApplication);
            (int exitCode, string log) = RunGenerator(directory, app, treatWarningsAsErrors: false,
                timeout: TimeSpan.FromMinutes(2));

            Assert.AreEqual(0, exitCode, log);

            HashSet<string> current = GetComWrappersTypeAssociations(Path.Combine(directory, "WinRT.Interop.dll"));

            Assert.AreEqual(1022, current.Count(static type => type.StartsWith("PrecisionBudget.Existing`1", StringComparison.Ordinal)), log);
            Assert.IsTrue(current.Any(static type => type.StartsWith("PrecisionBudget.Winning`1", StringComparison.Ordinal)), log);
            Assert.IsTrue(current.Any(static type => type.StartsWith("PrecisionBudget.Shared`1", StringComparison.Ordinal)), log);
            Assert.IsFalse(current.Any(static type => type.StartsWith("PrecisionBudget.Leaked`1", StringComparison.Ordinal)), log);

        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void StaticDispatch_LatePublicationsPreserveDefaultCoverage(bool joinLocalDefaults)
    {
        int count = joinLocalDefaults ? 340 : 1024;
        string directory = Directory.CreateTempSubdirectory("InteropStaticLateBudget_").FullName;

        try
        {
            string markers = string.Join('\n', Enumerable.Range(0, count).Select(index => $"public struct Marker{index};"));
            string roots = string.Join('\n', Enumerable.Range(0, count).Select(index => $"_ = new Target<Marker{index}>();"));
            string contracts = ProjectionWriterRunner.CompileSources(
                [$$"""
                using Windows.Foundation;
                namespace StaticLate;
                {{markers}}
                public struct ExistingArgument;
                public struct StaticArgument;
                public readonly struct Pair<X, Y>;
                public sealed class Node<T> : IStringable { public override string ToString() => "node"; }
                public interface IResult { object Create(); }
                public interface IResult<T> : IResult { object IResult.Create() => new Node<T>(); }
                public sealed class Result<T> : IResult<T>;
                public interface IDispatch { void Check<T>(); }
                public sealed class Target<U> : IDispatch
                {
                    public void Check<T>() => _ = new {{(joinLocalDefaults ? "Result" : "Node")}}<Pair<U, T>>();
                }
                public interface IDemand { object Create(); }
                public interface IDemand<T> : IDemand
                {
                    object IDemand.Create()
                    {
                        ((IDispatch)null!).Check<T>();
                        return null!;
                    }
                }
                public sealed class Demand<T> : IDemand<T>;
                public interface IFactory { static abstract object Create(); }
                public interface IFactory<T> : IFactory
                {
                    static object IFactory.Create()
                    {
                        ((IDispatch)null!).Check<T>();
                        return null!;
                    }
                }
                public sealed class Factory<T> : IFactory<T>;
                public static class Calls
                {
                    public static object Create<TFactory>() where TFactory : IFactory => TFactory.Create();
                }
                """], Path.Combine(directory, "Contracts.dll"));
            string statics = ProjectionWriterRunner.CompileSources(
                ["public static class Statics { public static object Create() => StaticLate.Calls.Create<StaticLate.Factory<StaticLate.StaticArgument>>(); }"],
                Path.Combine(directory, "A_Statics.dll"), additionalReferences: [contracts]);
            string defaults = ProjectionWriterRunner.CompileSources(
                ["public static class Defaults { public static object Create() => new StaticLate.Demand<StaticLate.ExistingArgument>(); }"],
                Path.Combine(directory, "B_Defaults.dll"), additionalReferences: [contracts]);
            string receivers = ProjectionWriterRunner.CompileSources(
                [$$"""
                using StaticLate;
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
                        _ = Statics.Create();
                        _ = Defaults.Create();
                        Receivers.Create();
                    }
                }
                """], Path.Combine(directory, "App.dll"), outputKind: OutputKind.ConsoleApplication,
                additionalReferences: [contracts, statics, defaults, receivers]);
            byte[]? previous = null;

            foreach (bool reverse in new[] { false, true })
            {
                (int exitCode, string log) = RunGenerator(directory, app,
                    additionalReferences: reverse ? [receivers, defaults, statics, contracts] : [contracts, statics, defaults, receivers],
                    treatWarningsAsErrors: false, maxDegreesOfParallelism: reverse ? 4 : 1, timeout: TimeSpan.FromMinutes(2));

                Assert.AreEqual(0, exitCode, log);
                StringAssert.Contains(log, "Supplemental generic discovery");

                string interop = Path.Combine(directory, "WinRT.Interop.dll");
                string[] nodes = [.. GetComWrappersTypeAssociations(interop).Where(static type =>
                    type.StartsWith("StaticLate.Node`1", StringComparison.Ordinal))];

                Assert.AreEqual(count, nodes.Length, log);
                Assert.IsTrue(nodes.All(static type => type.Contains("],[StaticLate.ExistingArgument,", StringComparison.Ordinal)),
                    "Static publication displaced an existing default-derived context.");

                byte[] output = File.ReadAllBytes(interop);

                if (previous is not null)
                {
                    CollectionAssert.AreEqual(previous, output);
                }

                previous = output;
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static void AssertStaticLimit(string source, string warning, bool treatWarningsAsErrors)
    {
        string directory = Directory.CreateTempSubdirectory("InteropStaticLimit_").FullName;

        try
        {
            string app = ProjectionWriterRunner.CompileSources([source], Path.Combine(directory, "StaticLimits.dll"),
                outputKind: OutputKind.ConsoleApplication);
            (int exitCode, string log) = RunGenerator(directory, app, treatWarningsAsErrors: treatWarningsAsErrors);

            StringAssert.Contains(log, warning);
            Assert.AreEqual(treatWarningsAsErrors, exitCode != 0, log);
            Assert.AreEqual(!treatWarningsAsErrors, File.Exists(Path.Combine(directory, "WinRT.Interop.dll")), log);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
