#include "pch.h"

using namespace winrt;
using namespace Windows::Foundation;
using namespace AuthoringWuxTest;

TEST(AuthoringWuxTest, Collections)
{
    DisposableClass disposed;
    disposed.Close();
    MultipleInterfaceMappingClass multipleInterfaces;
    Windows::UI::Xaml::Interop::IBindableIterable bindable = multipleInterfaces;
    Windows::Foundation::Collections::IVector<DisposableClass> vector = multipleInterfaces;
    Windows::UI::Xaml::Interop::IBindableVector bindableVector = multipleInterfaces;
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
}

TEST(AuthoringWuxTest, PropertyChanged)
{
    CustomNotifyPropertyChanged customNotifyPropertyChanged;
    Windows::UI::Xaml::Data::INotifyPropertyChanged propChanged = customNotifyPropertyChanged;
    winrt::hstring propName = L"Number";
    bool eventTriggered = false;
    auto token = propChanged.PropertyChanged(auto_revoke, [&eventTriggered, &propName](IInspectable sender, Windows::UI::Xaml::Data::PropertyChangedEventArgs args)
    {
        eventTriggered = (args.PropertyName() == propName);
    });

    customNotifyPropertyChanged.RaisePropertyChanged(propName);

    EXPECT_TRUE(eventTriggered);
}

int main(int argc, char** argv)
{
    ::testing::InitGoogleTest(&argc, argv);
    winrt::init_apartment(winrt::apartment_type::single_threaded);
    auto manager = winrt::Windows::UI::Xaml::Hosting::WindowsXamlManager::InitializeForCurrentThread();
    int result = RUN_ALL_TESTS();
    manager.Close();
    return result;
}