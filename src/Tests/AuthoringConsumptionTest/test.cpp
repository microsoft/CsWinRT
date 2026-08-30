#include "pch.h"

using namespace winrt;
using namespace Windows::Foundation;
using namespace AuthoringTest;
using namespace AuthoringTest::AnotherNamespace;

struct NativeDerivedComposable : winrt::AuthoringTest::ComposableBaseT<NativeDerivedComposable>
{
    static inline int32_t s_destructorCount{};

    NativeDerivedComposable()
        : ComposableBaseT()
    {
    }

    explicit NativeDerivedComposable(int32_t value)
        : ComposableBaseT(value)
    {
    }

    ~NativeDerivedComposable()
    {
        ++s_destructorCount;
    }

    winrt::hstring GetRuntimeClassName() const
    {
        return L"AuthoringTest.NativeDerivedComposable";
    }
};

// A native derived class that both consumes the '[Protected]' surface of the composable base and
// overrides its '[Overridable]' members. Each override calls the base implementation explicitly
// (through the 'ComposableBaseT<D>' base, which routes to the non-delegating inner object) so the
// tests can tell the two dispatch directions apart.
struct NativeOverridingComposable : winrt::AuthoringTest::ComposableBaseT<NativeOverridingComposable>
{
    using base_type = winrt::AuthoringTest::ComposableBaseT<NativeOverridingComposable>;

    static inline int32_t s_destructorCount{};
    static inline int32_t s_computeCoreValueCount{};

    explicit NativeOverridingComposable(int32_t value)
        : ComposableBaseT(value)
    {
    }

    ~NativeOverridingComposable()
    {
        ++s_destructorCount;
    }

    winrt::hstring GetRuntimeClassName() const
    {
        return L"AuthoringTest.NativeOverridingComposable";
    }

    int32_t ComputeValue()
    {
        return base_type::ComputeValue() + 1;
    }

    // Overrides the member of the authored '[Overridable]' interface of the composable base. Unlike the
    // members above (which come from the interface CsWinRT synthesizes out of the 'virtual' members of the
    // class), this one is declared by an interface the component authors itself, so the managed base can
    // name it and dispatch to this override through the controlling outer object.
    int32_t ComputeCoreValue()
    {
        ++s_computeCoreValueCount;

        return base_type::ComputeCoreValue() + 7;
    }

    winrt::hstring DescribeCore()
    {
        return L"NativeDerived:" + base_type::DescribeCore();
    }

    int32_t OverridableValue()
    {
        return base_type::OverridableValue() + 1;
    }

    // The '[Protected]' members are only reachable from within the derived implementation
    int32_t CallProtectedGetSecretValue()
    {
        return this->GetSecretValue();
    }

    winrt::hstring CallProtectedGetSecretTag()
    {
        return this->SecretTag();
    }

    void CallProtectedSetSecretTag(winrt::hstring const& value)
    {
        this->SecretTag(value);
    }

    // The base implementations, reached explicitly through the non-delegating inner object
    int32_t CallBaseComputeValue()
    {
        return base_type::ComputeValue();
    }

    winrt::hstring CallBaseDescribeCore()
    {
        return base_type::DescribeCore();
    }

    int32_t CallBaseOverridableValue()
    {
        return base_type::OverridableValue();
    }
};

TEST(AuthoringTest, ComposableClassStandaloneActivation)
{
    ComposableBase standalone;
    EXPECT_EQ(standalone.GetValue(), 42);
    EXPECT_EQ(standalone.GetName(), L"ComposableBase");

    ComposableBase parameterized(7);
    EXPECT_EQ(parameterized.GetValue(), 7);

    // Standalone activation is not aggregated, so the object keeps its own COM identity
    // and reports its own runtime class name.
    EXPECT_EQ(
        winrt::get_abi(standalone.as<winrt::Windows::Foundation::IUnknown>()),
        winrt::get_abi(standalone.as<winrt::Windows::Foundation::IUnknown>()));
    EXPECT_EQ(winrt::get_class_name(standalone), L"AuthoringTest.ComposableBase");
    EXPECT_EQ(
        winrt::get_abi(standalone.GetSelf().as<winrt::Windows::Foundation::IUnknown>()),
        winrt::get_abi(standalone.as<winrt::Windows::Foundation::IUnknown>()));
}

TEST(AuthoringTest, ComposableClassNativeDerivation)
{
    auto nativeDerived = winrt::make<NativeDerivedComposable>(11);

    EXPECT_EQ(nativeDerived.GetValue(), 11);
}

// The public surface of a composable base has to keep working through the delegating interface
// pointers of an aggregate: instance methods, read/write properties, and the statics that live on
// the activation factory rather than on the instance.
TEST(AuthoringTest, ComposableClassMembers)
{
    // Statics never involve an instance, so they behave the same in every scenario
    EXPECT_EQ(ComposableBase::DefaultTag(), L"default");
    EXPECT_EQ(ComposableBase::DescribeValue(5), L"ComposableBase(5)");

    ComposableBase standalone(3);

    EXPECT_EQ(standalone.GetValue(), 3);
    EXPECT_EQ(standalone.Tag(), L"base");

    standalone.Tag(L"standalone");

    EXPECT_EQ(standalone.Tag(), L"standalone");

    // The same members, reached through the base interface of an aggregate
    winrt::com_ptr<NativeDerivedComposable> nativeDerived = winrt::make_self<NativeDerivedComposable>(4);
    ComposableBase asBase = nativeDerived.as<ComposableBase>();

    EXPECT_EQ(asBase.GetValue(), 4);
    EXPECT_EQ(asBase.Tag(), L"base");

    asBase.Tag(L"aggregated");

    EXPECT_EQ(asBase.Tag(), L"aggregated");

    // The base instance of the aggregate is a distinct object, so it kept its own property value
    EXPECT_EQ(standalone.Tag(), L"standalone");

    // A C# class deriving from the composable base gets the inherited members as well
    CSharpDerivedComposable csharpDerived;

    EXPECT_EQ(csharpDerived.GetValue(), 84);
    EXPECT_EQ(csharpDerived.Tag(), L"base");

    csharpDerived.Tag(L"csharp");

    EXPECT_EQ(csharpDerived.Tag(), L"csharp");
}

// A native derived class reaches the '[Protected]' members of its composable base through the
// non-delegating inner object, exactly like a C++/WinRT composable base would expose them.
TEST(AuthoringTest, ComposableClassNativeDerivationProtectedMembers)
{
    winrt::com_ptr<NativeOverridingComposable> nativeDerived = winrt::make_self<NativeOverridingComposable>(5);

    EXPECT_EQ(nativeDerived->CallProtectedGetSecretValue(), 15);
    EXPECT_EQ(nativeDerived->CallProtectedGetSecretTag(), L"secret");

    nativeDerived->CallProtectedSetSecretTag(L"updated");

    EXPECT_EQ(nativeDerived->CallProtectedGetSecretTag(), L"updated");

    // The protected members are not part of the public surface of the runtime class, so they are
    // only reachable through the exclusive '[Protected]' interface of the base.
    ComposableBase asBase = nativeDerived.as<ComposableBase>();

    EXPECT_EQ(asBase.CallGetSecretValue(), 15);
    EXPECT_EQ(asBase.as<IComposableBaseProtected>().GetSecretValue(), 15);
    EXPECT_EQ(asBase.as<IComposableBaseProtected>().SecretTag(), L"updated");
}

// The protected surface is also available on a standalone (non aggregated) instance and to a C#
// derived class, where no COM aggregation is involved at all.
TEST(AuthoringTest, ComposableClassProtectedMembersWithoutAggregation)
{
    ComposableBase standalone(5);

    EXPECT_EQ(standalone.CallGetSecretValue(), 15);
    EXPECT_EQ(standalone.as<IComposableBaseProtected>().GetSecretValue(), 15);
    EXPECT_EQ(standalone.as<IComposableBaseProtected>().SecretTag(), L"secret");

    standalone.as<IComposableBaseProtected>().SecretTag(L"standalone-secret");

    EXPECT_EQ(standalone.as<IComposableBaseProtected>().SecretTag(), L"standalone-secret");

    CSharpDerivedComposable csharpDerived;

    EXPECT_EQ(csharpDerived.GetDerivedSecretValue(), 252);
    EXPECT_EQ(csharpDerived.as<IComposableBaseProtected>().GetSecretValue(), 252);
}

// Overridable dispatch, in both directions:
//
//  - going through the base projection (ie. through a delegating interface pointer of the aggregate)
//    must resolve on the controlling outer object, so the native override wins;
//  - invoking the base implementation explicitly from the native derived class must go through the
//    non-delegating inner object, so the managed base implementation runs.
//
// Note: these members come from the interface CsWinRT synthesizes out of the 'virtual' members of the
// composable class, which the authored component cannot name. A call the managed base implementation
// makes to one of them (e.g. 'CallComputeValue') is therefore plain managed virtual dispatch, and stays
// on the base implementation while the object is aggregated. Dispatching to the most derived
// implementation from managed code requires an authored '[Overridable]' interface, which is covered by
// 'ComposableClassNativeDerivationOverridableDispatchFromManagedCode' below.
TEST(AuthoringTest, ComposableClassNativeDerivationOverridableDispatch)
{
    winrt::com_ptr<NativeOverridingComposable> nativeDerived = winrt::make_self<NativeOverridingComposable>(10);

    ComposableBase asBase = nativeDerived.as<ComposableBase>();
    IComposableBaseOverrides asOverrides = asBase.as<IComposableBaseOverrides>();

    // Through the base projection: the native override runs (base value + 1)
    EXPECT_EQ(asOverrides.ComputeValue(), 21);
    EXPECT_EQ(asOverrides.DescribeCore(), L"NativeDerived:ComposableBase:10");
    EXPECT_EQ(asOverrides.OverridableValue(), 1011);

    // Explicitly invoking the base implementation: the managed base runs
    EXPECT_EQ(nativeDerived->CallBaseComputeValue(), 20);
    EXPECT_EQ(nativeDerived->CallBaseDescribeCore(), L"ComposableBase:10");
    EXPECT_EQ(nativeDerived->CallBaseOverridableValue(), 1010);

    // Every overridable interface pointer of the aggregate still has the identity of the outer
    EXPECT_EQ(
        winrt::get_abi(asOverrides.as<winrt::Windows::Foundation::IUnknown>()),
        winrt::get_abi(nativeDerived.as<winrt::Windows::Foundation::IUnknown>()));
}

