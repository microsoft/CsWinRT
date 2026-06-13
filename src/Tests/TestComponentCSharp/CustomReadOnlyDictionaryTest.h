#pragma once
#include "CustomReadOnlyDictionaryTest.g.h"

namespace winrt::TestComponentCSharp::implementation
{
    struct CustomReadOnlyDictionaryTest : CustomReadOnlyDictionaryTestT<CustomReadOnlyDictionaryTest>
    {
        CustomReadOnlyDictionaryTest();
        CustomReadOnlyDictionaryTest(winrt::Windows::Foundation::Collections::IMapView<winrt::hstring, winrt::hstring> const& mapView);

        winrt::hstring Lookup(winrt::hstring const& key);
        uint32_t Size();
        bool HasKey(winrt::hstring const& key);
        void Split(
            winrt::Windows::Foundation::Collections::IMapView<winrt::hstring, winrt::hstring>& first,
            winrt::Windows::Foundation::Collections::IMapView<winrt::hstring, winrt::hstring>& second);
        winrt::Windows::Foundation::Collections::IIterator<winrt::Windows::Foundation::Collections::IKeyValuePair<winrt::hstring, winrt::hstring>> First();

        winrt::Windows::Foundation::Collections::IMapView<winrt::hstring, winrt::hstring> _mapView;
    };
}

namespace winrt::TestComponentCSharp::factory_implementation
{
    struct CustomReadOnlyDictionaryTest : CustomReadOnlyDictionaryTestT<CustomReadOnlyDictionaryTest, implementation::CustomReadOnlyDictionaryTest>
    {
    };
}
