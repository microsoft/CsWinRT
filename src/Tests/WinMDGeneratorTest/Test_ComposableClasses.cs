// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using WinMDGeneratorTest.Helpers;

namespace WinMDGeneratorTest;

/// <summary>
/// End-to-end tests for the WinMD generator's handling of unsealed (composable) authored classes.
/// </summary>
/// <remarks>
/// A public unsealed class with at least one public constructor is projected as a composable Windows
/// Runtime class, so it gets a <c>[Composable]</c> factory instead of an <c>[Activatable]</c> one, and it
/// can become the inner object of a COM aggregate. Only interfaces authored in the same component can take
/// part in that, so classes implementing anything else are rejected with <c>CSWINRTWINMDGEN0015</c>, and
/// composition factories cannot take array or generic parameters (<c>CSWINRTWINMDGEN0016</c>). Unsealed
/// classes that never receive a composition factory (abstract types, or types whose constructors are all
/// non-public) are not composable, and must not be subject to either restriction.
/// </remarks>
[TestClass]
public class Test_ComposableClasses
{
    [TestMethod]
    public void UnsealedClassWithMultipleConstructors_GeneratesSuccessfully()
    {
        WinMDGeneratorRunner.AssertSuccess("""
            namespace Component;

            public class ComposableBase
            {
                public ComposableBase()
                : this(42)
                {
                }

                public ComposableBase(int value)
                {
                    Value = value;
                }

                public int Value { get; }
            }
            """);
    }

    [TestMethod]
    public void UnsealedClassWithAuthoredInterface_GeneratesSuccessfully()
    {
        WinMDGeneratorRunner.AssertSuccess("""
            namespace Component;

            public interface IThing
            {
                int GetThingValue();
            }

            public class ComposableBase : IThing
            {
                public int GetThingValue() => 42;
            }
            """);
    }

    [TestMethod]
    public void UnsealedClass_IsComposableAndNotActivatable()
    {
        var attributes = WinMDGeneratorRunner.GetGeneratedAttributes("""
            namespace Component;

            public class ComposableBase
            {
                public ComposableBase()
                {
                }

                public int Value => 42;
            }
            """);

        Assert.IsTrue(attributes["Component.ComposableBase"].Contains("Windows.Foundation.Metadata.ComposableAttribute"));
        Assert.IsFalse(attributes["Component.ComposableBase"].Contains("Windows.Foundation.Metadata.ActivatableAttribute"));
    }

    [TestMethod]
    public void SealedClass_IsActivatableAndNotComposable()
    {
        var attributes = WinMDGeneratorRunner.GetGeneratedAttributes("""
            namespace Component;

            public sealed class SealedClass
            {
                public SealedClass()
                {
                }

                public int Value => 42;
            }
            """);

        Assert.IsTrue(attributes["Component.SealedClass"].Contains("Windows.Foundation.Metadata.ActivatableAttribute"));
        Assert.IsFalse(attributes["Component.SealedClass"].Contains("Windows.Foundation.Metadata.ComposableAttribute"));
    }

    [TestMethod]
    public void UnsealedClassWithCustomMappedInterface_IsReported()
    {
        // 'IDisposable' is custom-mapped to 'IClosable', whose CCW vtable lives in 'WinRT.Runtime'
        // and is shared by every managed type, so no per-aggregate copy can be made for it
        WinMDGeneratorRunner.AssertFailure("""
            namespace Component;

            public class ComposableDisposable : System.IDisposable
            {
                public void Dispose()
                {
                }
            }
            """, error: "CSWINRTWINMDGEN0015");
    }

    [TestMethod]
    public void UnsealedClassWithGenericInterface_IsReported()
    {
        // Generic instantiations get their CCW vtables from the interop assembly, and those are
        // shared across the whole application, so they cannot be made aggregation-aware either
        WinMDGeneratorRunner.AssertFailure("""
            namespace Component;

            public class ComposableList : System.Collections.Generic.IReadOnlyList<int>
            {
                public int this[int index] => index;

                public int Count => 0;

                public System.Collections.Generic.IEnumerator<int> GetEnumerator() => null;

                System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => null;
            }
            """, error: "CSWINRTWINMDGEN0015");
    }