// The scenario the authored '[Overridable]' interface support exists for: a native derived aggregate is handed
// back to C#, and managed code makes two ordinary instance calls on it.
//
//  - 'DescribeSelf' is not overridable, so it runs the implementation authored in C# (the inner object);
//  - 'CallComputeCoreValue' is overridable, so it resolves the most derived implementation, which is the
//    override implemented by this C++ controlling outer. That override in turn calls the base implementation
//    explicitly, which comes back into the same managed instance through the non-delegating inner object.
//
// Both calls are made from inside C#, on the very same instance, and neither is simulated with a callback: the
// managed base resolves its controlling outer and calls the authored '[Overridable]' interface on it, which is
// the C#/WinRT equivalent of 'overridable()' in C++/WinRT.
TEST(AuthoringTest, ComposableClassNativeDerivationOverridableDispatchFromManagedCode)
{
    NativeOverridingComposable::s_computeCoreValueCount = 0;
    ComposableBase::ResetCallCounts();

    winrt::com_ptr<NativeOverridingComposable> nativeDerived = winrt::make_self<NativeOverridingComposable>(10);

    ComposableBase asBase = nativeDerived.as<ComposableBase>();

    // 'ComposableBase(10)' is the C# implementation of 'DescribeSelf', '107' is the C++ override of
    // 'ComputeCoreValue' (10 * 10, computed by the managed base implementation, plus 7), and '1' means the
    // managed code observed the instance as being aggregated.
    EXPECT_EQ(ComposableBase::ProbeAggregate(asBase), L"ComposableBase(10)|107|1");

    // The C++ override ran exactly once, reached from managed code
    EXPECT_EQ(NativeOverridingComposable::s_computeCoreValueCount, 1);

    // ...and it reached the C# implementation of the same member exactly once, through the inner object
    EXPECT_EQ(ComposableBase::ComputeCoreValueCallCount(), 1);

    // The non overridable member ran the C# implementation exactly once, without ever leaving managed code
    EXPECT_EQ(ComposableBase::DescribeSelfCallCount(), 1);
}

// Same two calls, made on instances that are not aggregated. Both must resolve to the implementation authored
// in C#: the overridable member has no more derived implementation to dispatch to, so it is plain managed
// virtual dispatch (which is also how a C# derived class gets its own override picked).
TEST(AuthoringTest, ComposableClassOverridableDispatchFromManagedCodeWithoutAggregation)
{
    ComposableBase::ResetCallCounts();

    ComposableBase standalone(10);

    EXPECT_EQ(ComposableBase::ProbeAggregate(standalone), L"ComposableBase(10)|100|0");
    EXPECT_EQ(standalone.CallComputeCoreValue(), 100);
    EXPECT_EQ(standalone.as<IComposableBaseOverridable>().ComputeCoreValue(), 100);

    EXPECT_EQ(ComposableBase::ComputeCoreValueCallCount(), 3);
    EXPECT_EQ(ComposableBase::DescribeSelfCallCount(), 1);

    ComposableBase::ResetCallCounts();

    CSharpDerivedComposable csharpDerived;

    EXPECT_EQ(ComposableBase::ProbeAggregate(csharpDerived), L"ComposableBase(84)|840|0");
    EXPECT_EQ(ComposableBase::ComputeCoreValueCallCount(), 1);

    // A native derived class that does not override the member is aggregated all the same, and the default
    // forwarder that C++/WinRT generates for it routes right back to the managed base implementation.
    ComposableBase::ResetCallCounts();

    winrt::com_ptr<NativeDerivedComposable> nativeDerived = winrt::make_self<NativeDerivedComposable>(10);

    EXPECT_EQ(ComposableBase::ProbeAggregate(nativeDerived.as<ComposableBase>()), L"ComposableBase(10)|100|1");
    EXPECT_EQ(ComposableBase::ComputeCoreValueCallCount(), 1);
}

// The round-trip has to leave the identity and the reference counts of the aggregate untouched: managed code
// resolving its controlling outer object must not disturb them, and neither must the calls it makes through it.
// Repeating it makes an unbalanced 'AddRef'/'Release' fail deterministically, either as a leak (the aggregate
// would outlive its last reference) or as a premature destruction.
//
// Note: resolving the controlling outer object produces an RCW for it, which holds a reference on the aggregate
// until it is collected, so the last release is only observable after a garbage collection. That is inherent to
// referencing a native object from managed code, and is why the check below forces one first.
TEST(AuthoringTest, ComposableClassNativeDerivationOverridableDispatchFromManagedCodeIdentityAndLifetime)
{
    // Flush any RCW an earlier test left holding a reference on its own aggregate, so the destructor
    // count below only ever reflects the aggregate this test creates
    ComposableBase::CollectManagedObjects();

    NativeOverridingComposable::s_destructorCount = 0;
    NativeOverridingComposable::s_computeCoreValueCount = 0;
    ComposableBase::ResetCallCounts();

    {
        winrt::com_ptr<NativeOverridingComposable> nativeDerived = winrt::make_self<NativeOverridingComposable>(3);

        winrt::Windows::Foundation::IUnknown outerIdentity = nativeDerived.as<winrt::Windows::Foundation::IUnknown>();

        ComposableBase asBase = nativeDerived.as<ComposableBase>();

        for (int i = 0; i < 100; ++i)
        {
            EXPECT_EQ(ComposableBase::ProbeAggregate(asBase), L"ComposableBase(3)|37|1");

            // Managed code still sees the very same instance, and it still marshals itself back out with
            // the identity of the controlling outer object
            ComposableBase self = asBase.GetSelfAsBase();

            EXPECT_TRUE(asBase.IsSameInstance(self));
            EXPECT_EQ(winrt::get_abi(self.as<winrt::Windows::Foundation::IUnknown>()), winrt::get_abi(outerIdentity));
        }

        EXPECT_EQ(NativeOverridingComposable::s_computeCoreValueCount, 100);
        EXPECT_EQ(ComposableBase::ComputeCoreValueCallCount(), 100);
        EXPECT_EQ(ComposableBase::DescribeSelfCallCount(), 100);

        // The aggregate is still alive and functional after all the round-trips
        EXPECT_EQ(NativeOverridingComposable::s_destructorCount, 0);
        EXPECT_EQ(asBase.GetValue(), 3);
    }

    // Every native reference is gone, so the aggregate is destroyed exactly once, as soon as the RCW that
    // managed code created for the controlling outer object is collected
    ComposableBase::CollectManagedObjects();

    EXPECT_EQ(NativeOverridingComposable::s_destructorCount, 1);
}

// Without a derived class overriding them, the overridable members of a composable class resolve to
// the base implementation, whether the instance is standalone or is aggregated by a native class
// that overrides nothing.
TEST(AuthoringTest, ComposableClassOverridableDispatchWithoutOverride)
{
    ComposableBase standalone(10);
    IComposableBaseOverrides standaloneOverrides = standalone.as<IComposableBaseOverrides>();

    EXPECT_EQ(standaloneOverrides.ComputeValue(), 20);
    EXPECT_EQ(standaloneOverrides.DescribeCore(), L"ComposableBase:10");
    EXPECT_EQ(standaloneOverrides.OverridableValue(), 1010);

    // The managed base can also invoke them itself, through plain managed virtual dispatch
    EXPECT_EQ(standalone.CallComputeValue(), 20);
    EXPECT_EQ(standalone.CallDescribeCore(), L"ComposableBase:10");
    EXPECT_EQ(standalone.CallOverridableValue(), 1010);

    // 'NativeDerivedComposable' overrides nothing, so the default forwarders in 'ComposableBaseT'
    // route every overridable member back to the base implementation on the inner object.
    winrt::com_ptr<NativeDerivedComposable> nativeDerived = winrt::make_self<NativeDerivedComposable>(10);
    IComposableBaseOverrides aggregatedOverrides = nativeDerived.as<ComposableBase>().as<IComposableBaseOverrides>();

    EXPECT_EQ(aggregatedOverrides.ComputeValue(), 20);
    EXPECT_EQ(aggregatedOverrides.DescribeCore(), L"ComposableBase:10");
    EXPECT_EQ(aggregatedOverrides.OverridableValue(), 1010);
}

// A C# class deriving from a composable base is a single managed object, so the overridable members
// are selected by plain managed virtual dispatch, both when called from managed code and when they
// are invoked through the '[Overridable]' interface of the CCW.
TEST(AuthoringTest, ComposableClassCSharpDerivationOverridableDispatch)
{
    CSharpDerivedComposable csharpDerived;

    EXPECT_EQ(csharpDerived.CallComputeValue(), 169);
    EXPECT_EQ(csharpDerived.CallDescribeCore(), L"CSharpDerived:ComposableBase:84");
    EXPECT_EQ(csharpDerived.CallOverridableValue(), 1085);

    IComposableBaseOverrides asOverrides = csharpDerived.as<IComposableBaseOverrides>();

    EXPECT_EQ(asOverrides.ComputeValue(), 169);
    EXPECT_EQ(asOverrides.DescribeCore(), L"CSharpDerived:ComposableBase:84");
    EXPECT_EQ(asOverrides.OverridableValue(), 1085);

    // No aggregation is involved, so all of these share the identity of the managed object
    EXPECT_EQ(
        winrt::get_abi(asOverrides.as<winrt::Windows::Foundation::IUnknown>()),
        winrt::get_abi(csharpDerived.as<winrt::Windows::Foundation::IUnknown>()));
}

// The protected and overridable interfaces of an aggregate have to keep the identity and the
// lifetime of the controlling outer object, exactly like the public ones.
TEST(AuthoringTest, ComposableClassNativeDerivationExclusiveInterfaceIdentityAndLifetime)
{
    NativeDerivedComposable::s_destructorCount = 0;

    IComposableBaseProtected asProtected{ nullptr };

    {
        winrt::com_ptr<NativeDerivedComposable> nativeDerived = winrt::make_self<NativeDerivedComposable>(5);

        winrt::Windows::Foundation::IUnknown outerIdentity = nativeDerived.as<winrt::Windows::Foundation::IUnknown>();

        ComposableBase asBase = nativeDerived.as<ComposableBase>();

        asProtected = asBase.as<IComposableBaseProtected>();

        EXPECT_EQ(
            winrt::get_abi(asProtected.as<winrt::Windows::Foundation::IUnknown>()),
            winrt::get_abi(outerIdentity));

        // Asking twice hands out the very same (cached) interface pointer
        EXPECT_EQ(winrt::get_abi(asBase.as<IComposableBaseProtected>()), winrt::get_abi(asProtected));

        EXPECT_EQ(NativeDerivedComposable::s_destructorCount, 0);
    }

    // The outer reference is gone, but the protected interface still keeps the aggregate alive
    EXPECT_EQ(NativeDerivedComposable::s_destructorCount, 0);
    EXPECT_EQ(asProtected.GetSecretValue(), 15);

    asProtected = nullptr;

    EXPECT_EQ(NativeDerivedComposable::s_destructorCount, 1);
}

// The aggregated inner object must not have a COM identity of its own: every interface reachable
// through the composing (outer) object has to answer 'QueryInterface(IID_IUnknown)' with the
// identity of the controlling outer, exactly like a C++/WinRT composable base would.
//
// Note: 'make_self' is used on purpose here. 'winrt::make' returns 'D::composable' for a composable
// type (ie. an interface of the aggregated inner object), which would make the checks below compare
// two pointers derived from the same one.
TEST(AuthoringTest, ComposableClassNativeDerivationComIdentity)
{
    winrt::com_ptr<NativeDerivedComposable> nativeDerived = winrt::make_self<NativeDerivedComposable>(11);

    // The identity of the controlling outer, resolved without going through the aggregated inner object
    winrt::Windows::Foundation::IUnknown outerIdentity = nativeDerived.as<winrt::Windows::Foundation::IUnknown>();

    ComposableBase asBase = nativeDerived.as<ComposableBase>();

    EXPECT_EQ(asBase.GetValue(), 11);

    EXPECT_EQ(
        winrt::get_abi(asBase.as<winrt::Windows::Foundation::IUnknown>()),
        winrt::get_abi(outerIdentity));

    // Asking the base interface for the base interface again must round-trip to the same identity
    ComposableBase asBaseAgain = asBase.as<ComposableBase>();

    EXPECT_EQ(
        winrt::get_abi(asBaseAgain.as<winrt::Windows::Foundation::IUnknown>()),
        winrt::get_abi(outerIdentity));

    // 'IInspectable' obtained from the base interface must also be the one of the controlling outer
    EXPECT_EQ(
        winrt::get_abi(asBase.as<winrt::Windows::Foundation::IInspectable>()),
        winrt::get_abi(nativeDerived.as<winrt::Windows::Foundation::IInspectable>()));

    // 'GetRuntimeClassName' is delegated as well, so the base interface reports the derived class
    EXPECT_EQ(winrt::get_class_name(asBase), L"AuthoringTest.NativeDerivedComposable");

    // The aggregated object marshals itself out with the identity of its controlling outer
    winrt::Windows::Foundation::IInspectable self = asBase.GetSelf();

    EXPECT_EQ(
        winrt::get_abi(self.as<winrt::Windows::Foundation::IUnknown>()),
        winrt::get_abi(outerIdentity));
}

