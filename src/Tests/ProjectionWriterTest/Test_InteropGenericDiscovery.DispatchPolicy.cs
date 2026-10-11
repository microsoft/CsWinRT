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
    public void GenericDispatch_VarianceKeepsActuallyObservedReceiverArguments(bool genericReceiver)
    {
        AssertDispatchSource($$"""
            using Windows.Foundation;
            namespace Binding;
            public struct Marker;
            public sealed class Pair<U,T>;
            public sealed class Node<T> : IStringable { public override string ToString() => "node"; }
            public interface IOuter<out U> { bool Check<T>(); }
            public sealed class Outer{{(genericReceiver ? "<U>" : "")}} : IOuter<{{(genericReceiver ? "U" : "string")}}>
            {
                public bool Check<T>() { _ = new Node<Pair<{{(genericReceiver ? "U" : "string")}},T>>(); return true; }
            }
            public static class Program
            {
                public static void Main() => _ = ((IOuter<object>)new Outer{{(genericReceiver ? "<string>" : "")}}()).Check<Marker>();
            }
            """, "Binding.Node`1[[Binding.Pair`2[[System.String,");
    }

    [TestMethod]
    public void GenericDispatch_ConstrainedSealedReceiverDoesNotExpandSibling()
    {
        AssertDispatchSource("""
            using Windows.Foundation;
            namespace Binding;
            public struct Marker;
            public sealed class Node<T> : IStringable { public override string ToString() => "node"; }
            public sealed class SiblingNode<T> : IStringable { public override string ToString() => "sibling"; }
            public interface IOuter { bool Check<T>(); }
            public sealed class Outer : IOuter { public bool Check<T>() { _ = new Node<T>(); return true; } }
            public sealed class Sibling : IOuter { public bool Check<T>() { _ = new SiblingNode<T>(); return true; } }
            public static class Program
            {
                public static void Main() => _ = Invoke<Outer>(new Outer());
                private static bool Invoke<T>(T value) where T : IOuter => value.Check<Marker>();
            }
            """, "Binding.SiblingNode`1[[Binding.Marker,", expected: false);
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
                ["namespace Policy; public struct Marker; public interface IOuter { object Create<T>(); }"],
                Path.Combine(directory, "Contracts.dll"));
            string implementation = ProjectionWriterRunner.CompileSources(
                ["namespace Policy; public sealed class Outer : IOuter { public object Create<T>() => new T[1]; }"],
                Path.Combine(directory, "Foreign.dll"), additionalReferences: [contracts]);
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
                    .. optOut ? new[] { "--marshalling-disabled-assembly-names Foreign" } : []
                ]);
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
