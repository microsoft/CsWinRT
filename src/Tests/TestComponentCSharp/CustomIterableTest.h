#pragma once
#include "CustomIterableTest.g.h"

namespace winrt::TestComponentCSharp::implementation
{
    struct CustomIterableTest : CustomIterableTestT<CustomIterableTest>
    {
        CustomIterableTest();
        CustomIterableTest(winrt::Windows::Foundation::Collections::IIterable<int32_t> const& iterable);

        winrt::Windows::Foundation::Collections::IIterator<int32_t> First();

        winrt::Windows::Foundation::Collections::IIterable<int32_t> _iterable;
    };
}

namespace winrt::TestComponentCSharp::factory_implementation
{
    struct CustomIterableTest : CustomIterableTestT<CustomIterableTest, implementation::CustomIterableTest>
    {
    };
}
