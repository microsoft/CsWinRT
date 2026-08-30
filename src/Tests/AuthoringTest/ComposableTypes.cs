using System;
using WindowsRuntime;
using WindowsRuntime.InteropServices;

namespace AuthoringTest;

/// <summary>
/// A Windows Runtime interface authored in this component, required by <see cref="IComposableThing"/>.
/// </summary>
/// <remarks>
/// Interface inheritance matters for COM aggregation: the CCW of a class exposes an entry for every
/// interface in the transitive closure of the ones it implements, and all of them can be handed out by
/// the non-delegating inner object of an aggregate, so all of them have to delegate <c>IUnknown</c>.
/// </remarks>
public interface IComposableThingBase
{
    int GetBaseThingValue();
}

/// <summary>
/// A Windows Runtime interface authored in this component and implemented by <see cref="ComposableBase"/>.
/// </summary>
/// <remarks>
/// Interfaces authored in the same component as a composable class are the only ones that can be exposed by
/// that class when it takes part in COM aggregation, because their CCW vtables are generated together with it
/// (and can therefore delegate their <c>IUnknown</c> methods to the controlling outer object).
/// </remarks>
public interface IComposableThing : IComposableThingBase
{
    int GetThingValue();
}

/// <summary>
/// The overridable surface of <see cref="ComposableBase"/>, declared explicitly as a Windows Runtime interface.
/// </summary>
/// <remarks>
/// <para>
/// This is the C# equivalent of an <c>[overridable] interface</c> member on a runtime class in MIDL, and it is the
/// same shape XAML uses (e.g. <c>IControlOverrides</c>). Because it is a real interface authored in this component,
/// it can be named from the component itself, which is what lets <see cref="ComposableBase"/> dispatch to the most
/// derived implementation of its members (see <see cref="ComposableBase.CallComputeCoreValue"/>).
/// </para>
/// <para>
/// The other way of declaring an overridable member is to simply make it <c>virtual</c> on the composable class
/// (see <see cref="ComposableBase.ComputeValue"/>), which CsWinRT projects onto a synthesized
/// <c>IComposableBaseOverrides</c> interface. That form cannot be named from the component, so it only supports
/// dispatching from the derived (composing) type down to the base implementation.
/// </para>
/// </remarks>
[WindowsRuntimeOverridable]
public interface IComposableBaseOverridable
{
    /// <summary>
    /// An overridable member a derived Windows Runtime type is allowed to replace.
    /// </summary>
    int ComputeCoreValue();
}

public class ComposableBase : IComposableThing, IComposableBaseOverridable
{
    private static int _computeCoreValueCallCount;
    private static int _describeSelfCallCount;

    private readonly int _value;

    public ComposableBase()
        : this(42)
    {
    }

    public ComposableBase(int value)
    {
        _value = value;
    }

    /// <summary>
    /// A read/write instance property, to verify that a native derived class can get and set state
    /// on the composable base it aggregates.
    /// </summary>
    public string Tag { get; set; } = "base";

    /// <summary>
    /// A static property on a composable class. Statics live on the activation factory, so they are
    /// reachable without ever creating an instance (aggregated or otherwise).
    /// </summary>
    public static string DefaultTag => "default";

    /// <summary>
    /// A static method on a composable class.
    /// </summary>
    public static string DescribeValue(int value) => $"ComposableBase({value})";

    /// <summary>
    /// A member only derived classes can call. It is projected onto the <c>[Protected]</c> exclusive
    /// interface of the runtime class, which is exactly what a native derived class queries for.
    /// </summary>
    protected int GetSecretValue() => _value * 3;

    /// <summary>
    /// A read/write property only derived classes can access, also projected onto the
    /// <c>[Protected]</c> exclusive interface.
    /// </summary>
    protected string SecretTag { get; set; } = "secret";

    /// <summary>
    /// A public overridable member. Windows Runtime has no notion of a public overridable member, so
    /// this is projected onto the <c>[Overridable]</c> exclusive interface (and is therefore surfaced
    /// as protected by the language projections), exactly like the XAML <c>On*</c> members.
    /// </summary>
    public virtual int ComputeValue() => _value * 2;

    /// <summary>
    /// A protected overridable member, also projected onto the <c>[Overridable]</c> exclusive interface.
    /// </summary>
    protected virtual string DescribeCore() => $"ComposableBase:{_value}";

    /// <summary>
    /// An overridable read-only property.
    /// </summary>
    protected virtual int OverridableValue => _value + 1000;

    public int GetValue() => _value;

    public string GetName() => GetType().Name;

    /// <summary>
    /// Invokes the protected surface from the base implementation, so tests can verify it keeps
    /// working while the instance is the inner object of a COM aggregate.
    /// </summary>
    public int CallGetSecretValue() => GetSecretValue();

    /// <summary>
    /// Invokes the overridable members through plain managed virtual dispatch, so tests can verify
    /// which implementation a C# derived class ends up running.
    /// </summary>
    public int CallComputeValue() => ComputeValue();

    /// <inheritdoc cref="CallComputeValue"/>
    public string CallDescribeCore() => DescribeCore();

    /// <inheritdoc cref="CallComputeValue"/>
    public int CallOverridableValue() => OverridableValue;

    /// <inheritdoc/>
    public int GetThingValue() => _value + 1;

    /// <inheritdoc/>
    public int GetBaseThingValue() => _value + 2;

    /// <summary>
    /// Returns the current instance as an <c>IInspectable</c>, so that consumers can verify that an aggregated
    /// object marshals itself to native with the identity of its controlling outer object.
    /// </summary>
    public object GetSelf() => this;

    /// <summary>
    /// Returns the current instance typed as the runtime class itself, exercising the class-typed marshalling path.
    /// </summary>
    public ComposableBase GetSelfAsBase() => this;