    [TestMethod]
    public void UnsealedClassWithGeneratedComInterface_IsReported()
    {
        // '[GeneratedComInterface]' interfaces get their CCW vtables from the BCL marshalling
        // infrastructure, which has no way to delegate 'IUnknown' to a controlling outer object
        WinMDGeneratorRunner.AssertFailure("""
            namespace Component;

            [System.Runtime.InteropServices.Marshalling.GeneratedComInterface]
            [System.Runtime.InteropServices.Guid("A6B0C1D2-3E4F-5061-7283-94A5B6C7D8E9")]
            public partial interface IComInterface
            {
                void Method();
            }

            public class ComposableWithComInterface : IComInterface
            {
                public void Method()
                {
                }
            }
            """, error: "CSWINRTWINMDGEN0015");
    }

    [TestMethod]
    public void UnsealedClassInheritingUnsupportedInterface_IsReported()
    {
        // The CCW of a derived composable class also exposes the interfaces of its authored base
        // classes, so the whole class hierarchy has to be validated, not just the class itself
        WinMDGeneratorRunner.AssertFailure("""
            namespace Component;

            public class DisposableBase : System.IDisposable
            {
                public void Dispose()
                {
                }
            }

            public class ComposableDerived : DisposableBase
            {
                public int Value => 42;
            }
            """, error: "CSWINRTWINMDGEN0015");
    }

    [TestMethod]
    public void AbstractClassWithUnsupportedInterface_GeneratesSuccessfully()
    {
        // Abstract classes never get a public composition factory, even when they explicitly
        // declare a public constructor. This is the shape of a typical MVVM base type.
        WinMDGeneratorRunner.AssertSuccess("""
            namespace Component;

            public abstract class ObservableObjectBase : System.ComponentModel.INotifyPropertyChanged
            {
                public ObservableObjectBase()
                {
                }

                public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

                protected void RaisePropertyChanged(string propertyName)
                {
                    PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
                }
            }
            """);
    }

    [TestMethod]
    public void AbstractClass_IsNotComposable()
    {
        var attributes = WinMDGeneratorRunner.GetGeneratedAttributes("""
            namespace Component;

            public abstract class AbstractBase
            {
                public AbstractBase()
                {
                }

                public int Value => 42;
            }
            """);

        Assert.IsFalse(attributes["Component.AbstractBase"].Contains("Windows.Foundation.Metadata.ComposableAttribute"));
        Assert.IsFalse(attributes["Component.AbstractBase"].Contains("Windows.Foundation.Metadata.ActivatableAttribute"));
    }

    [TestMethod]
    public void UnsealedClassWithNonPublicConstructorsAndUnsupportedInterface_GeneratesSuccessfully()
    {
        // An unsealed class whose constructors are all non-public gets no composition factory either,
        // so it is not composable and the aggregation constraints must not be enforced on it
        WinMDGeneratorRunner.AssertSuccess("""
            namespace Component;

            public class NonPublicConstructorBase : System.IDisposable
            {
                internal NonPublicConstructorBase()
                {
                }

                protected NonPublicConstructorBase(int value)
                {
                }

                public void Dispose()
                {
                }
            }
            """);
    }

    [TestMethod]
    public void UnsealedClassWithNonPublicConstructors_IsNotComposable()
    {
        var attributes = WinMDGeneratorRunner.GetGeneratedAttributes("""
            namespace Component;

            public class NonPublicConstructorBase
            {
                private NonPublicConstructorBase()
                {
                }

                public int Value => 42;
            }
            """);

        Assert.IsFalse(attributes["Component.NonPublicConstructorBase"].Contains("Windows.Foundation.Metadata.ComposableAttribute"));
        Assert.IsFalse(attributes["Component.NonPublicConstructorBase"].Contains("Windows.Foundation.Metadata.ActivatableAttribute"));
    }

