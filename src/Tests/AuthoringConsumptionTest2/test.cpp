#include "pch.h"

using namespace winrt;
using namespace Windows::Foundation;
using namespace Windows::Foundation::Collections;

// Activation tests across two CsWinRT components aggregated into one merged AOT host.

TEST(MultiComponent, CalculatorStatics)
{
    EXPECT_EQ(AuthoringTest3::Calculator::GetDefaultFactor(), 1);
    EXPECT_EQ(AuthoringTest3::Calculator::GetDefaultNumber(), 2);
}

TEST(MultiComponent, GreeterMethods)
{
    AuthoringTest2::Greeter greeter;
    EXPECT_EQ(greeter.Greet(L"world"), hstring(L"Hello, world!"));
    EXPECT_EQ(greeter.Add(2, 3), 5);
}

TEST(MultiComponent, BothComponentsActivateInOneProcess)
{
    AuthoringTest3::Calculator first;
    AuthoringTest2::Greeter second;

    EXPECT_EQ(first.GetFactor(), 1);
    EXPECT_EQ(second.Add(10, 20), 30);
}

// Generic instantiations from both components flow through the merged interop closure.
// If per-component interop generation had run independently, type-map registration would
// fail at publish time or these calls would fail at runtime.

TEST(MultiComponent, GenericCollectionsFromBothComponents)
{
    AuthoringTest2::Greeter greeter;
    auto numbers = greeter.GetNumbers();
    ASSERT_EQ(numbers.Size(), 6u);
    EXPECT_EQ(numbers.GetAt(0), 1);
    EXPECT_EQ(numbers.GetAt(5), 13);

    AuthoringTest3::Calculator calculator;
    auto bools = calculator.GetBools();
    EXPECT_GT(bools.Size(), 0u);

    auto uris = calculator.GetUris();
    EXPECT_GT(uris.Size(), 0u);
}

TEST(MultiComponent, GenericMapFromComponent2)
{
    AuthoringTest2::Greeter greeter;
    auto counts = greeter.GetCounts();

    ASSERT_EQ(counts.Size(), 3u);
    EXPECT_EQ(counts.Lookup(L"alpha"), 1);
    EXPECT_EQ(counts.Lookup(L"beta"), 2);
    EXPECT_EQ(counts.Lookup(L"gamma"), 3);
}

TEST(ManagedHelpers, PrivateEventDeliversValue)
{
    AuthoringTest2::Greeter greeter;

    EXPECT_EQ(greeter.RaiseLocalEvent(), 42);
    EXPECT_EQ(greeter.RaiseLocalEvent(), 42);
}

TEST(ManagedHelpers, InternalCollectionsStayManaged)
{
    AuthoringTest2::Greeter greeter;

    EXPECT_EQ(greeter.ExerciseInternalCollections(), 42);
}

TEST(ManagedHelpers, PublicNestedCollectionsStayManaged)
{
    AuthoringTest2::Greeter greeter;

    EXPECT_EQ(greeter.ExerciseNestedCollections(), 42);
}

TEST(ManagedHelpers, PublicAbiTypesStayManaged)
{
    AuthoringTest2::Greeter greeter;

    EXPECT_EQ(greeter.ExerciseAbiCollections(), 42);
}

TEST(ManagedHelpers, InternalListPreservesCovariance)
{
    AuthoringTest2::Greeter greeter;
    auto objects = greeter.GetLocalHelpers();
    ASSERT_EQ(objects.Size(), 1u);

    auto helper = objects.GetAt(0);
    EXPECT_EQ(helper.as<IStringable>().ToString(), hstring(L"local helper"));
    EXPECT_EQ(get_class_name(helper), hstring(L"Windows.Foundation.IStringable"));

    auto stringables = objects.as<IVectorView<IStringable>>();
    ASSERT_EQ(stringables.Size(), 1u);
    EXPECT_EQ(stringables.GetAt(0).ToString(), hstring(L"local helper"));
}

TEST(ManagedHelpers, PublicNestedArrayPreservesCovariance)
{
    AuthoringTest2::Greeter greeter;
    auto objects = greeter.GetNestedHelpers();
    auto iterator = objects.First();
    ASSERT_TRUE(iterator.HasCurrent());

    auto helper = iterator.Current();
    EXPECT_EQ(helper.as<IStringable>().ToString(), hstring(L"nested helper"));
    EXPECT_EQ(get_class_name(helper), hstring(L"Windows.Foundation.IStringable"));
    EXPECT_FALSE(iterator.MoveNext());

    auto stringables = objects.as<IIterable<IStringable>>().First();
    ASSERT_TRUE(stringables.HasCurrent());
    EXPECT_EQ(stringables.Current().ToString(), hstring(L"nested helper"));
    EXPECT_FALSE(stringables.MoveNext());
}

TEST(ManagedHelpers, PublicAbiStructMarshalsThroughItsInterface)
{
    AuthoringTest2::Greeter greeter;
    auto helper = greeter.GetBoxedManagedValue();

    EXPECT_EQ(helper.as<IStringable>().ToString(), hstring(L"42"));
    EXPECT_EQ(get_class_name(helper), hstring(L"Windows.Foundation.IStringable"));
}

TEST(AuthoredGenerics, RuntimeClassAndInterfaceArguments)
{
    AuthoringTest2::Greeter greeter;
    auto greeters = greeter.GetGreeters();
    ASSERT_EQ(greeters.Size(), 1u);
    EXPECT_EQ(greeters.GetAt(0).Greet(L"generics"), hstring(L"Hello, generics!"));
    EXPECT_EQ(get_class_name(greeters.GetAt(0)), hstring(L"AuthoringTest2.Greeter"));

    auto adders = greeter.GetAdders();
    ASSERT_EQ(adders.Size(), 1u);
    EXPECT_EQ(adders.GetAt(0).Add(20, 22), 42);
}

TEST(AuthoredGenerics, StructArgumentsAndBoxing)
{
    AuthoringTest2::Greeter greeter;
    auto values = greeter.GetAuthoredValues();
    ASSERT_EQ(values.Size(), 1u);
    EXPECT_EQ(values.Lookup(L"answer").Value, 42);

    auto iterator = values.First();
    ASSERT_TRUE(iterator.HasCurrent());
    EXPECT_EQ(iterator.Current().Key(), hstring(L"answer"));
    EXPECT_EQ(iterator.Current().Value().Value, 42);
    EXPECT_FALSE(iterator.MoveNext());

    auto boxed = greeter.GetBoxedAuthoredValue().as<IReference<AuthoringTest2::AuthoredValue>>();
    EXPECT_EQ(boxed.Value().Value, 42);
}

TEST(AuthoredGenerics, EnumAndDelegateArguments)
{
    AuthoringTest2::Greeter greeter;
    auto kinds = greeter.GetAuthoredKinds();
    ASSERT_EQ(kinds.Size(), 1u);
    EXPECT_EQ(kinds.GetAt(0), AuthoringTest2::AuthoredValueKind::Answer);

    auto callbacks = greeter.GetAuthoredCallbacks();
    ASSERT_EQ(callbacks.Size(), 1u);
    EXPECT_EQ(callbacks.GetAt(0)(21), 42);
}

int main(int argc, char** argv)
{
    init_apartment();
    testing::InitGoogleTest(&argc, argv);
    return RUN_ALL_TESTS();
}
