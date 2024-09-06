#include "pch.h"
#include "CustomIterableTest.h"
#include "CustomIterableTest.g.cpp"

namespace winrt::TestComponentCSharp::implementation
{
    CustomIterableTest::CustomIterableTest()
    {
        _iterable = winrt::single_threaded_vector(std::vector{ 0, 2, 4 });
    }

    CustomIterableTest::CustomIterableTest(winrt::Windows::Foundation::Collections::IIterable<int32_t> const& iterable)
    {
        _iterable = iterable;
    }

    winrt::Windows::Foundation::Collections::IIterator<int32_t> CustomIterableTest::First()
    {
        return _iterable.First();
    }
}