    /// <summary>
    /// Returns the current instance typed as an authored interface, exercising the interface-typed marshalling path.
    /// </summary>
    public IComposableThing GetSelfAsThing() => this;

    /// <summary>
    /// Returns the current instance typed as an inherited authored interface.
    /// </summary>
    public IComposableThingBase GetSelfAsThingBase() => this;

    /// <summary>
    /// Checks whether a class-typed object marshalled back in from native code is this very managed instance.
    /// </summary>
    public bool IsSameInstance(ComposableBase other) => ReferenceEquals(this, other);

    /// <summary>
    /// Checks whether an interface-typed object marshalled back in from native code is this very managed instance.
    /// </summary>
    public bool IsSameThing(IComposableThing other) => ReferenceEquals(this, other);

    /// <summary>
    /// The number of times the implementation of <see cref="ComputeCoreValue"/> declared by this class has run.
    /// </summary>
    public static int ComputeCoreValueCallCount => _computeCoreValueCallCount;

    /// <summary>
    /// The number of times <see cref="DescribeSelf"/> has run.
    /// </summary>
    public static int DescribeSelfCallCount => _describeSelfCallCount;

    /// <summary>
    /// Resets the instrumentation counters, so a test can assert on exact call counts.
    /// </summary>
    public static void ResetCallCounts()
    {
        _computeCoreValueCallCount = 0;
        _describeSelfCallCount = 0;
    }

    /// <summary>
    /// Runs a full garbage collection and waits for pending finalizers, so that tests can observe the native
    /// references held by managed wrappers being released.
    /// </summary>
    /// <remarks>
    /// Resolving the controlling outer object of an aggregate produces an RCW for it, which holds a reference on
    /// the aggregate until it is collected. That is inherent to referencing a native object from managed code, so
    /// tests asserting on the destruction of an aggregate they dispatched through have to force a collection first.
    /// </remarks>
    public static void CollectManagedObjects()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    /// <summary>
    /// A plain (non overridable) instance method. A derived Windows Runtime type cannot replace it, so it always
    /// runs this implementation, even while the instance is the inner object of a COM aggregate.
    /// </summary>
    public string DescribeSelf()
    {
        _describeSelfCallCount++;

        return $"ComposableBase({_value})";
    }

    /// <inheritdoc/>
    /// <remarks>
    /// This is the implementation of an authored <c>[Overridable]</c> member, so a derived Windows Runtime type
    /// (including one implemented in C++/WinRT) can replace it for the whole aggregate.
    /// </remarks>
    public virtual int ComputeCoreValue()
    {
        _computeCoreValueCallCount++;

        return _value * 10;
    }

    /// <summary>
    /// Dispatches to the most derived implementation of <see cref="IComposableBaseOverridable"/>.
    /// </summary>
    /// <remarks>
    /// This is the C#/WinRT equivalent of <c>overridable()</c> in C++/WinRT. When this instance is the inner object
    /// of a COM aggregate, the most derived implementation lives on the controlling outer object, so it has to be
    /// resolved through it: plain managed virtual dispatch would only ever reach this class (or a C# derived one),
    /// because the aggregate is two distinct objects. When there is no aggregation, this is just <c>this</c>, and
    /// ordinary managed virtual dispatch picks the right implementation.
    /// </remarks>
    private IComposableBaseOverridable Overridable =>
        WindowsRuntimeComposition.GetControllingOuterObject(this) as IComposableBaseOverridable ?? this;

    /// <summary>
    /// Invokes <see cref="IComposableBaseOverridable.ComputeCoreValue"/> on the most derived implementation.
    /// </summary>
    public int CallComputeCoreValue() => Overridable.ComputeCoreValue();

    /// <summary>
    /// Makes two instance calls on an instance of this runtime class handed to managed code by native code.
    /// </summary>
    /// <param name="instance">The instance to probe (possibly the inner object of a native COM aggregate).</param>
    /// <returns>The result of both calls, plus whether <paramref name="instance"/> is aggregated.</returns>
    /// <remarks>
    /// Both calls are ordinary instance calls made from C#. <see cref="DescribeSelf"/> is not overridable, so it
    /// always runs the implementation of this class, while <see cref="CallComputeCoreValue"/> resolves the most
    /// derived implementation, which is the one supplied by the derived Windows Runtime type when the instance is
    /// the inner object of an aggregate.
    /// </remarks>
    public static string ProbeAggregate(ComposableBase instance)
    {
        string fromInner = instance.DescribeSelf();
        int fromOuter = instance.CallComputeCoreValue();
        int isAggregated = WindowsRuntimeComposition.IsAggregated(instance) ? 1 : 0;

        return $"{fromInner}|{fromOuter}|{isAggregated}";
    }
}

public sealed class CSharpDerivedComposable : ComposableBase
{
    public CSharpDerivedComposable()
        : base(84)
    {
    }

    public int GetDerivedValue() => GetValue() * 2;

    /// <summary>
    /// Overrides the public overridable member of the base runtime class. A C# derived class is a
    /// single managed object (no COM aggregation involved), so plain managed virtual dispatch is what
    /// selects this implementation, both from managed code and from the CCW of the base interface.
    /// </summary>
    public override int ComputeValue() => base.ComputeValue() + 1;

    /// <inheritdoc cref="ComputeValue"/>
    protected override string DescribeCore() => "CSharpDerived:" + base.DescribeCore();

    /// <inheritdoc cref="ComputeValue"/>
    protected override int OverridableValue => base.OverridableValue + 1;

    /// <summary>
    /// A C# derived class can call the protected surface of its base as usual.
    /// </summary>
    public int GetDerivedSecretValue() => CallGetSecretValue();
}
