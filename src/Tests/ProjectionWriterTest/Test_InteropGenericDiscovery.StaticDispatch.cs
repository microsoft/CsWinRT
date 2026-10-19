// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;
using Microsoft.CodeAnalysis;
using ProjectionWriterTest.Helpers;

namespace ProjectionWriterTest;

/// <inheritdoc cref="Test_InteropGenericDiscovery"/>
public partial class Test_InteropGenericDiscovery
{
    private const string StaticOperationList = "System.Collections.Generic.List`1[[Windows.Foundation.IAsyncOperation`1[[System.Int64,";
    private const string StaticProgressList = "System.Collections.Generic.List`1[[Windows.Foundation.IAsyncActionWithProgress`1[[System.Int64,";

    [TestMethod]
    [DataRow("class")]
    [DataRow("struct")]
    public void StaticDispatch_AbstractNonGenericClassBodyControl(string receiverKind)
    {
        // Control: the observed receiver's own methods already expose this constructed body
        AssertStaticDispatchSource($"abstract-nongeneric-{receiverKind}", $$"""
            public interface IFactory { static abstract object Create(); }
            public {{receiverKind}} Factory<T> : IFactory
            {
                public static object Create() => new List<IAsyncOperation<T>>();
            }
            """, "Invoke<Factory<long>>()", """
            private static object Invoke<TFactory>() where TFactory : IFactory => TFactory.Create();
            """);
    }

    [TestMethod]
    [DataRow("class")]
    [DataRow("struct")]
    public void StaticDispatch_AbstractGenericMethodControl(string receiverKind)
    {
        // Control: this body needs the existing constrained generic method dispatch path
        AssertStaticDispatchSource($"abstract-generic-{receiverKind}", $$"""
            public interface IFactory { static abstract object Create<T>(); }
            public {{receiverKind}} Factory : IFactory
            {
                public static object Create<T>() => new List<IAsyncOperation<T>>();
            }
            """, "Invoke<Factory>()", """
            private static object Invoke<TFactory>() where TFactory : IFactory => TFactory.Create<long>();
            """, genericSlot: true);
    }

