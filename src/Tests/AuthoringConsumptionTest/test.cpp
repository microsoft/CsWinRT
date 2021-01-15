#include "pch.h"

using namespace winrt;
using namespace Windows::Foundation;
using namespace AuthoringTest;

TEST(AuthoringTest, Statics)
{
    EXPECT_EQ(TestClass::GetDefaultFactor(), 1);
    EXPECT_EQ(TestClass::GetDefaultNumber(), 2);
    EXPECT_EQ(StaticClass::GetNumber(), 4);
    EXPECT_EQ(StaticClass::GetNumber(2), 2);
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

TEST(AuthoringTest, Arrays)
{
    BasicClass basicClass;
    EXPECT_EQ(basicClass.GetSum({2, 3, 4, 6}), 15);

    com_array<int> arr(6);
    basicClass.PopulateArray(arr);
    for (int idx = 0; idx < arr.size(); idx++)
    {
        EXPECT_EQ(arr[idx], idx + 1);
    }

    com_array<int> arr2;
    basicClass.GetArrayOfLength(10, arr2);
    EXPECT_EQ(arr2.size(), 10);
    for (int idx = 0; idx < arr2.size(); idx++)
    {
        EXPECT_EQ(arr2[idx], idx + 1);
    }

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
    for (auto obj : vector.GetView())
    {
    }

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