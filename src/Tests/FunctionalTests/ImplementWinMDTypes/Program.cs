// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using ImplementWinMDTypes;

// Implements Windows Runtime types declared in 'TestComponent' metadata by deriving from the abstract
// bases the projection generates for them.

MyClass myClass = new();

if (myClass.One() != 1)
{
    return 101;
}

// The base is separate from the projected class (which is sealed) and bridges to it with an implicit
// conversion, creating a COM Callable Wrapper and resolving the projected type for it.
global::TestComponent.Class projectedClass = myClass;

if (projectedClass is null)
{
    return 102;
}

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

global::TestComponent.Composable projectedComposable = myComposable;

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
// authored type has to satisfy both.
MyDerived myDerived = new() { Value = 5 };

if (myDerived.Value != 5 || myDerived.One() != 1)
{
    return 109;
}

global::TestComponent.Derived projectedDerived = myDerived;

if (projectedDerived is null)
{
    return 110;
}

return 100;

namespace ImplementWinMDTypes
{
    /// <summary>
    /// Implements 'TestComponent.Class', a runtime class with default activation.
    /// </summary>
    public sealed class MyClass : global::ABI.TestComponent.Class
    {
        public override int One() => 1;
    }

    /// <summary>
    /// The activation factory for <see cref="MyClass"/>. <c>ActivateInstance</c> comes from
    /// <c>IActivationFactory</c>, which the base implements for a class with default activation.
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
    /// The Windows Runtime factory methods take an outer and an inner (raw COM aggregation). That is
    /// generated onto the base, leaving only the creation hooks to implement here.
    /// </remarks>
    [global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory(typeof(MyComposable))]
    public sealed class MyComposableFactory : global::ABI.TestComponent.ComposableActivationFactory
    {
        /// <summary>Exposes the protected creation hooks so the checks above can call them.</summary>
        public global::ABI.TestComponent.Composable Create() => CreateInstance();

        /// <inheritdoc cref="Create()"/>
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
    /// Implements 'TestComponent.Derived', a composable runtime class deriving from another one. Its base
    /// chains to <c>ABI.TestComponent.Composable</c>, so that class's members have to be supplied too.
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
