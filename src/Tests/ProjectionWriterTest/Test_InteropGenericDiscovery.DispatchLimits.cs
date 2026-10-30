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
    [DataRow(false)]
    [DataRow(true)]
    public void DefaultInterfaceBody_IncompleteSpecificityDiscardsReceiverSelection(bool treatWarningsAsErrors)
    {
        string hierarchy = string.Join('\n', Enumerable.Range(1, 39).Select(index =>
            $"public interface ILevel{index}<T> : ILevel{index - 1}<T>;"));
        string interfaces = string.Join(", ", Enumerable.Range(0, 41).Select(index => $"ILevel{index}<T>"));

        // Previsited ancestors keep the receiver walk shallow, but comparing the last override
        // against the base default still needs a complete, independently bounded interface hierarchy.
        AssertDispatchLimit($$"""
            using Windows.Foundation;
            namespace DispatchLimits;
            public struct Marker;
            public sealed class Node<T> : IStringable { public override string ToString() => "node"; }
            public interface IFactory { object Create(); }
            public interface ILevel0<T> : IFactory
            {
                object IFactory.Create() => new Node<T>();
            }
            {{hierarchy}}
            public interface ILevel40<T> : ILevel39<T>
            {
                object IFactory.Create() => null!;
            }
            public sealed class Factory<T> : {{interfaces}};
            public static class Program
            {
                public static void Main() => _ = ((IFactory)new Factory<Marker>()).Create();
            }
            """, RecursionWarning, treatWarningsAsErrors);
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public void DefaultInterfaceBody_MetadataHierarchyIsBounded(bool expanding, bool treatWarningsAsErrors)
    {
        int levels = expanding ? 10 : 40;
        string hierarchy = string.Join('\n', Enumerable.Range(0, levels).Select(index =>
            $"public interface ILevel{index}<T> : ILevel{index + 1}<{(expanding ? "Pair<T, T>" : "T")}>;"));

        AssertDispatchLimit($$"""
            using Windows.Foundation;
            namespace DispatchLimits;
            public struct Marker;
            public sealed class Pair<X, Y>;
            public sealed class Node<T> : IStringable { public override string ToString() => "node"; }
            public interface IFactory { object Create(); }
            public interface ILevel{{levels}}<T> : IFactory
            {
                object IFactory.Create() => new Node<T>();
            }
            {{hierarchy}}
            public sealed class Factory<T> : ILevel0<T>;
            public static class Program
            {
                public static void Main() => _ = ((IFactory)new Factory<Marker>()).Create();
            }
            """, expanding ? ComplexityWarning : RecursionWarning, treatWarningsAsErrors);
    }

    [TestMethod]
    [DataRow("Node<T>", RecursionWarning, false)]
    [DataRow("Node<T>", RecursionWarning, true)]
    [DataRow("Pair<T, T>", ComplexityWarning, false)]
    [DataRow("Pair<T, T>", ComplexityWarning, true)]
    public void DefaultInterfaceBody_ExpandingReceiversRespectLimits(string argument, string warning, bool treatWarningsAsErrors)
    {
        AssertDispatchLimit($$"""
            using Windows.Foundation;
            namespace DispatchLimits;
            public struct Marker;
            public sealed class Pair<X, Y>;
            public sealed class Node<T> : IStringable { public override string ToString() => "node"; }
            public interface IFactory { object Create(); }
            public interface IFactory<T> : IFactory
            {
                object IFactory.Create()
                {
                    _ = new Node<T>();
                    return new Factory<{{argument}}>();
                }
            }
            public sealed class Factory<T> : IFactory<T>;
            public static class Program
            {
                public static void Main() => _ = ((IFactory)new Factory<Marker>()).Create();
            }
            """, warning, treatWarningsAsErrors);
    }

    [TestMethod]
    public void DefaultInterfaceBody_DoesNotDisplaceExistingSupplementalCutoff()
    {
        string targets = string.Join('\n', Enumerable.Range(0, 1030).Select(index => $$"""
            public struct Marker{{index}};
            public sealed class Target{{index}} : IDispatch
            {
                public object Create<T>() => new Existing<Pair<Marker{{index}}, T>>();
            }
            """));
        string directory = Directory.CreateTempSubdirectory("InteropDefaultBodyCutoff_").FullName;

        try
        {
            HashSet<string>? original = null;
            byte[]? firstOutput = null;
            string app = Path.Combine(directory, "App.dll");

            foreach ((bool observeDefault, int parallelism) in new[] { (false, 1), (true, 1), (true, 4) })
            {
                if (parallelism == 1)
                {
                    _ = ProjectionWriterRunner.CompileSources(
                    [$$"""
                    using Windows.Foundation;
                    namespace Cutoff;
                    public struct Marker;
                    public sealed class Pair<X, Y>;
                    public sealed class Existing<T> : IStringable { public override string ToString() => "existing"; }
                    public sealed class Additional<T> : IStringable { public override string ToString() => "additional"; }
                    public interface IDispatch { object Create<T>(); }
                    {{targets}}
                    public interface IFactory { object Create(); }
                    public interface IFactory<T> : IFactory
                    {
                        object IFactory.Create() => new Additional<T>();
                    }
                    public sealed class Factory<T> : IFactory<T>;
                    public static class Program
                    {
                        public static void Main()
                        {
                            {{(observeDefault ? "_ = new Factory<Marker>();" : "")}}
                            _ = ((IDispatch)new Target0()).Create<Marker>();
                        }
                    }
                    """], app, outputKind: OutputKind.ConsoleApplication);
                }

                (int exitCode, string log) = RunGenerator(directory, app, maxDegreesOfParallelism: parallelism);

                Assert.AreEqual(0, exitCode, log);
                StringAssert.Contains(log, "Supplemental generic discovery");

                string interop = Path.Combine(directory, "WinRT.Interop.dll");
                HashSet<string> associations = GetComWrappersTypeAssociations(interop);
                HashSet<string> existing = [.. associations.Where(static type => type.StartsWith("Cutoff.Existing`1", StringComparison.Ordinal))];

                Assert.AreEqual(1024, existing.Count, log);
                Assert.IsFalse(associations.Any(static type => type.StartsWith("Cutoff.Additional`1", StringComparison.Ordinal)), log);

                if (original is not null)
                {
                    Assert.IsTrue(original.SetEquals(existing), "Default receivers displaced previously covered supplemental bodies.");
                }

                original = existing;

                if (observeDefault)
                {
                    byte[] output = File.ReadAllBytes(interop);

                    if (firstOutput is not null)
                    {
                        CollectionAssert.AreEqual(firstOutput, output);
                    }

                    firstOutput = output;
                }
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
    public void GenericDispatch_ExpandingCallsRespectExistingDepthLimit(bool treatWarningsAsErrors)
    {
        AssertDispatchLimit("""
            using Windows.Foundation;
            namespace DispatchLimits;
            public struct Marker;
            public sealed class Node<T> : IStringable { public override string ToString() => "node"; }
            public interface IOuter { void Check<T>(); }
            public sealed class Outer : IOuter
            {
                public void Check<T>()
                {
                    _ = new Node<T>();
                    ((IOuter)this).Check<Node<T>>();
                }
            }
            public static class Program
            {
                public static void Main() => ((IOuter)new Outer()).Check<Marker>();
            }
            """, RecursionWarning, treatWarningsAsErrors);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void GenericDispatch_ExponentialContextsRespectExistingComplexityLimit(bool treatWarningsAsErrors)
    {
        AssertDispatchLimit("""
            using Windows.Foundation;
            namespace DispatchLimits;
            public struct Marker;
            public sealed class Pair<X,Y>;
            public sealed class Node<T> : IStringable { public override string ToString() => "node"; }
            public interface IOuter { void Check<T>(); }
            public sealed class Outer : IOuter
            {
                public void Check<T>()
                {
                    _ = new Node<T>();
                    ((IOuter)this).Check<Pair<T,T>>();
                }
            }
            public static class Program
            {
                public static void Main() => ((IOuter)new Outer()).Check<Marker>();
            }
            """, ComplexityWarning, treatWarningsAsErrors);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void GenericDispatch_FanoutUsesSupplementalBudgetAndPreservesRoots(bool treatWarningsAsErrors)
    {
        string implementations = string.Join('\n', Enumerable.Range(0, 1030).Select(index => $$"""
            public struct Marker{{index}};
            public sealed class Outer{{index}} : IOuter
            {
                public void Check<T>() => _ = new Node<Pair<Marker{{index}},T>>();
            }
            """));
        AssertDispatchLimit($$"""
            using Windows.Foundation;
            namespace DispatchLimits;
            public struct Marker;
            public sealed class Pair<X,Y>;
            public sealed class Node<T> : IStringable { public override string ToString() => "node"; }
            public interface IOuter { void Check<T>(); }
            {{implementations}}
            public static class Program
            {
                public static void Main()
                {
                    _ = new Node<string>();
                    ((IOuter)new Outer0()).Check<Marker>();
                }
            }
            """, ExpansionWarning, treatWarningsAsErrors, verifyDeterminism: !treatWarningsAsErrors);
    }

    [TestMethod]
    public void SupplementalFanout_PreservesEveryOrdinaryMemberInstantiation()
    {
        string ordinaryMarkers = string.Join('\n', Enumerable.Range(0, 64).Select(index => $"public struct Marker{index};"));
        string ordinaryRoots = string.Join('\n', Enumerable.Range(0, 64).Select(index => $"_ = new Root<Marker{index}>();"));
        string targets = string.Join('\n', Enumerable.Range(0, 1030).Select(index => $$"""
            public sealed class Target{{index}} : IDispatch
            {
                public void Check<T>() { }
            }
            """));
        string directory = Directory.CreateTempSubdirectory("InteropAdditiveBudget_").FullName;

        try
        {
            string app = ProjectionWriterRunner.CompileSources(
                [$$"""
                using Windows.Foundation;
                namespace Additive;
                {{ordinaryMarkers}}
                public struct DispatchMarker;
                public sealed class Root<T> : IStringable
                {
                    public Middle<T> Create() => new();
                    public override string ToString() => "root";
                }
                public sealed class Middle<T> { public Leaf<T> Next() => new(); }
                public sealed class Leaf<T> : IStringable { public override string ToString() => "leaf"; }
                public interface IDispatch { void Check<T>(); }
                {{targets}}
                public static class Program
                {
                    public static void Main()
                    {
                        {{ordinaryRoots}}
                        ((IDispatch)new Target0()).Check<DispatchMarker>();
                    }
                }
                """], Path.Combine(directory, "Additive.dll"), outputKind: OutputKind.ConsoleApplication);
            byte[]? firstOutput = null;

            foreach (int parallelism in new[] { 1, 4 })
            {
                (int exitCode, string log) = RunGenerator(directory, app, maxDegreesOfParallelism: parallelism);

                Assert.AreEqual(0, exitCode, log);

                string interop = Path.Combine(directory, "WinRT.Interop.dll");
                HashSet<string> types = GetComWrappersTypeAssociations(interop);

                for (int index = 0; index < 64; index++)
                {
                    Assert.IsTrue(types.Any(type => type.StartsWith($"Additive.Leaf`1[[Additive.Marker{index},", StringComparison.Ordinal)),
                        $"Ordinary member 'Leaf<Marker{index}>' was displaced by dispatch fan-out.\n{log}");
                }

                StringAssert.Contains(log, "Supplemental generic discovery");
                Assert.IsFalse(log.Contains("Generic member discovery reached", StringComparison.Ordinal), log);

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
    public void ExhaustedPrimaryBudget_DoesNotDisableSupplementalDispatch()
    {
        string directory = Directory.CreateTempSubdirectory("InteropPrimaryExhaustion_").FullName;

        try
        {
            string app = ProjectionWriterRunner.CompileSources(
                ["""
                using Windows.Foundation;
                namespace Additive;
                public sealed class A<T>;
                public sealed class B<T>;
                public sealed class Expanding<T>
                {
                    public Expanding<A<T>> First() => new();
                    public Expanding<B<T>> Second() => new();
                }
                public struct Marker;
                public sealed class Required<T> : IStringable { public override string ToString() => "required"; }
                public interface IDispatch { void Check<T>(); }
                public sealed class Target : IDispatch
                {
                    public void Check<T>() => _ = new Required<T>();
                }
                public static class Program
                {
                    public static void Main()
                    {
                        _ = new Expanding<int>();
                        ((IDispatch)new Target()).Check<Marker>();
                    }
                }
                """], Path.Combine(directory, "Additive.dll"), outputKind: OutputKind.ConsoleApplication);
            (int exitCode, string log) = RunGenerator(directory, app);

            Assert.AreEqual(0, exitCode, log);
            StringAssert.Contains(log, "Generic member discovery reached");
            Assert.IsTrue(GetComWrappersTypeAssociations(Path.Combine(directory, "WinRT.Interop.dll")).Any(type =>
                type.StartsWith("Additive.Required`1[[Additive.Marker,", StringComparison.Ordinal)), log);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static void AssertDispatchLimit(string source, string warning, bool treatWarningsAsErrors, bool verifyDeterminism = false)
    {
        string directory = Directory.CreateTempSubdirectory("InteropDispatchLimits_").FullName;
        try
        {
            string app = ProjectionWriterRunner.CompileSources([source], Path.Combine(directory, "DispatchLimits.dll"),
                outputKind: OutputKind.ConsoleApplication);
            (int exitCode, string log) = RunGenerator(directory, app, treatWarningsAsErrors: treatWarningsAsErrors);
            StringAssert.Contains(log, warning);
            Assert.AreEqual(treatWarningsAsErrors, exitCode != 0, log);
            string interop = Path.Combine(directory, "WinRT.Interop.dll");
            Assert.AreEqual(!treatWarningsAsErrors, File.Exists(interop), log);

            if (verifyDeterminism)
            {
                Assert.IsTrue(GetComWrappersTypeAssociations(interop).Any(type =>
                    type.StartsWith("DispatchLimits.Node`1[[System.String,", StringComparison.Ordinal)), log);
                byte[] original = File.ReadAllBytes(interop);
                (exitCode, log) = RunGenerator(directory, app, maxDegreesOfParallelism: 4);
                Assert.AreEqual(0, exitCode, log);
                CollectionAssert.AreEqual(original, File.ReadAllBytes(interop), "Dispatch cutoff ordering must be stable.");
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
