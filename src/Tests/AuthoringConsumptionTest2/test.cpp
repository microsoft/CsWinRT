#include "pch.h"

using namespace winrt;
using namespace Windows::Foundation;

// Verifies multi-component aggregation: a single native exe references two managed CsWinRT
// components (AuthoringTest and AuthoringTest2) and the merged WinRT.Component.dll produced
// by Microsoft.Windows.CsWinRT.targets resolves activation for both.

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

int main(int argc, char** argv)
{
    init_apartment();
    testing::InitGoogleTest(&argc, argv);
    return RUN_ALL_TESTS();
}