    [TestMethod]
    public void UnsealedClassWithArrayConstructorParameter_IsReported()
    {
        // Composition factory methods get a dedicated CCW body that cannot marshal array parameters, so
        // they are rejected here rather than emitting a factory that always fails with 'E_NOTIMPL'
        WinMDGeneratorRunner.AssertFailure("""
            namespace Component;

            public class ComposableWithArray
            {
                public ComposableWithArray(int[] values)
                {
                }
            }
            """, error: "CSWINRTWINMDGEN0016");
    }

    [TestMethod]
    public void UnsealedClassWithGenericConstructorParameter_IsReported()
    {
        WinMDGeneratorRunner.AssertFailure("""
            namespace Component;

            public class ComposableWithGeneric
            {
                public ComposableWithGeneric(System.Collections.Generic.IReadOnlyList<int> values)
                {
                }
            }
            """, error: "CSWINRTWINMDGEN0016");
    }

    [TestMethod]
    public void SealedClassWithArrayConstructorParameter_GeneratesSuccessfully()
    {
        // Sealed classes get an activation factory, whose CCW body marshals arrays just fine
        WinMDGeneratorRunner.AssertSuccess("""
            namespace Component;

            public sealed class SealedWithArray
            {
                public SealedWithArray(int[] values)
                {
                }
            }
            """);
    }

    [TestMethod]
    public void AbstractClassWithArrayConstructorParameter_GeneratesSuccessfully()
    {
        // No public composition factory is emitted, so the parameter is never projected onto one
        WinMDGeneratorRunner.AssertSuccess("""
            namespace Component;

            public abstract class AbstractWithArray
            {
                protected AbstractWithArray(int[] values)
                {
                }
            }
            """);
    }

    /// <summary>
    /// The component source used by the protected/overridable member tests below.
    /// </summary>
    private const string ComposableMembersSource = """
        namespace Component;

        public class ComposableBase
        {
            public int PublicMethod() => 1;

            public int PublicProperty { get; set; }

            protected int ProtectedMethod() => 2;

            protected int ProtectedProperty { get; set; }

            public virtual int PublicVirtualMethod() => 3;

            protected virtual int ProtectedVirtualMethod() => 4;

            protected virtual int VirtualProperty => 5;
        }
        """;

    [TestMethod]
    public void ComposableClass_ProtectedMembersGoOnProtectedInterface()
    {
        var methods = WinMDGeneratorRunner.GetGeneratedMethods(ComposableMembersSource);

        CollectionAssert.AreEquivalent(
            new[] { "ProtectedMethod", "get_ProtectedProperty", "put_ProtectedProperty" },
            methods["Component.IComposableBaseProtected"].ToArray());
    }

    [TestMethod]
    public void ComposableClass_OverridableMembersGoOnOverridesInterface()
    {
        var methods = WinMDGeneratorRunner.GetGeneratedMethods(ComposableMembersSource);

        CollectionAssert.AreEquivalent(
            new[] { "PublicVirtualMethod", "ProtectedVirtualMethod", "get_VirtualProperty" },
            methods["Component.IComposableBaseOverrides"].ToArray());
    }

    [TestMethod]
    public void ComposableClass_VirtualInterfaceImplementationIsNotOverridable()
    {
        var methods = WinMDGeneratorRunner.GetGeneratedMethods("""
            namespace Component;

            public interface IThing
            {
                int GetValue();
            }

            public class ComposableBase : IThing
            {
                public virtual int GetValue() => 42;
            }
            """);

        Assert.IsFalse(methods["Component.IComposableBaseOverrides"].Contains("GetValue"));
    }

