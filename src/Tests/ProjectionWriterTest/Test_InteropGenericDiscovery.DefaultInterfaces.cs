// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using AsmResolver.DotNet;
using Microsoft.CodeAnalysis;
using ProjectionWriterTest.Helpers;

namespace ProjectionWriterTest;

/// <inheritdoc cref="Test_InteropGenericDiscovery"/>
public partial class Test_InteropGenericDiscovery
{
    [TestMethod]
    public void DefaultInterfaceBody_NonVirtualClassMethodDoesNotImplementSlot()
    {
        string directory = Directory.CreateTempSubdirectory("InteropDefaultNonVirtual_").FullName;

        try
        {
            string source = CreateDispatchSource("""
                public interface IFactory { bool Create(); }
                public interface IFactory<T> : IFactory
                {
                    bool IFactory.Create() => Build<T>();
                }
                public sealed class Outer<T> : IFactory<T>
                {
                    public bool Create() => false;
                }
                """, "_ = ((IFactory)new Outer<Marker>()).Create();", "");
            string app = ProjectionWriterRunner.CompileSources([source], Path.Combine(directory, "Dispatch.dll"),
                outputKind: OutputKind.ConsoleApplication);
            ModuleDefinition module = ModuleDefinition.FromBytes(File.ReadAllBytes(app));
            TypeDefinition receiver = module.TopLevelTypes.Single(static type => type.Name == "Outer`1");
            MethodDefinition method = receiver.Methods.Single(static method => method.Name == "Create");

            // C# emits an implicit implementation as virtual; metadata can instead expose an unrelated ordinary method
            Assert.IsTrue(method.IsVirtual);
            Assert.IsEmpty(receiver.MethodImplementations);

            method.IsVirtual = false;
            method.IsFinal = false;
            method.IsNewSlot = false;

            module.Write(app);

            (int exitCode, string log) = RunGenerator(directory, app, treatWarningsAsErrors: true);

            Assert.AreEqual(0, exitCode, log);
            Assert.IsTrue(GetComWrappersTypeAssociations(Path.Combine(directory, "WinRT.Interop.dll"))
                .Any(static type => type.StartsWith("Dispatch.Node`1[[Dispatch.Marker,", StringComparison.Ordinal)), log);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void ConstructedReceiver_DefaultInterfaceBody_ClosesNonGenericSlot()
    {
        AssertDispatchSource("""
            using System.Collections.Generic;
            using Windows.Foundation;
            using WindowsRuntime.InteropServices;

            object value = ((IFactory)new Factory<long>()).Create();

            unsafe
            {
                void* p = WindowsRuntimeMarshal.ConvertToUnmanaged(value);
                WindowsRuntimeMarshal.Free(p);
            }

            interface IFactory { object Create(); }
            interface IFactory<T> : IFactory
            {
                object IFactory.Create() => new List<IAsyncOperation<T>>();
            }
            sealed class Factory<T> : IFactory<T>;
            """, "System.Collections.Generic.List`1[[Windows.Foundation.IAsyncOperation`1[[System.Int64,");
    }

    [TestMethod]
    public void DefaultInterfaceBody_StaticFieldCacheRetainsOrdinaryTypeExpansion()
    {
        AssertDispatchSource("""
            using System.Collections.Generic;
            using Windows.Foundation;

            _ = ((IFactory)new Factory<long>()).Create();

            interface IFactory { object Create(); }
            interface IFactory<T> : IFactory
            {
                private static readonly object Value = new List<IAsyncOperation<T>>();
                object IFactory.Create() => Value;
            }
            sealed class Factory<T> : IFactory<T>;
            """, "System.Collections.Generic.List`1[[Windows.Foundation.IAsyncOperation`1[[System.Int64,");
    }

    [TestMethod]
    public void DefaultInterfaceBody_ComposesInheritedPermutedArguments()
    {
        AssertDispatchNode("""
            public interface IFactory { bool Create(); }
            public interface IFactory<X, Y> : IFactory
            {
                bool IFactory.Create() => Build<X, Y>();
            }
            public interface IMiddle<X, Y> : IFactory<Y[], X>;
            public class Base<X, Y> : IMiddle<Y, X>;
            public sealed class Outer<X, Y> : Base<Y, X>;
            """, "_ = ((IFactory)new Outer<First, Second>()).Create();",
            associationPrefix: "Dispatch.Node`1[[Dispatch.Pair`2[[Dispatch.Second[],",
            associationContains: "],[Dispatch.First,");
    }

    [TestMethod]
    public void DefaultInterfaceBody_ComposesNestedReceiverArguments()
    {
        AssertDispatchNode("""
            public interface IFactory { bool Create(); }
            public interface IFactory<X, Y> : IFactory
            {
                bool IFactory.Create() => Build<X, Y>();
            }
            public class Container<X>
            {
                public sealed class Outer<Y> : IFactory<X, Y>;
            }
            """, "_ = ((IFactory)new Container<First>.Outer<Second>()).Create();",
            associationPrefix: "Dispatch.Node`1[[Dispatch.Pair`2[[Dispatch.First,",
            associationContains: "],[Dispatch.Second,");
    }

    [TestMethod]
    public void DefaultInterfaceBody_FollowsOrdinaryAndGenericHelpers()
    {
        AssertDispatchNode("""
            public interface IFactory { bool Create(); }
            public interface IFactory<T> : IFactory
            {
                bool IFactory.Create() => Forward();
                private bool Forward() => Build<T>();
            }
            public sealed class Outer<T> : IFactory<T>;
            """, "_ = ((IFactory)new Outer<Marker>()).Create();");
    }

    [TestMethod]
    public void DefaultInterfaceBody_GenericMethodControlComposesReceiverAndMethodArguments()
    {
        // Control: the existing 'MethodSpec' path already closes generic default methods
        AssertDispatchNode("""
            public interface IFactory { bool Create<T>(); }
            public interface IFactory<X> : IFactory
            {
                bool IFactory.Create<T>() => Build<X, T>();
            }
            public sealed class Outer<X> : IFactory<X[]>;
            """, "_ = ((IFactory)new Outer<First>()).Create<Second>();",
            associationPrefix: "Dispatch.Node`1[[Dispatch.Pair`2[[Dispatch.First[],",
            associationContains: "],[Dispatch.Second,");
    }

    [TestMethod]
    [DataRow("public bool Create() => false;")]
    [DataRow("bool IFactory.Create() => false;")]
    public void DefaultInterfaceBody_ClassImplementationSuppressesDefault(string implementation)
    {
        AssertDispatchNode($$"""
            public interface IFactory { bool Create(); }
            public interface IFactory<T> : IFactory
            {
                bool IFactory.Create() => Build<T>();
            }
            public sealed class Outer<T> : IFactory<T>
            {
                {{implementation}}
            }
            """, "_ = ((IFactory)new Outer<Marker>()).Create();", expected: false);
    }

    [TestMethod]
    public void DefaultInterfaceBody_MostSpecificOverrideSuppressesBase()
    {
        AssertDispatchNode("""
            public interface IFactory { bool Create(); }
            public interface IFactory<T> : IFactory
            {
                bool IFactory.Create() => Build<T>();
            }
            public interface IDerived<T> : IFactory<T>
            {
                bool IFactory.Create() => false;
            }
            public sealed class Outer<T> : IDerived<T>;
            """, "_ = ((IFactory)new Outer<Marker>()).Create();", expected: false);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void DefaultInterfaceBody_MoreSpecificBodyOverridesReabstraction(bool producesNode)
    {
        AssertDispatchNode($$"""
            public interface IFactory { bool Create(); }
            public interface IFactory<T> : IFactory
            {
                bool IFactory.Create() => Build<T>();
            }
            public interface IDerived<T> : IFactory<T>
            {
                abstract bool IFactory.Create();
            }
            public interface IMore<T> : IDerived<T>
            {
                bool IFactory.Create() => {{(producesNode ? "Build<T>()" : "false")}};
            }
            public sealed class Outer<T> : IMore<T>;
            """, "_ = ((IFactory)new Outer<Marker>()).Create();", expected: producesNode);
    }

    [TestMethod]
    public void DefaultInterfaceBody_DoesNotScanUnrelatedOrStaticMembers()
    {
        AssertDispatchNode("""
            public interface IFactory { bool Create(); }
            public interface IFactory<T> : IFactory
            {
                bool IFactory.Create() => false;
                private bool Unrelated() => Build<T>();
                public static bool Static() => Build<T>();
            }
            public sealed class Outer<T> : IFactory<T>;
            """, "_ = ((IFactory)new Outer<Marker>()).Create();", expected: false);
    }

    [TestMethod]
    public void DefaultInterfaceBody_DoesNotSynthesizeReceiverArguments()
    {
        AssertDispatchNode("""
            public interface IFactory { bool Create(); }
            public interface IFactory<T> : IFactory
            {
                bool IFactory.Create() => Build<T>();
            }
            public sealed class Outer<T> : IFactory<T>;
            """, "_ = ((IFactory)null!).Create();", expected: false);
    }

    [TestMethod]
    public void DefaultInterfaceBody_NewSlotDoesNotSuppressInheritedSlot()
    {
        AssertDispatchNode("""
            public interface IFactory { bool Create(); }
            public interface IFactory<T> : IFactory
            {
                bool IFactory.Create() => Build<T>();
            }
            public interface IDerived<T> : IFactory<T>
            {
                new bool Create() => false;
            }
            public sealed class Outer<T> : IDerived<T>;
            """, "_ = ((IFactory)new Outer<Marker>()).Create();");
    }

    [TestMethod]
    public void DefaultInterfaceBody_ClosesSzArray()
    {
        AssertDispatchNode("""
            public interface IFactory { object Create(); }
            public interface IFactory<T> : IFactory
            {
                object IFactory.Create() => new T[1];
            }
            public sealed class Outer<T> : IFactory<T>;
            """, "_ = ((IFactory)new Outer<Marker>()).Create();",
            associationPrefix: "Dispatch.Marker[],");
    }

    [TestMethod]
    public void DefaultInterfaceBody_NewReceiverRunsToFixpoint()
    {
        AssertDispatchNode("""
            public interface IFactory { object Create(); }
            public interface IFirst<T> : IFactory
            {
                object IFactory.Create() => new Second<T>();
            }
            public interface ISecond<T> : IFactory
            {
                object IFactory.Create() => new Node<T>();
            }
            public sealed class Outer<T> : IFirst<T>;
            public sealed class Second<T> : ISecond<T>;
            """, "_ = ((IFactory)new Outer<Marker>()).Create();");
    }

    [TestMethod]
    public void DefaultInterfaceBody_VariantSlotKeepsActualReceiverArguments()
    {
        AssertDispatchNode("""
            public interface IFactory<out T> { object Create(); }
            public interface IDerived<T> : IFactory<T>
            {
                object IFactory<T>.Create() => new Node<T>();
            }
            public sealed class Outer<T> : IDerived<T>;
            """, "_ = ((IFactory<object>)new Outer<string>()).Create();",
            associationPrefix: "Dispatch.Node`1[[System.String,");
    }

    [TestMethod]
    public void DefaultInterfaceBody_InheritedClassImplementationSuppressesDefault()
    {
        AssertDispatchNode("""
            public interface IFactory { bool Create(); }
            public interface IFactory<T> : IFactory
            {
                bool IFactory.Create() => Build<T>();
            }
            public class Base<T>
            {
                public bool Create() => false;
            }
            public sealed class Outer<T> : Base<T>, IFactory<T>;
            """, "_ = ((IFactory)new Outer<Marker>()).Create();", expected: false);
    }

    [TestMethod]
    public void DefaultInterfaceBody_ReceiverFirstObservedInsideGenericCallChain()
    {
        AssertDispatchNode("""
            public interface IFactory { bool Create(); }
            public interface IFactory<T> : IFactory
            {
                bool IFactory.Create() => Build<T>();
            }
            public sealed class Outer<T> : IFactory<T>;
            """, "_ = Forward<Marker>();", helpers: """
            private static IFactory Forward<T>() => Create<T>();
            private static IFactory Create<T>() => new Outer<T>();
            """);
    }
}
