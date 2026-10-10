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

// The base is separate from the projected class (which is sealed) and bridges to it with an implicit
// conversion, creating a COM Callable Wrapper and resolving the projected type for it.
global::TestComponent.Class projectedClass = myClass;

if (projectedClass is null)
{
    return 102;
}

// Calls through the projected type go through the COM Callable Wrapper's vtable, and have to reach
// the authored overrides.
if (projectedClass.One() != 1)
{
    return 101;
}

// A composable class is authored exactly like a sealed one: the aggregation plumbing of its factory
// methods is generated, and the author only supplies the creation hooks (see 'MyComposableFactory').
MyComposable myComposable = new() { Value = 42 };

global::TestComponent.Composable projectedComposable = myComposable;

if (projectedComposable is null)
{
    return 106;
}

if (projectedComposable.Value != 42)
{
    return 104;
}

if (projectedComposable.One() != 1 || projectedComposable.Two() != 2 || projectedComposable.Three() != 3 || projectedComposable.Four() != 4)
{
    return 105;
}

// A native caller constructs a composable class with arguments through its composable factory interface,
// passing no outer for standalone activation. The generated plumbing forwards to the authored hook.
unsafe
{
    if (!NativeCreateComposableWithValue(7, outer: null, out void* composableInstance))
    {
        return 107;
    }

    try
    {
        if (!IsRuntimeClassName(composableInstance, "TestComponent.Composable") ||
            WindowsRuntimeObjectMarshaller.ConvertToManaged(composableInstance) is not global::TestComponent.Composable { Value: 7 })
        {
            return 107;
        }
    }
    finally
    {
        Release(composableInstance);
    }

    // Aggregating a C# implementation is not supported, so a non-null outer is rejected
    using WindowsRuntimeObjectReferenceValue outer = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(new object());

    if (NativeCreateComposableWithValue(7, outer.GetThisPtrUnsafe(), out _))
    {
        return 108;
    }
}

// A runtime class deriving from another composable one chains to its base's generated base, so the
// authored type has to satisfy both.
MyDerived myDerived = new() { Value = 5 };

global::TestComponent.Derived projectedDerived = myDerived;

if (projectedDerived is null)
{
    return 110;
}

if (projectedDerived.Value != 5 || projectedDerived.One() != 1)
{
    return 109;
}

// Everything above activates in managed code. Native callers instead go through the generated
// activation entry point and call the returned factory through its COM vtable.
unsafe
{
    // Activated by the name of the class it implements, so it has to report that runtime class
    // name rather than the implementing type's.
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

    // An unsealed class is only activatable through its composable factory in metadata, which a caller
    // activating it by class name does not use. Implemented here, its public parameterless constructor also
    // backs default activation through 'IActivationFactory' ('MyComposableFactory' for 'Composable', and a
    // generated factory for 'Derived').
    foreach (string composableClassName in (string[])["TestComponent.Composable", "TestComponent.Derived"])
    {
        if (!NativeActivate(composableClassName, out void* composableInstance))
        {
            return 124;
        }

        try
        {
            if (!IsRuntimeClassName(composableInstance, composableClassName))
            {
                return 125;
            }
        }
        finally
        {
            Release(composableInstance);
        }
    }
}

// Converting the same implementation again gives back the same projected instance, since the
// wrapper is cached against the authored object's COM pointer.
if (!ReferenceEquals(projectedClass, (global::TestComponent.Class)myClass))
{
    return 114;
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

// An author can get their own implementation back. The conversion is explicit because it can fail:
// the instance may wrap a native implementation, or a different one.
if (!ReferenceEquals((MyClass)projectedClass, myClass))
{
    return 118;
}

if (!ReferenceEquals((MyComposable)projectedComposable, myComposable))
{
    return 119;
}

// Marshalling an implementation back from native code gives the projected type, not the
// implementation, exactly as it would for a class implemented natively. This matters most for an
// API returning 'object', which no user-defined conversion can apply to.
unsafe
{
    if (!NativeActivate("TestComponent.Class", out void* nativeInstance))
    {
        return 120;
    }

    try
    {
        object marshalled = WindowsRuntimeObjectMarshaller.ConvertToManaged(nativeInstance);

        // The projected type is what the caller asked for, so a plain type test has to succeed. The
        // implementation is deliberately not handed back: it is unrelated to the projected type, so a caller
        // expecting the latter would silently get 'null' from every cast or type test it tried.
        if (marshalled is not global::TestComponent.Class marshalledClass)
        {
            return 121;
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
/// Calls <c>IComposableFactory.CreateWithValue</c> on the activation factory for 'TestComponent.Composable'
/// through its COM vtable, the way a native caller constructs the class with arguments.
/// </summary>
static unsafe bool NativeCreateComposableWithValue(int init, void* outer, out void* instance)
{
    instance = null;

    Guid iidIComposableFactory = new("B7C48344-637C-5FBC-A7F7-1A27FE08CF6B");
    void* factory = ABI.ImplementWinMDTypes.ManagedExports.GetActivationFactory("TestComponent.Composable".AsSpan());

    if (factory is null)
    {
        return false;
    }

    try
    {
        if (Marshal.QueryInterface((nint)factory, iidIComposableFactory, out nint composableFactory) != 0)
        {
            return false;
        }

        try
        {
            void* inner = null;
            void* created = null;

            // 'CreateWithValue' follows 'CreateInstance', after the 3 'IUnknown' and 3 'IInspectable' slots
            int hr = ((delegate* unmanaged[MemberFunction]<void*, int, void*, void**, void**, int>)(*(void***)composableFactory)[7])(
                (void*)composableFactory, init, outer, &inner, &created);

            // Without an outer, the instance is its own inner
            Release(inner);

            if (hr != 0 || created is null)
            {
                Release(created);

                return false;
            }

            instance = created;

            return true;
        }
        finally
        {
            Release((void*)composableFactory);
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
    /// generated onto the base, leaving only the construction itself to implement here.
    /// </remarks>
    [global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory(typeof(MyComposable))]
    public sealed class MyComposableFactory : global::ABI.TestComponent.ComposableActivationFactory
    {
        public override global::ABI.TestComponent.Composable ActivateInstance() => new MyComposable();

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
}