    [TestMethod]
    public void UnsealedExternalBaseWithParameterizedConstructor_UsesActivationFactory()
    {
        const string source = """
            namespace Component;

            public class DerivedRandom : System.Random
            {
                public DerivedRandom(int seed)
                    : base(seed)
                {
                }
            }
            """;

        var attributes = WinMDGeneratorRunner.GetGeneratedAttributes(source);
        var methods = WinMDGeneratorRunner.GetGeneratedMethods(source);

        Assert.IsTrue(attributes["Component.DerivedRandom"].Contains("Windows.Foundation.Metadata.ActivatableAttribute"));
        Assert.IsFalse(attributes["Component.DerivedRandom"].Contains("Windows.Foundation.Metadata.ComposableAttribute"));
        CollectionAssert.AreEquivalent(
            new[] { "CreateDerivedRandom" },
            methods["Component.IDerivedRandomFactory"].ToArray());
    }

    [TestMethod]
    public void AuthoredHierarchyOverExternalBase_UsesActivationFactories()
    {
        const string source = """
            namespace Component;

            public class Middle : System.Random
            {
                public Middle(int seed)
                    : base(seed)
                {
                }
            }

            public class Leaf : Middle
            {
                public Leaf(int seed)
                    : base(seed)
                {
                }
            }
            """;

        var attributes = WinMDGeneratorRunner.GetGeneratedAttributes(source);
        var methods = WinMDGeneratorRunner.GetGeneratedMethods(source);

        Assert.IsTrue(attributes["Component.Middle"].Contains("Windows.Foundation.Metadata.ActivatableAttribute"));
        Assert.IsFalse(attributes["Component.Middle"].Contains("Windows.Foundation.Metadata.ComposableAttribute"));
        Assert.IsTrue(attributes["Component.Leaf"].Contains("Windows.Foundation.Metadata.ActivatableAttribute"));
        Assert.IsFalse(attributes["Component.Leaf"].Contains("Windows.Foundation.Metadata.ComposableAttribute"));
        CollectionAssert.AreEquivalent(new[] { "CreateMiddle" }, methods["Component.IMiddleFactory"].ToArray());
        CollectionAssert.AreEquivalent(new[] { "CreateLeaf" }, methods["Component.ILeafFactory"].ToArray());
    }

    [TestMethod]
    public void ComposableClass_OverridableMembersAreNotOnThePublicSurface()
    {
        var methods = WinMDGeneratorRunner.GetGeneratedMethods(ComposableMembersSource);

        // Windows Runtime has no public overridable members, so a 'public virtual' member is projected
        // onto the '[Overridable]' interface only, and disappears from the default interface and the class
        CollectionAssert.AreEquivalent(
            new[] { "PublicMethod", "get_PublicProperty", "put_PublicProperty" },
            methods["Component.IComposableBaseClass"].ToArray());

        Assert.IsFalse(methods["Component.ComposableBase"].Contains("PublicVirtualMethod"));
        Assert.IsFalse(methods["Component.ComposableBase"].Contains("ProtectedMethod"));
        Assert.IsTrue(methods["Component.ComposableBase"].Contains("PublicMethod"));
    }

    [TestMethod]
    public void ComposableClass_ExclusiveInterfacesAreMarkedOnTheInterfaceImplementation()
    {
        var implementations = WinMDGeneratorRunner.GetGeneratedInterfaceImplementations(ComposableMembersSource);

        CollectionAssert.AreEquivalent(
            new[] { "Windows.Foundation.Metadata.ProtectedAttribute" },
            implementations["Component.ComposableBase:Component.IComposableBaseProtected"].ToArray());

        CollectionAssert.AreEquivalent(
            new[] { "Windows.Foundation.Metadata.OverridableAttribute" },
            implementations["Component.ComposableBase:Component.IComposableBaseOverrides"].ToArray());

        CollectionAssert.AreEquivalent(
            new[] { "Windows.Foundation.Metadata.DefaultAttribute" },
            implementations["Component.ComposableBase:Component.IComposableBaseClass"].ToArray());
    }

