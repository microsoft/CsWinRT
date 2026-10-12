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
    public void GenericInterfaceDispatch_ClosesImplementationBody()
    {
        AssertDispatchNode("""
            public interface IOuter { bool Check<T>(); }
            public sealed class Outer : IOuter
            {
                public bool Check<T>() => Build<T>();
            }
            """, "_ = ((IOuter)new Outer()).Check<Marker>();");
    }

    [TestMethod]
    public void ExplicitInterfaceDispatch_ClosesImplementationBody()
    {
        AssertDispatchNode("""
            public interface IOuter { bool Check<T>(); }
            public sealed class Outer : IOuter
            {
                bool IOuter.Check<T>() => Build<T>();
            }
            """, "_ = ((IOuter)new Outer()).Check<Marker>();");
    }

    [TestMethod]
    public void AbstractDispatch_ClosesImplementationBody()
    {
        AssertDispatchNode("""
            public abstract class Base { public abstract bool Check<T>(); }
            public sealed class Outer : Base
            {
                public override bool Check<T>() => Build<T>();
            }
            """, "_ = ((Base)new Outer()).Check<Marker>();");
    }

    [TestMethod]
    public void VirtualDispatch_ClosesImplementationBody()
    {
        AssertDispatchNode("""
            public class Base { public virtual bool Check<T>() => false; }
            public sealed class Outer : Base
            {
                public override bool Check<T>() => Build<T>();
            }
            """, "_ = ((Base)new Outer()).Check<Marker>();");
    }

    [TestMethod]
    public void SealedOverrideCall_ClosesImplementationBody()
    {
        AssertDispatchNode("""
            public class Base { public virtual bool Check<T>() => false; }
            public sealed class Outer : Base
            {
                public override bool Check<T>() => Build<T>();
            }
            """, "_ = new Outer().Check<Marker>();");
    }

    [TestMethod]
    public void MultilevelVirtualDispatch_ClosesImplementationBody()
    {
        AssertDispatchNode("""
            public class Base { public virtual bool Check<T>() => false; }
            public class Middle : Base { public override bool Check<T>() => false; }
            public sealed class Outer : Middle
            {
                public override bool Check<T>() => Build<T>();
            }
            """, "_ = ((Base)new Outer()).Check<Marker>();");
    }

    [TestMethod]
    public void InheritedInterfaceDispatch_ClosesImplementationBody()
    {
        AssertDispatchNode("""
            public interface IOuter { bool Check<T>(); }
            public class Base
            {
                public bool Check<T>() => Build<T>();
            }
            public sealed class Outer : Base, IOuter;
            """, "_ = ((IOuter)new Outer()).Check<Marker>();");
    }

    [TestMethod]
    public void DefaultInterfaceDispatch_ClosesImplementationBody()
    {
        AssertDispatchNode("""
            public interface IOuter
            {
                bool Check<T>() => Build<T>();
            }
            public sealed class Outer : IOuter;
            """, "_ = ((IOuter)new Outer()).Check<Marker>();");
    }

    [TestMethod]
    public void DefaultInterfaceOverride_ClosesImplementationBody()
    {
        AssertDispatchNode("""
            public interface IOuter { bool Check<T>() => false; }
            public interface IDerived : IOuter
            {
                bool IOuter.Check<T>() => Build<T>();
            }
            public sealed class Outer : IDerived;
            """, "_ = ((IOuter)new Outer()).Check<Marker>();");
    }

    [TestMethod]
    public void VirtualDelegateTarget_ClosesImplementationBody()
    {
        AssertDispatchNode("""
            public class Base { public virtual bool Check<T>() => false; }
            public sealed class Outer : Base
            {
                public override bool Check<T>() => Build<T>();
            }
            """, "_ = new System.Func<bool>(((Base)new Outer()).Check<Marker>)();");
    }

    [TestMethod]
    public void ConstrainedValueTypeDispatch_ClosesImplementationBody()
    {
        AssertDispatchNode("""
            public interface IOuter { bool Check<T>(); }
            public struct Outer : IOuter
            {
                public bool Check<T>() => Build<T>();
            }
            """, "_ = Constrained<Outer>();", helpers: """
            private static bool Constrained<T>() where T : IOuter => default(T)!.Check<Marker>();
            """);
    }

    [TestMethod]
    public void StaticAbstractDispatch_ClosesImplementationBody()
    {
        AssertDispatchNode("""
            public interface IOuter { static abstract bool Check<T>(); }
            public struct Outer : IOuter
            {
                public static bool Check<T>() => Build<T>();
            }
            """, "_ = Static<Outer>();", helpers: """
            private static bool Static<T>() where T : IOuter => T.Check<Marker>();
            """);
    }

    [TestMethod]
    public void StaticAbstractDelegateTarget_ClosesImplementationBody()
    {
        AssertDispatchNode("""
            public interface IOuter { static abstract bool Check<T>(); }
            public struct Outer : IOuter
            {
                public static bool Check<T>() => Build<T>();
            }
            """, "_ = Static<Outer>();", helpers: """
            private static bool Static<T>() where T : IOuter => new System.Func<bool>(T.Check<Marker>)();
            """);
    }

    [TestMethod]
    public void NewVirtualSlot_DoesNotOverrideBaseSlot()
    {
        AssertDispatchNode("""
            public class Base { public virtual bool Check<T>() => false; }
            public class Outer : Base
            {
                public new virtual bool Check<T>() => Build<T>();
            }
            """, "_ = ((Base)new Outer()).Check<Marker>();", expected: false);
    }

    [TestMethod]
    public void ExplicitBaseCall_DoesNotDispatchToOverride()
    {
        AssertDispatchNode("""
            public class Base { public virtual bool Check<T>() => false; }
            public class Outer : Base
            {
                public override bool Check<T>() => Build<T>();
                public bool Invoke<T>() => base.Check<T>();
            }
            """, "_ = new Outer().Invoke<Marker>();", expected: false);
    }

    [TestMethod]
    public void NonvirtualCall_DoesNotVisitHiddenMethod()
    {
        AssertDispatchNode("""
            public class Base { public bool Check<T>() => false; }
            public class Outer : Base
            {
                public new bool Check<T>() => Build<T>();
            }
            """, "_ = ((Base)new Outer()).Check<Marker>();", expected: false);
    }

    [TestMethod]
    public void IndependentReceiverAndMethodArguments_AreComposed()
    {
        AssertDispatchNode("""
            public interface IOuter { bool Check<T>(); }
            public sealed class Outer<U> : IOuter
            {
                public bool Check<T>() => Build<U, T>();
            }
            """, "_ = ((IOuter)new Outer<First>()).Check<Second>();", associationPrefix: "Dispatch.Node`1[[Dispatch.Pair`2[[Dispatch.First,");
    }

    [TestMethod]
    public void InterfaceArrayArgument_BindsReceiverElementType()
    {
        AssertDispatchNode("""
            public interface IOuter<V> { bool Check<T>(); }
            public sealed class Outer<U> : IOuter<U[]>
            {
                public bool Check<T>() => Build<U, T>();
            }
            """, "_ = ((IOuter<First[]>)new Outer<First>()).Check<Second>();", associationPrefix: "Dispatch.Node`1[[Dispatch.Pair`2[[Dispatch.First,");
    }

    [TestMethod]
    public void FixedInterfaceArgument_KeepsActualReceiverArguments()
    {
        AssertDispatchNode("""
            public interface IOuter<V> { bool Check<T>(); }
            public sealed class Outer<U> : IOuter<string>
            {
                public bool Check<T>() => Build<U, T>();
            }
            """, "_ = ((IOuter<string>)new Outer<First>()).Check<Second>();", associationPrefix: "Dispatch.Node`1[[Dispatch.Pair`2[[Dispatch.First,");
    }

    [TestMethod]
    public void RepeatedReceiverParameters_BindConsistently()
    {
        AssertDispatchNode("""
            public interface IOuter<V> { bool Check<T>(); }
            public sealed class Outer<U> : IOuter<Pair<U, U>>
            {
                public bool Check<T>() => Build<U, T>();
            }
            """, "_ = ((IOuter<Pair<First, First>>)new Outer<First>()).Check<Second>();", associationPrefix: "Dispatch.Node`1[[Dispatch.Pair`2[[Dispatch.First,");
    }

    [TestMethod]
    public void NestedReceiver_BindsContainingTypeArguments()
    {
        AssertDispatchNode("""
            public interface IOuter { bool Check<T>(); }
            public class Container<X>
            {
                public sealed class Outer<U> : IOuter
                {
                    public bool Check<T>() => Build<X, T>();
                }
            }
            """, "_ = ((IOuter)new Container<First>.Outer<string>()).Check<Second>();", associationPrefix: "Dispatch.Node`1[[Dispatch.Pair`2[[Dispatch.First,");
    }

    [TestMethod]
    public void InheritedImplementation_ComposesBaseArguments()
    {
        AssertDispatchNode("""
            public interface IOuter { bool Check<T>(); }
            public class Base<U>
            {
                public bool Check<T>() => Build<U, T>();
            }
            public sealed class Outer<X> : Base<X[]>, IOuter;
            """, "_ = ((IOuter)new Outer<First>()).Check<Second>();", associationPrefix: "Dispatch.Node`1[[Dispatch.Pair`2[[Dispatch.First[],");
    }

    [TestMethod]
    public void RepeatedReceiverParameters_RejectInconsistentArguments()
    {
        AssertDispatchNode("""
            public interface IOuter<V> { bool Check<T>(); }
            public sealed class Outer<U> : IOuter<Pair<U, U>>
            {
                public bool Check<T>() => Build<U, T>();
            }
            """, """
            _ = new Outer<First>();
            _ = ((IOuter<Pair<First, Second>>)null!).Check<Third>();
            """, associationPrefix: "Dispatch.Node`1[[Dispatch.Pair`2[[Dispatch.First,", expected: false);
    }

    [TestMethod]
    public void CovariantMethodImpl_RetainsFurtherOverrides()
    {
        AssertDispatchNode("""
            public class Base { public virtual object Create<T>() => new object(); }
            public class Middle : Base { public override IStringable Create<T>() => null!; }
            public sealed class Outer : Middle
            {
                public override IStringable Create<T>() => new Node<Pair<First, T>>();
            }
            """, "_ = ((Base)new Outer()).Create<Second>();", associationPrefix: "Dispatch.Node`1[[Dispatch.Pair`2[[Dispatch.First,");
    }

    [TestMethod]
    public void DispatchBinding_MatchesCustomModifiers()
    {
        AssertDispatchNode("""
            public interface IOuter { bool Check<T>(in int value); }
            public sealed class Outer : IOuter
            {
                public bool Check<T>(in int value) => Build<T>();
            }
            """, "_ = ((IOuter)new Outer()).Check<Marker>(42);");
    }

    [TestMethod]
    public void DispatchBinding_RejectsUnrelatedOverload()
    {
        AssertDispatchNode("""
            public interface IOuter { bool Check<T>(int value); }
            public sealed class Outer : IOuter
            {
                public bool Check<T>(int value) => false;
                public bool Check<T>(string value) => Build<T>();
            }
            """, "_ = ((IOuter)new Outer()).Check<Marker>(42);", expected: false);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void GenericDispatch_ClosesSzArraysInImplementationBodies(bool declaringArgument)
    {
        AssertDispatchNode($$"""
            public interface IOuter { object Create<T>(); }
            public sealed class Outer<U> : IOuter
            {
                public object Create<T>() => new {{(declaringArgument ? "U" : "T")}}[1];
            }
            """, """
            _ = new Node<int>();
            _ = ((IOuter)new Outer<First>()).Create<Second>();
            """, associationPrefix: declaringArgument ? "Dispatch.First[]," : "Dispatch.Second[],");
    }

    [TestMethod]
    public void VariantInterface_KeepsFixedImplementationArguments()
    {
        AssertDispatchNode("""
            public interface IOuter<out U> { bool Check<T>(); }
            public sealed class Outer : IOuter<string>
            {
                public bool Check<T>() => Build<string, T>();
            }
            """, "_ = ((IOuter<object>)new Outer()).Check<Marker>();", associationPrefix: "Dispatch.Node`1[[Dispatch.Pair`2[[System.String,");
    }

    [TestMethod]
    public void VariantInterface_KeepsActualGenericReceiverArguments()
    {
        AssertDispatchNode("""
            public interface IOuter<out U> { bool Check<T>(); }
            public sealed class Outer<U> : IOuter<U>
            {
                public bool Check<T>() => Build<U, T>();
            }
            """, "_ = ((IOuter<object>)new Outer<string>()).Check<Marker>();", associationPrefix: "Dispatch.Node`1[[Dispatch.Pair`2[[System.String,");
    }

    [TestMethod]
    public void ConstrainedSealedReceiver_DoesNotExpandSibling()
    {
        AssertDispatchNode("""
            public sealed class SiblingNode<T> : IStringable
            {
                public override string ToString() => "sibling";
            }
            public interface IOuter { bool Check<T>(); }
            public sealed class Outer : IOuter
            {
                public bool Check<T>() => Build<T>();
            }
            public sealed class Sibling : IOuter
            {
                public bool Check<T>()
                {
                    _ = new SiblingNode<T>();
                    return true;
                }
            }
            """, "_ = Invoke<Outer>(new Outer());", helpers: """
            private static bool Invoke<T>(T value) where T : IOuter => value.Check<Marker>();
            """, associationPrefix: "Dispatch.SiblingNode`1[[Dispatch.Marker,", expected: false);
    }

    private static void AssertDispatchNode(
        string declarations,
        string main,
        string helpers = "",
        string associationPrefix = "Dispatch.Node`1[[Dispatch.Marker,",
        bool expected = true)
    {
        AssertDispatchSource(CreateDispatchSource(declarations, main, helpers), associationPrefix, expected);
    }

    private static string CreateDispatchSource(string declarations, string main, string helpers)
    {
        return $$"""
            using Windows.Foundation;
            using static Dispatch.Program;
            namespace Dispatch;
            public struct Marker;
            public struct First;
            public struct Second;
            public struct Third;
            public sealed class Pair<U, T>;
            public sealed class Node<T> : IStringable
            {
                public override string ToString() => "node";
            }
            {{declarations}}
            public static class Program
            {
                public static void Main()
                {
                    {{main}}
                }
                public static bool Build<T>()
                {
                    _ = new Node<T>();
                    return true;
                }
                public static bool Build<U, T>()
                {
                    _ = new Node<Pair<U, T>>();
                    return true;
                }
                {{helpers}}
            }
            """;
    }

    private static void AssertDispatchSource(string source, string associationPrefix, bool expected = true)
    {
        string directory = Directory.CreateTempSubdirectory("InteropDispatchTest_").FullName;

        try
        {
            string app = ProjectionWriterRunner.CompileSources(
                sources: [source],
                assemblyPath: Path.Combine(directory, "Dispatch.dll"),
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
