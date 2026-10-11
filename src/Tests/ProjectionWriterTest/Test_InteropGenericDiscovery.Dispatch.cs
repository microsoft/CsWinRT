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
    [DataRow("interface")]
    [DataRow("explicit")]
    [DataRow("abstract")]
    [DataRow("virtual")]
    [DataRow("sealed")]
    [DataRow("multilevel")]
    [DataRow("inherited")]
    [DataRow("default")]
    [DataRow("default-override")]
    [DataRow("delegate")]
    [DataRow("constrained")]
    [DataRow("static-abstract")]
    [DataRow("static-delegate")]
    public void GenericDispatch_ClosesImplementationBody(string scenario)
    {
        (string declaration, string caller) = scenario switch
        {
            "interface" => ("public interface IOuter { bool Check<T>(); } public sealed class Outer : IOuter { public bool Check<T>() => Build<T>(); }",
                "((IOuter)new Outer()).Check<Marker>()"),
            "explicit" => ("public interface IOuter { bool Check<T>(); } public sealed class Outer : IOuter { bool IOuter.Check<T>() => Build<T>(); }",
                "((IOuter)new Outer()).Check<Marker>()"),
            "abstract" => ("public abstract class Base { public abstract bool Check<T>(); } public sealed class Outer : Base { public override bool Check<T>() => Build<T>(); }",
                "((Base)new Outer()).Check<Marker>()"),
            "virtual" or "sealed" => ("public class Base { public virtual bool Check<T>() => false; } public sealed class Outer : Base { public override bool Check<T>() => Build<T>(); }",
                scenario == "sealed" ? "new Outer().Check<Marker>()" : "((Base)new Outer()).Check<Marker>()"),
            "multilevel" => ("public class Base { public virtual bool Check<T>() => false; } public class Middle : Base { public override bool Check<T>() => false; } public sealed class Outer : Middle { public override bool Check<T>() => Build<T>(); }",
                "((Base)new Outer()).Check<Marker>()"),
            "inherited" => ("public interface IOuter { bool Check<T>(); } public class Base { public bool Check<T>() => Build<T>(); } public sealed class Outer : Base, IOuter;",
                "((IOuter)new Outer()).Check<Marker>()"),
            "default" => ("public interface IOuter { bool Check<T>() => Build<T>(); } public sealed class Outer : IOuter;",
                "((IOuter)new Outer()).Check<Marker>()"),
            "default-override" => ("public interface IOuter { bool Check<T>() => false; } public interface IDerived : IOuter { bool IOuter.Check<T>() => Build<T>(); } public sealed class Outer : IDerived;",
                "((IOuter)new Outer()).Check<Marker>()"),
            "delegate" => ("public class Base { public virtual bool Check<T>() => false; } public sealed class Outer : Base { public override bool Check<T>() => Build<T>(); }",
                "new System.Func<bool>(((Base)new Outer()).Check<Marker>)()"),
            "constrained" => ("public interface IOuter { bool Check<T>(); } public struct Outer : IOuter { public bool Check<T>() => Build<T>(); }",
                "Constrained<Outer>()"),
            "static-abstract" or "static-delegate" => ("public interface IOuter { static abstract bool Check<T>(); } public struct Outer : IOuter { public static bool Check<T>() => Build<T>(); }",
                "Static<Outer>()"),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };
        string source = $$"""
            using Windows.Foundation;
            using static Dispatch.Program;
            namespace Dispatch;
            public struct Marker;
            public sealed class Node<T> : IStringable { public override string ToString() => "node"; }
            {{declaration}}
            public static class Program
            {
                public static void Main() => _ = {{caller}};
                public static bool Build<T>() { _ = new Node<T>(); return true; }
                {{(scenario == "constrained" ? "private static bool Constrained<T>() where T : IOuter => default(T)!.Check<Marker>();" : "")}}
                {{(scenario == "static-abstract" ? "private static bool Static<T>() where T : IOuter => T.Check<Marker>();" : "")}}
                {{(scenario == "static-delegate" ? "private static bool Static<T>() where T : IOuter => new System.Func<bool>(T.Check<Marker>)();" : "")}}
            }
            """;
        AssertDispatchSource(source, "Dispatch.Node`1[[Dispatch.Marker,");
    }

    [TestMethod]
    [DataRow("newslot")]
    [DataRow("base-call")]
    [DataRow("nonvirtual")]
    public void NondispatchCalls_DoNotVisitUnrelatedBodies(string scenario)
    {
        string declaration = scenario switch
        {
            "newslot" => "public class Base { public virtual bool Check<T>() => false; } public sealed class Outer : Base { public new virtual bool Check<T>() => Build<T>(); }",
            "base-call" => "public class Base { public virtual bool Check<T>() => false; } public class Outer : Base { public override bool Check<T>() => Build<T>(); public bool Invoke<T>() => base.Check<T>(); }",
            "nonvirtual" => "public class Base { public bool Check<T>() => false; } public sealed class Outer : Base { public new bool Check<T>() => Build<T>(); }",
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };
        // A new virtual method cannot be declared on a sealed class.
        declaration = declaration.Replace("sealed class Outer", "class Outer", StringComparison.Ordinal);
        string caller = scenario == "base-call" ? "new Outer().Invoke<Marker>()" : "((Base)new Outer()).Check<Marker>()";
        AssertDispatchSource($$"""
            using Windows.Foundation;
            using static Dispatch.Program;
            namespace Dispatch;
            public struct Marker;
            public sealed class Node<T> : IStringable { public override string ToString() => "node"; }
            {{declaration}}
            public static class Program
            {
                public static void Main() => _ = {{caller}};
                public static bool Build<T>() { _ = new Node<T>(); return true; }
            }
            """, "Dispatch.Node`1[[Dispatch.Marker,", expected: false);
    }

    private static void AssertDispatchSource(string source, string associationPrefix, bool expected = true)
    {
        string directory = Directory.CreateTempSubdirectory("InteropDispatchTest_").FullName;
        try
        {
            string app = ProjectionWriterRunner.CompileSources([source], Path.Combine(directory, "Dispatch.dll"),
                outputKind: OutputKind.ConsoleApplication);
            (int exitCode, string log) = RunGenerator(directory, app, treatWarningsAsErrors: true);
            Assert.AreEqual(0, exitCode, log);
            Assert.AreEqual(expected, GetComWrappersTypeAssociations(Path.Combine(directory, "WinRT.Interop.dll"))
                .Any(type => type.StartsWith(associationPrefix, StringComparison.Ordinal)), log);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