    [TestMethod]
    public void ComposableClass_ExclusiveInterfacesAreExclusiveToTheClass()
    {
        var attributes = WinMDGeneratorRunner.GetGeneratedAttributes(ComposableMembersSource);

        Assert.IsTrue(attributes["Component.IComposableBaseProtected"].Contains("Windows.Foundation.Metadata.ExclusiveToAttribute"));
        Assert.IsTrue(attributes["Component.IComposableBaseOverrides"].Contains("Windows.Foundation.Metadata.ExclusiveToAttribute"));
        Assert.IsTrue(attributes["Component.IComposableBaseProtected"].Contains("Windows.Foundation.Metadata.GuidAttribute"));
        Assert.IsTrue(attributes["Component.IComposableBaseOverrides"].Contains("Windows.Foundation.Metadata.GuidAttribute"));
    }

    [TestMethod]
    public void SealedClass_HasNoProtectedOrOverridableInterfaces()
    {
        // A sealed class cannot be derived from at all, so its protected members stay internal to the
        // component (a sealed class cannot declare new virtual members in the first place)
        var methods = WinMDGeneratorRunner.GetGeneratedMethods("""
            namespace Component;

            public sealed class SealedClass
            {
                public int PublicMethod() => 1;

                private int PrivateMethod() => 2;
            }
            """);

        Assert.IsFalse(methods.Contains("Component.ISealedClassProtected"));
        Assert.IsFalse(methods.Contains("Component.ISealedClassOverrides"));

        CollectionAssert.AreEquivalent(new[] { "PublicMethod" }, methods["Component.ISealedClassClass"].ToArray());
    }

    [TestMethod]
    public void UnsealedClassWithNonPublicConstructors_HasNoProtectedOrOverridableInterfaces()
    {
        // A class that never receives a composition factory is not composable, so it has no derived
        // types outside the component either
        var methods = WinMDGeneratorRunner.GetGeneratedMethods("""
            namespace Component;

            public class NonPublicConstructorBase
            {
                internal NonPublicConstructorBase()
                {
                }

                protected int ProtectedMethod() => 2;

                public virtual int PublicVirtualMethod() => 3;
            }
            """);

        Assert.IsFalse(methods.Contains("Component.INonPublicConstructorBaseProtected"));
        Assert.IsFalse(methods.Contains("Component.INonPublicConstructorBaseOverrides"));
    }

    [TestMethod]
    public void ComposableClass_MembersOverridingAnAuthoredBaseAreNotRedeclared()
    {
        // The override introduces no new Windows Runtime surface: it is already declared by the
        // '[Overridable]' interface of the base runtime class
        var methods = WinMDGeneratorRunner.GetGeneratedMethods("""
            namespace Component;

            public class ComposableBase
            {
                public virtual int PublicVirtualMethod() => 3;

                public virtual int PublicVirtualProperty { get; set; }
            }

            public sealed class DerivedClass : ComposableBase
            {
                public override int PublicVirtualMethod() => 4;

                public override int PublicVirtualProperty { get; set; }

                public int DerivedMethod() => 5;
            }
            """);

        CollectionAssert.AreEquivalent(
            new[] { "PublicVirtualMethod", "get_PublicVirtualProperty", "put_PublicVirtualProperty" },
            methods["Component.IComposableBaseOverrides"].ToArray());
        CollectionAssert.AreEquivalent(new[] { "DerivedMethod" }, methods["Component.IDerivedClassClass"].ToArray());

        Assert.IsFalse(methods["Component.DerivedClass"].Contains("PublicVirtualMethod"));
        Assert.IsFalse(methods["Component.DerivedClass"].Contains("get_PublicVirtualProperty"));
        Assert.IsFalse(methods["Component.DerivedClass"].Contains("put_PublicVirtualProperty"));
    }