// All interfaces handed out by the aggregated inner object must share the lifetime of the
// controlling outer: releasing the outer reference while a base interface is still alive must
// not destroy the native object, and releasing the last base interface must destroy it.
TEST(AuthoringTest, ComposableClassNativeDerivationLifetime)
{
    NativeDerivedComposable::s_destructorCount = 0;

    ComposableBase asBase{ nullptr };

    {
        winrt::com_ptr<NativeDerivedComposable> nativeDerived = winrt::make_self<NativeDerivedComposable>(5);

        asBase = nativeDerived.as<ComposableBase>();

        EXPECT_EQ(NativeDerivedComposable::s_destructorCount, 0);
    }

    // The outer reference is gone, but the base interface still keeps the aggregate alive
    EXPECT_EQ(NativeDerivedComposable::s_destructorCount, 0);
    EXPECT_EQ(asBase.GetValue(), 5);

    asBase = nullptr;

    EXPECT_EQ(NativeDerivedComposable::s_destructorCount, 1);
}

TEST(AuthoringTest, ComposableClassCSharpDerivation)
{
    CSharpDerivedComposable csharpDerived;
    EXPECT_EQ(csharpDerived.GetValue(), 84);
    EXPECT_EQ(csharpDerived.GetDerivedValue(), 168);
    EXPECT_EQ(csharpDerived.GetName(), L"CSharpDerivedComposable");

    // A C# class deriving from a C# composable base is a single managed object, so it is not
    // aggregated: the inherited interface must share its identity.
    EXPECT_EQ(
        winrt::get_abi(csharpDerived.as<winrt::Windows::Foundation::IUnknown>()),
        winrt::get_abi(csharpDerived.as<ComposableBase>().as<winrt::Windows::Foundation::IUnknown>()));
}

// The CCW interfaces of a composable class use an aggregation-aware 'IUnknown' implementation, but a
// standalone (non-aggregated) instance must behave exactly like any other authored object: the pointers
// it hands out keep its own COM identity, and marshalling them back into managed code must resolve the
// very same managed instance rather than wrapping the CCW in a new RCW.
TEST(AuthoringTest, ComposableClassStandaloneSelfMarshalling)
{
    ComposableBase standalone(13);

    winrt::Windows::Foundation::IUnknown identity = standalone.as<winrt::Windows::Foundation::IUnknown>();

    // The loop also validates reference counting: an unbalanced 'QueryInterface'/'Release' pair on
    // either the CCW or the returned interface would show up as a leak or as a premature destruction.
    for (int i = 0; i < 100; ++i)
    {
        // Class-typed round-trip (the return type is the composable runtime class itself)
        ComposableBase self = standalone.GetSelfAsBase();

        EXPECT_EQ(winrt::get_abi(self.as<winrt::Windows::Foundation::IUnknown>()), winrt::get_abi(identity));
        EXPECT_TRUE(standalone.IsSameInstance(self));

        // Interface-typed round-trip (the return type is an authored Windows Runtime interface)
        IComposableThing thing = standalone.GetSelfAsThing();

        EXPECT_EQ(winrt::get_abi(thing.as<winrt::Windows::Foundation::IUnknown>()), winrt::get_abi(identity));
        EXPECT_TRUE(standalone.IsSameThing(thing));
        EXPECT_EQ(thing.GetThingValue(), 14);

        // Inherited (required) authored interface
        IComposableThingBase thingBase = standalone.GetSelfAsThingBase();

        EXPECT_EQ(winrt::get_abi(thingBase.as<winrt::Windows::Foundation::IUnknown>()), winrt::get_abi(identity));
        EXPECT_EQ(thingBase.GetBaseThingValue(), 15);

        // 'IInspectable'-typed round-trip
        winrt::Windows::Foundation::IInspectable inspectable = standalone.GetSelf();

        EXPECT_EQ(winrt::get_abi(inspectable.as<winrt::Windows::Foundation::IUnknown>()), winrt::get_abi(identity));
    }

    EXPECT_EQ(standalone.GetValue(), 13);
    EXPECT_EQ(standalone.GetThingValue(), 14);
}

// An aggregated object must hand out the identity of its controlling outer for every interface it
// marshals itself as, and must do so without disturbing the reference count of either the CCW or the
// controlling outer. Doing this in a loop makes a leaked CCW reference (which would keep the aggregate
// alive forever) or an over-released outer (which would tear it down early) fail deterministically.
TEST(AuthoringTest, ComposableClassNativeDerivationSelfMarshalling)
{
    NativeDerivedComposable::s_destructorCount = 0;

    {
        winrt::com_ptr<NativeDerivedComposable> nativeDerived = winrt::make_self<NativeDerivedComposable>(21);

        winrt::Windows::Foundation::IUnknown outerIdentity = nativeDerived.as<winrt::Windows::Foundation::IUnknown>();

        ComposableBase asBase = nativeDerived.as<ComposableBase>();

        for (int i = 0; i < 100; ++i)
        {
            ComposableBase self = asBase.GetSelfAsBase();

            EXPECT_EQ(winrt::get_abi(self.as<winrt::Windows::Foundation::IUnknown>()), winrt::get_abi(outerIdentity));
            EXPECT_TRUE(asBase.IsSameInstance(self));

            IComposableThing thing = asBase.GetSelfAsThing();

            EXPECT_EQ(winrt::get_abi(thing.as<winrt::Windows::Foundation::IUnknown>()), winrt::get_abi(outerIdentity));
            EXPECT_TRUE(asBase.IsSameThing(thing));
            EXPECT_EQ(thing.GetThingValue(), 22);

            // Interfaces reachable only through interface inheritance must delegate 'IUnknown' too
            IComposableThingBase thingBase = asBase.GetSelfAsThingBase();

            EXPECT_EQ(winrt::get_abi(thingBase.as<winrt::Windows::Foundation::IUnknown>()), winrt::get_abi(outerIdentity));
            EXPECT_EQ(thingBase.GetBaseThingValue(), 23);

            winrt::Windows::Foundation::IInspectable inspectable = asBase.GetSelf();

            EXPECT_EQ(winrt::get_abi(inspectable.as<winrt::Windows::Foundation::IUnknown>()), winrt::get_abi(outerIdentity));
        }

        // The aggregate is still alive and functional after all the round-trips
        EXPECT_EQ(NativeDerivedComposable::s_destructorCount, 0);
        EXPECT_EQ(asBase.GetValue(), 21);
    }

    // And it is destroyed exactly once, when the last reference goes away
    EXPECT_EQ(NativeDerivedComposable::s_destructorCount, 1);
}

// Not every interface a CCW exposes can get a per-aggregate delegating vtable copy: the built-in ones every
// CCW carries ('IStringable', 'IWeakReferenceSource', 'IMarshal', 'IAgileObject') come from shared vtables in
// 'WinRT.Runtime'. Those must never be handed out through the aggregated inner object with an identity of
// their own: either the controlling outer answers them itself, or they are simply not available.
// Interfaces that cannot take part in aggregation at all are rejected at build time (CSWINRTWINMDGEN0015).
TEST(AuthoringTest, ComposableClassNativeDerivationSharedVtableInterfaceIdentity)
{
    winrt::com_ptr<NativeDerivedComposable> nativeDerived = winrt::make_self<NativeDerivedComposable>(31);

    winrt::Windows::Foundation::IUnknown outerIdentity = nativeDerived.as<winrt::Windows::Foundation::IUnknown>();

    ComposableBase asBase = nativeDerived.as<ComposableBase>();

    // A standalone instance does expose these (they are part of every CCW), so this is really about
    // what the aggregate does with them, not about whether the managed object implements them.
    ComposableBase standalone(31);

    EXPECT_NE(standalone.try_as<winrt::Windows::Foundation::IStringable>(), nullptr);

    if (auto stringable = asBase.try_as<winrt::Windows::Foundation::IStringable>())
    {
        EXPECT_EQ(winrt::get_abi(stringable.as<winrt::Windows::Foundation::IUnknown>()), winrt::get_abi(outerIdentity));
    }

    if (auto agile = asBase.try_as<winrt::impl::IAgileObject>())
    {
        EXPECT_EQ(winrt::get_abi(agile.as<winrt::Windows::Foundation::IUnknown>()), winrt::get_abi(outerIdentity));
    }

    // Weak references are resolved by the controlling outer too, so they round-trip to its identity
    winrt::weak_ref<ComposableBase> weakBase = winrt::make_weak(asBase);
    ComposableBase resolvedBase = weakBase.get();

    ASSERT_NE(resolvedBase, nullptr);
    EXPECT_EQ(winrt::get_abi(resolvedBase.as<winrt::Windows::Foundation::IUnknown>()), winrt::get_abi(outerIdentity));
    EXPECT_EQ(resolvedBase.GetValue(), 31);
}

// The CCW of a standalone (non-aggregated) instance of a composable class must be a completely ordinary one,
// keeping the 'IUnknown' implementation the runtime provides in native code. The clearest observable proof of
// that is the set of interfaces every CCW carries and whose vtables are shared across the whole application:
// they must all still be there, all with the identity of the object itself. Those are exactly the interfaces
// an aggregate cannot hand out, so they cannot come from any aggregation-specific code path.
TEST(AuthoringTest, ComposableClassStandaloneSharedVtableInterfaceIdentity)
{
    ComposableBase standalone(17);

    winrt::Windows::Foundation::IUnknown identity = standalone.as<winrt::Windows::Foundation::IUnknown>();

    auto stringable = standalone.try_as<winrt::Windows::Foundation::IStringable>();

    ASSERT_NE(stringable, nullptr);
    EXPECT_EQ(winrt::get_abi(stringable.as<winrt::Windows::Foundation::IUnknown>()), winrt::get_abi(identity));

    auto agile = standalone.try_as<winrt::impl::IAgileObject>();

    ASSERT_NE(agile, nullptr);
    EXPECT_EQ(winrt::get_abi(agile.as<winrt::Windows::Foundation::IUnknown>()), winrt::get_abi(identity));

    // Weak references go through 'IWeakReferenceSource', which the runtime implements on every CCW
    winrt::weak_ref<ComposableBase> weakBase = winrt::make_weak(standalone);
    ComposableBase resolvedBase = weakBase.get();

    ASSERT_NE(resolvedBase, nullptr);
    EXPECT_EQ(winrt::get_abi(resolvedBase.as<winrt::Windows::Foundation::IUnknown>()), winrt::get_abi(identity));
    EXPECT_EQ(resolvedBase.GetValue(), 17);
    EXPECT_TRUE(standalone.IsSameInstance(resolvedBase));
}

