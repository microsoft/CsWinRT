#include "pch.h"

using namespace winrt;
using namespace Windows::Foundation;
using namespace AuthoringSample;

TEST(AuthoringTest, Statics)
{
    EXPECT_EQ(TestClass::GetDefaultFactor(), 1);
    EXPECT_EQ(TestClass::GetDefaultNumber(), 2);
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
}