    [TestMethod]
    public void MembersOverridingAbstractAuthoredBaseAreDeclaredOnDerivedClass()
    {
        var methods = WinMDGeneratorRunner.GetGeneratedMethods("""
            namespace Component;

            public abstract class AbstractBase
            {
                public virtual int PublicVirtualMethod() => 3;

                public virtual int PublicVirtualProperty { get; set; }
            }

            public sealed class DerivedClass : AbstractBase
            {
                public override int PublicVirtualMethod() => 4;

                public override int PublicVirtualProperty { get; set; }
            }
            """);

        CollectionAssert.AreEquivalent(
            new[] { "PublicVirtualMethod", "get_PublicVirtualProperty", "put_PublicVirtualProperty" },
            methods["Component.IDerivedClassClass"].ToArray());
        Assert.IsTrue(methods["Component.DerivedClass"].Contains("PublicVirtualMethod"));
        Assert.IsTrue(methods["Component.DerivedClass"].Contains("get_PublicVirtualProperty"));
        Assert.IsTrue(methods["Component.DerivedClass"].Contains("put_PublicVirtualProperty"));
    }

    [TestMethod]
    public void MembersOverridingConcreteClassAboveFlattenedAbstractBaseAreNotRedeclared()
    {
        var methods = WinMDGeneratorRunner.GetGeneratedMethods("""
            namespace Component;

            public abstract class AbstractBase
            {
                public virtual int PublicVirtualMethod() => 1;

                public virtual int PublicVirtualProperty { get; set; }
            }

            public class MiddleClass : AbstractBase
            {
                public override int PublicVirtualMethod() => 2;

                public override int PublicVirtualProperty { get; set; }
            }

            public sealed class LeafClass : MiddleClass
            {
                public override int PublicVirtualMethod() => 3;

                public override int PublicVirtualProperty { get; set; }

                public int LeafMethod() => 4;
            }
            """);

        CollectionAssert.AreEquivalent(
            new[] { "PublicVirtualMethod", "get_PublicVirtualProperty", "put_PublicVirtualProperty" },
            methods["Component.IMiddleClassClass"].ToArray());
        CollectionAssert.AreEquivalent(new[] { "LeafMethod" }, methods["Component.ILeafClassClass"].ToArray());
        Assert.IsFalse(methods["Component.LeafClass"].Contains("PublicVirtualMethod"));
        Assert.IsFalse(methods["Component.LeafClass"].Contains("get_PublicVirtualProperty"));
        Assert.IsFalse(methods["Component.LeafClass"].Contains("put_PublicVirtualProperty"));
    }

    [TestMethod]
    public void ComposableClass_ProtectedEventsAreNotProjected()
    {
        // Protected and overridable events have no '[UnsafeAccessor]'-based CCW dispatch, so they are
        // deliberately left out of the Windows Runtime surface rather than emitted as broken vtables
        var methods = WinMDGeneratorRunner.GetGeneratedMethods("""
            namespace Component;

            public delegate void ThingHandler(int value);

            public class ComposableBase
            {
                protected event ThingHandler ProtectedEvent;

                public int PublicMethod() => ProtectedEvent is null ? 0 : 1;
            }
            """);

        Assert.IsFalse(methods.Contains("Component.IComposableBaseProtected"));
        Assert.IsFalse(methods["Component.IComposableBaseClass"].Contains("add_ProtectedEvent"));
    }

    [TestMethod]
    public void ComposableClass_AuthoredOverridableInterfaceIsMarkedOverridable()
    {
        // An authored interface marked '[WindowsRuntimeOverridable]' is the overridable surface of the composable
        // classes implementing it, exactly like an '[overridable] interface' member on a runtime class in MIDL
        var implementations = WinMDGeneratorRunner.GetGeneratedInterfaceImplementations(OverridableAttributeDeclaration + """
            namespace Component
            {
                [WindowsRuntime.WindowsRuntimeOverridable]
                public interface IThingOverrides
                {
                    int ComputeCoreValue();
                }

                public interface IThing
                {
                    int GetThingValue();
                }

                public class ComposableBase : IThing, IThingOverrides
                {
                    public int GetThingValue() => 1;

                    public virtual int ComputeCoreValue() => 2;
                }
            }
            """);

        Assert.IsTrue(implementations["Component.ComposableBase:Component.IThingOverrides"].Contains("Windows.Foundation.Metadata.OverridableAttribute"));

        // Every other implemented interface is unaffected
        Assert.IsFalse(implementations["Component.ComposableBase:Component.IThing"].Contains("Windows.Foundation.Metadata.OverridableAttribute"));
    }