// Every interface pointer the aggregated inner object hands out is created once and cached, so asking for the
// same interface twice has to produce the very same pointer, and must not disturb any reference count.
TEST(AuthoringTest, ComposableClassNativeDerivationInterfacePointersAreStable)
{
    NativeDerivedComposable::s_destructorCount = 0;

    {
        winrt::com_ptr<NativeDerivedComposable> nativeDerived = winrt::make_self<NativeDerivedComposable>(41);

        ComposableBase firstBase = nativeDerived.as<ComposableBase>();
        ComposableBase secondBase = nativeDerived.as<ComposableBase>();

        EXPECT_EQ(winrt::get_abi(firstBase), winrt::get_abi(secondBase));

        IComposableThing firstThing = nativeDerived.as<IComposableThing>();
        IComposableThing secondThing = firstBase.as<IComposableThing>();

        EXPECT_EQ(winrt::get_abi(firstThing), winrt::get_abi(secondThing));
        EXPECT_EQ(firstThing.GetThingValue(), 42);

        EXPECT_EQ(NativeDerivedComposable::s_destructorCount, 0);
    }

    EXPECT_EQ(NativeDerivedComposable::s_destructorCount, 1);
}

// A C# class deriving from a C# composable base is a single managed object, so its own CCW is used and
// no aggregation is involved: the interfaces it hands out must resolve back to that very same instance.
TEST(AuthoringTest, ComposableClassCSharpDerivationSelfMarshalling)
{
    CSharpDerivedComposable csharpDerived;

    winrt::Windows::Foundation::IUnknown identity = csharpDerived.as<winrt::Windows::Foundation::IUnknown>();

    ComposableBase asBase = csharpDerived.as<ComposableBase>();

    ComposableBase self = asBase.GetSelfAsBase();

    EXPECT_EQ(winrt::get_abi(self.as<winrt::Windows::Foundation::IUnknown>()), winrt::get_abi(identity));
    EXPECT_TRUE(asBase.IsSameInstance(self));

    IComposableThing thing = asBase.GetSelfAsThing();

    EXPECT_EQ(winrt::get_abi(thing.as<winrt::Windows::Foundation::IUnknown>()), winrt::get_abi(identity));
    EXPECT_TRUE(asBase.IsSameThing(thing));
    EXPECT_EQ(thing.GetThingValue(), 85);

    IComposableThingBase thingBase = asBase.GetSelfAsThingBase();

    EXPECT_EQ(winrt::get_abi(thingBase.as<winrt::Windows::Foundation::IUnknown>()), winrt::get_abi(identity));
    EXPECT_EQ(thingBase.GetBaseThingValue(), 86);
}

TEST(AuthoringTest, Statics)
{
    EXPECT_EQ(TestClass::GetDefaultFactor(), 1);
    EXPECT_EQ(TestClass::GetDefaultNumber(), 2);
    EXPECT_EQ(StaticClass::GetNumber(), 4);
    EXPECT_EQ(StaticClass::GetNumber(2), 2);
    EXPECT_EQ(TestClass::DefaultNumber(), 0);
    TestClass::DefaultNumber(4);
    EXPECT_EQ(TestClass::DefaultNumber(), 4);

    int result = 0;
    auto token = TestClass::StaticDelegateEvent(auto_revoke, [&result](uint32_t value)
    {
        result = value;
    });
    TestClass::FireStaticDelegate(1);
    EXPECT_EQ(result, 1);
    token.revoke();
    TestClass::FireStaticDelegate(2);
    EXPECT_EQ(result, 1);

    EXPECT_EQ(StaticClass::Number(), 0);
    StaticClass::Number(2);
    EXPECT_EQ(StaticClass::Number(), 2);

    double result2 = 0;
    auto token2 = StaticClass::DelegateEvent(auto_revoke, [&result2](double value)
    {
        result2 = value;
    });
    StaticClass::FireDelegate(4.5);
    EXPECT_EQ(result2, 4.5);
}

TEST(AuthoringTest, FunctionCalls)
{
    TestClass testClass;
    EXPECT_EQ(testClass.Factor(), 1);
    EXPECT_EQ(testClass.GetFactor(), 1);
    EXPECT_EQ(testClass.GetNumber(), 2);
    EXPECT_EQ(testClass.GetNumber(true), 2);
    EXPECT_EQ(testClass.GetNumberWithDelta(true, 3), 5);
    EXPECT_EQ(testClass.GetNumberWithDelta(false, 3), 5);
    EXPECT_EQ(testClass.GetDouble(), 2.0);
    EXPECT_EQ(testClass.GetThree(), 3);
    testClass.Factor(2);
    EXPECT_EQ(testClass.Factor(), 2);
    EXPECT_EQ(testClass.GetFactor(), 2);

    SingleInterfaceClass singleInterfaceClass;
    EXPECT_EQ(singleInterfaceClass.GetDouble(), 4);
    EXPECT_EQ(singleInterfaceClass.GetNumStr(4.4), L"4.4");
    singleInterfaceClass.Number(2);
    EXPECT_EQ(singleInterfaceClass.Number(), 2);
}

TEST(AuthoringTest, Factory)
{
    TestClass testClass(4);
    EXPECT_EQ(testClass.GetFactor(), 4);
    EXPECT_EQ(testClass.GetNumber(), 8);
    EXPECT_EQ(testClass.GetNumber(true), 2);
    EXPECT_EQ(testClass.GetNumber(false), 8);
    EXPECT_EQ(testClass.GetNumberWithDelta(true, 3), 5);
    EXPECT_EQ(testClass.GetNumberWithDelta(false, 3), 11);
    EXPECT_EQ(testClass.GetDouble(), 8.0);
    EXPECT_EQ(testClass.GetThree(), 3);
}

TEST(AuthoringTest, Interface)
{
    TestClass testClass(3);
    IDouble doubleInterface = testClass;
    EXPECT_EQ(doubleInterface.GetDouble(), 6.0);
    EXPECT_EQ(doubleInterface.GetDouble(false), 6.0);
    EXPECT_EQ(doubleInterface.GetDouble(true), 2.0);

    IAnotherInterface anotherInterface = testClass;
    EXPECT_EQ(anotherInterface.GetThree(), 3);
}

TEST(AuthoringTest, ImplementExternalInterface)
{
    IWwwFormUrlDecoderEntry www = CustomWWW();
    EXPECT_EQ(www.Name(), hstring(L"CustomWWW"));
    EXPECT_EQ(www.Value(), hstring(L"CsWinRT"));
}

TEST(AuthoringTest, InterfaceInheritance)
{
    InterfaceInheritance interfaceInheritance;
    EXPECT_EQ(interfaceInheritance.GetDouble(), 2);
    EXPECT_EQ(interfaceInheritance.GetNumStr(2.5), hstring(L"2.5"));
    interfaceInheritance.SetNumber(4);
    EXPECT_EQ(interfaceInheritance.Number(), 4);
    EXPECT_EQ(interfaceInheritance.Name(), hstring(L"IInterfaceInheritance"));
    EXPECT_EQ(interfaceInheritance.Value(), hstring(L"InterfaceInheritance"));

    IDouble doubleInterface = interfaceInheritance;
    EXPECT_EQ(doubleInterface.GetDouble(false), 2.5);

    IInterfaceInheritance interfaceInheritanceInterface = interfaceInheritance;
    interfaceInheritanceInterface.SetNumber(2);
    EXPECT_EQ(interfaceInheritanceInterface.Number(), 2);

    IWwwFormUrlDecoderEntry www = interfaceInheritance;
    EXPECT_EQ(www.Name(), hstring(L"IInterfaceInheritance"));
    EXPECT_EQ(www.Value(), hstring(L"InterfaceInheritance"));
}

TEST(AuthoringTest, ReturnTypes)
{
    BasicClass basicClass;

    auto p = basicClass.GetPoint();
    EXPECT_EQ(p.X, 2);
    EXPECT_EQ(p.Y, 3);

    auto www = basicClass.GetCustomWWW();
    EXPECT_EQ(www.Name(), hstring(L"CustomWWW"));
    EXPECT_EQ(www.Value(), hstring(L"CsWinRT"));
}

TEST(AuthoringTest, Structs)
{
    BasicClass basicClass;
    auto basicStruct = basicClass.GetBasicStruct();
    EXPECT_EQ(basicStruct.X, 4);
    EXPECT_EQ(basicStruct.Y, 8);
    EXPECT_EQ(basicStruct.Value, hstring(L"CsWinRT"));

    BasicStruct anotherBasicStruct;
    anotherBasicStruct.X = 4;
    anotherBasicStruct.Y = 6;
    auto result = basicClass.GetSumOfInts(anotherBasicStruct);
    EXPECT_EQ(result, 10);

    auto complexStruct = basicClass.GetComplexStruct();
    EXPECT_EQ(complexStruct.X.GetInt32(), 12);
    EXPECT_EQ(complexStruct.Val.GetBoolean(), true);
    EXPECT_EQ(complexStruct.BasicStruct.X, 4);
    EXPECT_EQ(complexStruct.BasicStruct.Y, 8);
    EXPECT_EQ(complexStruct.BasicStruct.Value, hstring(L"CsWinRT"));

    ComplexStruct anotherComplexStruct;
    anotherComplexStruct.X = 6;
    anotherComplexStruct.Val = false;
    anotherComplexStruct.BasicStruct = anotherBasicStruct;
    result = basicClass.GetX(anotherComplexStruct).GetInt32();
    EXPECT_EQ(result, 6);
}

TEST(AuthoringTest, Enums)
{
    BasicClass basicClass;
    EXPECT_EQ(basicClass.GetBasicEnum(), BasicEnum::First);
    EXPECT_EQ(basicClass.GetFlagsEnum(), FlagsEnum::Second | FlagsEnum::Third);

    basicClass.SetBasicEnum(BasicEnum::Second);
    EXPECT_EQ(basicClass.GetBasicEnum(), BasicEnum::Second);
    basicClass.SetFlagsEnum(FlagsEnum::Fourth);
    EXPECT_EQ(basicClass.GetFlagsEnum(), FlagsEnum::Fourth);
}

TEST(AuthoringTest, Events)
{
    int result = 0;
    int result2 = 0;

    TestClass testClass;
    auto token = testClass.BasicDelegateEvent(auto_revoke, [&result](uint32_t value)
    {
        result = value;
    });

    auto token2 = testClass.BasicDelegateEvent2(auto_revoke, [&result2](uint32_t value)
    {
        result2 = value;
    });

    testClass.FireBasicDelegate(3);
    EXPECT_EQ(result, 3);
    EXPECT_EQ(testClass.DelegateValue(), 3);
    EXPECT_EQ(result2, 0);

    testClass.FireBasicDelegate2(5);
    EXPECT_EQ(result, 3);
    EXPECT_EQ(result2, 5);

    // unregister handler, value shouldn't change.
    token.revoke();
    testClass.FireBasicDelegate(12);
    EXPECT_EQ(result, 3);
    EXPECT_EQ(result2, 5);

    IAnotherInterface anotherInterface = testClass;
    double doubleResult;
    anotherInterface.ComplexDelegateEvent([&doubleResult, &result](double value, int32_t value2) -> bool
    {
        doubleResult = value;
        result = value2;
        return true;
    });

    EXPECT_EQ(anotherInterface.FireComplexDelegate(8.8, 9), true);
    EXPECT_EQ(doubleResult, 8.8);
    EXPECT_EQ(result, 9);

    SingleInterfaceClass singleInterfaceClass;
    auto token3 = singleInterfaceClass.DoubleDelegateEvent(auto_revoke, [&](double value)
    {
    });
    token3.revoke();
}

