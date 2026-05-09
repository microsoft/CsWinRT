#include "pch.h"

using namespace winrt;
using namespace Windows::Foundation;
using namespace Windows::Foundation::Collections;

// Activation tests across two CsWinRT components aggregated into one WinRT.Component.dll.

TEST(MultiComponent, AuthoringTestStatics)
{
    EXPECT_EQ(AuthoringTest::TestClass::GetDefaultFactor(), 1);
    EXPECT_EQ(AuthoringTest::TestClass::GetDefaultNumber(), 2);
}

TEST(MultiComponent, AuthoringTest2Greeter)
{
    AuthoringTest2::Greeter greeter;
    EXPECT_EQ(greeter.Greet(L"world"), hstring(L"Hello, world!"));
    EXPECT_EQ(greeter.Add(2, 3), 5);
}

TEST(MultiComponent, BothComponentsActivateInOneProcess)
{
    AuthoringTest::TestClass first;
    AuthoringTest2::Greeter second;

    EXPECT_EQ(first.GetFactor(), 1);
    EXPECT_EQ(second.Add(10, 20), 30);
}

// Generic instantiations from both components flow through the merged WinRT.Interop.dll.
// If per-component interop generation had run independently, type-map registration would
// fail at publish time or these calls would fail at runtime.

TEST(MultiComponent, GenericCollectionsFromBothComponents)
{
    AuthoringTest2::Greeter greeter;
    auto numbers = greeter.GetNumbers();
    ASSERT_EQ(numbers.Size(), 6u);
    EXPECT_EQ(numbers.GetAt(0), 1);
    EXPECT_EQ(numbers.GetAt(5), 13);

    auto bools = AuthoringTest::TestClass::GetBools();
    EXPECT_GT(bools.Size(), 0u);

    auto uris = AuthoringTest::TestClass::GetUris();
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

int main(int argc, char** argv)
{
    init_apartment();
    testing::InitGoogleTest(&argc, argv);
    return RUN_ALL_TESTS();
}
