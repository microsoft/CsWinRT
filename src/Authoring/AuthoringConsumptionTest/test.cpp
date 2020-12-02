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
}

TEST(AuthoringTest, CustomTypeInterfaceImplementations)
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

    Windows::Foundation::Collections::IMap map = dictionary;
    map.Clear();
    EXPECT_EQ(map.Size(), 0);
}