TEST(AuthoringTest, CCWCaching)
{
    BasicClass basicClass;

    basicClass.SetBasicEnum(BasicEnum::Second);
    EXPECT_EQ(basicClass.GetBasicEnum(), BasicEnum::Second);
    basicClass.SetFlagsEnum(FlagsEnum::Fourth);
    EXPECT_EQ(basicClass.GetFlagsEnum(), FlagsEnum::Fourth);

    auto copy = basicClass.ReturnParameter(basicClass);
    EXPECT_EQ(copy.GetBasicEnum(), BasicEnum::Second);
    EXPECT_EQ(copy.GetFlagsEnum(), FlagsEnum::Fourth);
    EXPECT_EQ(basicClass, copy);
}

IAsyncOperation<int32_t> GetIntAsync(int num)
{
    co_return num;
}

TEST(AuthoringTest, Arrays)
{
    BasicClass basicClass;
    EXPECT_EQ(basicClass.GetSum({2, 3, 4, 6}), 15);

    com_array<int> arr(6);
    basicClass.PopulateArray(arr);
    for (auto idx = 0u; idx < arr.size(); idx++)
    {
        EXPECT_EQ(arr[idx], idx + 1);
    }

    com_array<int> arr2;
    basicClass.GetArrayOfLength(10, arr2);
    EXPECT_EQ(arr2.size(), 10);
    for (auto idx = 0u; idx < arr2.size(); idx++)
    {
        EXPECT_EQ(arr2[idx], idx + 1);
    }

    // Array marshaling on AOT needs dynamic code.
#ifndef AOT
    std::array<BasicStruct, 2> basicStructArr;
    basicStructArr[0] = basicClass.GetBasicStruct();
    basicStructArr[1].X = 4;
    basicStructArr[1].Y = 6;
    basicStructArr[1].Value = L"WinRT";
    auto result = basicClass.ReturnArray(basicStructArr);
    EXPECT_EQ(result.size(), 2);
    EXPECT_EQ(result[0].X, basicStructArr[0].X);
    EXPECT_EQ(result[0].Y, basicStructArr[0].Y);
    EXPECT_EQ(result[0].Value, basicStructArr[0].Value);
    EXPECT_EQ(result[1].X, basicStructArr[1].X);
    EXPECT_EQ(result[1].Y, basicStructArr[1].Y);
    EXPECT_EQ(result[1].Value, basicStructArr[1].Value);
#endif
}

TEST(AuthoringTest, CustomTypes)
{
    BasicClass basicClass;

    auto dateTime = basicClass.GetDate();
    EXPECT_TRUE(dateTime.time_since_epoch().count() != 0);

    auto now = winrt::clock::now();
    basicClass.SetDate(now);
    auto dateTime2 = basicClass.GetDate();
    EXPECT_EQ(dateTime2, now);
    EXPECT_TRUE(dateTime != dateTime2);

    auto timeSpan = basicClass.GetTimespan();
    EXPECT_EQ(timeSpan.count(), 100);

    TestClass testClass;
    testClass.SetProjectedDisposableObject();

    testClass.DisposableObject().Close();
    EXPECT_FALSE(testClass.DisposableClassObject().IsDisposed());
    testClass.DisposableClassObject().Close();
    EXPECT_TRUE(testClass.DisposableClassObject().IsDisposed());

    testClass.SetNonProjectedDisposableObject();
    testClass.DisposableObject().Close();

    testClass.IntAsyncOperation(GetIntAsync(24));
    EXPECT_EQ(testClass.GetIntAsyncOperation().get(), 24);
    testClass.SetIntAsyncOperation(GetIntAsync(50));

    auto vector = winrt::single_threaded_vector(std::vector<IInspectable>{ winrt::box_value(0), winrt::box_value(1), winrt::box_value(2) });
    testClass.ObjectList(vector);
    EXPECT_EQ(testClass.GetObjectListSum(), 3);

    auto disposableObjects = testClass.GetDisposableObjects();
    EXPECT_EQ(disposableObjects.Size(), 3);
    for(auto obj : disposableObjects)
    {
        obj.Close();
    }

    for (auto uri : TestClass::GetUris())
    {
        EXPECT_NE(uri, nullptr);
    }
    EXPECT_EQ(TestClass::GetUris().Size(), 2);
    EXPECT_NE(TestClass::GetUris().First(), nullptr);

    testClass.SetTypeToTestClass();
    auto type = testClass.Type();
    EXPECT_EQ(type.Kind, Windows::UI::Xaml::Interop::TypeKind::Metadata);
    EXPECT_EQ(type.Name, L"AuthoringTest.TestClass");

    auto erasedProjecteds = testClass.GetTypeErasedProjectedObjects();
    EXPECT_EQ(erasedProjecteds.Size(), 6);
    for (auto obj : erasedProjecteds)
    {
        auto pv = obj.try_as<IPropertyValue>();
        EXPECT_NE(pv, nullptr);
    }

    auto erasedNonProjecteds = testClass.GetTypeErasedNonProjectedObjects();
    EXPECT_EQ(erasedNonProjecteds.Size(), 3);
    for (auto obj : erasedNonProjecteds)
    {
        auto pv = obj.try_as<IPropertyValue>();
        EXPECT_EQ(pv, nullptr);
    }

    // Array marshaling on AOT needs dynamic code.
#ifndef AOT
    auto erasedProjectedArrays = testClass.GetTypeErasedProjectedArrays();
    EXPECT_EQ(erasedProjectedArrays.Size(), 7);
    for (auto obj : erasedProjectedArrays)
    {
        auto ra = obj.try_as<IPropertyValue>();
        EXPECT_NE(ra, nullptr);
        auto type = ra.Type();
    }
#endif
}

TEST(AuthoringTest, Async)
{
    TestClass testClass;
    auto asyncOperation = testClass.GetDoubleAsyncOperation();
    EXPECT_EQ(asyncOperation.wait_for(std::chrono::seconds(2)), AsyncStatus::Completed);
    EXPECT_EQ(asyncOperation.GetResults(), 4.0);

    auto asyncOperation2 = testClass.GetStructAsyncOperation();
    EXPECT_EQ(asyncOperation2.wait_for(std::chrono::seconds(2)), AsyncStatus::Completed);
    auto result = asyncOperation2.GetResults();
    EXPECT_EQ(result.X, 2);
    EXPECT_EQ(result.Y, 4);
    EXPECT_EQ(result.Value, L"Test");
}

TEST(AuthoringTest, CustomDictionaryImplementations)
{
    CustomDictionary dictionary;

    BasicStruct basicStruct{1, 2};
    BasicStruct basicStruct2{ 2, 2 };
    BasicStruct basicStruct3{ 3, 3 };
    EXPECT_FALSE(dictionary.Insert(L"first", basicStruct));
    EXPECT_FALSE(dictionary.Insert(L"second", basicStruct3));
    EXPECT_TRUE(dictionary.Insert(L"second", basicStruct2));
    EXPECT_FALSE(dictionary.Insert(L"third", basicStruct3));
    EXPECT_EQ(dictionary.Size(), 3);

    EXPECT_TRUE(dictionary.HasKey(L"first"));
    EXPECT_FALSE(dictionary.HasKey(L"fourth"));
    EXPECT_TRUE(dictionary.HasKey(L"third"));

    hstring keys[] = {L"first", L"second", L"third" };
    BasicStruct values[] = { basicStruct, basicStruct2, basicStruct3 };
    int idx = 0;
    for (auto entry : dictionary)
    {
        EXPECT_EQ(entry.Key(), keys[idx]);
        EXPECT_EQ(entry.Value(), values[idx]);
        idx++;
    }
    EXPECT_EQ(idx, 3);

    idx = 0;
    for (auto entry : dictionary.GetView())
    {
        EXPECT_EQ(entry.Key(), keys[idx]);
        EXPECT_EQ(entry.Value(), values[idx]);
        idx++;
    }
    EXPECT_EQ(idx, 3);

    EXPECT_EQ(dictionary.GetView().TryLookup(L"second").value(), basicStruct2);
    EXPECT_FALSE(dictionary.GetView().TryLookup(L"fourth").has_value());
  
    TestClass testClass;
    EXPECT_EQ(testClass.GetSum(dictionary, L"second"), 4);

    CustomReadOnlyDictionary readOnlyDictionary(dictionary);
    EXPECT_TRUE(readOnlyDictionary.HasKey(L"first"));
    EXPECT_FALSE(readOnlyDictionary.HasKey(L"fourth"));
    EXPECT_TRUE(readOnlyDictionary.HasKey(L"third"));
    EXPECT_EQ(readOnlyDictionary.Size(), 3);

    EXPECT_EQ(readOnlyDictionary.TryLookup(L"second").value(), basicStruct2);
    EXPECT_FALSE(readOnlyDictionary.TryLookup(L"fourth").has_value());

    Windows::Foundation::Collections::IMapView<hstring, AuthoringTest::BasicStruct> mapSplit1, mapSplit2;
    readOnlyDictionary.Split(mapSplit1, mapSplit2);
    EXPECT_NE(mapSplit1, nullptr);
    EXPECT_NE(mapSplit2, nullptr);
    EXPECT_TRUE(mapSplit1.HasKey(L"first"));
    EXPECT_FALSE(mapSplit1.HasKey(L"third"));
    EXPECT_TRUE(mapSplit2.HasKey(L"third"));

    Windows::Foundation::Collections::IMap<hstring, AuthoringTest::BasicStruct> map = dictionary;
    map.Clear();
    EXPECT_EQ(map.Size(), 0);
}

TEST(AuthoringTest, CustomVectorImplementations)
{
    TestClass testClass;
    testClass.SetProjectedDisposableObject();
    DisposableClass disposed;
    disposed.Close();

    CustomVector vector;
    EXPECT_EQ(vector.Size(), 0);
    vector.Append(DisposableClass());
    vector.Append(DisposableClass());
    vector.Append(testClass.DisposableClassObject());
    vector.Append(disposed);
    EXPECT_EQ(vector.Size(), 4);

    auto first = vector.First();
    EXPECT_TRUE(first.HasCurrent());
    EXPECT_FALSE(first.Current().IsDisposed());
    first.Current().Close();
    EXPECT_TRUE(first.Current().IsDisposed());
    EXPECT_FALSE(vector.GetAt(2).IsDisposed());
    EXPECT_TRUE(vector.GetAt(3).IsDisposed());
    for (auto obj : vector.GetView())
    {
        obj.Close();
    }
    EXPECT_TRUE(vector.GetAt(3).IsDisposed());

    std::array<DisposableClass, 2> view{};
    EXPECT_EQ(vector.GetMany(2, view), 2);
    EXPECT_EQ(view.size(), 2);
    for (auto &obj : view)
    {
        EXPECT_TRUE(obj.IsDisposed());
    }

    CustomVectorView vectorView(vector);
    EXPECT_EQ(vectorView.Size(), 4);
    auto firstView = vectorView.First();
    EXPECT_TRUE(firstView.HasCurrent());
    EXPECT_TRUE(firstView.Current().IsDisposed());
    firstView.Current().Close();
    EXPECT_TRUE(vectorView.GetAt(2).IsDisposed());
    EXPECT_TRUE(vectorView.GetAt(3).IsDisposed());
    uint32_t index = 0;
    EXPECT_TRUE(vectorView.IndexOf(disposed, index));
    EXPECT_EQ(index, 3);
    EXPECT_TRUE(vectorView.IndexOf(testClass.DisposableClassObject(), index));
    EXPECT_EQ(index, 2);

    vector.Clear();
    EXPECT_EQ(vector.Size(), 0);
}

