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
    [DataRow("independent")]
    [DataRow("interface-array")]
    [DataRow("fixed-interface")]
    [DataRow("repeated")]
    [DataRow("nested")]
    [DataRow("inherited")]
    public void GenericDispatch_ComposesReceiverAndMethodArguments(string scenario)
    {
        (string declaration, string caller) = scenario switch
        {
            "independent" => ("public interface IOuter { bool Check<T>(); } public sealed class Outer<U> : IOuter { public bool Check<T>() => Build<U,T>(); }",
                "((IOuter)new Outer<First>()).Check<Second>()"),
            "interface-array" => ("public interface IOuter<V> { bool Check<T>(); } public sealed class Outer<U> : IOuter<U[]> { public bool Check<T>() => Build<U,T>(); }",
                "((IOuter<First[]>)new Outer<First>()).Check<Second>()"),
            "fixed-interface" => ("public interface IOuter<V> { bool Check<T>(); } public sealed class Outer<U> : IOuter<string> { public bool Check<T>() => Build<U,T>(); }",
                "((IOuter<string>)new Outer<First>()).Check<Second>()"),
            "repeated" => ("public interface IOuter<V> { bool Check<T>(); } public sealed class Outer<U> : IOuter<Pair<U,U>> { public bool Check<T>() => Build<U,T>(); }",
                "((IOuter<Pair<First,First>>)new Outer<First>()).Check<Second>()"),
            "nested" => ("public interface IOuter { bool Check<T>(); } public class Container<X> { public sealed class Outer<U> : IOuter { public bool Check<T>() => Build<X,T>(); } }",
                "((IOuter)new Container<First>.Outer<string>()).Check<Second>()"),
            "inherited" => ("public interface IOuter { bool Check<T>(); } public class Base<U> { public bool Check<T>() => Build<U,T>(); } public sealed class Outer<X> : Base<X[]>, IOuter;",
                "((IOuter)new Outer<First>()).Check<Second>()"),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };
        AssertDispatchSource($$"""
            using Windows.Foundation;
            using static Binding.Program;
            namespace Binding;
            public struct First;
            public struct Second;
            public sealed class Pair<X,Y>;
            public sealed class Node<T> : IStringable { public override string ToString() => "node"; }
            {{declaration}}
            public static class Program
            {
                public static void Main() => _ = {{caller}};
                public static bool Build<U,T>() { _ = new Node<Pair<U,T>>(); return true; }
            }
            """, scenario == "inherited"
                ? "Binding.Node`1[[Binding.Pair`2[[Binding.First[],"
                : "Binding.Node`1[[Binding.Pair`2[[Binding.First,");
    }

    [TestMethod]
    public void GenericDispatch_RepeatedReceiverParametersMustAgree()
    {
        AssertDispatchSource("""
            using Windows.Foundation;
            namespace Binding;
            public struct First;
            public struct Second;
            public struct Third;
            public sealed class Pair<X,Y>;
            public sealed class Node<T> : IStringable { public override string ToString() => "node"; }
            public interface IOuter<V> { bool Check<T>(); }
            public sealed class Outer<U> : IOuter<Pair<U,U>>
            {
                public bool Check<T>() { _ = new Node<Pair<U,T>>(); return true; }
            }
            public static class Program
            {
                public static void Main()
                {
                    _ = new Outer<First>();
                    _ = ((IOuter<Pair<First,Second>>)null!).Check<Third>();
                }
            }
            """, "Binding.Node`1[[Binding.Pair`2[[Binding.First,", expected: false);
    }

    [TestMethod]
    public void GenericDispatch_CovariantMethodImplRetainsFurtherOverrides()
    {
        AssertDispatchSource("""
            using Windows.Foundation;
            namespace Binding;
            public struct First;
            public struct Second;
            public sealed class Pair<X,Y>;
            public sealed class Node<T> : IStringable { public override string ToString() => "node"; }
            public class Base { public virtual object Create<T>() => new object(); }
            public class Middle : Base { public override IStringable Create<T>() => null!; }
            public sealed class Outer : Middle
            {
                public override IStringable Create<T>() => new Node<Pair<First,T>>();
            }
            public static class Program
            {
                public static void Main() => _ = ((Base)new Outer()).Create<Second>();
            }
            """, "Binding.Node`1[[Binding.Pair`2[[Binding.First,");
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void GenericDispatch_BindsFullSignaturesIncludingModifiers(bool overload)
    {
        AssertDispatchSource($$"""
            using Windows.Foundation;
            namespace Binding;
            public struct Marker;
            public sealed class Node<T> : IStringable { public override string ToString() => "node"; }
            public interface IOuter { bool Check<T>({{(overload ? "int" : "in int")}} value); }
            public sealed class Outer : IOuter
            {
                public bool Check<T>({{(overload ? "int" : "in int")}} value) {{(overload ? "=> false;" : "{ _ = new Node<T>(); return true; }")}}
                {{(overload ? "public bool Check<T>(string value) { _ = new Node<T>(); return true; }" : "")}}
            }
            public static class Program
            {
                public static void Main() => _ = ((IOuter)new Outer()).Check<Marker>(42);
            }
            """, "Binding.Node`1[[Binding.Marker,", expected: !overload);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void GenericDispatch_ClosesSzArraysInImplementationBodies(bool declaringArgument)
    {
        AssertDispatchSource($$"""
            using Windows.Foundation;
            namespace Binding;
            public struct First;
            public struct Second;
            public sealed class Node<T> : IStringable { public override string ToString() => "node"; }
            public interface IOuter { object Create<T>(); }
            public sealed class Outer<U> : IOuter
            {
                public object Create<T>() => new {{(declaringArgument ? "U" : "T")}}[1];
            }
            public static class Program
            {
                public static void Main()
                {
                    _ = new Node<int>();
                    _ = ((IOuter)new Outer<First>()).Create<Second>();
                }
            }
            """, declaringArgument ? "Binding.First[]," : "Binding.Second[],");
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
                public sealed class Pair<U,T>;
                public sealed class Node<T> : IStringable { public override string ToString() => "node"; }
                public interface IOuter { bool Check<T>(); }
                """], Path.Combine(directory, "Contracts.dll"));
            string implementation = ProjectionWriterRunner.CompileSources(
                [$$"""
                namespace Across;
                public sealed class Outer<U> : IOuter
                {
                    public bool Check<T>()
                    {
                        {{(fixpoint ? "_ = new Next<U>(); return ((IOuter)new Next<U>()).Check<T>();" : "_ = new Node<Pair<U,T>>(); return true;")}}
                    }

                }
                public sealed class Next<U> : IOuter
                {
                    public bool Check<T>() { _ = new Node<Pair<U,T>>(); return true; }
                }
                """], Path.Combine(directory, "Implementation.dll"), additionalReferences: [contracts]);
            string producer = ProjectionWriterRunner.CompileSources(
                ["namespace Across; public static class Producer { public static IOuter Create() => new Outer<First>(); }"],
                Path.Combine(directory, "Producer.dll"), additionalReferences: [contracts, implementation]);
            string caller = ProjectionWriterRunner.CompileSources(
                ["namespace Across; public static class Caller { public static bool Invoke<T>(IOuter value) => value.Check<T>(); }"],
                Path.Combine(directory, "Caller.dll"), additionalReferences: [contracts]);
            string app = ProjectionWriterRunner.CompileSources(
                ["public static class Program { public static void Main() => _ = Across.Caller.Invoke<Across.Second>(Across.Producer.Create()); }"],
                Path.Combine(directory, "App.dll"), outputKind: OutputKind.ConsoleApplication,
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
}
