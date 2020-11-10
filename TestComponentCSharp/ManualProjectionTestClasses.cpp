#include "pch.h"
#include "ManualProjectionTestClasses.h"
#include "CustomBindableIteratorTest.g.cpp"
#include "CustomDisposableTest.g.cpp"

namespace winrt
{
	template <typename T, typename Allocator = std::allocator<T>>
	Windows::Foundation::Collections::IVectorView<T> single_threaded_vector_view(std::vector<T, Allocator>&& values = {})
	{
		return make<impl::input_vector_view<T, std::vector<T, Allocator>>>(std::move(values));
	}

	template <typename K, typename V, typename Allocator = std::allocator<K>>
	Windows::Foundation::Collections::IMapView<K, V> single_threaded_map_view(std::map<K, V, Allocator>&& values = {})
	{
		return make<impl::input_map_view<K, V, std::map<K, V, Allocator>>>(std::move(values));
	}
}

namespace winrt::TestComponentCSharp::implementation
{
	CustomBindableIteratorTest::CustomBindableIteratorTest()
	{
		
	}

	bool CustomBindableIteratorTest::MoveNext()
	{
		return true;
	}

	Windows::Foundation::IInspectable CustomBindableIteratorTest::Current()
	{
		return Windows::Foundation::PropertyValue::CreateInt32(27861);
	}

	bool CustomBindableIteratorTest::HasCurrent()
	{
		return true;
	}

	CustomDisposableTest::CustomDisposableTest()
	{
	}

	void CustomDisposableTest::Close()
	{
	}
}