TEST(AuthoringTest, Overloads)
{
    TestClass testClass;
    EXPECT_EQ(testClass.Get(2), 2);
    EXPECT_EQ(testClass.Get(L"CsWinRT"), L"CsWinRT");
    EXPECT_EQ(testClass.GetNumStr(4.1), L"4.1");
    EXPECT_EQ(testClass.GetNumStr(4), L"4");

    IDouble doubleInterface = testClass;
    EXPECT_EQ(doubleInterface.GetNumStr(2.2), L"2.2");
    EXPECT_EQ(doubleInterface.GetNumStr(8), L"8");
}

TEST(AuthoringTest, XamlMappings)
{
    CustomVector2 vector;
    EXPECT_EQ(vector.Size(), 0);
    vector.Append(DisposableClass());
    vector.Append(DisposableClass());
    vector.Append(TestClass());
    EXPECT_EQ(vector.Size(), 3);

    auto first = vector.First();
    EXPECT_TRUE(first.HasCurrent());
    EXPECT_FALSE(first.Current().as<DisposableClass>().IsDisposed());
    first.Current().as<DisposableClass>().Close();
    EXPECT_TRUE(first.Current().as<DisposableClass>().IsDisposed());
    EXPECT_FALSE(vector.GetAt(1).as<DisposableClass>().IsDisposed());

    vector.RemoveAt(0);
    EXPECT_EQ(vector.Size(), 2);
    vector.Clear();
    EXPECT_EQ(vector.Size(), 0);

    CustomXamlServiceProvider serviceProvider;
    EXPECT_EQ(serviceProvider.GetService(winrt::xaml_typename<CustomVector2>()).as<IStringable>().ToString(), L"CustomVector2");

    bool eventTriggered = false;
    CustomCommand command;
    EXPECT_FALSE(command.CanExecute(nullptr));
    auto token = command.CanExecuteChanged(auto_revoke, [&eventTriggered](IInspectable sender, IInspectable args)
    {
        eventTriggered = true;
    });
    command.SetCanExecute(true);
    EXPECT_TRUE(eventTriggered);
    EXPECT_TRUE(command.CanExecute(nullptr));
}

TEST(AuthoringTest, ExplicitInterfaces)
{
    ExplicltlyImplementedClass explicltlyImplementedClass;
    IDouble doubleInterface = explicltlyImplementedClass;
    EXPECT_EQ(doubleInterface.GetDouble(), 4);
    EXPECT_EQ(doubleInterface.GetNumStr(4), L"4");
    doubleInterface.Number(2);
    EXPECT_EQ(doubleInterface.Number(), 2);

    IDouble2 double2Interface = explicltlyImplementedClass;
    EXPECT_EQ(double2Interface.GetDouble(), 8);
    EXPECT_EQ(double2Interface.GetNumStr(4), L"8");
    EXPECT_EQ(double2Interface.Number(), 4);
    double2Interface.Number(2);
    EXPECT_EQ(double2Interface.Number(), 8);

    bool eventTriggered = false, event2Triggered = false;
    auto token = doubleInterface.DoubleDelegateEvent(auto_revoke, [&eventTriggered](double value)
    {
        eventTriggered = (value == 4);
    });
    auto token2 = double2Interface.DoubleDelegateEvent(auto_revoke, [&event2Triggered](double value)
    {
        event2Triggered = (value == 8);
    });
    explicltlyImplementedClass.TriggerEvent(4);
    EXPECT_TRUE(eventTriggered);
    EXPECT_TRUE(event2Triggered);
    token.revoke();

    DisposableClass disposed;
    disposed.Close();
    MultipleInterfaceMappingClass multipleInterfaces;
    Microsoft::UI::Xaml::Interop::IBindableIterable bindable = multipleInterfaces;
    Windows::Foundation::Collections::IVector<DisposableClass> vector = multipleInterfaces;
    Microsoft::UI::Xaml::Interop::IBindableVector bindableVector = multipleInterfaces;
    EXPECT_EQ(vector.Size(), 0);
    EXPECT_EQ(bindableVector.Size(), 0);
    vector.Append(DisposableClass());
    vector.Append(DisposableClass());
    vector.Append(disposed);
    bindableVector.Append(DisposableClass());
    EXPECT_EQ(vector.Size(), 4);
    EXPECT_EQ(bindableVector.Size(), 4);

    auto first = vector.First();
    EXPECT_TRUE(first.HasCurrent());
    EXPECT_FALSE(first.Current().IsDisposed());
    auto bindableFirst = bindable.First();
    EXPECT_TRUE(bindableFirst.HasCurrent());
    EXPECT_FALSE(bindableFirst.Current().as<DisposableClass>().IsDisposed());
    bindableFirst.Current().as<DisposableClass>().Close();
    EXPECT_TRUE(first.Current().IsDisposed());
    EXPECT_FALSE(vector.GetAt(1).IsDisposed());
    EXPECT_TRUE(vector.GetAt(2).IsDisposed());
    EXPECT_TRUE(bindableVector.First().Current().as<DisposableClass>().IsDisposed());
    EXPECT_FALSE(bindableVector.GetAt(3).as<DisposableClass>().IsDisposed());
    EXPECT_TRUE(bindableVector.GetAt(2).as<DisposableClass>().IsDisposed());
    for (auto obj : vector.GetView())
    {
        obj.Close();
    }

    std::array<DisposableClass, 2> view{};
    EXPECT_EQ(vector.GetMany(1, view), 2);
    EXPECT_EQ(view.size(), 2);
    for (auto& obj : view)
    {
        EXPECT_TRUE(obj.IsDisposed());
    }

    CustomDictionary2 dictionary;

    EXPECT_FALSE(dictionary.Insert(L"first", 1));
    EXPECT_FALSE(dictionary.Insert(L"second", 2));
    EXPECT_TRUE(dictionary.Insert(L"second", 4));
    EXPECT_FALSE(dictionary.Insert(L"third", 4));
    EXPECT_EQ(dictionary.Size(), 3);

    EXPECT_TRUE(dictionary.HasKey(L"first"));
    EXPECT_FALSE(dictionary.HasKey(L"fourth"));
    EXPECT_TRUE(dictionary.HasKey(L"third"));

    dictionary.Clear();
    EXPECT_FALSE(dictionary.HasKey(L"first"));
    EXPECT_FALSE(dictionary.HasKey(L"fourth"));
    EXPECT_FALSE(dictionary.HasKey(L"third"));
}

TEST(AuthoringTest, PartialClass)
{
    PartialClass partialClass;
    partialClass.SetNumber(2);
    EXPECT_EQ(partialClass.GetNumber(), 2);
    EXPECT_EQ(partialClass.GetNumberAsString(), L"2");
    partialClass.SetNumber(4);
    EXPECT_EQ(partialClass.Number(), 4);
    EXPECT_EQ(partialClass.Number2(), 8);
    PartialStruct result = partialClass.GetPartialStruct();
    EXPECT_EQ(result.X, 4);
    EXPECT_EQ(result.Y, 5);
    EXPECT_EQ(result.Z, 6);

    PartialClass partialClass2(1);
    IPartialInterface partialInterface = partialClass2;
    EXPECT_EQ(partialInterface.GetNumberAsString(), L"1");
    EXPECT_EQ(partialClass2.GetNumber(), 1);
    partialClass2.SetNumber(2);
    EXPECT_EQ(partialInterface.GetNumberAsString(), L"2");

    PartialStruct partialStruct{ 3, 4, 5 };
    EXPECT_EQ(partialStruct.X, 3);
    EXPECT_EQ(partialStruct.Y, 4);
    EXPECT_EQ(partialStruct.Z, 5);
}

/*
TEST(AuthoringTest, MixedWinRTClassicCOM)
{
    TestMixedWinRTCOMWrapper wrapper;

    // Normal WinRT methods work as you'd expect
    EXPECT_EQ(wrapper.HelloWorld(), L"Hello from mixed WinRT/COM");

    // Verify we can grab the internal interface
    IID internalInterface1Iid;
    check_hresult(IIDFromString(L"{C7850559-8FF2-4E54-A237-6ED813F20CDC}", &internalInterface1Iid));
    winrt::com_ptr<::IUnknown> unknown1 = wrapper.as<::IUnknown>();
    winrt::com_ptr<::IUnknown> internalInterface1;
    EXPECT_EQ(unknown1->QueryInterface(internalInterface1Iid, internalInterface1.put_void()), S_OK);

    // Verify we can grab the nested public interface (in an internal type)
    IID internalInterface2Iid;
    check_hresult(IIDFromString(L"{8A08E18A-8D20-4E7C-9242-857BFE1E3159}", &internalInterface2Iid));
    winrt::com_ptr<::IUnknown> unknown2 = wrapper.as<::IUnknown>();
    winrt::com_ptr<::IUnknown> internalInterface2;
    EXPECT_EQ(unknown2->QueryInterface(internalInterface2Iid, internalInterface2.put_void()), S_OK);

    typedef int (__stdcall* GetNumber)(void*, int*);

    int number;

    // Validate the first call on IInternalInterface1
    EXPECT_EQ(reinterpret_cast<GetNumber>((*reinterpret_cast<void***>(internalInterface1.get()))[3])(internalInterface1.get(), &number), S_OK);
    EXPECT_EQ(number, 42);

    // Validate the second call on IInternalInterface2
    EXPECT_EQ(reinterpret_cast<GetNumber>((*reinterpret_cast<void***>(internalInterface2.get()))[3])(internalInterface2.get(), &number), S_OK);
    EXPECT_EQ(number, 123);
}
*/
TEST(AuthoringTest, GetRuntimeClassName)
{
    CustomDictionary2 dictionary;
    EXPECT_EQ(winrt::get_class_name(dictionary), L"AuthoringTest.CustomDictionary2");

    DisposableClass disposed;
    EXPECT_EQ(winrt::get_class_name(disposed), L"AuthoringTest.DisposableClassImpl");

    // TestMixedWinRTCOMWrapper wrapper;
    // EXPECT_EQ(winrt::get_class_name(wrapper), L"AuthoringTest.TestMixedWinRTCOMWrapper");

    TestClass testClass;
    testClass.SetNonProjectedDisposableObject();
    EXPECT_EQ(winrt::get_class_name(testClass.DisposableObject()), L"Windows.Foundation.IClosable");

    testClass.SetProjectedDisposableObject();
    EXPECT_EQ(winrt::get_class_name(testClass.DisposableObject()), L"AuthoringTest.DisposableClassImpl");
}

