#pragma once
#include "IBindableIteratorTest.g.h"
#include "IDictionaryTest.g.h"
#include "IDisposableTest.g.h"

namespace winrt::TestComponentCSharp::implementation
{
	struct IBindableIteratorTest : IBindableIteratorTestT<IBindableIteratorTest>
	{
		IBindableIteratorTest();
		bool MoveNext();
		Windows::Foundation::IInspectable Current();
		bool HasCurrent();
	};

	struct IDictionaryTest : IDictionaryTestT<IDictionaryTest>
	{
	private:
		Windows::Foundation::Collections::IMap<hstring, hstring> _map{
			winrt::single_threaded_map<hstring, hstring>()
		};
	public:
		IDictionaryTest();
		hstring Lookup(hstring key);
		bool HasKey(hstring key);
		Windows::Foundation::Collections::IMapView<hstring, hstring> GetView();
		bool Insert(hstring key, hstring value);
		void Remove(hstring key);
		void Clear();
		int Size();
		Windows::Foundation::Collections::IIterator<Windows::Foundation::Collections::IKeyValuePair<hstring, hstring>> First();

		static bool Consume(Windows::Foundation::Collections::IMap<hstring, hstring> map);
	};

	struct IDisposableTest : IDisposableTestT<IDisposableTest>
	{
		IDisposableTest();
		void Close();
	};
}

namespace winrt::TestComponentCSharp::factory_implementation
{
	struct IBindableIteratorTest : IBindableIteratorTestT<IBindableIteratorTest, implementation::IBindableIteratorTest, Windows::Foundation::IStringable>
	{
		hstring ToString()
		{
			return L"IBindableIteratorTest";
		}
	};

	struct IDictionaryTest : IDictionaryTestT<IDictionaryTest, implementation::IDictionaryTest, Windows::Foundation::IStringable>
	{
		hstring ToString()
		{
			return L"IDictionaryTest";
		}
	};

	struct IDisposableTest : IDisposableTestT<IDisposableTest, implementation::IDisposableTest, Windows::Foundation::IStringable>
	{
		hstring ToString()
		{
			return L"IDisposableTest";
		}
	};
}