    [TestMethod]
    public void ComposableClass_AuthoredOverridableInterfaceMembersStayOffTheClassSurface()
    {
        // The members of an overridable interface are not part of the public surface of the runtime class:
        // they are only reachable through the interface, which is how a derived type replaces them
        var methods = WinMDGeneratorRunner.GetGeneratedMethods(OverridableAttributeDeclaration + """
            namespace Component
            {
                [WindowsRuntime.WindowsRuntimeOverridable]
                public interface IThingOverrides
                {
                    int ComputeCoreValue();
                }

                public class ComposableBase : IThingOverrides
                {
                    public virtual int ComputeCoreValue() => 2;

                    public int PublicMethod() => 3;
                }
            }
            """);

        CollectionAssert.AreEquivalent(new[] { "ComputeCoreValue" }, methods["Component.IThingOverrides"].ToArray());
        CollectionAssert.AreEquivalent(new[] { "PublicMethod" }, methods["Component.IComposableBaseClass"].ToArray());

        // The member is declared by the authored interface, so it is not synthesized a second time
        Assert.IsFalse(methods.Contains("Component.IComposableBaseOverrides"));
    }

    [TestMethod]
    public void ComposableClass_WithOnlyAuthoredOverridableInterfaceGetsEmptyDefaultInterface()
    {
        const string source = OverridableAttributeDeclaration + """
            namespace Component
            {
                [WindowsRuntime.WindowsRuntimeOverridable]
                public interface IThingOverrides
                {
                    int ComputeCoreValue();
                }

                public class ComposableBase : IThingOverrides
                {
                    public virtual int ComputeCoreValue() => 2;
                }
            }
            """;

        var implementations = WinMDGeneratorRunner.GetGeneratedInterfaceImplementations(source);
        var methods = WinMDGeneratorRunner.GetGeneratedMethods(source);

        Assert.IsTrue(implementations["Component.ComposableBase:Component.IThingOverrides"].Contains("Windows.Foundation.Metadata.OverridableAttribute"));
        Assert.IsFalse(implementations["Component.ComposableBase:Component.IThingOverrides"].Contains("Windows.Foundation.Metadata.DefaultAttribute"));
        Assert.IsTrue(implementations["Component.ComposableBase:Component.IComposableBaseClass"].Contains("Windows.Foundation.Metadata.DefaultAttribute"));
        Assert.AreEqual(0, methods["Component.IComposableBaseClass"].Count());
        Assert.IsFalse(methods["Component.ComposableBase"].Contains("ComputeCoreValue"));
    }

    [TestMethod]
    public void ComposableClass_OverridableInterfaceDeclaredFirstIsNotDefault()
    {
        const string source = OverridableAttributeDeclaration + """
            namespace Component
            {
                [WindowsRuntime.WindowsRuntimeOverridable]
                public interface IThingOverrides
                {
                    int ComputeCoreValue();
                }

                public interface IThing
                {
                    int GetValue();
                }

                public class ComposableBase : IThingOverrides, IThing
                {
                    public virtual int ComputeCoreValue() => 2;
                    public int GetValue() => 3;
                }
            }
            """;

        var implementations = WinMDGeneratorRunner.GetGeneratedInterfaceImplementations(source);

        Assert.IsTrue(implementations["Component.ComposableBase:Component.IThingOverrides"].Contains("Windows.Foundation.Metadata.OverridableAttribute"));
        Assert.IsFalse(implementations["Component.ComposableBase:Component.IThingOverrides"].Contains("Windows.Foundation.Metadata.DefaultAttribute"));
        Assert.IsTrue(implementations["Component.ComposableBase:Component.IThing"].Contains("Windows.Foundation.Metadata.DefaultAttribute"));
    }

