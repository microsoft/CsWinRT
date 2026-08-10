#include "pch.h"

using namespace winrt;
using namespace winrt::Windows::Foundation;
using namespace winrt::Windows::Foundation::Collections;

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

// A class implemented from existing metadata is absent from the implementing component's '.winmd', so it
// reaches the host purely through the generated activation entry points. It is activated by the name of
// the class it implements, and has to report that name rather than the implementing type's. The raw ABI
// is used because the projected type belongs to 'TestComponent', not to either component here.

TEST(MultiComponent, ImplementedWinMDTypeActivatesAndReportsImplementedClassName)
{
    winrt::hstring className{ L"TestComponent.Class" };
    winrt::guid iid = winrt::guid_of<winrt::Windows::Foundation::IActivationFactory>();

    winrt::Windows::Foundation::IActivationFactory factory{ nullptr };

    HRESULT hr = RoGetActivationFactory(
        static_cast<HSTRING>(winrt::get_abi(className)),
        reinterpret_cast<GUID const&>(iid),
        winrt::put_abi(factory));

    ASSERT_EQ(hr, S_OK);
    ASSERT_TRUE(factory != nullptr);

    auto instance = factory.ActivateInstance<winrt::Windows::Foundation::IInspectable>();

    ASSERT_TRUE(instance != nullptr);
    EXPECT_EQ(winrt::get_class_name(instance), winrt::hstring{ L"TestComponent.Class" });
}

int main(int argc, char** argv)
{
    init_apartment();
    testing::InitGoogleTest(&argc, argv);
    return RUN_ALL_TESTS();
}
