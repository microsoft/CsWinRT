#include "pch.h"
#include "CustomReadOnlyDictionaryTest.h"
#include "CustomReadOnlyDictionaryTest.g.cpp"

namespace winrt::TestComponentCSharp::implementation
{
    CustomReadOnlyDictionaryTest::CustomReadOnlyDictionaryTest()
    {
        // Default contents: a small set of string -> string entries suitable for 'Keys'/'Values' tests.
        std::map<winrt::hstring, winrt::hstring> initial{
            { L"apples", L"1" },
            { L"oranges", L"2" },
            { L"pears", L"3" }
        };
        _mapView = winrt::single_threaded_map_view<winrt::hstring, winrt::hstring>(std::move(initial));
    }

    CustomReadOnlyDictionaryTest::CustomReadOnlyDictionaryTest(winrt::Windows::Foundation::Collections::IMapView<winrt::hstring, winrt::hstring> const& mapView)
    {
        _mapView = mapView;
    }

    winrt::hstring CustomReadOnlyDictionaryTest::Lookup(winrt::hstring const& key)
    {
        return _mapView.Lookup(key);
    }

    uint32_t CustomReadOnlyDictionaryTest::Size()
    {
        return _mapView.Size();
    }

    bool CustomReadOnlyDictionaryTest::HasKey(winrt::hstring const& key)
    {
        return _mapView.HasKey(key);
    }

    void CustomReadOnlyDictionaryTest::Split(
        winrt::Windows::Foundation::Collections::IMapView<winrt::hstring, winrt::hstring>& first,
        winrt::Windows::Foundation::Collections::IMapView<winrt::hstring, winrt::hstring>& second)
    {
        _mapView.Split(first, second);
    }

    winrt::Windows::Foundation::Collections::IIterator<winrt::Windows::Foundation::Collections::IKeyValuePair<winrt::hstring, winrt::hstring>> CustomReadOnlyDictionaryTest::First()
    {
        return _mapView.First();
    }
}
