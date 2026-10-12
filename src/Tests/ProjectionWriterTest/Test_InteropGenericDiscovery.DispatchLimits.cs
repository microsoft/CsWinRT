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
