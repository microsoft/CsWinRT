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

    CustomIterableTest::CustomIterableTest(bool useCustomIterator)
        :CustomIterableTest()
    {
        _useCustomIterator = useCustomIterator;
    }

    winrt::TestComponentCSharp::CustomIterableTest CustomIterableTest::CreateWithCustomIterator()
    {
        return winrt::make<CustomIterableTest>(true);
    }

    winrt::Windows::Foundation::Collections::IIterator<int32_t> CustomIterableTest::First()
    {
        if (_useCustomIterator)
        {
            return winrt::TestComponentCSharp::CustomIteratorTest(_iterable.First());
        }
        else
        {
            return _iterable.First();
        }
    }
}