    [TestMethod]
    public void ComposableClass_IgnoredInterfaceBeforeOverridableGetsEmptyDefaultInterface()
    {
        const string source = OverridableAttributeDeclaration + """
            namespace Component
            {
                [WindowsRuntime.WindowsRuntimeOverridable]
                public interface IThingOverrides
                {
                    int ComputeCoreValue();
                }

                public class ComposableBase : System.IEquatable<ComposableBase>, IThingOverrides
                {
                    public bool Equals(ComposableBase other) => ReferenceEquals(this, other);
                    public virtual int ComputeCoreValue() => 2;
                }
            }
            """;

        var implementations = WinMDGeneratorRunner.GetGeneratedInterfaceImplementations(source);

        Assert.IsTrue(implementations["Component.ComposableBase:Component.IThingOverrides"].Contains("Windows.Foundation.Metadata.OverridableAttribute"));
        Assert.IsFalse(implementations["Component.ComposableBase:Component.IThingOverrides"].Contains("Windows.Foundation.Metadata.DefaultAttribute"));
        Assert.IsTrue(implementations["Component.ComposableBase:Component.IComposableBaseClass"].Contains("Windows.Foundation.Metadata.DefaultAttribute"));
    }

    [TestMethod]
    public void SealedClass_AuthoredOverridableInterfaceIsNotMarkedOverridable()
    {
        // A sealed class cannot be derived from, so it is not composable and the interface stays an ordinary one
        var implementations = WinMDGeneratorRunner.GetGeneratedInterfaceImplementations(OverridableAttributeDeclaration + """
            namespace Component
            {
                [WindowsRuntime.WindowsRuntimeOverridable]
                public interface IThingOverrides
                {
                    int ComputeCoreValue();
                }

                public sealed class SealedClass : IThingOverrides
                {
                    public int ComputeCoreValue() => 2;
                }
            }
            """);

        Assert.IsFalse(implementations["Component.SealedClass:Component.IThingOverrides"].Contains("Windows.Foundation.Metadata.OverridableAttribute"));
    }

    [TestMethod]
    public void ComposableClass_AuthoredOverridableAttributeIsNotCopiedToMetadata()
    {
        // '[WindowsRuntimeOverridable]' is a C# authoring marker, not a Windows Runtime attribute: it is
        // projected as '[Overridable]' on the interface implementation and must not leak into the metadata
        var attributes = WinMDGeneratorRunner.GetGeneratedAttributes(OverridableAttributeDeclaration + """
            namespace Component
            {
                [WindowsRuntime.WindowsRuntimeOverridable]
                public interface IThingOverrides
                {
                    int ComputeCoreValue();
                }

                public class ComposableBase : IThingOverrides
                {
                    public virtual int ComputeCoreValue() => 2;
                }
            }
            """);

        Assert.IsFalse(attributes["Component.IThingOverrides"].Contains("WindowsRuntime.WindowsRuntimeOverridableAttribute"));
    }

    /// <summary>
    /// The declaration of the authoring marker attribute, matching the one shipped in <c>WinRT.Runtime</c>.
    /// </summary>
    /// <remarks>
    /// The generator matches the attribute by full name, and test components are compiled against the base class
    /// library only, so declaring it locally (as <c>internal</c>, to keep it out of the generated metadata) is
    /// enough to exercise the exact same code path a real component takes.
    /// </remarks>
    private const string OverridableAttributeDeclaration = """
        namespace WindowsRuntime
        {
            internal sealed class WindowsRuntimeOverridableAttribute : System.Attribute;
        }

        """;
}
