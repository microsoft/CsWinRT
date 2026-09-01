// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using ImplementWinMDTypes;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;

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

// 'TestComponent.Class' can only be activated through the parameterless 'ActivateInstance', and no factory
// is declared for 'MyClass' below, so CsWinRT generates one that constructs it.
ABI.ImplementWinMDTypes.ImplementWinMDTypes_MyClassActivationFactory classFactory = new();

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

// Everything above activates in managed code. Native callers instead go through the generated activation
// entry point ('DllGetActivationFactory' forwards to it), and then call the returned factory through its
// COM vtable. The checks below take that same path.
unsafe
{
    // A type implemented in C# is activated by the name of the class it implements, and its factory is the
    // one written above. The activated instance must be indistinguishable from the real thing to a native
    // caller, so it has to report the implemented runtime class name rather than the implementing type's.
    if (!NativeActivate("TestComponent.Class", out void* classInstance))
    {
        return 111;
    }

    try
    {
        if (!IsRuntimeClassName(classInstance, "TestComponent.Class"))
        {
            return 112;
        }
    }
    finally
    {
        Release(classInstance);
    }

    // A class the application does not implement is not activatable from here
    if (ABI.ImplementWinMDTypes.ManagedExports.GetActivationFactory("TestComponent.NotImplemented".AsSpan()) is not null)
    {
        return 113;
    }
}

// Converting the same implementation again gives back the same projected instance. The wrapper is cached
// against the COM pointer for the authored object, which is itself cached, so identity holds for as long
// as the projected instance is alive.
if (!ReferenceEquals(projectedClass, (global::TestComponent.Class)myClass))
{
    return 114;
}

// The projected instance wraps the implementation, so it is a distinct object from it
if (ReferenceEquals(projectedClass, myClass))
{
    return 115;
}

// A separate implementation gets its own projected instance
MyClass otherClass = new();

if (ReferenceEquals(projectedClass, (global::TestComponent.Class)otherClass))
{
    return 116;
}

// The same holds for a composable class, which resolves through a different wrapper type
if (!ReferenceEquals(projectedComposable, (global::TestComponent.Composable)myComposable))
{
    return 117;
}

// An author can get their own implementation back from a projected instance wrapping it. The conversion is
// explicit because it can fail (the instance may wrap a native implementation, or a different one), unlike
// the implicit one above, which only ever has to create a wrapper.
if (!ReferenceEquals((MyClass)projectedClass, myClass))
{
    return 118;
}

if (!ReferenceEquals((MyComposable)projectedComposable, myComposable))
{
    return 119;
}

// Marshalling an implementation back from native code gives the projected type, not the implementation.
// This is what a consumer receiving one from an API sees, and it is the same thing they would get for a
// class implemented natively, or activated out of process. It matters most for an API returning 'object'
// (no conversion can apply to one, as none can be declared from a base type), which is how the extension
// and factory APIs that hand out these classes are shaped.
unsafe
{
    if (!NativeActivate("TestComponent.Class", out void* nativeInstance))
    {
        return 120;
    }

    try
    {
        object marshalled = WindowsRuntimeObjectMarshaller.ConvertToManaged(nativeInstance);

        // The projected type is what the caller asked for, so a plain type test has to succeed
        if (marshalled is not global::TestComponent.Class marshalledClass)
        {
            return 121;
        }

        // The implementation is deliberately not handed back: it is unrelated to the projected type, so a
        // caller expecting the latter would silently get 'null' from every cast or type test it tried.
        if (marshalled is MyClass)
        {
            return 122;
        }

        // The implementation behind it is still reachable, through the same explicit conversion as above
        if ((MyClass)marshalledClass is null)
        {
            return 123;
        }
    }
    finally
    {
        Release(nativeInstance);
    }
}

return 100;

/// <summary>
/// Activates a runtime class the way a native caller does: through the generated activation entry point,
/// then <c>IActivationFactory.ActivateInstance</c> on the returned factory's COM vtable.
/// </summary>
static unsafe bool NativeActivate(string runtimeClassName, out void* instance)
{
    instance = null;

    void* factory = ABI.ImplementWinMDTypes.ManagedExports.GetActivationFactory(runtimeClassName.AsSpan());

    if (factory is null)
    {
        return false;
    }

    try
    {
        if (Marshal.QueryInterface((nint)factory, WellKnownInterfaceIIDs.IID_IActivationFactory, out nint activationFactory) != 0)
        {
            return false;
        }

        try
        {
            void* activated;

            // 'IActivationFactory.ActivateInstance' follows the 3 'IUnknown' and 3 'IInspectable' slots
            int hr = ((delegate* unmanaged[MemberFunction]<void*, void**, int>)(*(void***)activationFactory)[6])((void*)activationFactory, &activated);

            if (hr != 0)
            {
                return false;
            }

            instance = activated;

            return instance is not null;
        }
        finally
        {
            Release((void*)activationFactory);
        }
    }
    finally
    {
        Release(factory);
    }
}

/// <summary>
/// Checks the runtime class name a COM object reports through <c>IInspectable.GetRuntimeClassName</c>.
/// </summary>
static unsafe bool IsRuntimeClassName(void* instance, string expected)
{
    void* name = null;

    try
    {
        // 'GetRuntimeClassName' is the second of the three 'IInspectable' slots
        if (((delegate* unmanaged[MemberFunction]<void*, void**, int>)(*(void***)instance)[4])(instance, &name) != 0)
        {
            return false;
        }

        uint length;
        char* buffer = WindowsGetStringRawBuffer((nint)name, &length);

        return expected == new string(buffer, 0, (int)length);
    }
    finally
    {
        _ = WindowsDeleteString((nint)name);
    }
}

static unsafe void Release(void* ptr)
{
    if (ptr is not null)
    {
        _ = Marshal.Release((nint)ptr);
    }
}

[DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
static extern unsafe char* WindowsGetStringRawBuffer(nint hstring, uint* length);

[DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
static extern int WindowsDeleteString(nint hstring);

namespace ImplementWinMDTypes
{
    /// <summary>
    /// Implements 'TestComponent.Class', a runtime class with default activation. It declares no factory,
    /// so CsWinRT generates one (see the checks above): the only thing a factory can do for a class with
    /// default activation is construct the implementation.
    /// </summary>
    public sealed class MyClass : global::ABI.TestComponent.Class
    {
        public override int One() => 1;
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