    [TestMethod]
    [DataRow("class", false)]
    [DataRow("struct", false)]
    [DataRow("class", true)]
    [DataRow("struct", true)]
    public void StaticDispatch_ConstructedInterfaceDefault(string receiverKind, bool genericSlot)
    {
        string parameters = genericSlot ? "<TUnused>" : "";

        AssertStaticDispatchSource($"constructed-default-{receiverKind}-{genericSlot}", $$"""
            public interface IFactory { static abstract object Create{{parameters}}(); }
            public interface IFactory<T> : IFactory
            {
                static object IFactory.Create{{parameters}}() => new List<IAsyncOperation<T>>();
            }
            public {{receiverKind}} Factory<T> : IFactory<T>;
            """, "Invoke<Factory<long>>()", $$"""
            private static object Invoke<TFactory>() where TFactory : IFactory => TFactory.Create{{(genericSlot ? "<int>" : "")}}();
            """, genericSlot: genericSlot);
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public void StaticDispatch_ClassReplacementSelectsOnlyClassBody(bool genericSlot, bool explicitImplementation)
    {
        string parameters = genericSlot ? "<TUnused>" : "";
        string implementation = explicitImplementation ? "static object IFactory.Create" : "public static object Create";

        AssertStaticDispatchSource($"class-replacement-{genericSlot}-{explicitImplementation}", $$"""
            public interface IFactory { static abstract object Create{{parameters}}(); }
            public interface IFactory<T> : IFactory
            {
                static object IFactory.Create{{parameters}}() => new List<IAsyncOperation<T>>();
            }
            public sealed class Factory<T> : IFactory<T>
            {
                {{implementation}}{{parameters}}() => new List<IAsyncActionWithProgress<T>>();
            }
            """, "Invoke<Factory<long>>()", $$"""
            private static object Invoke<TFactory>() where TFactory : IFactory => TFactory.Create{{(genericSlot ? "<int>" : "")}}();
            """, genericSlot: genericSlot, expected: StaticProgressList, excluded: StaticOperationList);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void StaticDispatch_MoreSpecificInterfaceReplacement(bool genericSlot)
    {
        string parameters = genericSlot ? "<TUnused>" : "";

        AssertStaticDispatchSource($"interface-replacement-{genericSlot}", $$"""
            public interface IFactory { static abstract object Create{{parameters}}(); }
            public interface IFactory<T> : IFactory
            {
                static object IFactory.Create{{parameters}}() => new List<IAsyncOperation<T>>();
            }
            public interface IDerived<T> : IFactory<T>
            {
                static object IFactory.Create{{parameters}}() => new List<IAsyncActionWithProgress<T>>();
            }
            public sealed class Factory<T> : IDerived<T>;
            """, "Invoke<Factory<long>>()", $$"""
            private static object Invoke<TFactory>() where TFactory : IFactory => TFactory.Create{{(genericSlot ? "<int>" : "")}}();
            """, genericSlot: genericSlot, expected: StaticProgressList, excluded: StaticOperationList);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void StaticDispatch_ReabstractionAndFurtherDefault(bool genericSlot)
    {
        string parameters = genericSlot ? "<TUnused>" : "";

        AssertStaticDispatchSource($"reabstract-further-default-{genericSlot}", $$"""
            public interface IFactory { static abstract object Create{{parameters}}(); }
            public interface IFactory<T> : IFactory
            {
                static object IFactory.Create{{parameters}}() => new List<IAsyncOperation<T>>();
            }
            public interface IReabstract<T> : IFactory<T>
            {
                static abstract object IFactory.Create{{parameters}}();
            }
            public interface IDerived<T> : IReabstract<T>
            {
                static object IFactory.Create{{parameters}}() => new List<IAsyncActionWithProgress<T>>();
            }
            public sealed class Factory<T> : IDerived<T>;
            """, "Invoke<Factory<long>>()", $$"""
            private static object Invoke<TFactory>() where TFactory : IFactory => TFactory.Create{{(genericSlot ? "<int>" : "")}}();
            """, genericSlot: genericSlot, expected: StaticProgressList, excluded: StaticOperationList);
    }

    [TestMethod]
    public void StaticDispatch_GenericDeclaringInterfaceDefaultControl()
    {
        // Control: composing the call's declaring interface already provides a whole-type scan
        AssertStaticDispatchSource("generic-declaration-control", """
            public interface IFactory<T>
            {
                static virtual object Create() => new List<IAsyncOperation<T>>();
            }
            public sealed class Factory<T> : IFactory<T>;
            """, "Invoke<Factory<long>, long>()", """
            private static object Invoke<TFactory, T>() where TFactory : IFactory<T> => TFactory.Create();
            """);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void StaticDispatch_StaticVirtualDeclarationOverride(bool genericSlot)
    {
        string parameters = genericSlot ? "<TUnused>" : "";

        AssertStaticDispatchSource($"virtual-declaration-{genericSlot}", $$"""
            public interface IFactory { static virtual object Create{{parameters}}() => null!; }
            public interface IFactory<T> : IFactory
            {
                static object IFactory.Create{{parameters}}() => new List<IAsyncOperation<T>>();
            }
            public sealed class Factory<T> : IFactory<T>;
            """, "Invoke<Factory<long>>()", $$"""
            private static object Invoke<TFactory>() where TFactory : IFactory => TFactory.Create{{(genericSlot ? "<int>" : "")}}();
            """, genericSlot: genericSlot);
    }

    [TestMethod]
    public void StaticDispatch_DelegateUsesConstrainedFunctionPointer()
    {
        AssertStaticDispatchSource("default-delegate", """
            public interface IFactory { static abstract object Create(); }
            public interface IFactory<T> : IFactory
            {
                static object IFactory.Create() => new List<IAsyncOperation<T>>();
            }
            public sealed class Factory<T> : IFactory<T>;
            """, "Invoke<Factory<long>>()", """
            private static object Invoke<TFactory>() where TFactory : IFactory => new System.Func<object>(TFactory.Create)();
            """, delegateTarget: true);
    }

    [TestMethod]
    public void StaticDispatch_TypeParameterCaller()
    {
        AssertStaticDispatchSource("type-parameter-caller", """
            public interface IFactory { static abstract object Create(); }
            public interface IFactory<T> : IFactory
            {
                static object IFactory.Create() => new List<IAsyncOperation<T>>();
            }
            public sealed class Factory<T> : IFactory<T>;
            public sealed class Caller<TFactory> where TFactory : IFactory
            {
                public object Invoke() => TFactory.Create();
            }
            """, "new Caller<Factory<long>>().Invoke()", "");
    }

    [TestMethod]
    public void StaticDispatch_MultihopConstrainedCaller()
    {
        AssertStaticDispatchSource("multihop-caller", """
            public interface IFactory { static abstract object Create(); }
            public interface IFactory<T> : IFactory
            {
                static object IFactory.Create() => new List<IAsyncOperation<T>>();
            }
            public sealed class Factory<T> : IFactory<T>;
            """, "Invoke<Factory<long>>()", """
            private static object Invoke<TFactory>() where TFactory : IFactory => Forward<TFactory>();
            private static object Forward<TFactory>() where TFactory : IFactory => TFactory.Create();
            """);
    }

    [TestMethod]
    public void StaticDispatch_StaticPropertyGetter()
    {
        AssertStaticDispatchSource("property-getter", """
            public interface IFactory { static abstract object Value { get; } }
            public interface IFactory<T> : IFactory
            {
                static object IFactory.Value => new List<IAsyncOperation<T>>();
            }
            public sealed class Factory<T> : IFactory<T>;
            """, "Invoke<Factory<long>>()", """
            private static object Invoke<TFactory>() where TFactory : IFactory => TFactory.Value;
            """, expectedSlotName: "get_Value");
    }

    [TestMethod]
    public void StaticDispatch_EventAdderDoesNotSelectRemover()
    {
        AssertStaticDispatchSource("event-adder", """
            public interface IFactory { static abstract event System.Action Changed; }
            public interface IFactory<T> : IFactory
            {
                static event System.Action IFactory.Changed
                {
                    add => _ = new List<IAsyncOperation<T>>();
                    remove => _ = new List<IAsyncActionWithProgress<T>>();
                }
            }
            public sealed class Factory<T> : IFactory<T>;
            """, "Invoke<Factory<long>>()", """
            private static object Invoke<TFactory>() where TFactory : IFactory
            {
                TFactory.Changed += static () => { };
                return null!;
            }
            """, excluded: StaticProgressList, expectedSlotName: "add_Changed");
    }

    [TestMethod]
    public void StaticDispatch_ConversionOperatorMatchesReturnType()
    {
        AssertStaticDispatchSource("conversion-operator", """
            public interface IFactory<TSelf> where TSelf : IFactory<TSelf>
            {
                static abstract implicit operator int(TSelf value);
                static abstract implicit operator string(TSelf value);
            }
            public interface IDefault<TSelf, T> : IFactory<TSelf> where TSelf : IDefault<TSelf, T>
            {
                static implicit IFactory<TSelf>.operator int(TSelf value)
                {
                    _ = new List<IAsyncOperation<T>>();
                    return 0;
                }
                static implicit IFactory<TSelf>.operator string(TSelf value)
                {
                    _ = new List<IAsyncActionWithProgress<T>>();
                    return "";
                }
            }
            public struct Factory<T> : IDefault<Factory<T>, T>;
            """, "Invoke<Factory<long>>()", """
            private static object Invoke<TFactory>() where TFactory : IFactory<TFactory> => (int)default(TFactory)!;
            """, excluded: StaticProgressList, expectedSlotName: "op_Implicit");
    }

    [TestMethod]
    public void StaticDispatch_NestedPermutedReceiverArguments()
    {
        AssertStaticDispatchSource("nested-permuted-receiver", """
            public interface IFactory { static abstract object Create(); }
            public interface IFactory<X, Y> : IFactory
            {
                static object IFactory.Create() => new List<IAsyncOperationWithProgress<X, Y>>();
            }
            public interface IMiddle<X, Y> : IFactory<Y, X>;
            public class Base<X, Y> : IMiddle<Y, X>;
            public class Container<X>
            {
                public sealed class Factory<Y> : Base<X, Y>;
            }
            """, "Invoke<Container<long>.Factory<int>>()", """
            private static object Invoke<TFactory>() where TFactory : IFactory => TFactory.Create();
            """, expected: "System.Collections.Generic.List`1[[Windows.Foundation.IAsyncOperationWithProgress`2[[System.Int64,",
            expectedArgument: "],[System.Int32,");
    }

    [TestMethod]
    public void StaticDispatch_MultipleConstructedSlotsMatchExactly()
    {
        AssertStaticDispatchSource("multiple-constructed-slots", """
            public sealed class First<T>;
            public sealed class Second<T>;
            public interface IFactory<TSlot> { static abstract object Create(); }
            public interface IFirst<T> : IFactory<First<T>>
            {
                static object IFactory<First<T>>.Create() => new List<IAsyncOperation<T>>();
            }
            public interface ISecond<T> : IFactory<Second<T>>
            {
                static object IFactory<Second<T>>.Create() => new List<IAsyncActionWithProgress<T>>();
            }
            public sealed class Factory<T> : IFirst<T>, ISecond<T>;
            """, "Invoke<Factory<long>, long>()", """
            private static object Invoke<TFactory, T>() where TFactory : IFactory<First<T>> => TFactory.Create();
            """, excluded: StaticProgressList);
    }

    [TestMethod]
    public void StaticDispatch_ReceiverWithoutClosedDemandDoesNotRunDefault()
    {
        AssertStaticDispatchSource("receiver-without-demand", """
            public interface IFactory { static abstract object Create(); }
            public interface IFactory<T> : IFactory
            {
                static object IFactory.Create() => new List<IAsyncOperation<T>>();
            }
            public sealed class Factory<T> : IFactory<T>;
            """, "new Factory<long>()", """
            private static object Invoke<TFactory>() where TFactory : IFactory => TFactory.Create();
            """, expectedPresent: false);
    }

    [TestMethod]
    public void StaticDispatch_InterfaceReceiverDefaultControl()
    {
        // Control: the interface argument itself already supplies the constructed owner
        AssertStaticDispatchSource("interface-receiver-control", """
            public interface IFactory { static abstract object Create(); }
            public interface IFactory<T> : IFactory
            {
                static object IFactory.Create() => new List<IAsyncOperation<T>>();
            }
            """, "Invoke<IFactory<long>>()", """
            private static object Invoke<TFactory>() where TFactory : IFactory => TFactory.Create();
            """);
    }

    [TestMethod]
    public void StaticDispatch_GenericInterfaceReceiverNeedsMethodArguments()
    {
        AssertStaticDispatchSource("generic-interface-receiver", """
            public interface IFactory { static abstract object Create<TMethod>(); }
            public interface IFactory<T> : IFactory
            {
                static object IFactory.Create<TMethod>() => new List<IAsyncOperationWithProgress<T, TMethod>>();
            }
            """, "Invoke<IFactory<long>>()", """
            private static object Invoke<TFactory>() where TFactory : IFactory => TFactory.Create<int>();
            """, genericSlot: true,
            expected: "System.Collections.Generic.List`1[[Windows.Foundation.IAsyncOperationWithProgress`2[[System.Int64,",
            expectedArgument: "],[System.Int32,");
    }

    [TestMethod]
    public void StaticDispatch_NonGenericDerivedInterfaceControl()
    {
        // Control: the non-generic interface's closed parent is already an explicit 'TypeSpec' root
        AssertStaticDispatchSource("nongeneric-derived-interface-control", """
            public interface IFactory { static abstract object Create(); }
            public interface IFactory<T> : IFactory
            {
                static object IFactory.Create() => new List<IAsyncOperation<T>>();
            }
            public interface IConcrete : IFactory<long>;
            public sealed class Factory : IConcrete;
            """, "Invoke<Factory>()", """
            private static object Invoke<TFactory>() where TFactory : IFactory => TFactory.Create();
            """);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void StaticDispatch_InheritedGenericClassMappingUsesMethodImpl(bool reimplements)
    {
        AssertStaticDispatchSource($"inherited-class-control-{reimplements}", $$"""
            public interface IFactory { static abstract object Create<TMethod>(); }
            public class Base<T> : IFactory
            {
                public static object Create<TMethod>() => new List<IAsyncOperationWithProgress<T, TMethod>>();
            }
            public sealed class Factory<T> : Base<T>{{(reimplements ? ", IFactory" : "")}}
            {
                public static new object Create<TMethod>() => new List<IAsyncOperationWithProgress<TMethod, T>>();
            }
            """, "Invoke<Factory<long>>()", """
            private static object Invoke<TFactory>() where TFactory : IFactory => TFactory.Create<int>();
            """, genericSlot: true,
            expected: $"System.Collections.Generic.List`1[[Windows.Foundation.IAsyncOperationWithProgress`2[[System.{(reimplements ? "Int32" : "Int64")},",
            expectedArgument: $"],[System.{(reimplements ? "Int64" : "Int32")},",
            excluded: $"System.Collections.Generic.List`1[[Windows.Foundation.IAsyncOperationWithProgress`2[[System.{(reimplements ? "Int64" : "Int32")},");
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void StaticDispatch_NonGenericInheritedClassControl(bool reimplements)
    {
        // Control: the receiver and base 'TypeSpec'-s already expose their own non-generic static bodies
        // The generic-method rows separately verify that dispatch does not introduce unselected closed contexts.
        AssertStaticDispatchSource($"nongeneric-inherited-class-{reimplements}", $$"""
            public interface IFactory { static abstract object Create(); }
            public class Base<A, B> : IFactory
            {
                public static object Create() => new List<IAsyncOperationWithProgress<A, B>>();
            }
            public sealed class Factory<X, Y> : Base<Y, X>{{(reimplements ? ", IFactory" : "")}}
            {
                public static new object Create() => new List<IAsyncOperationWithProgress<X, Y>>();
            }
            """, "Invoke<Factory<int, long>>()", """
            private static object Invoke<TFactory>() where TFactory : IFactory => TFactory.Create();
            """,
            expected: $"System.Collections.Generic.List`1[[Windows.Foundation.IAsyncOperationWithProgress`2[[System.{(reimplements ? "Int32" : "Int64")},",
            expectedArgument: $"],[System.{(reimplements ? "Int64" : "Int32")},");
    }

    [TestMethod]
    public void StaticDispatch_InheritedClassWinsOverInterfaceDefault()
    {
        AssertStaticDispatchSource("inherited-class-default-replacement", """
            public interface IFactory { static abstract object Create<TMethod>(); }
            public interface IFactory<T> : IFactory
            {
                static object IFactory.Create<TMethod>() => new List<IAsyncActionWithProgress<T>>();
            }
            public class Base<T> : IFactory
            {
                public static object Create<TMethod>() => new List<IAsyncOperationWithProgress<T, TMethod>>();
            }
            public sealed class Factory<X, Y> : Base<Y>, IFactory<Y>;
            """, "Invoke<Factory<int, long>>()", """
            private static object Invoke<TFactory>() where TFactory : IFactory => TFactory.Create<bool>();
            """, genericSlot: true,
            expected: "System.Collections.Generic.List`1[[Windows.Foundation.IAsyncOperationWithProgress`2[[System.Int64,",
            expectedArgument: "],[System.Boolean,", excluded: StaticProgressList);
    }

    [TestMethod]
    public void StaticDispatch_AbstractClassReceiver()
    {
        AssertStaticDispatchSource("abstract-class-receiver", """
            public interface IFactory { static abstract object Create(); }
            public interface IFactory<T> : IFactory
            {
                static object IFactory.Create() => new List<IAsyncOperation<T>>();
            }
            public abstract class Factory<T> : IFactory<T>;
            """, "Invoke<Factory<long>>()", """
            private static object Invoke<TFactory>() where TFactory : IFactory => TFactory.Create();
            """);
    }

    [TestMethod]
    public void StaticDispatch_StaticCacheUsesCanonicalDescendants()
    {
        AssertStaticDispatchSource("static-default-cache", """
            public interface IFactory { static abstract object Create(); }
            public interface IFactory<T> : IFactory
            {
                private static readonly object Cached = new List<IAsyncOperation<T>>();
                static object IFactory.Create() => Cached;
            }
            public sealed class Factory<T> : IFactory<T>;
            """, "Invoke<Factory<long>>()", """
            private static object Invoke<TFactory>() where TFactory : IFactory => TFactory.Create();
            """);
    }

    [TestMethod]
    public void StaticDispatch_ReceiverClosesBodyWithOpenMethodArgument()
    {
        AssertStaticDispatchSource("open-method-argument", """
            public interface IFactory { static abstract object Create<TUnused>(); }
            public interface IFactory<T> : IFactory
            {
                static object IFactory.Create<TUnused>() => new List<IAsyncOperation<T>>();
            }
            public sealed class Factory<T> : IFactory<T>;
            public sealed class Caller<TFactory> where TFactory : IFactory
            {
                public object Invoke<TUnused>() => TFactory.Create<TUnused>();
            }
            """, "new Caller<Factory<long>>()", "", genericSlot: true);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void StaticDispatch_ResolvedDiamondSelectsOnlyFinalBody(bool genericSlot)
    {
        string parameters = genericSlot ? "<TUnused>" : "";

        AssertStaticDispatchSource($"resolved-diamond-{genericSlot}", $$"""
            public interface IFactory { static abstract object Create{{parameters}}(); }
            public interface IFactory<T> : IFactory
            {
                static object IFactory.Create{{parameters}}() => new List<IAsyncOperation<T>>();
            }
            public interface ILeft<T> : IFactory<T>
            {
                static object IFactory.Create{{parameters}}() => new List<IAsyncActionWithProgress<T>>();
            }
            public interface IRight<T> : IFactory<T>
            {
                static object IFactory.Create{{parameters}}() => new List<IAsyncActionWithProgress<T>>();
            }
            public interface IResolved<T> : ILeft<T>, IRight<T>
            {
                static object IFactory.Create{{parameters}}() => new List<IAsyncOperationWithProgress<T, int>>();
            }
            public sealed class Factory<T> : IResolved<T>;
            """, "Invoke<Factory<long>>()", $$"""
            private static object Invoke<TFactory>() where TFactory : IFactory => TFactory.Create{{(genericSlot ? "<bool>" : "")}}();
            """, genericSlot: genericSlot,
            expected: "System.Collections.Generic.List`1[[Windows.Foundation.IAsyncOperationWithProgress`2[[System.Int64,",
            expectedArgument: "],[System.Int32,", excluded: StaticOperationList, excludedAlso: StaticProgressList);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void StaticDispatch_ReabstractionUsesClassImplementation(bool genericSlot)
    {
        string parameters = genericSlot ? "<TUnused>" : "";

        AssertStaticDispatchSource($"reabstract-class-{genericSlot}", $$"""
            public interface IFactory { static abstract object Create{{parameters}}(); }
            public interface IFactory<T> : IFactory
            {
                static object IFactory.Create{{parameters}}() => new List<IAsyncOperation<T>>();
            }
            public interface IReabstract<T> : IFactory<T>
            {
                static abstract object IFactory.Create{{parameters}}();
            }
            public sealed class Factory<T> : IReabstract<T>
            {
                public static object Create{{parameters}}() => new List<IAsyncActionWithProgress<T>>();
            }
            """, "Invoke<Factory<long>>()", $$"""
            private static object Invoke<TFactory>() where TFactory : IFactory => TFactory.Create{{(genericSlot ? "<int>" : "")}}();
            """, genericSlot: genericSlot, expected: StaticProgressList, excluded: StaticOperationList);
    }

    [TestMethod]
    public void StaticDispatch_UncalledSlotsAndPrivateBodiesRemainAbsent()
    {
        AssertStaticDispatchSource("uncalled-static-members", """
            public interface IFactory { static abstract object Create(); }
            public interface IFactory<T> : IFactory
            {
                static object IFactory.Create() => new List<IAsyncOperation<T>>();
                static virtual object Other() => new List<IAsyncActionWithProgress<T>>();
                private static object Private() => new List<IAsyncActionWithProgress<T>>();
            }
            public interface IUnrelated<T> : IFactory
            {
                static object IFactory.Create() => new List<IAsyncActionWithProgress<T>>();
            }
            public sealed class Factory<T> : IFactory<T>;
            public sealed class Unused<T> : IUnrelated<T>;
            """, "Invoke<Factory<long>>()", """
            private static object Invoke<TFactory>() where TFactory : IFactory => TFactory.Create();
            """, excluded: StaticProgressList);
    }

    [TestMethod]
    public void StaticDispatch_UnboundReceiverDoesNotDiagnoseLargeDeclaringType()
    {
        string argument = "T";

        for (int index = 0; index < 9; index++)
        {
            argument = $"Pair<{argument}, {argument}>";
        }

        AssertStaticDispatchSource("unbound-large-declaration", """
            public sealed class Pair<X, Y>;
            public interface IFactory<T> { static abstract object Create(); }
            public interface IDefault<T> : IFactory<T>
            {
                static object IFactory<T>.Create() => new List<IAsyncOperation<T>>();
            }
            """, "new object()", $$"""
            private static object Invoke<TFactory, T>() where TFactory : IFactory<{{argument}}> => TFactory.Create();
            """, expectedPresent: false);
    }

    [TestMethod]
    public void StaticDispatch_ObservedFromInstanceDefaultBody()
    {
        AssertStaticDispatchSource("instance-default-to-static", """
            public interface IFactory { static abstract object Create(); }
            public interface IFactory<T> : IFactory
            {
                static object IFactory.Create() => new List<IAsyncOperation<T>>();
            }
            public sealed class Factory<T> : IFactory<T>;
            public interface IInstanceFactory { object Create(); }
            public interface IInstanceFactory<T> : IInstanceFactory
            {
                object IInstanceFactory.Create() => Calls.Invoke<Factory<T>>();
            }
            public sealed class InstanceFactory<T> : IInstanceFactory<T>;
            public static class Calls
            {
                public static object Invoke<TFactory>() where TFactory : IFactory => TFactory.Create();
            }
            """, "((IInstanceFactory)new InstanceFactory<long>()).Create()", "");
    }

    [TestMethod]
    public void StaticDispatch_NewInstanceDefaultReceiverRetainsStaticPhase()
    {
        AssertStaticDispatchSource("static-to-instance-default", """
            public interface IFactory { static abstract object Create(); }
            public interface IFactory<T> : IFactory
            {
                static object IFactory.Create() => new InstanceFactory<T>();
            }
            public sealed class Factory<T> : IFactory<T>;
            public interface IInstanceFactory { object Create(); }
            public interface IInstanceFactory<T> : IInstanceFactory
            {
                object IInstanceFactory.Create() => new List<IAsyncOperation<T>>();
            }
            public sealed class InstanceFactory<T> : IInstanceFactory<T>;
            """, "Invoke<Factory<long>>()", """
            private static object Invoke<TFactory>() where TFactory : IFactory => TFactory.Create();
            """);
    }

    private static void AssertStaticDispatchSource(
        string name,
        string declarations,
        string invocation,
        string helpers,
        bool genericSlot = false,
        string expected = StaticOperationList,
        string? excluded = null,
        bool delegateTarget = false,
        string expectedSlotName = "Create",
        string? expectedArgument = null,
        bool expectedPresent = true,
        string? excludedAlso = null)
    {
        string source = $$"""
            using System.Collections.Generic;
            using Windows.Foundation;
            namespace StaticDispatch;
            {{declarations}}
            public static class Program
            {
                public static void Main() => _ = {{invocation}};
                {{helpers}}
            }
            """;
        string directory = Directory.CreateTempSubdirectory($"InteropStaticDispatch_{name}_").FullName;

        try
        {
            string app = ProjectionWriterRunner.CompileSources([source], Path.Combine(directory, "StaticDispatch.dll"),
                outputKind: OutputKind.ConsoleApplication);
            ModuleDefinition module = ModuleDefinition.FromBytes(File.ReadAllBytes(app));
            MethodDefinition invoke = module.GetAllTypes()
                .SelectMany(static type => type.Methods)
                .Single(static method => method.CilMethodBody?.Instructions.Any(static instruction =>
                    instruction.OpCode.Code == CilCode.Constrained) is true);
            CilInstructionCollection instructions = invoke.CilMethodBody!.Instructions;
            int constraintIndex = Enumerable.Range(0, instructions.Count).Single(index =>
                instructions[index].OpCode.Code == CilCode.Constrained);

            Assert.AreEqual(delegateTarget ? CilCode.Ldftn : CilCode.Call, instructions[constraintIndex + 1].OpCode.Code);
            Assert.AreEqual(genericSlot, instructions[constraintIndex + 1].Operand is MethodSpecification);
            Assert.AreEqual(expectedSlotName, ((IMethodDescriptor)instructions[constraintIndex + 1].Operand!).Name!.ToString());

            string metadata = string.Join("\n", module.GetAllTypes().SelectMany(type =>
                type.Methods.Select(method => $"{type.FullName}: {method.Attributes} {method.Signature}")
                    .Concat(type.MethodImplementations.Select(implementation =>
                        $"{type.FullName}: MethodImpl {implementation.Declaration} -> {implementation.Body}"))))
                + "\n" + string.Join("\n", instructions);
            (int exitCode, string log) = RunGenerator(directory, app, treatWarningsAsErrors: true);
            string output = Path.Combine(directory, "WinRT.Interop.dll");
            string[] associations = File.Exists(output) ? [.. GetComWrappersTypeAssociations(output).Order(StringComparer.Ordinal)] : [];
            Assert.AreEqual(0, exitCode, log + "\n" + metadata);
            Assert.AreEqual(expectedPresent, associations.Any(type =>
                type.StartsWith(expected, StringComparison.Ordinal) &&
                (expectedArgument is null || type.Contains(expectedArgument, StringComparison.Ordinal))), log + "\n" + metadata);

            if (excluded is not null)
            {
                Assert.IsFalse(associations.Any(type => type.StartsWith(excluded, StringComparison.Ordinal)), log + "\n" + metadata);
            }

            if (excludedAlso is not null)
            {
                Assert.IsFalse(associations.Any(type => type.StartsWith(excludedAlso, StringComparison.Ordinal)), log + "\n" + metadata);
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