TEST(AuthoringTest, XamlMetadataProvider)
{
    CustomXamlMetadataProvider provider;
    EXPECT_NE(provider.GetXamlType(winrt::xaml_typename<Windows::Foundation::IReference<double>>()), nullptr);
    EXPECT_NE(provider.GetXamlType(winrt::xaml_typename<Windows::Foundation::IReference<Windows::Foundation::TimeSpan>>()), nullptr);
    EXPECT_NE(provider.GetXamlType(winrt::xaml_typename<Windows::Foundation::IReference<BasicEnum>>()), nullptr);
    EXPECT_NE(provider.GetXamlType(winrt::xaml_typename<Windows::Foundation::IReference<FlagsEnum>>()), nullptr);
}

TEST(AuthoringTest, CustomInterfaceGuid)
{
    CustomInterfaceGuidClass customInterfaceGuidClass;
    winrt::com_ptr<::IUnknown> customInterfaceClassUnknown = customInterfaceGuidClass.as<::IUnknown>();
    ICustomInterfaceGuid customInterface;

    IID customInterfaceIid;
    check_hresult(IIDFromString(L"{26D8EE57-8B1B-46F4-A4F9-8C6DEEEAF53A}", &customInterfaceIid));
    check_hresult(customInterfaceClassUnknown->QueryInterface(customInterfaceIid, reinterpret_cast<void**>(winrt::put_abi(customInterface))));

    EXPECT_EQ(customInterface.HelloWorld(), L"Hello World!");
}

TEST(AuthoringTest, CustomOverloadNames)
{
    // Test interface-level overloads with user-specified OverloadAttribute names
    CustomOverloadNamesClass obj;
    EXPECT_EQ(obj.Lookup(hstring(L"test")), hstring(L"found:test"));
    EXPECT_EQ(obj.Lookup(42), 420);

    // Through the interface
    ICustomOverloadNames iface = obj;
    EXPECT_EQ(iface.Lookup(hstring(L"hello")), hstring(L"found:hello"));
    EXPECT_EQ(iface.Lookup(7), 70);

    // Test class-level overloads with mixed user-specified and auto-generated names
    EXPECT_EQ(obj.Transform(hstring(L"abc")), hstring(L"ABC"));
    EXPECT_EQ(obj.Transform(5), 10);
    EXPECT_EQ(obj.Transform(1.0), 1.5);

    // Verify the user-specified ABI names actually appear in the generated projection.
    // The abi<ICustomOverloadNames>::type vtable has virtual methods named exactly
    // after the [Overload("...")] values. If the winmd had auto-generated names
    // (e.g. "Lookup2") instead of "LookupByIndex", these calls would fail to compile
    using abi_type = winrt::impl::abi_t<ICustomOverloadNames>;
    auto raw = static_cast<abi_type*>(winrt::get_abi(iface));
    {
        int32_t result = 0;
        EXPECT_EQ(raw->LookupByIndex(7, &result), S_OK);
        EXPECT_EQ(result, 70);
    }
    {
        bool result = false;
        EXPECT_EQ(raw->LookupByFlag(false, &result), S_OK);
        EXPECT_TRUE(result);
    }

    // Same check for the class-level synthesized interface
    using class_abi_type = winrt::impl::abi_t<ICustomOverloadNamesClassClass>;
    ICustomOverloadNamesClassClass classIface = obj.as<ICustomOverloadNamesClassClass>();
    auto rawClass = static_cast<class_abi_type*>(winrt::get_abi(classIface));
    {
        int32_t result = 0;
        EXPECT_EQ(rawClass->TransformNumber(5, &result), S_OK);
        EXPECT_EQ(result, 10);
    }
    {
        // The auto-generated overload coexists with the author-named one: because "TransformNumber"
        // is author-specified, it does not consume a numeric suffix, so Transform(double) becomes
        // "Transform2" (not "Transform3")
        double result = 0.0;
        EXPECT_EQ(rawClass->Transform2(1.0, &result), S_OK);
        EXPECT_EQ(result, 1.5);
    }
}

TEST(AuthoringTest, CustomOverloadNamesCollision)
{
    // The author names one overload "M2" (matching the auto-generated pattern), so the
    // auto-generated name for the remaining overload must skip "M2" and become "M3"
    OverloadCollisionClass obj;
    EXPECT_EQ(obj.M(hstring(L"abc")), hstring(L"abc"));
    EXPECT_EQ(obj.M(21), 42);
    EXPECT_TRUE(obj.M(false));

    using class_abi_type = winrt::impl::abi_t<IOverloadCollisionClassClass>;
    IOverloadCollisionClassClass classIface = obj.as<IOverloadCollisionClassClass>();
    auto raw = static_cast<class_abi_type*>(winrt::get_abi(classIface));
    {
        int32_t result = 0;
        EXPECT_EQ(raw->M2(21, &result), S_OK);
        EXPECT_EQ(result, 42);
    }
    {
        bool result = false;
        EXPECT_EQ(raw->M3(false, &result), S_OK);
        EXPECT_TRUE(result);
    }
}

TEST(AuthoringTest, CustomOverloadNamesDefaultNotFirst)
{
    // [DefaultOverload] is on the second-declared overload, so it keeps the original ABI name
    // ("Get") while the author-specified name on the first overload ("GetByIndex") is honored
    DefaultOverloadNotFirstClass obj;
    EXPECT_EQ(obj.Get(5), 105);
    EXPECT_EQ(obj.Get(hstring(L"abc")), hstring(L"key:abc"));

    IDefaultOverloadNotFirst iface = obj;
    EXPECT_EQ(iface.Get(7), 107);
    EXPECT_EQ(iface.Get(hstring(L"z")), hstring(L"key:z"));

    // The author-specified name lives on the non-default (first-declared) overload
    using abi_type = winrt::impl::abi_t<IDefaultOverloadNotFirst>;
    auto raw = static_cast<abi_type*>(winrt::get_abi(iface));
    {
        int32_t result = 0;
        EXPECT_EQ(raw->GetByIndex(7, &result), S_OK);
        EXPECT_EQ(result, 107);
    }
}

TEST(AuthoringTest, NonActivatableFactory)
{
    EXPECT_EQ(NonActivatableFactory::Create().GetText(), L"Test123");
}

TEST(AuthoringTest, TypeOnlyActivatableViaItsOwnFactory)
{
    EXPECT_EQ(TypeOnlyActivatableViaItsOwnFactory::Create().GetText(), L"Hello!");
}

TEST(AuthoringTest, ExplicitlyImplementedICustomPropertyProvider)
{
    CustomPropertyProviderWithExplicitImplementation userObject;

    // We should be able to cast to 'ICustomPropertyProvider'
    auto propertyProvider = userObject.as<Microsoft::UI::Xaml::Data::ICustomPropertyProvider>();

    auto providerType = propertyProvider.Type();
    EXPECT_EQ(providerType.Kind, Windows::UI::Xaml::Interop::TypeKind::Metadata);
    EXPECT_EQ(providerType.Name, L"AuthoringTest.CustomPropertyProviderWithExplicitImplementation");

    auto customProperty = propertyProvider.GetCustomProperty(L"TestCustomProperty");
    
    EXPECT_NE(customProperty, nullptr);
    EXPECT_TRUE(customProperty.CanRead());
    EXPECT_FALSE(customProperty.CanWrite());
    EXPECT_EQ(customProperty.Name(), L"TestCustomProperty");

    auto propertyType = customProperty.Type();
    EXPECT_EQ(propertyType.Kind, Windows::UI::Xaml::Interop::TypeKind::Metadata);
    EXPECT_EQ(propertyType.Name, L"AuthoringTest.CustomPropertyWithExplicitImplementation");

    auto propertyValue = customProperty.GetValue(nullptr);
    EXPECT_EQ(winrt::unbox_value<hstring>(propertyValue), L"TestPropertyValue");
}

TEST(AuthoringTest, GeneratedCustomPropertyStructType)
{
    auto userObject = CustomPropertyRecordTypeFactory::CreateStruct();

    // We should be able to cast to 'ICustomPropertyProvider'
    auto propertyProvider = userObject.as<Microsoft::UI::Xaml::Data::ICustomPropertyProvider>();

    auto customProperty = propertyProvider.GetCustomProperty(L"Value");

    EXPECT_NE(customProperty, nullptr);
    EXPECT_TRUE(customProperty.CanRead());
    EXPECT_FALSE(customProperty.CanWrite());
    EXPECT_EQ(customProperty.Name(), L"Value");

    auto propertyValue = customProperty.GetValue(userObject);
    EXPECT_EQ(winrt::unbox_value<hstring>(propertyValue), L"CsWinRTFromStructType");
}

TEST(AuthoringTest, GeneratedCustomPropertyRecordType)
{
    auto userObject = CustomPropertyRecordTypeFactory::CreateRecord();

    // We should be able to cast to 'ICustomPropertyProvider'
    auto propertyProvider = userObject.as<Microsoft::UI::Xaml::Data::ICustomPropertyProvider>();

    auto customProperty = propertyProvider.GetCustomProperty(L"Value");

    EXPECT_NE(customProperty, nullptr);
    EXPECT_TRUE(customProperty.CanRead());
    EXPECT_FALSE(customProperty.CanWrite());
    EXPECT_EQ(customProperty.Name(), L"Value");

    auto propertyValue = customProperty.GetValue(userObject);
    EXPECT_EQ(winrt::unbox_value<hstring>(propertyValue), L"CsWinRTFromRecordType");
}

TEST(AuthoringTest, CustomPropertyRecordStructTypeFactoryAndICPP)
{
    auto userObject = CustomPropertyRecordTypeFactory::CreateRecordStruct();

    // We should be able to cast to 'ICustomPropertyProvider'
    auto propertyProvider = userObject.as<Microsoft::UI::Xaml::Data::ICustomPropertyProvider>();

    auto customProperty = propertyProvider.GetCustomProperty(L"Value");

    EXPECT_NE(customProperty, nullptr);
    EXPECT_TRUE(customProperty.CanRead());
    EXPECT_FALSE(customProperty.CanWrite());
    EXPECT_EQ(customProperty.Name(), L"Value");

    auto propertyValue = customProperty.GetValue(userObject);
    EXPECT_EQ(winrt::unbox_value<hstring>(propertyValue), L"CsWinRTFromRecordStructType");
}

TEST(AuthoringTest, MultiConstructorClass)
{
    MultiConstructorClass obj0;
    EXPECT_EQ(obj0.Name(), L"");
    EXPECT_EQ(obj0.Value(), 0);

    MultiConstructorClass obj1(L"hello");
    EXPECT_EQ(obj1.Name(), L"hello");
    EXPECT_EQ(obj1.Value(), 0);

    MultiConstructorClass obj2(L"hello", 42);
    EXPECT_EQ(obj2.Name(), L"hello");
    EXPECT_EQ(obj2.Value(), 42);

    BasicStruct bs{ 1, 2, L"test" };
    MultiConstructorClass obj3(L"hello", 42, bs);
    EXPECT_EQ(obj3.Name(), L"hello");
    EXPECT_EQ(obj3.Value(), 42);
    EXPECT_EQ(obj3.Data().X, 1);
    EXPECT_EQ(obj3.Data().Y, 2);
    EXPECT_EQ(obj3.Data().Value, L"test");
}

