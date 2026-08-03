// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using ImplementWinMDTypes;

// Exit code convention for functional tests: 100 means success, anything else identifies the check
// that failed. Every check below exercises a Windows Runtime type that is declared in 'TestComponent'
// metadata but implemented here in C#, via the abstract bases the projection generates for it.

// A type deriving from the generated base is a normal managed object, so its members dispatch directly
MyClass myClass = new();

if (myClass.One() != 1)
{
    return 101;
}

// The generated base is separate from the projected class (which is sealed), and bridges to it with an
// implicit conversion. That conversion creates a COM Callable Wrapper for the authored object and then
// resolves the projected type for it, so the result must be a usable 'TestComponent.Class'.
global::TestComponent.Class? projectedClass = myClass;

if (projectedClass is null)
{
    return 102;
}

// The activation factory is authored the same way, by deriving from the generated factory base
MyClassFactory classFactory = new();

if (classFactory.ActivateInstance() is not MyClass)
{
    return 103;
}

// A composable class is authored exactly like a sealed one: the aggregation plumbing of its factory
// methods is generated, and the author only supplies the creation hooks (see 'MyComposableFactory').
MyComposable myComposable = new() { Value = 42 };

if (myComposable.Value != 42)
{
    return 104;
}

if (myComposable.One() != 1 || myComposable.Two() != 2 || myComposable.Three() != 3 || myComposable.Four() != 4)
{
    return 105;
}

global::TestComponent.Composable? projectedComposable = myComposable;

if (projectedComposable is null)
{
    return 106;
}

// The composable factory hands out authored instances through the generated plumbing
MyComposableFactory composableFactory = new();

if (composableFactory.Create() is not MyComposable { Value: 0 })
{
    return 107;
}

if (composableFactory.Create(7) is not MyComposable { Value: 7 })
{
    return 108;
}

// A runtime class deriving from another composable one chains to its base's generated base, so the
// authored type has to satisfy both. 'MyDerived' does, which is what this check confirms.
MyDerived myDerived = new() { Value = 5 };

if (myDerived.Value != 5 || myDerived.One() != 1)
{
    return 109;
}

global::TestComponent.Derived? projectedDerived = myDerived;

if (projectedDerived is null)
{
    return 110;
}

return 100;

namespace ImplementWinMDTypes
{
    /// <summary>
    /// Implements 'TestComponent.Class', a runtime class with default activation. Every member the
    /// Windows Runtime type declares is abstract on the base, so the compiler guarantees none is missed.
    /// </summary>
    public sealed class MyClass : global::ABI.TestComponent.Class
    {
        public override int One() => 1;
    }

    /// <summary>
    /// The activation factory for <see cref="MyClass"/>. 'ActivateInstance' comes from
    /// 'IActivationFactory', which the base implements for a class with default activation.
    /// </summary>
    [global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory(typeof(MyClass))]
    public sealed class MyClassFactory : global::ABI.TestComponent.ClassActivationFactory
    {
        public override object ActivateInstance() => new MyClass();
    }

    /// <summary>
    /// Implements the composable 'TestComponent.Composable'.
    /// </summary>
    public class MyComposable : global::ABI.TestComponent.Composable
    {
        public override int Value { get; set; }

        public override int One() => 1;

        public override int Two() => 2;

        public override int Three() => 3;

        public override int Four() => 4;
    }

    /// <summary>
    /// The activation factory for <see cref="MyComposable"/>.
    /// </summary>
    /// <remarks>
    /// The Windows Runtime factory methods are declared as
    /// <c>CreateInstance(&lt;args&gt;, object baseInterface, out object innerInterface)</c>, i.e. raw COM
    /// aggregation. That is generated onto the base, so all that is implemented here are the creation
    /// hooks, which is the same shape as the sealed <see cref="MyClassFactory"/> above.
    /// </remarks>
    [global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory(typeof(MyComposable))]
    public sealed class MyComposableFactory : global::ABI.TestComponent.ComposableActivationFactory
    {
        /// <summary>Test hook exposing the generated parameterless creation path.</summary>
        public global::ABI.TestComponent.Composable Create() => CreateInstance();

        /// <summary>Test hook exposing the generated parameterized creation path.</summary>
        public global::ABI.TestComponent.Composable Create(int init) => CreateWithValue(init);

        protected override global::ABI.TestComponent.Composable CreateInstance() => new MyComposable();

        protected override global::ABI.TestComponent.Composable CreateWithValue(int init) => new MyComposable { Value = init };

        public override int ExpectComposable(global::TestComponent.Composable t) => 0;

        public override int ExpectRequiredOne(global::TestComponent.IRequiredOne t) => 0;

        public override int ExpectRequiredTwo(global::TestComponent.IRequiredTwo t) => 0;

        public override int ExpectRequiredThree(global::TestComponent.IRequiredThree t) => 0;

        public override int ExpectRequiredFour(global::TestComponent.IRequiredFour t) => 0;
    }

    /// <summary>
    /// Implements 'TestComponent.Derived', a composable runtime class deriving from another one. Its
    /// generated base chains to <c>ABI.TestComponent.Composable</c>, so the base class's members are
    /// abstract here too and have to be supplied as well.
    /// </summary>
    public sealed class MyDerived : global::ABI.TestComponent.Derived
    {
        public override int Value { get; set; }

        public override int One() => 1;

        public override int Two() => 2;

        public override int Three() => 3;

        public override int Four() => 4;
    }

    /// <summary>
    /// The activation factory for <see cref="MyDerived"/>.
    /// </summary>
    [global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory(typeof(MyDerived))]
    public sealed class MyDerivedFactory : global::ABI.TestComponent.DerivedActivationFactory
    {
        protected override global::ABI.TestComponent.Derived CreateInstance() => new MyDerived();
    }
}
