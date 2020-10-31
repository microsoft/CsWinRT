#include "pch.h"
#include "ManualProjectionTestClasses.h"
#include "IBindableIteratorTest.g.cpp"
#include "IDictionaryTest.g.cpp"
#include "IDisposableTest.g.cpp"

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
	IBindableIteratorTest::IBindableIteratorTest()
	{
		
	}

	bool IBindableIteratorTest::MoveNext()
	{
		return true;
	}

	Windows::Foundation::IInspectable IBindableIteratorTest::Current()
	{
		return Windows::Foundation::PropertyValue::CreateInt32(27861);
	}

	bool IBindableIteratorTest::HasCurrent()
	{
		return true;
	}
	IDictionaryTest::IDictionaryTest()
	{
	}
	hstring IDictionaryTest::Lookup(hstring key)
	{
		return _map.Lookup(key);
	}
	bool IDictionaryTest::HasKey(hstring key)
	{
		return _map.HasKey(key);
	}
	Windows::Foundation::Collections::IMapView<hstring, hstring> IDictionaryTest::GetView()
	{
		return _map.GetView();
	}
	bool IDictionaryTest::Insert(hstring key, hstring value)
	{
		return _map.Insert(key, value);
	}
	void IDictionaryTest::Remove(hstring key)
	{
		_map.Remove(key);
	}
	void IDictionaryTest::Clear()
	{
		_map.Clear();
	}
	int IDictionaryTest::Size()
	{
		return _map.Size();
	}

	Windows::Foundation::Collections::IIterator<Windows::Foundation::Collections::IKeyValuePair<hstring, hstring>> IDictionaryTest::First()
	{
		return _map.First();
	}

	bool IDictionaryTest::Consume(Windows::Foundation::Collections::IMap<hstring, hstring> map)
	{
		map.Clear();

		map.Insert(L"key.cpp", L"value.cpp");
		if (map.Size() != 1)
		{
			return false;
		}
		if (!map.HasKey(L"key.cpp"))
		{
			return false;
		}
		
		return true;
		
	}
	
	IDisposableTest::IDisposableTest()
	{
	}
	void IDisposableTest::Close()
	{
	}
}