TEST(AuthoringTest, FactoryAndStaticClass)
{
    FactoryAndStaticClass obj1(L"myId");
    EXPECT_EQ(obj1.Id(), L"myId");

    FactoryAndStaticClass obj2(L"myId", 2);
    EXPECT_EQ(obj2.Id(), L"myId_v2");

    EXPECT_EQ(FactoryAndStaticClass::DefaultId(), L"default");

    auto defaultObj = FactoryAndStaticClass::CreateDefault();
    EXPECT_EQ(defaultObj.Id(), L"default");
}

TEST(AuthoringTest, AsyncMethodClass)
{
    AsyncMethodClass asyncClass;

    auto action = asyncClass.DoWorkAsync();
    EXPECT_EQ(action.wait_for(std::chrono::seconds(2)), AsyncStatus::Completed);

    auto operation = asyncClass.ComputeAsync();
    EXPECT_EQ(operation.wait_for(std::chrono::seconds(2)), AsyncStatus::Completed);
    EXPECT_EQ(operation.GetResults(), 42);
}

TEST(AuthoringTest, DeprecatedMembersClass)
{
    DeprecatedMembersClass obj;

    // Members marked [Deprecated(DeprecationType.Remove)] are omitted from the projection, so they are
    // intentionally not referenced here. Their ABI vtable slot is still preserved (stubbed to E_NOTIMPL):
    // the new members below sit after the removed slots in vtable order, so their correct dispatch confirms
    // the removed slots remain in place and the layout did not shift. The deprecated members and the new
    // members both remain projected and fully usable.
    obj.OldMethod();
    obj.NewMethod();
    EXPECT_EQ(obj.OldProp(), L"OldProp");
    EXPECT_EQ(obj.NewProp(), L"NewProp");

    DeprecatedMembersClass::OldStatic();
    DeprecatedMembersClass::NewStatic();

    auto oldToken = obj.OldEvent(auto_revoke, [](IInspectable const&, int32_t const&) {});
    auto newToken = obj.NewEvent(auto_revoke, [](IInspectable const&, int32_t const&) {});
}

TEST(AuthoringTest, FullFeaturedClass)
{
    FullFeaturedClass obj;
    EXPECT_EQ(obj.Name(), L"");
    obj.Name(L"test");
    EXPECT_EQ(obj.Name(), L"test");
    EXPECT_EQ(obj.Count(), 0);

    obj.DoWork();
    EXPECT_EQ(obj.GetData(0), L"");

    hstring eventData;
    auto token = obj.DataChanged(auto_revoke, [&eventData](IInspectable const&, hstring const& args)
    {
        eventData = args;
    });
    obj.RaiseDataChanged();
    EXPECT_EQ(eventData, L"changed");
}

TEST(AuthoringTest, ContractVersionedClasses)
{
    ContractVersionedClass obj1;
    obj1.Name(L"versioned");
    EXPECT_EQ(obj1.Name(), L"versioned");

    ContractVersionedClassV2 obj2;
    obj2.Name(L"v2");
    obj2.Count(42);
    EXPECT_EQ(obj2.Name(), L"v2");
    EXPECT_EQ(obj2.Count(), 42);
}

TEST(AuthoringTest, ContractVersionedMembersClass)
{
    ContractVersionedMembersClass obj;
    obj.TrackName(L"Song");
    obj.Volume(80);
    EXPECT_EQ(obj.TrackName(), L"Song");
    EXPECT_EQ(obj.Volume(), 80);
    EXPECT_EQ(obj.GetNowPlaying(), L"Song (Vol=80)");

    IContractVersionedMembersV1 v1 = obj;
    EXPECT_EQ(v1.TrackName(), L"Song");

    IContractVersionedMembersV2 v2 = obj;
    EXPECT_EQ(v2.Volume(), 80);

    hstring changedTrack;
    auto token = obj.TrackChanged(auto_revoke, [&changedTrack](IInspectable const&, hstring const& args)
    {
        changedTrack = args;
    });
    obj.RaiseTrackChanged();
    EXPECT_EQ(changedTrack, L"Song");
}

TEST(AuthoringTest, VersionedMembersClass)
{
    VersionedMembersClass obj;
    obj.Message(L"Alert");
    obj.Urgency(3.5);
    EXPECT_EQ(obj.Message(), L"Alert");
    EXPECT_EQ(obj.Urgency(), 3.5);
    EXPECT_EQ(obj.Format(), L"Alert: 3.5");

    IVersionedMembersV1 v1 = obj;
    EXPECT_EQ(v1.Message(), L"Alert");

    IVersionedMembersV2 v2 = obj;
    EXPECT_EQ(v2.Urgency(), 3.5);

    double changedUrgency = 0;
    auto token = obj.UrgencyChanged(auto_revoke, [&changedUrgency](IInspectable const&, double args)
    {
        changedUrgency = args;
    });
    obj.RaiseUrgencyChanged();
    EXPECT_EQ(changedUrgency, 3.5);
}

TEST(AuthoringTest, OverloadedMethodClass)
{
    OverloadedMethodClass obj;
    EXPECT_EQ(obj.Format(42), L"42");
    EXPECT_EQ(obj.Format(3.14), L"3.14");
    EXPECT_EQ(obj.Format(L"hello"), L"hello");

    EXPECT_EQ(OverloadedMethodClass::Parse(L"123"), 123);
    EXPECT_EQ(OverloadedMethodClass::Parse(L"FF", 16), 255);
}

TEST(AuthoringTest, NestedStructs)
{
    InnerStruct inner{ 10, 20 };
    EXPECT_EQ(inner.A, 10);
    EXPECT_EQ(inner.B, 20);

    OuterStruct outer{ inner, 30 };
    EXPECT_EQ(outer.Inner.A, 10);
    EXPECT_EQ(outer.Inner.B, 20);
    EXPECT_EQ(outer.C, 30);
}

TEST(AuthoringTest, FlagsAndSignedEnums)
{
    EXPECT_EQ(static_cast<uint32_t>(DetailedFlags::None), 0u);
    EXPECT_EQ(static_cast<uint32_t>(DetailedFlags::ReadWrite), 3u);
    EXPECT_EQ(static_cast<uint32_t>(DetailedFlags::All), 7u);

    EXPECT_EQ(static_cast<int32_t>(Priority::Low), -1);
    EXPECT_EQ(static_cast<int32_t>(Priority::Normal), 0);
    EXPECT_EQ(static_cast<int32_t>(Priority::Critical), 2);
}

TEST(AuthoringTest, StaticComplexProps)
{
    EXPECT_EQ(StaticComplexProps::DefaultName(), L"Default");
    auto defaultStruct = StaticComplexProps::DefaultStruct();
    EXPECT_EQ(defaultStruct.X, 1);
    EXPECT_EQ(defaultStruct.Y, 2);
    EXPECT_EQ(StaticComplexProps::MaxCount(), 100);
    StaticComplexProps::MaxCount(50);
    EXPECT_EQ(StaticComplexProps::MaxCount(), 50);
}

TEST(AuthoringTest, NullableParamClass)
{
    NullableParamClass obj;
    EXPECT_EQ(obj.NullableIntProp(), nullptr);
    obj.NullableIntProp(42);
    EXPECT_EQ(obj.NullableIntProp().Value(), 42);

    obj.NullableDoubleProp(3.14);
    EXPECT_EQ(obj.NullableDoubleProp().Value(), 3.14);

    obj.NullableBoolProp(true);
    EXPECT_EQ(obj.NullableBoolProp().Value(), true);

    EXPECT_EQ(obj.GetValueOrDefault(IReference<int>(5), 0), 5);
    EXPECT_EQ(obj.GetValueOrDefault(nullptr, 99), 99);

    EXPECT_EQ(obj.TryGetValue(L"key"), nullptr);
}

TEST(AuthoringTest, MappedTypeParamClass)
{
    MappedTypeParamClass obj;

    auto timestamp = obj.GetTimestamp();
    EXPECT_TRUE(timestamp.time_since_epoch().count() != 0);
    obj.SetTimestamp(timestamp);

    auto duration = obj.GetDuration();
    EXPECT_TRUE(duration.count() > 0);
    obj.SetDuration(duration);

    auto uri = obj.GetUri();
    EXPECT_NE(uri, nullptr);
    obj.SetUri(uri);

    auto formatted = obj.FormatTimestamp(timestamp, duration);
    EXPECT_FALSE(formatted.empty());
}

TEST(AuthoringTest, DisposableResource)
{
    DisposableResource resource;
    EXPECT_EQ(resource.Name(), L"Resource");
    resource.Reset();
    resource.Close();

    ICustomResource customResource = resource;
    EXPECT_EQ(customResource.Name(), L"Resource");
}

TEST(AuthoringTest, NotifyWithCustomInterface)
{
    NotifyWithCustomInterface obj;
    obj.Name(L"test");
    EXPECT_EQ(obj.Name(), L"test");

    ICustomResource customResource = obj;
    EXPECT_EQ(customResource.Name(), L"test");

    obj.Reset();
    EXPECT_EQ(obj.Name(), L"");
}

TEST(AuthoringTest, MixedArrayClass)
{
    MixedArrayClass obj;

    std::array<int, 3> src = { 10, 20, 30 };
    std::array<int, 3> dst = {};
    obj.CopyToSpan(src, dst);
    EXPECT_EQ(dst[0], 10);
    EXPECT_EQ(dst[1], 20);
    EXPECT_EQ(dst[2], 30);

    auto result = obj.TransformArray(src);
    EXPECT_EQ(result.size(), 3);
    EXPECT_EQ(result[0], 10);
    EXPECT_EQ(result[1], 20);
    EXPECT_EQ(result[2], 30);

    std::array<int, 4> buf = {};
    obj.FillWithIndex(buf);
    EXPECT_EQ(buf[0], 0);
    EXPECT_EQ(buf[1], 1);
    EXPECT_EQ(buf[2], 2);
    EXPECT_EQ(buf[3], 3);
}

TEST(AuthoringTest, CustomNotifyPropertyChanged)
{
    CustomNotifyPropertyChanged obj;
    // Verify we can cast to INotifyPropertyChanged
    auto npc = obj.as<Microsoft::UI::Xaml::Data::INotifyPropertyChanged>();
    EXPECT_NE(npc, nullptr);
}

TEST(AuthoringTest, CustomNotifyCollectionChanged)
{
    CustomNotifyCollectionChanged obj;
    // Verify we can cast to INotifyCollectionChanged
    auto ncc = obj.as<Microsoft::UI::Xaml::Interop::INotifyCollectionChanged>();
    EXPECT_NE(ncc, nullptr);
}

TEST(AuthoringTest, CustomNotifyDataErrorInfo)
{
    CustomNotifyDataErrorInfo obj;
    EXPECT_FALSE(obj.HasErrors());
}

TEST(AuthoringTest, CustomEnumerable)
{
    CustomVector2 vector;
    vector.Append(DisposableClass());
    vector.Append(DisposableClass());
    CustomEnumerable enumerable(vector);
    auto iterator = enumerable.First();
    EXPECT_TRUE(iterator.HasCurrent());
}

TEST(AuthoringTest, NonActivatableType)
{
    // NonActivatableType can only be created via NonActivatableFactory
    auto obj = NonActivatableFactory::Create();
    EXPECT_EQ(obj.GetText(), L"Test123");
}